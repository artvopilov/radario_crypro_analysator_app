using System;
using CryptoAnalysatorWebApp.TradeBots.Common;
using CryptoAnalysatorWebApp.TradeBots.Interfaces;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CryptoAnalysatorWebApp.Models;
using CryptoAnalysatorWebApp.TelegramBot;
using CryptoAnalysatorWebApp.TradeBots.Common.Objects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.Buffers;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using System.Diagnostics;
using CryptoAnalysatorWebApp.Models.Db;

namespace CryptoAnalysatorWebApp.TradeBots {
    public class BittrexTradeBot : CommonTradeBot<ResponseWrapper> {
        private const decimal FeeBuy = (decimal) 0.0025;
        private const decimal FeeSell = (decimal) 0.0025;

        public BittrexTradeBot(string apiKey, string apiSecret, string baseUrl = "https://bittrex.com/api/v1.1/") :
            base(apiKey, apiSecret, baseUrl) {
            var responseBalances = GetBalances().Result;
            Console.WriteLine(responseBalances.Result);
            JArray currenciesOnBalance = (JArray) responseBalances.Result;
            
            JObject currencyBtc = (JObject)currenciesOnBalance.FirstOrDefault(cur => (string) cur["Currency"] == "BTC");
            JObject currencyEth= (JObject)currenciesOnBalance.FirstOrDefault(cur => (string) cur["Currency"] == "ETH");
            
            BalanceBtc = currencyBtc == null ? 0 : (decimal)currencyBtc["Available"];
            BalanceEth = currencyEth == null ? 0 : (decimal)currencyEth["Available"];
        }

        public BittrexTradeBot(string baseUrl = "https://bittrex.com/api/v1.1/") : base(baseUrl) { }

        protected override HttpRequestMessage CreateRequest(string method, bool includeAuth,
            Dictionary<string, string> parameters) {
            string parametersString;
            string completeUrl;
            HttpRequestMessage request;
            
            if (!includeAuth) {
                if (parameters.Count == 0) {
                    parametersString = "";
                } else {
                    parametersString = string.Join('&',
                        parameters.Select(param => WebUtility.UrlEncode(param.Key) + '=' + WebUtility.UrlEncode(param.Value)));
                }
                completeUrl = baseUrl + method + '?' + parametersString;
                request = new HttpRequestMessage(HttpMethod.Get, completeUrl);
                return request;
            }
 
            parameters.Add("apikey", apiKey);
            parameters.Add("nonce", DateTime.Now.Ticks.ToString());

            parametersString = string.Join('&',
                parameters.Select(param => WebUtility.UrlEncode(param.Key) + '=' + WebUtility.UrlEncode(param.Value)));

            completeUrl = baseUrl + method + '?' + parametersString;
            Console.WriteLine("CompleteUrl: " + completeUrl);

            var hashText = MakeApiSignature(completeUrl);
            
            request = new HttpRequestMessage(HttpMethod.Get, completeUrl);
            request.Headers.Add(SignHeaderName, hashText);

            return request;
        }
        
        protected override async Task<ResponseWrapper> ExecuteRequest(string method, bool includeAuth,
            Dictionary<string, string> parameters = null) {
            if (parameters == null) {
                parameters = new Dictionary<string, string>();
            }

            HttpRequestMessage request = CreateRequest(method, includeAuth, parameters);

            HttpResponseMessage response = await httpClient.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            ResponseWrapper responseContentJson = JsonConvert.DeserializeObject<ResponseWrapper>(responseContent);
            return responseContentJson;
        }

        public override async Task<ResponseWrapper> GetAllPairs() {
            ResponseWrapper responseAllPairs = await ExecuteRequest("public/getmarketsummaries", false);
            return responseAllPairs;
        }

        public override async Task<ResponseWrapper> GetOrderBook(string pair) {
            Dictionary<string, string> parametres = new Dictionary<string, string> {
                {"market", pair},
                { "type", "both" },
            };
            var responseOrderBook = await ExecuteRequest("public/getorderbook", false, parametres);
            return responseOrderBook;
        }

        public sealed override async Task<ResponseWrapper> GetBalances() {
            ResponseWrapper responseBalances = await ExecuteRequest("account/getbalances", true);
            return responseBalances;
        }

        public override async Task<ResponseWrapper> GetOpenOrders(string pair = null) {
            var parametres = new Dictionary<string, string>();
            if (pair != null) parametres.Add("market", pair);
            ResponseWrapper responseOpenOrders = await ExecuteRequest("market/getopenorders", true, parametres);
            return responseOpenOrders;
        }

        public override async Task<ResponseWrapper> CreateBuyOrder(string pair, decimal quantity, decimal rate) {
            Dictionary<string, string> parametres = new Dictionary<string, string> {
                {"market", pair},
                {"quantity", quantity.ToString(CultureInfo.InvariantCulture)},
                { "rate", rate.ToString(CultureInfo.InvariantCulture) }
            };
            ResponseWrapper responseBuyOrder = await ExecuteRequest("market/buylimit", true, parametres);
            return responseBuyOrder;
        }

        public override async Task<ResponseWrapper> CreateSellORder(string pair, decimal quantity, decimal rate) {
            Dictionary<string, string> parametres = new Dictionary<string, string> {
                {"market", pair},
                {"quantity", quantity.ToString(CultureInfo.InvariantCulture)},
                { "rate", rate.ToString(CultureInfo.InvariantCulture) }
            };
            ResponseWrapper responseSellOrder = await ExecuteRequest("market/selllimit", true, parametres);
            return responseSellOrder;
        }

        public override async Task<ResponseWrapper> CancelOrder(string orderId) {
            Dictionary<string, string> parametres = new Dictionary<string, string> {
                {"uuid", orderId}
            };
            ResponseWrapper responseCancelOrder = await ExecuteRequest("market/cancel", true, parametres);
            return responseCancelOrder;
        }

        public override void StartTrading(TelegramBotClient client, long chatId, ManualResetEvent signal) {
            Thread thread = new Thread(() => Trade(TradeAmountBtc, TradeAmountEth, client, chatId, signal));
            thread.Start();
        }

        public override async void Trade(decimal amountBtc, decimal amountEth, TelegramBotClient client, long chatId, ManualResetEvent signal) {
            bool canTradeWithBtc = amountBtc > 0 ? true : false;
            bool canTradeWithEth = amountEth > 0 ? true : false;
            
            RestartTrading:
            client.SendTextMessageAsync(chatId, "Searching crossRates... ");
            Stopwatch fullTime = new Stopwatch();
            fullTime.Start();
            allPairs.Clear();
            decimal resultDealDone = 1;
            signal.WaitOne();
            
            ExchangePair crossBittrex;
            lock (TimeService.TimeCrossesByMarket) {
                crossBittrex = TimeService.TimeCrossesByMarket.Keys.FirstOrDefault(cross => cross.Market == "Bittrex");
                /*if (!canTradeWithBtc) {
                    crossBittrex = TimeService.TimeCrossesByMarket.Keys.FirstOrDefault(cross =>
                        cross.Market == "Bittrex" && cross.PurchasePath.Split('-')[0] == "ETH");
                } else if (!canTradeWithEth) {
                    crossBittrex = TimeService.TimeCrossesByMarket.Keys.FirstOrDefault(cross =>
                        cross.Market == "Bittrex" && cross.PurchasePath.Split('-')[0] == "BTC");
                } else {
                    crossBittrex =
                        TimeService.TimeCrossesByMarket.Keys.FirstOrDefault(cross => cross.Market == "Bittrex");
                }*/
            }

            if (crossBittrex == null) {
                goto RestartTrading;
            }
            
            ResponseWrapper responseAllPairs = await GetAllPairs();
            foreach (var pair in (JArray) responseAllPairs.Result) {
                allPairs.Add((string) pair["MarketName"], new ExchangePair {    
                    Pair = (string)pair["MarketName"],
                    PurchasePrice = (decimal) pair["Ask"] * (1 + FeeBuy),
                    SellPrice = (decimal) pair["Bid"] * (1 - FeeSell)
                });
            }

            allPairs.Add("BTC-BTC", new ExchangePair {
                Pair = "BTC-BTC",
                PurchasePrice = 1,
                SellPrice = 1
            });
            
            fullTime.Stop();
            client.SendTextMessageAsync(chatId, $"Crossrate found: {crossBittrex.PurchasePath} {crossBittrex.SellPath} Spread: {crossBittrex.Spread}% " +
                                                $"Time searching: {fullTime.Elapsed}");
            Stopwatch crossRateActualTime = new Stopwatch();
            crossRateActualTime.Start();            
            
            string[] devidedPurchasePath = crossBittrex.PurchasePath.ToUpper().Split('-').ToArray();
            string[] devidedSellPath = crossBittrex.SellPath.ToUpper().Split('-').ToArray();
            (decimal minEqualToBtc, decimal resultDealF) =
                await FindMinAmountForTrade(devidedPurchasePath, devidedSellPath, (decimal)0.0007);//devidedPurchasePath[0] == "BTC" ? amountBtc : amountEth * allPairs["BTC-ETH"].PurchasePrice);
            
            if (minEqualToBtc * (decimal)0.97 < (decimal) 0.0005) {
                client.SendTextMessageAsync(chatId, $"Crossrate is not efficient (minEqualToBtc:{minEqualToBtc}). Trading stopped");
                signal.Reset();
                goto RestartTrading;
            }

            if (resultDealF <= 1) {
                client.SendTextMessageAsync(chatId, $"Crossrate is not efficient (resultDealF: {resultDealF}). Trading stopped");
                signal.Reset();
                goto RestartTrading;
            }
            
            decimal myAmount = devidedPurchasePath[0] == "BTC" ? minEqualToBtc * (decimal)0.97 : minEqualToBtc / allPairs["BTC-ETH"].PurchasePrice * (decimal)0.97;
            crossRateActualTime.Stop();
            client.SendTextMessageAsync(chatId, $"Trading amount: {myAmount}  resultDealF found: {resultDealF} Time: {crossRateActualTime.Elapsed}");
            //goto RestartTrading;

            // Buy proccess
            for (int i = 0; i <= devidedPurchasePath.Length - 2; i++) {
                bool purchase = true;
                string pair = $"{devidedPurchasePath[i]}-{devidedPurchasePath[i + 1]}";
                if (!allPairs.Keys.Contains(pair)) {
                    pair = $"{devidedPurchasePath[i + 1]}-{devidedPurchasePath[i]}";
                    purchase = false;
                }

                if (purchase) {
                    try { 
                        (myAmount, resultDealDone) = await BuyCurrency(myAmount, pair, resultDealDone);
                        client.SendTextMessageAsync(chatId, $"Pair: {pair}, Amount got: {myAmount} Type: BuyCurrency");
                    } catch (Exception e) {
                        Console.WriteLine($"Error while purchasing {crossBittrex.PurchasePath}: {pair}" +
                                          $"Error: {e.Message}  {e.StackTrace}");
                        break;
                    }
                } else {
                    try { 
                        (myAmount, resultDealDone) = await SellCurrency(myAmount, pair, resultDealDone, client, chatId);
                        client.SendTextMessageAsync(chatId, $"Pair: {pair}, Amount got: {myAmount} Type: SellCurrency");
                    } catch (Exception e) {
                        Console.WriteLine($"Error while purchasing {crossBittrex.PurchasePath}: {pair}" +
                                          $"Error: {e.Message}  {e.StackTrace}");
                        break;
                    }
                }
            }
            
            //Sell process
            for (int i = devidedSellPath.Length - 1; i > 0; i--) {
                bool sell = true;
                string pair = $"{devidedSellPath[i - 1]}-{devidedSellPath[i]}";
                if (!allPairs.Keys.Contains(pair)) {
                    pair = $"{devidedSellPath[i]}-{devidedSellPath[i - 1]}";
                    sell = false;
                }

                if (sell) {
                    try {
                        (myAmount, resultDealDone) = await SellCurrency(myAmount, pair, resultDealDone, client, chatId);
                        client.SendTextMessageAsync(chatId, $"Pair: {pair}, Amount got: {myAmount} Type: SellCurrency");
                    } catch (Exception e) {
                        Console.WriteLine($"Error while selling {crossBittrex.PurchasePath}: {pair}" +
                                          $"Error: {e.Message}  {e.StackTrace}");
                        break;
                    } 
                } else {
                    try { 
                        (myAmount, resultDealDone) = await BuyCurrency(myAmount, pair, resultDealDone);
                        client.SendTextMessageAsync(chatId, $"Pair: {pair}, Amount got: {myAmount} Type: BuyCurrency");
                    } catch (Exception e) {
                        Console.WriteLine($"Error while selling {crossBittrex.PurchasePath}: {pair}" +
                                          $"Error: {e.Message}  {e.StackTrace}");
                        break;
                    }
                }
            }
            
            TradeBotsStorage<ResponseWrapper>.DeleteTradeBot(chatId, "bittrex");
            client.SendTextMessageAsync(chatId, $"resultDealDone: {resultDealDone}");
            client.SendTextMessageAsync(chatId, "Bot on bittrex has done his job and was destroyed");
        }

        
        private async Task<(decimal, decimal)> BuyCurrency(decimal myAmount, string pair, decimal resultDealDone) {
            decimal buyRate = allPairs[pair].PurchasePrice;
            decimal bestBuyRate = 0;

            decimal boughtQuantityResult = 0;
            while (myAmount > 0) {
                var sellOrders = (JArray)GetOrderBook(pair).Result.Result["sell"];//= (JArray)GetOrderBook(pair).Result.Result["sell"]; // FOR IMITATION
                decimal bestBuyRateNew = (decimal)sellOrders[0]["Rate"]; // FOR IMITATION
                decimal bestBuyQuantity = (decimal)sellOrders[0]["Quantity"]; // FOR IMITATION
                if (bestBuyRate == bestBuyRateNew || bestBuyRateNew > buyRate) {
                    await Task.Delay(5000);
                    continue;
                }

                bestBuyRate = bestBuyRateNew;
                if (bestBuyQuantity < myAmount / (bestBuyRate * (1 + FeeBuy))) {
                    boughtQuantityResult += bestBuyQuantity;
                    myAmount -= bestBuyQuantity * (bestBuyRate * (1 + FeeBuy));
                } else {
                    boughtQuantityResult += myAmount / (bestBuyRate * (1 + FeeBuy));
                    myAmount = 0;
                }
            }
            
            return (boughtQuantityResult, resultDealDone / (buyRate * (1 + FeeBuy)));

            decimal buyQuantity = myAmount / (buyRate * (1 + FeeBuy));

            ResponseWrapper responseBuyOrder = await CreateBuyOrder(pair, buyQuantity, buyRate);
            string orderUuid = (string)responseBuyOrder.Result["uuid"];
            
            if (!responseBuyOrder.Success) {
                Console.WriteLine($"[ResponseError] Pair: {pair}, mess: {responseBuyOrder.Message} buyQuantity: {buyQuantity}{pair}" +
                                  $"\nRate: {buyRate}");
                return (0, 1);
            }

            // (string checkOrder, decimal quantityBought, string orderUuid) = CheckOrder(orderUuid).Result;
            // Console.WriteLine($"[Check order]: {checkOrder}");
            var checkOrder = "";
            while (checkOrder != "Ok") {
                decimal quantityRemains;
                (checkOrder, quantityRemains) = await CheckOrder(orderUuid);
                Console.WriteLine($"[Check order]: {checkOrder} quantityRemains: {quantityRemains}");
                await Task.Delay(5000);
            }
            
            return (buyQuantity, resultDealDone / (buyRate * (1 + FeeBuy)));
        }

        private async Task<(decimal, decimal)> SellCurrency(decimal myAmount, string pair, decimal resultDealDone, TelegramBotClient client, long chatId) {
            decimal sellRate = allPairs[pair].SellPrice;
            decimal bestSellRate = 0;

            decimal gotQuantityRemember = 0;
            while (myAmount > 0) {
                JArray buyOrders = (JArray)GetOrderBook(pair).Result.Result["buy"];
                decimal bestSellRateNew = (decimal)buyOrders[0]["Rate"];
                decimal bestSellQuantity = (decimal)buyOrders[0]["Quantity"];
                if (bestSellRateNew == bestSellRate || bestSellRateNew < sellRate) {
                    await Task.Delay(5000);
                    continue;
                }

                bestSellRate = bestSellRateNew;
                if (bestSellQuantity < myAmount) {
                    myAmount -= bestSellQuantity;
                    gotQuantityRemember += bestSellQuantity * (bestSellRate * (1 - FeeSell));
                } else {
                    myAmount = 0;
                    gotQuantityRemember += myAmount * (bestSellRate * (1 - FeeSell));
                }

            }

            return (gotQuantityRemember, resultDealDone * (sellRate * (1 - FeeSell)));
            
            decimal sellQuantity = myAmount;

            ResponseWrapper responseSellOrder = await CreateSellORder(pair, sellQuantity, sellRate);
            string orderUuid = (string)responseSellOrder.Result["uuid"];
            
            if (!responseSellOrder.Success) {
                Console.WriteLine($"[ResponseError] Pair: {pair}, mess: {responseSellOrder.Message}");
                return (0, 1);
            }

            //(string checkOrder, decimal quantitySold) = CheckOrder(orderUuid).Result;
            //Console.WriteLine($"After SellLimit checkOrder:{checkOrder} quantitySold:{quantitySold}");
            
            var checkOrder = "";
            while (checkOrder != "Ok") {
                decimal quantityRemains;
                (checkOrder, quantityRemains) = await CheckOrder(orderUuid);
                Console.WriteLine($"[Check order]: {checkOrder} quantityRemains: {quantityRemains}");
                await Task.Delay(5000);
            }

            return (sellQuantity * (sellRate * (1 - FeeSell)), resultDealDone * (sellRate * (1 - FeeSell)));
        }

        private async Task<(string, decimal)> CheckOrder(string orderUuid) {
            var responseOpenOrders = await GetOpenOrders();
            var ordersArr = ((JArray) responseOpenOrders.Result);
            if (ordersArr.Count == 0 || ordersArr.Where(ord => (string)ord["OrderUuid"] == orderUuid).ToArray().Length == 0) {
                return ("Ok", 0);
            }

            var order = ordersArr.Where(ord => (string)ord["OrderUuid"] == orderUuid).ToArray()[0];
            
            decimal quantity = (decimal)order["Quantity"];
            decimal quantityRemaining = (decimal)order["QuantityRemaining"];
            Console.WriteLine($"Quantity remains: quantity:{quantity} quantityRemaining:{quantityRemaining}");

            if (quantityRemaining == 0) {
                return ("Ok", 0);
            } else if (quantity != quantityRemaining) {
                return ("Remains", quantityRemaining);
            } else {
                return ("Fail", quantityRemaining);
            }
        }

        private async Task<(decimal, decimal)> FindMinAmountForTrade(string[] devidedPurchasePath, string[] devidedSellPath, decimal minEqualToBtc) {
            decimal resultDeal = 1;
            List<Tuple<Task<ResponseWrapper>, string, string>> exchangeProcessTasks = new List<Tuple<Task<ResponseWrapper>, string, string>>();
            
            
            Stopwatch timeFindingMinEq = Stopwatch.StartNew();
            for (int i = 0; i <= devidedPurchasePath.Length - 2; i++) {
                bool purchase = true;
                string pair = $"{devidedPurchasePath[i]}-{devidedPurchasePath[i + 1]}";
                if (!allPairs.Keys.Contains(pair)) {
                    pair = $"{devidedPurchasePath[i + 1]}-{devidedPurchasePath[i]}";
                    purchase = false;
                }

                if (purchase) {                    
                    var responseOrders = GetOrderBook(pair);
                    exchangeProcessTasks.Add(new Tuple<Task<ResponseWrapper>, string, string>(responseOrders, "purchase", pair));
                } else {
                    var responseOrders = GetOrderBook(pair);
                    exchangeProcessTasks.Add(new Tuple<Task<ResponseWrapper>, string, string>(responseOrders, "sell", pair));
                }
                
            }
            
            for (int i = devidedSellPath.Length - 1; i > 0; i--) {
                bool sell = true;
                string pair = $"{devidedSellPath[i - 1]}-{devidedSellPath[i]}";
                if (!allPairs.Keys.Contains(pair)) {
                    pair = $"{devidedSellPath[i]}-{devidedSellPath[i - 1]}";
                    sell = false;
                }

                if (sell) {
                    var responseOrders = GetOrderBook(pair);
                    exchangeProcessTasks.Add(new Tuple<Task<ResponseWrapper>, string, string>(responseOrders, "sell", pair));
                } else {                    
                    var responseOrders = GetOrderBook(pair);
                    exchangeProcessTasks.Add(new Tuple<Task<ResponseWrapper>, string, string>(responseOrders, "purchase", pair));
                }
            }

            await Task.WhenAll(exchangeProcessTasks.ConvertAll(x => x.Item1));
            timeFindingMinEq.Stop();
            Console.WriteLine($"Time  {timeFindingMinEq.Elapsed.Milliseconds}  Now  {DateTime.Now}  TimeMlscnds: {DateTime.Now.Millisecond}");
            
            Console.WriteLine("FOR TRADING");
            foreach (var deal in exchangeProcessTasks) {
                Console.WriteLine($"DEAL: {deal.Item3}");
                var result = deal.Item1.Result;
                var pair = deal.Item3;
                var pairWithBtc = pair != "USDT-BTC" ? $"BTC-{pair.Split('-')[1]}" : pair;
                decimal equalToBtc;
                
                if (deal.Item2 == "purchase") {
                    JArray sellOrders;
                    try {
                        sellOrders = (JArray)result.Result["sell"];
                    } catch (Exception e) {
                        Console.WriteLine($"Exception: {e.Message} Where: {e.StackTrace}  SellOrders: {result.Result["sell"]}");
                        return (0, 0);
                    }
                    var bestBuyQuantity = (decimal)sellOrders[0]["Quantity"];
                    equalToBtc = allPairs[pairWithBtc].PurchasePrice * bestBuyQuantity;
                    
                    var bestBuyRate = (decimal)sellOrders[0]["Rate"] * (1 + FeeBuy);
                    Console.WriteLine($"{pair}  {bestBuyRate}  Buy");
                    allPairs[pair].PurchasePrice = (decimal)sellOrders[0]["Rate"];
                    resultDeal /= bestBuyRate;
                } else {
                    JArray buyOrders;
                    try {
                        buyOrders = (JArray)result.Result["buy"];
                    } catch (Exception e) {
                        Console.WriteLine($"Exception: {e.Message} Where: {e.StackTrace}  BuyOrders: {result.Result["buy"]}");
                        return (0, 0);
                    }
                    var bestSellQuantity = (decimal)buyOrders[0]["Quantity"];
                    equalToBtc = allPairs[pairWithBtc].SellPrice * bestSellQuantity;
                    
                    var bestSellRate = (decimal)buyOrders[0]["Rate"] * (1 - FeeSell);
                    Console.WriteLine($"{pair}  {bestSellRate}  Sell");
                    allPairs[pair].SellPrice = (decimal)buyOrders[0]["Rate"];
                    resultDeal *= bestSellRate;
                }
                
                if (equalToBtc < minEqualToBtc) {
                    minEqualToBtc = equalToBtc;
                }
            }
            
            Console.WriteLine($"minEqualToBtc:{minEqualToBtc}");
            Console.WriteLine($"resultDeal {resultDeal}");
            return (minEqualToBtc, resultDeal);
        }
    }
}