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
    public class BittrexTradeBot : CommonTradeBot {
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
            //client.SendTextMessageAsync(chatId, "Searching crossRates... ");
            //Stopwatch fullTime = new Stopwatch();
            //fullTime.Start();
            allPairs.Clear();
            decimal resultDealDone = 1;
            signal.WaitOne();
            
            ExchangePair crossBittrex;
            lock (TimeService.TimeCrossesByMarket) {
                if (!canTradeWithBtc) {
                    crossBittrex = TimeService.TimeCrossesByMarket.Keys.FirstOrDefault(cross =>
                        cross.Market == "Bittrex" && cross.PurchasePath.Split('-')[0] == "ETH");
                } else if (!canTradeWithEth) {
                    crossBittrex = TimeService.TimeCrossesByMarket.Keys.FirstOrDefault(cross =>
                        cross.Market == "Bittrex" && cross.PurchasePath.Split('-')[0] == "BTC");
                } else {
                    crossBittrex =
                        TimeService.TimeCrossesByMarket.Keys.FirstOrDefault(cross => cross.Market == "Bittrex");
                }
            }

            if (crossBittrex == null) {
                goto RestartTrading;
            }
            
            string currentTime = DateTime.Now.ToString("G", DateTimeFormatInfo.InvariantInfo);
            ResponseWrapper responseAllPairs = await GetAllPairs();
            foreach (var pair in (JArray) responseAllPairs.Result) {
                allPairs.Add((string) pair["MarketName"], new ExchangePair {
                    Pair = (string)pair["MarketName"],
                    PurchasePrice = (decimal) pair["Ask"],
                    SellPrice = (decimal) pair["Bid"],
                    TimeInserted = currentTime
                });
            }

            allPairs.Add("BTC-BTC", new ExchangePair {
                Pair = "BTC-BTC",
                PurchasePrice = 1,
                SellPrice = 1,
                TimeInserted = currentTime
            });
            
            PairsCollectionService pairsAfterAnalysisCollection = new PairsCollectionService("PairsAfterAnalysis"); // NEW FOR DB STATISTICS
            await pairsAfterAnalysisCollection.InsertMany(allPairs.Values.ToArray()); // NEW FOR DB STATISTICS
            PairsCollectionService pairsCollectionService = new PairsCollectionService("Crossrates"); // NEW FOR DB STATISTICS
            crossBittrex.InsertCounter = pairsAfterAnalysisCollection.CurrentDbId;
            
            //fullTime.Stop();
            /*client.SendTextMessageAsync(chatId, $"Crossrate found: {crossBittrex.PurchasePath} {crossBittrex.SellPath} Spread: {crossBittrex.Spread}% " +
                                                $"Time searching: {fullTime.Elapsed}");*/
            //Stopwatch crossRateActualTime = new Stopwatch();
            //crossRateActualTime.Start();            
            
            string[] devidedPurchasePath = crossBittrex.PurchasePath.ToUpper().Split('-').ToArray();
            string[] devidedSellPath = crossBittrex.SellPath.ToUpper().Split('-').ToArray();
            (decimal minEqualToBtc, decimal resultDealF) =
                await FindMinAmountForTrade(devidedPurchasePath, devidedSellPath, (decimal)0.007);//devidedPurchasePath[0] == "BTC" ? amountBtc : amountEth * allPairs["BTC-ETH"].PurchasePrice);
            if (minEqualToBtc * (decimal)0.96 < (decimal) 0.0005) {
                //client.SendTextMessageAsync(chatId, $"Crossrate is not efficient (minEqualToBtc:{minEqualToBtc}). Trading stopped");
                signal.Reset();
                Console.WriteLine(await pairsCollectionService.UpdateDbOk(crossBittrex, $"minEqualToBtc:{minEqualToBtc}")); // NEW FOR DB STATISTICS
                goto RestartTrading;
            }

            if (resultDealF <= 1) {
                //client.SendTextMessageAsync(chatId, $"Crossrate is not efficient (resultDealF: {resultDealF}). Trading stopped");
                signal.Reset();
                Console.WriteLine(await pairsCollectionService.UpdateDbOk(crossBittrex, $"resultDealF: {resultDealF}")); // NEW FOR DB STATISTICS
                goto RestartTrading;
            }
            
            decimal myAmount = devidedPurchasePath[0] == "BTC" ? minEqualToBtc * (decimal)0.96 : minEqualToBtc / allPairs["BTC-ETH"].PurchasePrice * (decimal)0.97;
            Console.WriteLine(await pairsCollectionService.UpdateDbOk(crossBittrex, $"myAmount: {myAmount}")); // NEW FOR DB STATISTICS
            goto RestartTrading;
            //crossRateActualTime.Stop();
            /*client.SendTextMessageAsync(chatId, $"Trading amount: {myAmount}  resultDealF found: {resultDealF} Time: {crossRateActualTime.Elapsed}");*/

            // Buy proccess
            for (int i = 0; i <= devidedPurchasePath.Length - 2; i++) {
                if (myAmount == 0) {
                    client.SendTextMessageAsync(chatId, "Stoped trading: amount == 0");
                    break;
                }
                bool purchase = true;
                string pair = $"{devidedPurchasePath[i]}-{devidedPurchasePath[i + 1]}";
                if (!allPairs.Keys.Contains(pair)) {
                    pair = $"{devidedPurchasePath[i + 1]}-{devidedPurchasePath[i]}";
                    purchase = false;
                }

                if (purchase) {
                    try { 
                        (myAmount, resultDealDone) = BuyCurrency(myAmount, pair, resultDealDone).Result;
                        client.SendTextMessageAsync(chatId, $"Pair: {pair}, Amount got: {myAmount} Type: BuyCurrency");
                        if (myAmount == 0) {
                            Console.WriteLine("Error: Amount == 0");
                            client.SendTextMessageAsync(chatId, $"Smth gone wrong while purchasing: Amount == 0");
                            break;
                        }
                    } catch (Exception e) {
                        Console.WriteLine($"Error while purchasing {crossBittrex.PurchasePath}: {pair}" +
                                          $"Error: {e.Message}  {e.StackTrace}");
                        client.SendTextMessageAsync(chatId, $"Error while purchasing {crossBittrex.PurchasePath}: {pair}");
                        break;
                    }
                } else {
                    try { 
                        (myAmount, resultDealDone) = SellCurrency(myAmount, pair, resultDealDone, client, chatId).Result;
                        client.SendTextMessageAsync(chatId, $"Pair: {pair}, Amount got: {myAmount} Type: SellCurrency");
                        if (myAmount == 0) {
                            Console.WriteLine("Error: Amount == 0");
                            client.SendTextMessageAsync(chatId, $"Smth gone wrong while purchasing: Amount == 0");
                            break;
                        }
                    } catch (Exception e) {
                        Console.WriteLine($"Error while purchasing {crossBittrex.PurchasePath}: {pair}" +
                                          $"Error: {e.Message}  {e.StackTrace}");
                        client.SendTextMessageAsync(chatId, $"Error while purchasing {crossBittrex.PurchasePath}: {pair}");
                        break;
                    }
                }
            }
            
            //Sell process
            for (int i = devidedSellPath.Length - 1; i > 0; i--) {
                if (myAmount == 0) {
                    client.SendTextMessageAsync(chatId, "Stoped trading: amount == 0");
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
                        (myAmount, resultDealDone) = SellCurrency(myAmount, pair, resultDealDone, client, chatId).Result;
                        client.SendTextMessageAsync(chatId, $"Pair: {pair}, Amount got: {myAmount} Type: SellCurrency");
                        if (myAmount == 0) {
                            Console.WriteLine("Error: Amount == 0");
                            client.SendTextMessageAsync(chatId, $"Smth gone wrong while selling: Amount == 0");
                            break;
                        }
                    } catch (Exception e) {
                        Console.WriteLine($"Error while selling {crossBittrex.PurchasePath}: {pair}" +
                                          $"Error: {e.Message}  {e.StackTrace}");
                        client.SendTextMessageAsync(chatId, $"Error while selling {crossBittrex.PurchasePath}: {pair}");
                        break;
                    } 
                } else {
                    try { 
                        (myAmount, resultDealDone) = BuyCurrency(myAmount, pair, resultDealDone).Result;
                        client.SendTextMessageAsync(chatId, $"Pair: {pair}, Amount got: {myAmount} Type: BuyCurrency");
                        if (myAmount == 0) {
                            Console.WriteLine("Error: Amount == 0");
                            client.SendTextMessageAsync(chatId, $"Smth gone wrong while selling: Amount == 0");
                            break;
                        }
                    } catch (Exception e) {
                        Console.WriteLine($"Error while selling {crossBittrex.PurchasePath}: {pair}" +
                                          $"Error: {e.Message}  {e.StackTrace}");
                        client.SendTextMessageAsync(chatId, $"Error while selling {crossBittrex.PurchasePath}: {pair}");
                        break;
                    }
                }
            }
            
            TradeBotsStorage.DeleteTradeBot(chatId, "bittrex");
            client.SendTextMessageAsync(chatId, $"resultDealDone: {resultDealDone}");
            client.SendTextMessageAsync(chatId, "Bot on bittrex has done his job and was destroyed");
        }

        private async Task<(decimal, decimal)> BuyCurrency(decimal myAmount, string pair, decimal resultDealDone) {
            JArray sellOrders = (JArray)GetOrderBook(pair).Result.Result["sell"];
            decimal bestBuyRate = (decimal) sellOrders[0]["Rate"];
            decimal bestBuyQuantity = (decimal)sellOrders[0]["Quantity"];
            decimal buyQuantity = bestBuyQuantity * bestBuyRate * (1 + FeeBuy) > myAmount
                ? myAmount / bestBuyRate  * (1 + FeeBuy)
                : bestBuyQuantity;
            return (buyQuantity, 1);

            resultDealDone /= (bestBuyRate * (1 + FeeBuy));
            ResponseWrapper responseBuyOrder = await CreateBuyOrder(pair, buyQuantity, bestBuyRate);
            if (!responseBuyOrder.Success) {
                Console.WriteLine($"[ResponseError] Pair: {pair}, mess: {responseBuyOrder.Message} buyQuantity: {buyQuantity}{pair}" +
                                  $"\nRate: {bestBuyRate} BestQu: {bestBuyQuantity}");
                return (0, 1);
            }

            (string checkOrder, decimal quantityBought, string orderUuid) = CheckOrder(pair, "LIMIT_BUY").Result;
            Console.WriteLine($"[Check order]: {checkOrder}");
            if (checkOrder == "Ok") {
                myAmount = buyQuantity;
                return (myAmount, resultDealDone);
            }

            if (checkOrder == "Remains") {
                myAmount = quantityBought;
                CancelOrder(orderUuid);
                return (myAmount, resultDealDone);
            }

            myAmount = 0;
            CancelOrder(orderUuid);
            return (myAmount, resultDealDone);
        }

        private async Task<(decimal, decimal)> SellCurrency(decimal myAmount, string pair, decimal resultDealDone, TelegramBotClient client, long chatId) {
            JArray buyOrders = (JArray)GetOrderBook(pair).Result.Result["buy"];
            decimal bestSellRate = (decimal)buyOrders[0]["Rate"];
            decimal bestSellQuantity = (decimal)buyOrders[0]["Quantity"];
            decimal sellQuantity = bestSellQuantity > myAmount ? myAmount : bestSellQuantity;
            
            client.SendTextMessageAsync(chatId, $"[SellCurrency]: bestSellRate {bestSellRate}, bestSellQuantity {bestSellQuantity}, myAmount {myAmount} sellQuantity {sellQuantity}");
            return (sellQuantity * bestSellRate * (1 - FeeSell), 1);

            resultDealDone *= (bestSellRate * (1 - FeeSell));
            ResponseWrapper responseSellOrder = await CreateSellORder(pair, sellQuantity, bestSellRate);
            if (!responseSellOrder.Success) {
                Console.WriteLine($"[ResponseError] Pair: {pair}, mess: {responseSellOrder.Message}");
                return (0, 1);
            }

            (string checkOrder, decimal quantitySold, string orderUuid) = CheckOrder(pair, "LIMIT_SELL").Result;
            Console.WriteLine($"After SellLimit checkOrder:{checkOrder} quantitySold:{quantitySold}");
            if (checkOrder == "Ok") {
                myAmount = sellQuantity * bestSellRate * (1 - FeeSell);
                return (myAmount, resultDealDone);
            }

            if (checkOrder == "Remains") {
                myAmount = quantitySold * bestSellRate * (1 - FeeSell);
                CancelOrder(orderUuid);
                return (myAmount, resultDealDone);
            }

            myAmount = 0;
            CancelOrder(orderUuid);
            return (myAmount, resultDealDone);
        }

        private async Task<(string, decimal, string)> CheckOrder(string pair, string orderType) {
            var responseOpenOrders = await GetOpenOrders(pair);
            var ordersArr = ((JArray) responseOpenOrders.Result);
            if (ordersArr.Count == 0 || ordersArr.Where(ord => (string)ord["OrderType"] == orderType).ToArray().Length == 0) {
                return ("Ok", 0, "");
            }

            var order = ordersArr.Where(ord => (string)ord["OrderType"] == orderType).ToArray()[0];
            
            decimal quantity = (decimal)order["Quantity"];
            decimal quantityRemaining = (decimal)order["QuantityRemaining"];
            Console.WriteLine($"Quantity remains: quantity:{quantity} quantityRemaining:{quantityRemaining}");
            if (quantity != quantityRemaining) {
                return ("Remains", quantity - quantityRemaining, (string)order["OrderUuid"]);
            } else {
                return ("Fail", 0, (string)order["OrderUuid"]);
            }
        }

        private async Task<(decimal, decimal)> FindMinAmountForTrade(string[] devidedPurchasePath, string[] devidedSellPath, decimal minEqualToBtc) {
            decimal resultDeal = 1;
            
            for (int i = 0; i <= devidedPurchasePath.Length - 2; i++) {
                bool purchase = true;
                string pair = $"{devidedPurchasePath[i]}-{devidedPurchasePath[i + 1]}";
                if (!allPairs.Keys.Contains(pair)) {
                    pair = $"{devidedPurchasePath[i + 1]}-{devidedPurchasePath[i]}";
                    purchase = false;
                }

                string pairWithBtc;

                if (purchase) {
                    pairWithBtc = pair != "USDT-BTC" ? $"BTC-{pair.Split('-')[1]}" : pair;
                    //pairWithBtc = $"BTC-{pair.Split('-')[1]}";
                    /*if (pair.Split('-')[1] == "USDT") {
                        pairWithBtc = "USDT-BTC";
                        //return (0, 1);
                    }*/
                    Console.WriteLine(pair);
                    
                    var responseOrders = await GetOrderBook(pair);
                    JArray sellOrders;
                    try {
                        sellOrders = (JArray)responseOrders.Result["sell"];
                    } catch (Exception e) {
                        Console.WriteLine($"Exception: {e.Message} Where: {e.StackTrace}  SellOrders: {responseOrders.Result["sell"]}");
                        return (0, 0);
                    }
                    decimal bestBuyQuantity = (decimal)sellOrders[0]["Quantity"];
                    Console.WriteLine($"[FindMinAmountForTrade] bestBuyQuantity:{bestBuyQuantity} pairWithBtc: {pairWithBtc}={allPairs[pairWithBtc].PurchasePrice}");
                    decimal equalToBtc = allPairs[pairWithBtc].PurchasePrice * bestBuyQuantity;
                    if (equalToBtc < minEqualToBtc) {
                        minEqualToBtc = equalToBtc;
                    }
                    
                    decimal bestBuyRate = (decimal)sellOrders[0]["Rate"];
                    resultDeal /= (bestBuyRate * (1 + FeeBuy));

                } else {
                    pairWithBtc = pair != "USDT-BTC" ? $"BTC-{pair.Split('-')[1]}" : pair;
                    //pairWithBtc = $"BTC-{pair.Split('-')[1]}";
                    /*if (pair.Split('-')[1] == "USDT") {
                        pairWithBtc = "USDT-BTC";
                        //return (0, 1);
                    }*/
                    Console.WriteLine(pair);
                    
                    var responseOrders = await GetOrderBook(pair);
                    JArray buyOrders;
                    try {
                        buyOrders = (JArray)responseOrders.Result["buy"];
                    } catch (Exception e) {
                        Console.WriteLine($"Exception: {e.Message} Where: {e.StackTrace}  BuyOrders: {responseOrders.Result["buy"]}");
                        return (0, 0);
                    }
                    decimal bestSellQuantity = (decimal)buyOrders[0]["Quantity"];
                    Console.WriteLine($"[FindMinAmountForTrade] bestSellQuantity:{bestSellQuantity} pairWithBtc: {pairWithBtc}={allPairs[pairWithBtc].SellPrice}");
                    decimal equalToBtc = allPairs[pairWithBtc].SellPrice * bestSellQuantity;
                    if (equalToBtc < minEqualToBtc) {
                        minEqualToBtc = equalToBtc;
                    }
                    
                    decimal bestSellRate = (decimal)buyOrders[0]["Rate"];
                    resultDeal *= (bestSellRate * (1 - FeeSell));
                }
                
            }

            for (int i = devidedSellPath.Length - 1; i > 0; i--) {
                bool sell = true;
                string pair = $"{devidedSellPath[i - 1]}-{devidedSellPath[i]}";
                if (!allPairs.Keys.Contains(pair)) {
                    pair = $"{devidedSellPath[i]}-{devidedSellPath[i - 1]}";
                    sell = false;
                }

                string pairWithBtc;

                if (sell) {
                    pairWithBtc = pair != "USDT-BTC" ? $"BTC-{pair.Split('-')[1]}" : pair;
                    //pairWithBtc = $"BTC-{pair.Split('-')[1]}";
                    /*if (pair.Split('-')[1] == "USDT") {
                        pairWithBtc = "USDT-BTC";
                        //return (0, 1);
                    }*/
                    Console.WriteLine(pair);
                    
                    var responseOrders = await GetOrderBook(pair);
                    JArray buyOrders;
                    try {
                        buyOrders = (JArray)responseOrders.Result["buy"];
                    } catch (Exception e) {
                        Console.WriteLine($"Exception: {e.Message} Where: {e.StackTrace} BuyOrders: {responseOrders.Result["buy"]}");
                        return (0, 0);
                    }
                    decimal bestSellQuantity = (decimal)buyOrders[0]["Quantity"];
                    Console.WriteLine($"[FindMinAmountForTrade] bestSellQuantity:{bestSellQuantity} pairWithBtc: {pairWithBtc}={allPairs[pairWithBtc].SellPrice}");
                    decimal equalToBtc = allPairs[pairWithBtc].SellPrice * bestSellQuantity;
                    if (equalToBtc < minEqualToBtc) {
                        minEqualToBtc = equalToBtc;
                    }
                    
                    decimal bestSellRate = (decimal)buyOrders[0]["Rate"];
                    resultDeal *= (bestSellRate * (1 - FeeSell));
                } else {
                    pairWithBtc = pair != "USDT-BTC" ? $"BTC-{pair.Split('-')[1]}" : pair;
                    //pairWithBtc = $"BTC-{pair.Split('-')[1]}";
                    /*if (pair.Split('-')[1] == "USDT") {
                        pairWithBtc = "USDT-BTC";
                        //return (0, 1);
                    }*/
                    Console.WriteLine(pair);
                    
                    var responseOrders = await GetOrderBook(pair);
                    JArray sellOrders;
                    try {
                        sellOrders = (JArray) responseOrders.Result["sell"];
                    } catch (Exception e) {
                        Console.WriteLine($"Exception: {e.Message} Where: {e.StackTrace} SellOrders: {responseOrders.Result["sell"]}");
                        return (0, 0);
                    }
                    decimal bestBuyQuantity = (decimal)sellOrders[0]["Quantity"];
                    Console.WriteLine($"[FindMinAmountForTrade] bestBuyQuantity:{bestBuyQuantity} pairWithBtc: {pairWithBtc}={allPairs[pairWithBtc].PurchasePrice}");
                    decimal equalToBtc = allPairs[pairWithBtc].PurchasePrice * bestBuyQuantity;
                    if (equalToBtc < minEqualToBtc) {
                        minEqualToBtc = equalToBtc;
                    }
                    
                    decimal bestBuyRate = (decimal)sellOrders[0]["Rate"];
                    resultDeal /= (bestBuyRate * (1 + FeeBuy));
                }
            }

            Console.WriteLine($"minEqualToBtc:{minEqualToBtc}");
            Console.WriteLine($"resultDeal {resultDeal}");
            return (minEqualToBtc, resultDeal);
        }
    }
}