using System;
using Newtonsoft.Json.Linq;
using CryptoAnalysatorWebApp.Models.Common;

namespace CryptoAnalysatorWebApp.Models
{
    public class PoloniexMarket : BasicCryptoMarket {
        public PoloniexMarket(string url = "https://poloniex.com/public?command=", string command = "returnTicker",
            decimal feeTaker = (decimal)0.0025, decimal feeMaker = (decimal)0.0015) : base(url, command, feeTaker, feeMaker) {
        }

        protected override void ProcessResponse(string response) {
            var responseJSON = JObject.Parse(response);

            foreach (var pair in responseJSON) {
                ExchangePair exPair = new ExchangePair();
                exPair.Pair = (string)pair.Key.Replace('_', '-');
                exPair.PurchasePrice = (decimal)pair.Value["lowestAsk"] * (1 + _feeTaker);
                exPair.SellPrice = (decimal)pair.Value["highestBid"] * (1 - _feeMaker);
                exPair.StockExchangeSeller = "Poloniex";

                _pairs.Add(exPair);
                CheckAddUsdtPair(exPair);
            }

            CreateCrossRates();
            Console.WriteLine("[INFO] PoloniexMarket is ready");
        }
    }
}
