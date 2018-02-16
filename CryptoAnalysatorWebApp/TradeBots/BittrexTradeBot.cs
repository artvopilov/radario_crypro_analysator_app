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

        public override void StartTrading(TelegramBotClient client, long chatId) {
            Thread thread = new Thread(() => Trade(TradeAmountBtc, TradeAmountEth, client, chatId));
            thread.Start();
        }

        public override async void Trade(decimal amountBtc, decimal amountEth, TelegramBotClient client, long chatId) {
            allPairs.Clear();
            ResponseWrapper responseAllPairs = await GetAllPairs();
            foreach (var pair in (JArray)responseAllPairs.Result) {
                allPairs.Add((string)pair["MarketName"], (decimal)pair["Last"]);
            }
            allPairs.Add("BTC-BTC", 1);

            ExchangePair crossBittrex;
            bool canTradeWithBtc = amountBtc > 0 ? true : false;
            bool canTradeWithEth = amountEth > 0 ? true : false;
            if (!canTradeWithBtc) {
                crossBittrex = TimeService.TimeCrossesByMarket.Keys.FirstOrDefault(cross =>
                    cross.Market == "Bittrex" && cross.PurchasePath.Split('-')[0] == "ETH");
            } else if (!canTradeWithEth) {
                crossBittrex = TimeService.TimeCrossesByMarket.Keys.FirstOrDefault(cross =>
                    cross.Market == "Bittrex" && cross.PurchasePath.Split('-')[0] == "BTC");
            } else {
                crossBittrex = TimeService.TimeCrossesByMarket.Keys.FirstOrDefault(cross => cross.Market == "Bittrex");
            }
                        
            while (crossBittrex == null) {
                client.SendTextMessageAsync(chatId, "No crossrates");
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

                Thread.Sleep(2000);
            }
            client.SendTextMessageAsync(chatId, $"Crossrate found: {crossBittrex.PurchasePath} {crossBittrex.SellPath} Spread: {crossBittrex.Spread}%");
            
            
            string[] devidedPurchasePath = crossBittrex.PurchasePath.ToUpper().Split('-').ToArray();
            string[] devidedSellPath = crossBittrex.SellPath.ToUpper().Split('-').ToArray();
            decimal minEqualToBtc = await FindMinAmountForTrade(devidedPurchasePath, devidedSellPath, devidedPurchasePath[0] == "BTC" ? amountBtc : amountEth * allPairs["BTC-ETH"]);
            if (minEqualToBtc * (decimal)0.96 < (decimal) 0.0005) {
                client.SendTextMessageAsync(chatId, $"Crossrate is not efficient (minEqualToBtc:{minEqualToBtc}). Trading stopped");
                TradeBotsStorage.DeleteTradeBot(chatId, "bittrex");
                client.SendTextMessageAsync(chatId, "Bot on bittrex has done his job and was destroyed");
                return;
            }
            
            decimal myAmount = devidedPurchasePath[0] == "BTC" ? minEqualToBtc * (decimal)0.96 : minEqualToBtc / allPairs["BTC-ETH"] * (decimal)0.97;
            
            client.SendTextMessageAsync(chatId, $"Trading amount: {myAmount}");

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
                        myAmount = BuyCurrency(myAmount, pair).Result;
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
                        myAmount = SellCurrency(myAmount, pair).Result;
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
                        myAmount = SellCurrency(myAmount, pair).Result;
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
                        myAmount = BuyCurrency(myAmount, pair).Result;
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
            client.SendTextMessageAsync(chatId, "Bot on bittrex has done his job and was destroyed");
        }

        private async Task<decimal> BuyCurrency(decimal myAmount, string pair) {
            JArray sellOrders = (JArray)GetOrderBook(pair).Result.Result["sell"];
            decimal bestBuyRate = (decimal) sellOrders[0]["Rate"];
            decimal bestBuyQuantity = (decimal)sellOrders[0]["Quantity"];
            decimal buyQuantity = bestBuyQuantity * bestBuyRate * (1 + FeeBuy) > myAmount
                ? myAmount / bestBuyRate  * (1 + FeeBuy)
                : bestBuyQuantity;
            //return myAmount / bestBuyRate  * (1 + FeeBuy);
                    
            ResponseWrapper responseBuyOrder = await CreateBuyOrder(pair, buyQuantity, bestBuyRate);
            if (!responseBuyOrder.Success) {
                Console.WriteLine($"[ResponseError] Pair: {pair}, mess: {responseBuyOrder.Message} buyQuantity: {buyQuantity}{pair}" +
                                  $"\nRate: {bestBuyRate} BestQu: {bestBuyQuantity}");
                return 0;
            }

            (string checkOrder, decimal quantityBought, string orderUuid) = CheckOrder(pair, "LIMIT_BUY").Result;
            Console.WriteLine($"[Check order]: {checkOrder}");
            if (checkOrder == "Ok") {
                myAmount = buyQuantity;
                return myAmount;
            }

            if (checkOrder == "Remains") {
                myAmount = quantityBought;
                CancelOrder(orderUuid);
                return myAmount;
            }

            myAmount = 0;
            CancelOrder(orderUuid);
            return myAmount;
        }

        private async Task<decimal> SellCurrency(decimal myAmount, string pair) {
            JArray buyOrders = (JArray)GetOrderBook(pair).Result.Result["buy"];
            decimal bestSellRate = (decimal)buyOrders[0]["Rate"];
            decimal bestSellQuantity = (decimal)buyOrders[0]["Quantity"];
            decimal sellQuantity = bestSellQuantity > myAmount ? myAmount : bestSellQuantity;

            //return myAmount * bestSellRate * (1 - FeeSell);
                    
            ResponseWrapper responseSellOrder = await CreateSellORder(pair, sellQuantity, bestSellRate);
            if (!responseSellOrder.Success) {
                Console.WriteLine($"[ResponseError] Pair: {pair}, mess: {responseSellOrder.Message}");
                return 0;
            }

            (string checkOrder, decimal quantitySold, string orderUuid) = CheckOrder(pair, "LIMIT_SELL").Result;
            Console.WriteLine($"Afetr SellLimit checkOrder:{checkOrder} quantitySold:{quantitySold}");
            if (checkOrder == "Ok") {
                myAmount = sellQuantity * bestSellRate * (1 - FeeSell);
                return myAmount;
            }

            if (checkOrder == "Remains") {
                myAmount = quantitySold * bestSellRate * (1 - FeeSell);
                CancelOrder(orderUuid);
                return myAmount;
            }

            myAmount = 0;
            CancelOrder(orderUuid);
            return myAmount;
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

        private async Task<decimal> FindMinAmountForTrade(string[] devidedPurchasePath, string[] devidedSellPath, decimal minEqualToBtc) {
            for (int i = 0; i <= devidedPurchasePath.Length - 2; i++) {
                bool purchase = true;
                string pair = $"{devidedPurchasePath[i]}-{devidedPurchasePath[i + 1]}";
                if (!allPairs.Keys.Contains(pair)) {
                    pair = $"{devidedPurchasePath[i + 1]}-{devidedPurchasePath[i]}";
                    purchase = false;
                }

                if (pair.Contains("USDT")) {
                    return 0;
                }
                Console.WriteLine(pair);

                if (purchase) {
                    var responseOrders = await GetOrderBook(pair);
                    JArray sellOrders = (JArray)responseOrders.Result["sell"];
                    decimal bestBuyQuantity = (decimal)sellOrders[0]["Quantity"];
                    Console.WriteLine($"[FindMinAmountForTrade] bestBuyQuantity:{bestBuyQuantity} pair: BTC-{pair.Split('-')[1]}={allPairs[$"BTC-{pair.Split('-')[1]}"]}");
                    decimal equalToBtc = allPairs[$"BTC-{pair.Split('-')[1]}"] * bestBuyQuantity;
                    if (equalToBtc < minEqualToBtc) {
                        minEqualToBtc = equalToBtc;
                    }

                } else {
                    var responseOrders = await GetOrderBook(pair);
                    JArray buyOrders = (JArray)responseOrders.Result["buy"];
                    decimal bestSellQuantity = (decimal)buyOrders[0]["Quantity"];
                    Console.WriteLine($"[FindMinAmountForTrade] bestSellQuantity:{bestSellQuantity} pair: BTC-{pair.Split('-')[0]}={allPairs[$"BTC-{pair.Split('-')[0]}"]}");
                    decimal equalToBtc = allPairs[$"BTC-{pair.Split('-')[0]}"] * bestSellQuantity;
                    if (equalToBtc < minEqualToBtc) {
                        minEqualToBtc = equalToBtc;
                    }
                }
                
            }

            for (int i = devidedSellPath.Length - 1; i > 0; i--) {
                bool sell = true;
                string pair = $"{devidedSellPath[i - 1]}-{devidedSellPath[i]}";
                if (!allPairs.Keys.Contains(pair)) {
                    pair = $"{devidedSellPath[i]}-{devidedSellPath[i - 1]}";
                    sell = false;
                }
                Console.WriteLine(pair);

                if (sell) {
                    var responseOrders = await GetOrderBook(pair);
                    JArray buyOrders = (JArray)responseOrders.Result["buy"];
                    decimal bestSellQuantity = (decimal)buyOrders[0]["Quantity"];
                    Console.WriteLine($"[FindMinAmountForTrade] bestSellQuantity:{bestSellQuantity} pair: BTC-{pair.Split('-')[0]}={allPairs[$"BTC-{pair.Split('-')[0]}"]}");
                    decimal equalToBtc = allPairs[$"BTC-{pair.Split('-')[0]}"] * bestSellQuantity;
                    if (equalToBtc < minEqualToBtc) {
                        minEqualToBtc = equalToBtc;
                    }
                } else {
                    var responseOrders = await GetOrderBook(pair);
                    JArray sellOrders = (JArray)responseOrders.Result["sell"];
                    decimal bestBuyQuantity = (decimal)sellOrders[0]["Quantity"];
                    Console.WriteLine($"[FindMinAmountForTrade] bestBuyQuantity:{bestBuyQuantity} pair: BTC-{pair.Split('-')[1]}={allPairs[$"BTC-{pair.Split('-')[1]}"]}");
                    decimal equalToBtc = allPairs[$"BTC-{pair.Split('-')[1]}"] * bestBuyQuantity;
                    if (equalToBtc < minEqualToBtc) {
                        minEqualToBtc = equalToBtc;
                    }
                }
            }

            Console.WriteLine($"minEqualToBtc:{minEqualToBtc}");
            return minEqualToBtc;
        }
    }
}