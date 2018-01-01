using System;
using Newtonsoft.Json.Linq;
using CryptoAnalysatorWebApp.Models.Common;

namespace CryptoAnalysatorWebApp.Models
{
    public class BittrexMarket : BasicCryptoMarket {
        public BittrexMarket(string url = "https://bittrex.com/api/v1.1/public/", string command = "getmarketsummaries",
            decimal feeTaker = (decimal)0.0025, decimal feeMaker = (decimal)0.0025, string orderBookCommand = "getorderbook") 
            : base(url, command, feeTaker, feeMaker, orderBookCommand) {
        }

        protected override void ProcessResponsePairs(string response) {
            var responseJSON = JObject.Parse(response)["result"];

            foreach (JObject pair in responseJSON) {
                ExchangePair exPair = new ExchangePair();
                exPair.Pair = (string)pair["MarketName"];
                exPair.PurchasePrice = (decimal)pair["Ask"] * (1 + _feeTaker);
                exPair.SellPrice = (decimal)pair["Bid"] * (1 - _feeMaker);
                exPair.StockExchangeSeller = "Bittrex";

                _pairs.Add(exPair);
                CheckAddUsdtPair(exPair);
            }

            CreateCrossRates();
            Console.WriteLine("[INFO] BittrexMarket is ready");
        }

        public decimal LoadOrder(string currencyPair, bool isSeller) {
            string query = _basicUrl + _orderBookCommand + $"?market={currencyPair}&type=both";
            string response = GetResponse(query);
                
            JToken responseJson = JObject.Parse(response)["result"];
            if (isSeller) {
                return (decimal)responseJson["sell"][0]["Rate"] * (1 + _feeTaker);
            } else {
                return (decimal)responseJson["buy"][0]["Rate"] * (1 - _feeMaker);
            }

        }
    }
}
