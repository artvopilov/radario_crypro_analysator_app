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
using System.Threading;
using CryptoAnalysatorWebApp.Models;
using Newtonsoft.Json;
using Telegram.Bot;

namespace CryptoAnalysatorWebApp.TradeBots.Common {
    public abstract class CommonTradeBot<TResult> : ITradeBot<TResult> {
        protected const string SignHeaderName = "apisign";
        
        protected readonly string apiKey;
        private readonly string apiSecret;
        protected readonly string baseUrl;
        protected readonly HttpClient httpClient;
        protected readonly Dictionary<string, ExchangePair> allPairs;
        protected Dictionary<string, decimal> walletBalances;
        protected List<Thread> tradeThreads;

        public decimal TradeAmountBtc { get; set; }
        public decimal TradeAmountEth { get; set; }
        public IReadOnlyDictionary<string, decimal> WalletBalances => walletBalances;

        protected CommonTradeBot(string apiKey, string apiSecret, string baseUrl) {
            this.apiSecret = apiSecret;
            this.apiKey = apiKey;
            this.baseUrl = baseUrl;
            httpClient = new HttpClient();
            TradeAmountBtc = 0;
            TradeAmountEth = 0;
            allPairs = new Dictionary<string, ExchangePair>();
            walletBalances = new Dictionary<string, decimal>();
            tradeThreads = new List<Thread>();
        }

        protected CommonTradeBot(string baseUrl) {
            this.baseUrl = baseUrl;
            httpClient = new HttpClient();
            TradeAmountBtc = 0;
            TradeAmountEth = 0;
            allPairs = new Dictionary<string, ExchangePair>();
            tradeThreads = new List<Thread>();
            walletBalances = new Dictionary<string, decimal>();
        }

        protected abstract HttpRequestMessage CreateRequest(string method, bool includeAuth,
            Dictionary<string, string> parameters);

        protected abstract Task<TResult> ExecuteRequest(string method, bool includeAuth,
            Dictionary<string, string> parameters = null);
        
        public abstract Task<TResult> GetBalances();
        public abstract Task<TResult> GetBalance(string currency);
        public abstract Task<TResult> CreateBuyOrder(string pair, decimal quantity, decimal rate);
        public abstract Task<TResult> CreateSellORder(string pair, decimal quantity, decimal rate);
        public abstract Task<TResult> CancelOrder(string orderId);
        public abstract Task<TResult> GetAllPairs();
        public abstract Task<TResult> GetOrderBook(string pair);
        public abstract Task<TResult> GetOpenOrders(string pair = null);    
        public abstract void Trade(decimal amountBtc, decimal amountEth, TelegramBotClient client, long chatId, ManualResetEvent signal);
        public (decimal, decimal) StartTrading(decimal amountBtc, decimal amountEth, TelegramBotClient client, long chatId, ManualResetEvent signal) {
            if (amountBtc > walletBalances["BTC"] || amountEth > walletBalances["ETH"]) {
                return (0, 0);
            }

            amountBtc = amountBtc > (decimal)0.0007 ? (decimal)0.0007 : amountBtc;
            amountEth = amountEth > (decimal) 0.009 ? (decimal) 0.009 : amountEth;
            
            Thread thread = new Thread(() => Trade(amountBtc, amountEth, client, chatId, signal));
            thread.Start();
            return (amountBtc, amountEth);
        }    

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
    }
}