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
using System.Threading.Tasks;
using CryptoAnalysatorWebApp.Models;
using CryptoAnalysatorWebApp.TradeBots.Common.Objects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.Buffers;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace CryptoAnalysatorWebApp.TradeBots {
    public class BittrexTradeBot : CommonTradeBot {
        private decimal feeBuy = (decimal)0.0025;
        private decimal feeSell = (decimal)0.0025;
        
        public BittrexTradeBot(string apiKey, string apiSecret, string baseUrl = "https://bittrex.com/api/v1.1/") :
            base(apiKey, apiSecret, baseUrl) {
            var responseBalances = GetBalances().Result;
            JArray currenciesOnBalance = (JArray) responseBalances.Result;
            BalanceBtc = (decimal) currenciesOnBalance.First(cur => (string) cur["Currency"] == "BTC");
            BalanceEth = (decimal) currenciesOnBalance.First(cur => (string) cur["Currency"] == "ETH");
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
            ResponseWrapper responseAllPairs = await ExecuteRequest("public/getmarkets", false);
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
            ResponseWrapper responseOpenOrders = await ExecuteRequest("market/getopenorders", true);
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
            ResponseWrapper responseSellOrder = await ExecuteRequest("market/buylimit", true, parametres);
            return responseSellOrder;
        }

        public override async Task<ResponseWrapper> CancelOrder(string orderId) {
            Dictionary<string, string> parametres = new Dictionary<string, string> {
                {"uuid", orderId}
            };
            ResponseWrapper responseCancelOrder = await ExecuteRequest("market/cancel", true, parametres);
            return responseCancelOrder;
        }

        public override async void Trade(decimal amountBtc, decimal amountEth) {
            ResponseWrapper responseAllPairs =  await GetAllPairs();
            foreach (var pair in (JArray)responseAllPairs.Result) {
                allPairs.Add((string)pair["MarketName"]);
            }
            
            var crossBittrex = TimeService.TimeCrossesByMarket.Keys.FirstOrDefault(cross => cross.Market == "Bittrex");
            if (crossBittrex == null) {
                return;
            }
            string[] devidedPurchasePath = crossBittrex.PurchasePath.ToUpper().Split('-').ToArray();
            string[] devidedSellPath = crossBittrex.SellPath.ToUpper().Split('-').ToArray();

            decimal myAmount = devidedPurchasePath[0] == "BTC" ? amountBtc : amountEth;
            
            for (int i = 0; i <= devidedPurchasePath.Length - 2; i++) {
                bool purchase = true;
                string pair = $"{devidedPurchasePath[i]}-{devidedPurchasePath[i + 1]}";
                if (!allPairs.Contains(pair)) {
                    pair = $"{devidedPurchasePath[i + 1]}-{devidedPurchasePath[i]}";
                    purchase = false;
                }

                if (purchase) {
                    JArray sellOrders = (JArray)GetOrderBook(pair).Result.Result["sell"];
                    decimal bestBuyRate = (decimal)sellOrders[0]["Rate"] * (1 + feeBuy);
                    decimal bestBuyQuantity = (decimal)sellOrders[0]["Quantity"];
                    decimal buyQuantity = bestBuyQuantity > myAmount / bestBuyRate
                        ? myAmount / bestBuyRate
                        : bestBuyQuantity;
                    
                    ResponseWrapper responseBuyOrder = await CreateBuyOrder(pair, buyQuantity, bestBuyRate);
                    if (!responseBuyOrder.Success) {
                        break;
                    }

                    (string checkOrder, decimal quantityBought, string orderUuid) = CheckOrder(pair).Result;
                    if (checkOrder == "Ok") {
                        myAmount = buyQuantity;
                    } else if (checkOrder == "Bought") {
                        myAmount = quantityBought;
                        CancelOrder(orderUuid);
                    } else {
                        myAmount = 0;
                        CancelOrder(orderUuid);
                        break;
                    }


                } else {
                    JArray buyOrders = (JArray)GetOrderBook(pair).Result.Result["buy"];
                    
                }
            }
        }

        private async Task<(string, decimal, string)> CheckOrder(string pair) {
            var responseOpenOrders = await GetOpenOrders(pair);
            if (((JArray)responseOpenOrders.Result).Count == 0) {
                return ("Ok", 0, "");
            }

            var order = ((JArray) responseOpenOrders.Result)[0];
            
            decimal quantity = (decimal)order["Quantity"];
            decimal quantityRemaining = (decimal)order["QuantityRemaining"];
            if (quantity != quantityRemaining) {
                return ("Bought", quantity - quantityRemaining, (string)order["OrderUuid"]);
            } else {
                return ("Fail", 0, (string)order["OrderUuid"]);
            }
        }
    }
}