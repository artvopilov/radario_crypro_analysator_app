using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CryptoAnalysatorWebApp.Models;
using CryptoAnalysatorWebApp.Models.Db;
using CryptoAnalysatorWebApp.TradeBots.Common;
using CryptoAnalysatorWebApp.TradeBots.Common.Objects;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;

namespace CryptoAnalysatorWebApp.TradeBots {
    public class PoloniexTradeBot : CommonTradeBot<JObject> {
        private const decimal FeeBuy = (decimal) 0.0025;
        private const decimal FeeSell = (decimal) 0.0025;

        public PoloniexTradeBot(string baseUrl = "https://poloniex.com/public?command=") : base(baseUrl) { }
        
        protected override HttpRequestMessage CreateRequest(string method, bool includeAuth,
            Dictionary<string, string> parameters) {
            string parametersString;

            if (parameters.Count == 0) {
                parametersString = "";
            } else {
                parametersString = string.Join('&',
                    parameters.Select(param => WebUtility.UrlEncode(param.Key) + '=' + WebUtility.UrlEncode(param.Value)));
            }
            string completeUrl = baseUrl + method + '&' + parametersString;
            Console.WriteLine($"CompleteUrl: {completeUrl}");
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, completeUrl);
            return request;
        }
        
        protected override async Task<JObject> ExecuteRequest(string method, bool includeAuth,
            Dictionary<string, string> parameters = null) {
            if (parameters == null) {
                parameters = new Dictionary<string, string>();
            }

            HttpRequestMessage request = CreateRequest(method, includeAuth, parameters);

            HttpResponseMessage response = await httpClient.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            JObject responseContentJson = JObject.Parse(responseContent);
            return responseContentJson;
        }

        public override async Task<JObject> GetAllPairs() {
            JObject responseAllPairs = await ExecuteRequest("returnTicker", false);
            return responseAllPairs;
        }

        public override async Task<JObject> GetOrderBook(string pair) {
            Dictionary<string, string> parametres = new Dictionary<string, string> {
                {"currencyPair", pair.Replace('-', '_')},
                { "depth", "10" },
            };
            var responseOrderBook = await ExecuteRequest("returnOrderBook", false, parametres);
            return responseOrderBook;
        }

        public sealed override async Task<JObject> GetBalances() {
            JObject responseAllPairs = await ExecuteRequest("returnTicker", false);
            return responseAllPairs;
        }

        public override async Task<JObject> GetOpenOrders(string pair = null) {
            JObject responseAllPairs = await ExecuteRequest("returnTicker", false);
            return responseAllPairs;
        }

        public override async Task<JObject> CreateBuyOrder(string pair, decimal quantity, decimal rate) {
            JObject responseAllPairs = await ExecuteRequest("returnTicker", false);
            return responseAllPairs;
        }

        public override async Task<JObject> CreateSellORder(string pair, decimal quantity, decimal rate) {
            JObject responseAllPairs = await ExecuteRequest("returnTicker", false);
            return responseAllPairs;
        }

        public override async Task<JObject> CancelOrder(string orderId) {
            JObject responseAllPairs = await ExecuteRequest("returnTicker", false);
            return responseAllPairs;
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
                crossBittrex = TimeService.TimeCrossesByMarket.Keys.FirstOrDefault(cross => cross.Market == "Poloniex");
                /*if (!canTradeWithBtc) {
                    crossBittrex = TimeService.TimeCrossesByMarket.Keys.FirstOrDefault(cross =>
                        cross.Market == "Poloniex" && cross.PurchasePath.Split('-')[0] == "ETH");
                } else if (!canTradeWithEth) {
                    crossBittrex = TimeService.TimeCrossesByMarket.Keys.FirstOrDefault(cross =>
                        cross.Market == "Poloniex" && cross.PurchasePath.Split('-')[0] == "BTC");
                } else {
                    crossBittrex =
                        TimeService.TimeCrossesByMarket.Keys.FirstOrDefault(cross => cross.Market == "Poloniex");
                }*/
            }

            if (crossBittrex == null) {
                goto RestartTrading;
            }
            
            string currentTime = DateTime.Now.ToString("G", DateTimeFormatInfo.InvariantInfo);
            var responseAllPairs = await GetAllPairs();
            try {
                foreach (var pair in responseAllPairs) {
                    Console.WriteLine(pair.Key.Split('-')[0]);
                    if (pair.Key.Split('-').Length > 1) {
                        Console.WriteLine("AAAAAAAA");
                        continue;
                    }
                    allPairs.Add((string) pair.Key, new ExchangePair {
                        Pair = (string) pair.Key.Replace('_', '-'),
                        PurchasePrice = (decimal) pair.Value["lowestAsk"] * (1 + FeeBuy),
                        SellPrice = (decimal) pair.Value["highestBid"] * (1 - FeeSell),
                        TimeInserted = currentTime
                    });
                }
            } catch (Exception e) {
                Console.WriteLine($"Error: {e.Message}  Where: {e.StackTrace}");
            }

            allPairs.Add("BTC-BTC", new ExchangePair {
                Pair = "BTC-BTC",
                PurchasePrice = 1,
                SellPrice = 1,
                TimeInserted = currentTime
            });
            
            fullTime.Stop();
            client.SendTextMessageAsync(chatId, $"Crossrate found: {crossBittrex.PurchasePath} {crossBittrex.SellPath} Spread: {crossBittrex.Spread}% " +
                                                $"Time searching: {fullTime.Elapsed}");
            Stopwatch crossRateActualTime = new Stopwatch();
            crossRateActualTime.Start();            
            
            string[] devidedPurchasePath = crossBittrex.PurchasePath.ToUpper().Split('-').ToArray();
            string[] devidedSellPath = crossBittrex.SellPath.ToUpper().Split('-').ToArray();
            (decimal minEqualToBtc, decimal resultDealF) =
                await FindMinAmountForTrade(devidedPurchasePath, devidedSellPath, (decimal)0.007);//devidedPurchasePath[0] == "BTC" ? amountBtc : amountEth * allPairs["BTC-ETH"].PurchasePrice);
            
            //PairsCollectionService pairsAfterAnalysisCollection = new PairsCollectionService("PairsAfterAnalysis"); // NEW FOR DB STATISTICS
            //await pairsAfterAnalysisCollection.InsertMany(allPairs.Values.ToArray()); // NEW FOR DB STATISTICS
            //PairsCollectionService pairsCollectionService = new PairsCollectionService("Crossrates"); // NEW FOR DB STATISTICS
            //crossBittrex.InsertCounter = pairsAfterAnalysisCollection.CurrentDbId;
            
            if (minEqualToBtc * (decimal)0.97 < (decimal) 0.0005) {
                client.SendTextMessageAsync(chatId, $"Crossrate is not efficient (minEqualToBtc:{minEqualToBtc}). Trading stopped");
                signal.Reset();
                //Console.WriteLine(await pairsCollectionService.UpdateDbOk(crossBittrex, $"minEqualToBtc:{minEqualToBtc}")); // NEW FOR DB STATISTICS
                goto RestartTrading;
            }

            if (resultDealF <= 1) {
                client.SendTextMessageAsync(chatId, $"Crossrate is not efficient (resultDealF: {resultDealF}). Trading stopped");
                signal.Reset();
                //Console.WriteLine(await pairsCollectionService.UpdateDbOk(crossBittrex, $"resultDealF: {resultDealF}")); // NEW FOR DB STATISTICS
                goto RestartTrading;
            }

            decimal myAmount = minEqualToBtc;//devidedPurchasePath[0] == "BTC" ? minEqualToBtc * (decimal)0.96 : minEqualToBtc / allPairs["BTC-ETH"].PurchasePrice * (decimal)0.97;
            //Console.WriteLine(await pairsCollectionService.UpdateDbOk(crossBittrex, $"myAmount: {myAmount}")); // NEW FOR DB STATISTICS
            crossRateActualTime.Stop();
            client.SendTextMessageAsync(chatId, $"Trading amount: {myAmount}  resultDealF found: {resultDealF} Time: {crossRateActualTime.Elapsed}");
            goto RestartTrading;
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
                    JToken sellOrders;
                    try {
                        sellOrders = responseOrders["asks"];
                    } catch (Exception e) {
                        Console.WriteLine($"Exception: {e.Message} Where: {e.StackTrace}");
                        return (0, 0);
                    }
                    decimal bestBuyQuantity = (decimal)sellOrders[0][1];
                    Console.WriteLine($"[FindMinAmountForTrade] bestBuyQuantity:{bestBuyQuantity} pairWithBtc: {pairWithBtc}={allPairs[pairWithBtc].PurchasePrice}");
                    decimal equalToBtc = allPairs[pairWithBtc].PurchasePrice * bestBuyQuantity;
                    if (equalToBtc < minEqualToBtc) {
                        minEqualToBtc = equalToBtc;
                    }
                    
                    decimal bestBuyRate = decimal.Parse((string)sellOrders[0][0]);
                    allPairs[pair].PurchasePrice = bestBuyRate * (1 + FeeBuy);
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
                    JToken buyOrders;
                    try {
                        buyOrders = responseOrders["bids"];
                    } catch (Exception e) {
                        Console.WriteLine($"Exception: {e.Message} Where: {e.StackTrace} ");
                        return (0, 0);
                    }
                    decimal bestSellQuantity = (decimal)buyOrders[0][1];
                    Console.WriteLine($"[FindMinAmountForTrade] bestSellQuantity:{bestSellQuantity} pairWithBtc: {pairWithBtc}={allPairs[pairWithBtc].SellPrice}");
                    decimal equalToBtc = allPairs[pairWithBtc].SellPrice * bestSellQuantity;
                    if (equalToBtc < minEqualToBtc) {
                        minEqualToBtc = equalToBtc;
                    }
                    
                    decimal bestSellRate = decimal.Parse((string)buyOrders[0][0]);
                    allPairs[pair].SellPrice = bestSellRate * (1 - FeeSell);
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
                    JToken buyOrders;
                    try {
                        buyOrders = responseOrders["bids"];
                    } catch (Exception e) {
                        Console.WriteLine($"Exception: {e.Message} Where: {e.StackTrace}");
                        return (0, 0);
                    }
                    decimal bestSellQuantity = (decimal)buyOrders[0][1];
                    Console.WriteLine($"[FindMinAmountForTrade] bestSellQuantity:{bestSellQuantity} pairWithBtc: {pairWithBtc}={allPairs[pairWithBtc].SellPrice}");
                    decimal equalToBtc = allPairs[pairWithBtc].SellPrice * bestSellQuantity;
                    if (equalToBtc < minEqualToBtc) {
                        minEqualToBtc = equalToBtc;
                    }
                    
                    decimal bestSellRate = decimal.Parse((string)buyOrders[0][0]);
                    allPairs[pair].SellPrice = bestSellRate * (1 - FeeSell);
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
                    JToken sellOrders;
                    try {
                        sellOrders = responseOrders["asks"];
                    } catch (Exception e) {
                        Console.WriteLine($"Exception: {e.Message} Where: {e.StackTrace}");
                        return (0, 0);
                    }
                    decimal bestBuyQuantity = (decimal)sellOrders[0][1];
                    Console.WriteLine($"[FindMinAmountForTrade] bestBuyQuantity:{bestBuyQuantity} pairWithBtc: {pairWithBtc}={allPairs[pairWithBtc].PurchasePrice}");
                    decimal equalToBtc = allPairs[pairWithBtc].PurchasePrice * bestBuyQuantity;
                    if (equalToBtc < minEqualToBtc) {
                        minEqualToBtc = equalToBtc;
                    }
                    
                    decimal bestBuyRate = decimal.Parse((string)sellOrders[0][0]);
                    allPairs[pair].PurchasePrice = bestBuyRate * (1 + FeeBuy);
                    resultDeal /= (bestBuyRate * (1 + FeeBuy));
                }
            }

            Console.WriteLine($"minEqualToBtc:{minEqualToBtc}");
            Console.WriteLine($"resultDeal {resultDeal}");
            return (minEqualToBtc, resultDeal);
        }
    }
}