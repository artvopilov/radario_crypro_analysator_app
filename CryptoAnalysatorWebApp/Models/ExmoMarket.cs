using System;
using Newtonsoft.Json.Linq;
using CryptoAnalysatorWebApp.Models.Common;
using CryptoAnalysatorWebApp.Interfaces;

namespace CryptoAnalysatorWebApp.Models
{
    public class ExmoMarket : BasicCryptoMarket {
        public ExmoMarket(string url = "https://api.exmo.com/v1/", string command = "ticker",
            decimal feeTaker = (decimal)0.002, decimal feeMaker = (decimal)0.002) : base(url, command, feeTaker, feeMaker) {
        }

        protected override void ProcessResponse(string response) {
            var responseJSON = JObject.Parse(response);

            foreach (var pair in responseJSON) {
                ExchangePair exPair = new ExchangePair();
                char[] signsSplit = { '_' };
                string[] splitedPair = pair.Key.Split(signsSplit);
                exPair.Pair = splitedPair[1] + '-' + splitedPair[0];

                exPair.PurchasePrice = ((decimal)pair.Value["sell_price"]) * (1 + _feeTaker);
                exPair.SellPrice = ((decimal)pair.Value["buy_price"]) * (1 - _feeMaker);
                exPair.StockExchangeSeller = "Exmo";

                _pairs.Add(exPair);
                CheckAddUsdtPair(exPair);
            }

            CreateCrossRates();
            Console.WriteLine("[INFO] ExmoMarket is ready");
        }
    }
}
