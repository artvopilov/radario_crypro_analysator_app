using System;
using System.Collections.Generic;
using System.Linq;
using CryptoAnalysatorWebApp.TradeBots.Interfaces;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CryptoAnalysatorWebApp.TradeBots.Common.Objects;
using System.Net;
using System.Net.Http;
using System.Reflection.Emit;
using System.Text;
using Newtonsoft.Json;

namespace CryptoAnalysatorWebApp.TradeBots.Common {
    public abstract class CommonTradeBot : ITradeBot {
        protected const string SignHeaderName = "apisign";
        
        protected readonly string apiKey;
        private readonly string apiSecret;
        protected readonly string baseUrl;
        protected readonly HttpClient httpClient;
        protected readonly List<string> allPairs;

        public bool Ready { get; set; }
        public decimal BalanceBtc { get; set; }
        public decimal BalanceEth { get; set; }
        public decimal TradeAmountBtc { get; set; }
        public decimal TradeAmountEth { get; set; }

        protected CommonTradeBot(string apiKey, string apiSecret, string baseUrl) {
            this.apiSecret = apiSecret;
            this.apiKey = apiKey;
            this.baseUrl = baseUrl;
            httpClient = new HttpClient();
            Ready = false;
            BalanceBtc = 0;
            BalanceEth = 0;
            TradeAmountBtc = 0;
            TradeAmountEth = 0;
        }

        protected abstract HttpRequestMessage CreateRequest(string method, bool includeAuth,
            Dictionary<string, string> parameters);

        protected abstract Task<ResponseWrapper> ExecuteRequest(string method, bool includeAuth,
            Dictionary<string, string> parameters = null);
        
        public abstract Task<ResponseWrapper> GetBalances();
        public abstract Task<ResponseWrapper> CreateBuyOrder(string pair, decimal quantity, decimal rate);
        public abstract Task<ResponseWrapper> CreateSellORder(string pair, decimal quantity, decimal rate);
        public abstract Task<ResponseWrapper> CancelOrder(string orderId);
        public abstract Task<ResponseWrapper> GetAllPairs();
        public abstract Task<ResponseWrapper> GetOrderBook(string pair);
        public abstract Task<ResponseWrapper> GetOpenOrders(string pair = null);
        public abstract void Trade(decimal amountBtc, decimal amountEth);

        protected string MakeApiSignature(string completeUrl) {
            var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(apiSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(completeUrl));
            var hashText = ByteToHexString(hash);
            return hashText;
        }

        private string ByteToHexString(byte[] buff) {
            string sbinary = "";
            foreach (byte t in buff)
                sbinary += t.ToString("X2"); /* hex format */
            return sbinary;
        }

        public void MakeReadyToTrade(decimal amountBtc, decimal amountEth) {
            Ready = true;
            BalanceBtc = amountBtc;
            BalanceEth = amountEth;
        }
    }
}