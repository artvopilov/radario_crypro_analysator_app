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
using System.IO;
using CryptoAnalysatorWebApp.Models.Db;
using CryptoAnalysatorWebApp.Models.AnalyzingAlgorithms;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;

namespace CryptoAnalysatorWebApp.TradeBots {
    public class BittrexTradeBot : CommonTradeBot<ResponseWrapper> {
        private const decimal FeeBuy = (decimal) 0.0025;
        private const decimal FeeSell = (decimal) 0.0025;

        public BittrexTradeBot(string apiKey, string apiSecret, string baseUrl = "https://bittrex.com/api/v1.1/") :
            base(apiKey, apiSecret, baseUrl) {
            var responseBalances = GetBalances().Result;
            Console.WriteLine(responseBalances.Result);
            JArray currenciesOnBalance = (JArray) responseBalances.Result;

            foreach (var currency in currenciesOnBalance) {
                string currencyName = (string)currency["Currency"];
                walletBalances[currencyName] = (decimal)currency["Available"];
            }
            
            //CheckOrder("3de9f9db-0cbb-4459-be88-7d3dc6f44f9c");
            /*var ord = (string)GetOpenOrders().Result.Result[0]["OrderUuid"];
            var res = CancelOrder(ord).Result;*/
        }

        public BittrexTradeBot(string baseUrl = "https://bittrex.com/api/v1.1/") : base(baseUrl) {
            walletBalances["BTC"] = (decimal)0.0006;
            walletBalances["ETH"] = (decimal)0.009;
        }

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

        protected async Task<string> ExecuteReqAllPairs(string method) {
                HttpRequestMessage request = CreateRequest(method, false, new Dictionary<string, string>());

                HttpResponseMessage response = await httpClient.SendAsync(request);
                string responseContent = await response.Content.ReadAsStringAsync();

                return responseContent;
        }
        
        protected override async Task<ResponseWrapper> ExecuteRequest(string method, bool includeAuth,
            Dictionary<string, string> parameters = null) {
            if (parameters == null) {
                parameters = new Dictionary<string, string>();
            }

            HttpRequestMessage request = CreateRequest(method, includeAuth, parameters);

            HttpResponseMessage response = await httpClient.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            ResponseWrapper responseContentJson;
            try {
                responseContentJson = JsonConvert.DeserializeObject<ResponseWrapper>(responseContent);
            } catch (Exception e) {
                Console.WriteLine($"EXC WHILE EXC REQ: {response}");
                throw;
            }
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

        public sealed override async Task<ResponseWrapper> GetBalance(string currency) {
            Dictionary<string, string> parametres = new Dictionary<string, string> {
                {"currency", currency}
            };
            ResponseWrapper responseBalance = await ExecuteRequest("account/getbalances", true, parametres);
            return responseBalance;
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

        private async Task<decimal> DefineAmountToTrade(ExchangePair crossBittrex, decimal amountBtc, decimal amountEth) {
            string firtsCurInChain = crossBittrex.PurchasePath.ToUpper().Split('-')[0];
            if (firtsCurInChain == "BTC") {
                return amountBtc > 0 ? amountBtc : (decimal) 0.00051;
            } else if (firtsCurInChain == "ETH") {
                return amountEth > 0 ? amountEth : (decimal) 0.0082;
            } else {
                return (decimal) 0.00055 / allPairs[$"BTC-{firtsCurInChain}"].PurchasePrice;
            }
            
            decimal curBalance = (decimal) (await GetBalance(firtsCurInChain)).Result["Available"];
            lock (allPairs) {
                if (firtsCurInChain == "BTC") {
                    if ((decimal) 0.00051 <= curBalance) {
                        return (decimal) 0.00051;
                    } 
                } else if (firtsCurInChain == "USDT") {
                    if ((decimal) 0.00051 <= curBalance / allPairs["USDT-BTC"].PurchasePrice) {
                        return (decimal) 0.00051 * allPairs["USDT-BTC"].SellPrice;
                    }
                } else {
                    if ((decimal) 0.00055 <= curBalance * allPairs[$"BTC-{firtsCurInChain}"].SellPrice) {
                        return (decimal) 0.00051 / allPairs[$"BTC-{firtsCurInChain}"].PurchasePrice;
                    }
                }
            }
            return 0;
        }

        public override async void Trade(decimal amountBtc, decimal amountEth, TelegramBotClient client, long chatId, ManualResetEvent signal) {
            bool canTradeWithEth = amountEth > 0 ? true : false;
            bool canTradeWithBtc = amountBtc > 0 ? true : false;
            
            RestartTrading:
            if (!TradeOn) {
                return;
            }
            if (!canTradeWithBtc && !canTradeWithEth) {
                client.SendTextMessageAsync(chatId, "[Trading stopped]");
                return;
            }
            client.SendTextMessageAsync(chatId, "Searching crossRates... ");
            Stopwatch fullTime = new Stopwatch();
            fullTime.Start();
            decimal resultDealDone = 1;
            signal.WaitOne();
            
            ExchangePair crossBittrex;
            lock (TimeService.TimeCrossesByMarket) {
                DateTime maxDt = TimeService.TimeCrossesByMarket.Values.Max();
                crossBittrex = (canTradeWithEth
                                   ? TimeService.TimeCrossesByMarket.FirstOrDefault(d =>
                                       d.Value.Equals(maxDt) && d.Key.PurchasePath.Split('-')[0] == "ETH" && d.Key.Market == "Bittrex").Key
                                   : null) ?? (canTradeWithBtc
                                   ? TimeService.TimeCrossesByMarket.FirstOrDefault(d =>
                                       d.Value.Equals(maxDt) && d.Key.PurchasePath.Split('-')[0] == "BTC" && d.Key.Market == "Bittrex").Key
                                   :
                                   null);
            }

            AfterCrossBittrexFound:
            if (crossBittrex == null || crossBittrex.Market != "Bittrex") {
                signal.Reset();
                goto RestartTrading;
            }
            
            ResponseWrapper responseAllPairs = await GetAllPairs();
            lock (allPairs) {
                allPairs.Clear();
                foreach (var pair in (JArray) responseAllPairs.Result) {
                    allPairs.Add((string) pair["MarketName"], new ExchangePair {    
                        Pair = (string)pair["MarketName"],
                        PurchasePrice = (decimal) pair["Ask"],
                        SellPrice = (decimal) pair["Bid"]
                    });
                }

                allPairs.Add("BTC-BTC", new ExchangePair {
                    Pair = "BTC-BTC",
                    PurchasePrice = 1,
                    SellPrice = 1
                });
            }
            
            fullTime.Stop();
            client.SendTextMessageAsync(chatId, $"[FOUND]: {crossBittrex.PurchasePath.Replace("-", "->")}   {crossBittrex.SellPath.Replace("-", "<-")}\n" +
                                                $"Profit: {crossBittrex.Spread}%\n" +
                                                $"Time searching: {fullTime.Elapsed}");
            Stopwatch crossRateActualTime = new Stopwatch();
            decimal myAmount = await DefineAmountToTrade(crossBittrex, amountBtc, amountEth);
            if (myAmount == 0) {
                signal.Reset();
                client.SendTextMessageAsync(chatId, $"Amount {crossBittrex.PurchasePath.Split('-')[0]} is too small");
                goto RestartTrading;
            }
            
            string[] devidedPurchasePath = crossBittrex.PurchasePath.ToUpper().Split('-').ToArray();
            string[] devidedSellPath = crossBittrex.SellPath.ToUpper().Split('-').ToArray();
            (decimal minEqualToBtc, decimal resultDealF) =
                await FindMinAmountForTrade(devidedPurchasePath, devidedSellPath, myAmount); 
            
            if (minEqualToBtc < (decimal) 0.0005) {
                await client.SendTextMessageAsync(chatId, $"[UNPROFITABLE:( ] amount in order: {minEqualToBtc} btc\n" +
                                                    $"Must be more than 0.0005 btc");
                signal.Reset();
                goto RestartTrading;
            }

            /*if (resultDealF <= 1) {
                await client.SendTextMessageAsync(chatId, $"[UNPROFITABLE:( ] profit coefficient: {resultDealF}");
                signal.Reset();
                goto RestartTrading;
            }*/
            canTradeWithEth = false;
            canTradeWithBtc = false;

            crossRateActualTime.Stop();
            client.SendTextMessageAsync(chatId, $"Trading amount: {myAmount} {crossBittrex.PurchasePath.Split('-')[0]} Profit coefficient: {resultDealF}");

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
                        (myAmount, resultDealDone, crossBittrex) = await BuyCurrency(myAmount, pair, resultDealDone, crossBittrex);
                        client.SendTextMessageAsync(chatId, $"Pair: {pair}, Amount got: {myAmount} Type: BuyCurrency");
                    } catch (Exception e) {
                        Console.WriteLine($"Error while purchasing {crossBittrex.PurchasePath}: {pair}" +
                                          $"Error: {e.Message}  {e.StackTrace}");
                        break;
                    }
                } else {
                    try { 
                        (myAmount, resultDealDone, crossBittrex) = await SellCurrency(myAmount, pair, resultDealDone, crossBittrex);
                        client.SendTextMessageAsync(chatId, $"Pair: {pair}, Amount got: {myAmount} Type: SellCurrency");
                    } catch (Exception e) {
                        Console.WriteLine($"Error while purchasing {crossBittrex.PurchasePath}: {pair}" +
                                          $"Error: {e.Message}  {e.StackTrace}");
                        break;
                    }
                }

                if (myAmount == 0 && resultDealDone == 1) {
                    goto AfterCrossBittrexFound;
                }
            }
            
            //Sell process
            for (int i = devidedSellPath.Length - 1; i > 0; i--) {
                if (i < 1) {
                    break;
                }
                
                bool sell = true;
                string pair = $"{devidedSellPath[i - 1]}-{devidedSellPath[i]}";
                if (!allPairs.Keys.Contains(pair)) {
                    pair = $"{devidedSellPath[i]}-{devidedSellPath[i - 1]}";
                    sell = false;
                }

                if (sell) {
                    try {
                        (myAmount, resultDealDone, crossBittrex) = await SellCurrency(myAmount, pair, resultDealDone, crossBittrex);
                        client.SendTextMessageAsync(chatId, $"Pair: {pair}, Amount got: {myAmount} Type: SellCurrency");
                    } catch (Exception e) {
                        Console.WriteLine($"Error while selling {crossBittrex.PurchasePath}: {pair}" +
                                          $"Error: {e.Message}  {e.StackTrace}");
                        break;
                    } 
                } else {
                    try { 
                        (myAmount, resultDealDone, crossBittrex) = await BuyCurrency(myAmount, pair, resultDealDone, crossBittrex);
                        client.SendTextMessageAsync(chatId, $"Pair: {pair}, Amount got: {myAmount} Type: BuyCurrency");
                    } catch (Exception e) {
                        Console.WriteLine($"Error while selling {crossBittrex.PurchasePath}: {pair}" +
                                          $"Error: {e.Message}  {e.StackTrace}");
                        break;
                    }
                }
                
                if (myAmount == 0 && resultDealDone == 1) {
                    goto AfterCrossBittrexFound;
                }
            }
            
            client.SendTextMessageAsync(chatId, $"profit coefficient: {resultDealDone}");
            client.SendTextMessageAsync(chatId, "Bot on bittrex has done his job");
        }

        
        private async Task<(decimal, decimal, ExchangePair)> BuyCurrency(decimal myAmount, string pair, decimal resultDealDone, ExchangePair crossBittrex) {
            decimal buyRate = allPairs[pair].PurchasePrice;
            Console.WriteLine($"[Try to buy] {pair}  rate: {buyRate}");

            decimal bestBuyRate = 0;

            decimal boughtQuantityResult = 0;
            while (myAmount > 0) {
                var sellOrders = (JArray)GetOrderBook(pair).Result.Result["sell"];//= (JArray)GetOrderBook(pair).Result.Result["sell"]; // FOR IMITATION
                decimal bestBuyRateNew = (decimal)sellOrders[0]["Rate"]; // FOR IMITATION
                decimal bestBuyQuantity = (decimal)sellOrders[0]["Quantity"]; // FOR IMITATION
                if (bestBuyRate == bestBuyRateNew || bestBuyRateNew > buyRate) {
                    List<ExchangePair> crossRatesByMarket1 = new List<ExchangePair>();
                    Dictionary<string, int> curStartChain1 = new Dictionary<string, int>{ [pair.Split('-')[0]] = 0 };
                    FloydWarshell.FindCrossesOnMarket(ref crossRatesByMarket1, curStartChain1, new BittrexMarket(), false);
                    if (crossRatesByMarket1.Count != 0 && boughtQuantityResult == 0) {
                        crossBittrex = crossRatesByMarket1.First();
                        if (!crossBittrex.SellPath.Split('-').Contains("BTC")) {
                            crossBittrex.SellPath = "BTC-" + crossBittrex?.SellPath;
                        } else if (crossBittrex.SellPath.Split('-').Contains("BTC")) {
                            string[] splitedSellPath = crossBittrex.SellPath.Split('-');
                            crossBittrex.SellPath = "";
                            int ind = splitedSellPath.Length - 1;
                            string cur = splitedSellPath[ind--];
                            while (cur != "BTC") {
                                crossBittrex.SellPath = '-' + cur + crossBittrex.SellPath;
                                cur = splitedSellPath[ind--];
                            }

                            crossBittrex.SellPath = "BTC" + crossBittrex.SellPath;
                        }
                        Console.WriteLine($"[NEW] {crossBittrex?.PurchasePath} {crossBittrex?.SellPath} {crossBittrex?.Spread}");

                        return (0, 1, crossBittrex);
                    }
                    
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
            
            return (boughtQuantityResult, resultDealDone / (buyRate * (1 + FeeBuy)), crossBittrex);

            decimal buyQuantity = decimal.Truncate((myAmount / (buyRate * (1 + FeeBuy))) * 100000000) / 100000000;

            ResponseWrapper responseBuyOrder = await CreateBuyOrder(pair, buyQuantity, buyRate);
            string orderUuid;
            Console.WriteLine(responseBuyOrder.Result);
            try {
                 orderUuid = (string)responseBuyOrder.Result["uuid"];
            } catch (Exception e) {
                Console.WriteLine($"EXCEPTION BUY: {responseBuyOrder.Message}");
                throw;
            }
            
            
            if (!responseBuyOrder.Success) {
                Console.WriteLine($"[ResponseError] Pair: {pair}, mess: {responseBuyOrder.Message} buyQuantity: {buyQuantity}{pair}" +
                                  $"\nRate: {buyRate}");
                return (0, 1, crossBittrex);
            }

            var checkOrder = "";
            decimal quantityRemains;
            (checkOrder, quantityRemains) = await CheckOrder(orderUuid);
            List<ExchangePair> crossRatesByMarket = new List<ExchangePair>();
            Dictionary<string, int> curStartChain = new Dictionary<string, int>{ [pair.Split('-')[0]] = 0 };
            
            while (checkOrder != "Ok") {
                Console.WriteLine($"[Check order]: {checkOrder} quantityRemains: {quantityRemains}");
                /*if (checkOrder == "Fail") {
                    FloydWarshell.FindCrossesOnMarket(ref crossRatesByMarket, curStartChain, new BittrexMarket(), false);
                    if (crossRatesByMarket.Count != 0) {
                        crossBittrex = crossRatesByMarket.FirstOrDefault();
                        if (crossBittrex?.SellPath != null && !crossBittrex.SellPath.Split('-').Contains("ETH")) {
                            crossBittrex.SellPath = "ETH-" + crossBittrex?.SellPath;
                        } else if (crossBittrex?.SellPath != null && crossBittrex.SellPath.Split('-').Contains("ETH")) {
                            string[] splitedSellPath = crossBittrex.SellPath.Split('-');
                            crossBittrex.SellPath = "";
                            int ind = splitedSellPath.Length - 1;
                            string cur = splitedSellPath[ind--];
                            while (cur != "ETH") {
                                crossBittrex.SellPath = '-' + cur + crossBittrex.SellPath;
                                cur = splitedSellPath[ind--];
                            }

                            crossBittrex.SellPath = "ETH" + crossBittrex.SellPath;
                        }
                        
                        bool canceled = (await CancelOrder(orderUuid)).Success;
                        if (canceled) {
                            return (0, 1, crossBittrex);
                        }
                    }
                }*/
                
                await Task.Delay(4000);
                (checkOrder, quantityRemains) = await CheckOrder(orderUuid);
            }
            
            return (buyQuantity, resultDealDone / (buyRate * (1 + FeeBuy)), crossBittrex);
        }

        private async Task<(decimal, decimal, ExchangePair)> SellCurrency(decimal myAmount, string pair, decimal resultDealDone, ExchangePair crossBittrex) {
            decimal sellRate = allPairs[pair].SellPrice;
            Console.WriteLine($"[Try to sell] {pair}  rate: {sellRate}");
            
            decimal bestSellRate = 0;

            Console.WriteLine($"MyAmount: {myAmount}");
            decimal gotQuantityRemember = 0;
            while (myAmount > 0) {
                JArray buyOrders = (JArray)GetOrderBook(pair).Result.Result["buy"];
                decimal bestSellRateNew = (decimal)buyOrders[0]["Rate"];
                decimal bestSellQuantity = (decimal)buyOrders[0]["Quantity"];
                if (bestSellRateNew == bestSellRate || bestSellRateNew < sellRate && gotQuantityRemember == 0) {
                    List<ExchangePair> crossRatesByMarket1 = new List<ExchangePair>();
                    Dictionary<string, int> curStartChain1 = new Dictionary<string, int>{ [pair.Split('-')[1]] = 0 };
                    FloydWarshell.FindCrossesOnMarket(ref crossRatesByMarket1, curStartChain1, new BittrexMarket(), false);
                    if (crossRatesByMarket1.Count != 0) {
                        crossBittrex = crossRatesByMarket1.FirstOrDefault();
                        if (crossBittrex?.SellPath != null && !crossBittrex.SellPath.Split('-').Contains("BTC")) {
                            crossBittrex.SellPath = "BTC-" + crossBittrex?.SellPath;
                        } else if (crossBittrex?.SellPath != null && crossBittrex.SellPath.Split('-').Contains("BTC")) {
                            string[] splitedSellPath = crossBittrex.SellPath.Split('-');
                            crossBittrex.SellPath = "";
                            int ind = splitedSellPath.Length - 1;
                            string cur = splitedSellPath[ind--];
                            while (cur != "BTC") {
                                crossBittrex.SellPath = '-' + cur + crossBittrex.SellPath;
                                cur = splitedSellPath[ind--];
                            }

                            crossBittrex.SellPath = "BTC" + crossBittrex.SellPath;
                        }
                        Console.WriteLine($"[NEW] {crossBittrex?.PurchasePath} {crossBittrex?.SellPath} {crossBittrex?.Spread}");                        
                        return (0, 1, crossBittrex);
                    }
                    await Task.Delay(5000);
                    continue;
                }

                bestSellRate = bestSellRateNew;
                if (bestSellQuantity < myAmount) {
                    myAmount -= bestSellQuantity;
                    gotQuantityRemember += bestSellQuantity * (bestSellRate * (1 - FeeSell));
                } else {
                    gotQuantityRemember += myAmount * (bestSellRate * (1 - FeeSell));
                    myAmount = 0;
                }

            }
            Console.WriteLine($"gotQuantityRemember {gotQuantityRemember}");

            return (gotQuantityRemember, resultDealDone * (sellRate * (1 - FeeSell)), crossBittrex);
            decimal sellQuantity = myAmount;

            ResponseWrapper responseSellOrder = await CreateSellORder(pair, sellQuantity, sellRate);
            string orderUuid;
            try {
                orderUuid = (string)responseSellOrder.Result["uuid"];
            } catch (Exception e) {
                Console.WriteLine($"EXCEPTION BUY: {responseSellOrder.Message}");
                throw;
            }
            
            if (!responseSellOrder.Success) {
                Console.WriteLine($"[ResponseError] Pair: {pair}, mess: {responseSellOrder.Message}");
                return (0, 1, crossBittrex);
            }
            
            var checkOrder = "";
            decimal quantityRemains;
            (checkOrder, quantityRemains) = await CheckOrder(orderUuid);
            List<ExchangePair> crossRatesByMarket = new List<ExchangePair>();
            Dictionary<string, int> curStartChain = new Dictionary<string, int>{ [pair.Split('-')[1]] = 0 };
            
            while (checkOrder != "Ok") {
                Console.WriteLine($"[Check order]: {checkOrder} quantityRemains: {quantityRemains}");
                /*if (checkOrder == "Fail") {
                    FloydWarshell.FindCrossesOnMarket(ref crossRatesByMarket, curStartChain, new BittrexMarket(), false);
                    if (crossRatesByMarket.Count != 0) {
                        crossBittrex = crossRatesByMarket.FirstOrDefault();
                        if (crossBittrex?.SellPath != null && !crossBittrex.SellPath.Split('-').Contains("ETH")) {
                            crossBittrex.SellPath = "ETH-" + crossBittrex?.SellPath;
                        } else if (crossBittrex?.SellPath != null && crossBittrex.SellPath.Split('-').Contains("ETH")) {
                            string[] splitedSellPath = crossBittrex.SellPath.Split('-');
                            crossBittrex.SellPath = "";
                            int ind = splitedSellPath.Length - 1;
                            string cur = splitedSellPath[ind--];
                            while (cur != "ETH") {
                                crossBittrex.SellPath = '-' + cur + crossBittrex.SellPath;
                                cur = splitedSellPath[ind--];
                            }

                            crossBittrex.SellPath = "ETH" + crossBittrex.SellPath;
                        }
                        
                        bool canceled = (await CancelOrder(orderUuid)).Success;
                        if (canceled) {
                            return (0, 1, crossBittrex);
                        }
                    }
                }*/
                
                await Task.Delay(4000);
                (checkOrder, quantityRemains) = await CheckOrder(orderUuid);
            }

            return (decimal.Truncate((sellQuantity * (sellRate * (1 - FeeSell))) * 100000000) / 100000000, resultDealDone * (sellRate * (1 - FeeSell)), crossBittrex);
        }

        private async Task<(string, decimal)> CheckOrder(string orderUuid) {
            await Task.Delay(500);
            var responseOpenOrders = await GetOpenOrders();
            var ordersArr = ((JArray) responseOpenOrders.Result);
            Console.WriteLine(ordersArr.ToString());
            Console.WriteLine(responseOpenOrders.Message);
            Console.WriteLine(orderUuid);
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

        private async Task<(decimal, decimal)> FindMinAmountForTrade(string[] devidedPurchasePath, string[] devidedSellPath, decimal amount) {
            decimal resultDeal = 1;
            List<Tuple<Task<ResponseWrapper>, string, string>> exchangeProcessTasks = new List<Tuple<Task<ResponseWrapper>, string, string>>();
            decimal minEqualToBtc = ConvertToBtc(devidedPurchasePath[0], amount);
            
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
                    equalToBtc = ConvertToBtc(pair.Split('-')[1], bestBuyQuantity); //pairWithBtc != "USDT-BTC" ? allPairs[pairWithBtc].SellPrice * (1 - FeeSell) * bestBuyQuantity : bestBuyQuantity;
                    
                    var bestBuyRate = (decimal)sellOrders[0]["Rate"] * (1 + FeeBuy);
                    Console.WriteLine($"{pair}  {bestBuyRate}  Buy");
                    lock (allPairs) {
                        allPairs[pair].PurchasePrice = (decimal)sellOrders[0]["Rate"];
                    }
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
                    equalToBtc = ConvertToBtc(pair.Split('-')[1], bestSellQuantity);//pairWithBtc != "USDT-BTC" ? bestSellQuantity * allPairs[pairWithBtc].SellPrice * (1 - FeeSell) : bestSellQuantity;
                    
                    var bestSellRate = (decimal)buyOrders[0]["Rate"] * (1 - FeeSell);
                    Console.WriteLine($"{pair}  {bestSellRate}  Sell");
                    lock (allPairs) {
                        allPairs[pair].SellPrice = (decimal)buyOrders[0]["Rate"];
                    }
                    resultDeal *= bestSellRate;
                }
                
                if (equalToBtc < minEqualToBtc) {
                    Console.WriteLine($"[BTC_equal] {equalToBtc} Pair: {pair}  {deal.Item2} Time {DateTime.Now}");
                    minEqualToBtc = equalToBtc;
                }
            }
            
            Console.WriteLine($"minEqualToBtc:{minEqualToBtc}");
            Console.WriteLine($"resultDeal {resultDeal}");
            return (minEqualToBtc, resultDeal);
        }

        private decimal ConvertToBtc(string currency, decimal amount) {
            if (currency == "BTC") {
                return amount;
            } else if (currency == "USDT") {
                return amount / (allPairs["USDT-BTC"].PurchasePrice * (1 + FeeBuy));
            } else {
                return amount * (allPairs[$"BTC-{currency}"].SellPrice * (1 - FeeSell));
            }
        }
    }
}