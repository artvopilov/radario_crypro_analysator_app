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
using CryptoAnalysatorWebApp.TradeBots.Common.Objects;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;


namespace CryptoAnalysatorWebApp.TradeBots {
    public class BittrexTradeBot : CommonTradeBot {
        public BittrexTradeBot(string apiKey, string apiSecret, string baseUrl = "https://bittrex.com/api/v1.1/") :
            base(apiKey, apiSecret, baseUrl) { }

        protected override HttpRequestMessage CreateRequest(string method, Dictionary<string, string> parameters) {     
            parameters.Add("apikey", apiKey);
            parameters.Add("nonce", DateTime.Now.Ticks.ToString());

            string parametersString = string.Join('&',
                parameters.Select(param => WebUtility.UrlEncode(param.Key) + '=' + WebUtility.UrlEncode(param.Value)));

            string completeUrl = baseUrl + method + '?' + parametersString;

            var hashText = MakeApiSignature(completeUrl);
            
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, completeUrl);
            request.Headers.Add(SignHeaderName, hashText);

            return request;
        }
        
        protected override async Task<ResponseWrapper> ExecuteRequest(string method,
            Dictionary<string, string> parameters = null) {
            if (parameters == null) {
                parameters = new Dictionary<string, string>();
            }

            HttpRequestMessage request = CreateRequest(method, parameters);

            HttpResponseMessage response = await httpClient.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            ResponseWrapper responseContentJson = JsonConvert.DeserializeObject<ResponseWrapper>(responseContent);
            return responseContentJson;
        }

        public override async Task<ResponseWrapper> GetBalances() {
            ResponseWrapper responseBalances = await ExecuteRequest("account/getbalances");
            return responseBalances;
        }

        public override async Task<ResponseWrapper> CreateBuyOrder(string pair, decimal quantity, decimal rate) {
            Dictionary<string, string> parametres = new Dictionary<string, string> {
                {"market", pair},
                {"quantity", quantity.ToString(CultureInfo.InvariantCulture)},
                { "rate", rate.ToString(CultureInfo.InvariantCulture) }
            };
            ResponseWrapper responseBuyOrder = await ExecuteRequest("market/buylimit", parametres);
            return responseBuyOrder;
        }

        public override async Task<ResponseWrapper> CreateSellORder(string pair, decimal quantity, decimal rate) {
            Dictionary<string, string> parametres = new Dictionary<string, string> {
                {"market", pair},
                {"quantity", quantity.ToString(CultureInfo.InvariantCulture)},
                { "rate", rate.ToString(CultureInfo.InvariantCulture) }
            };
            ResponseWrapper responseSellOrder = await ExecuteRequest("market/buylimit", parametres);
            return responseSellOrder;
        }

        public override async Task<ResponseWrapper> CancelOrder(string orderId) {
            Dictionary<string, string> parametres = new Dictionary<string, string> {
                {"uuid", orderId}
            };
            ResponseWrapper responseCancelOrder = await ExecuteRequest("market/cancel", parametres);
            return responseCancelOrder;
        }

        public override void Trade() {
            
        }
    }
}