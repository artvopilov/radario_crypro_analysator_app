using System;
using Newtonsoft.Json.Linq;
using CryptoAnalysatorWebApp.Models.Common;

namespace CryptoAnalysatorWebApp.Models
{
    public class PoloniexMarket : BasicCryptoMarket {
        public PoloniexMarket(string url = "https://poloniex.com/public?command=", string command = "returnTicker",
            decimal feeTaker = (decimal)0.0025, decimal feeMaker = (decimal)0.0015, string orderBookCommand = "returnOrderBook", string marketName = "Poloniex") : 
            base(url, command, feeTaker, feeMaker, orderBookCommand, marketName) {
        }

        protected override void ProcessResponsePairs(string response) {
            var responseJSON = JObject.Parse(response);

            foreach (var pair in responseJSON) {
                ExchangePair exPair = new ExchangePair();
                exPair.Pair = (string)pair.Key.Replace('_', '-');
                exPair.PurchasePrice = (decimal)pair.Value["lowestAsk"] * (1 + _feeTaker);
                exPair.SellPrice = (decimal)pair.Value["highestBid"] * (1 - _feeMaker);
                exPair.StockExchangeSeller = "Poloniex";

                bool pairIsOk = CheckPairPrices(exPair);
                if (pairIsOk) {
                    _pairs.Add(exPair.Pair, exPair);
                }   
            }

            CreateCrossRates();
            Console.WriteLine("[INFO] PoloniexMarket is ready");
        }

        public override decimal LoadOrder(string currencyPair, bool isSeller, bool reversePice = false) {
            if (!_pairs.ContainsKey(currencyPair)) {
                currencyPair = $"{currencyPair.Split('-')[1]}-{currencyPair.Split('-')[0]}";
                isSeller = isSeller == true ? false : true;
                reversePice = reversePice == true ? false : true;
            }

            currencyPair = currencyPair.Replace('-', '_');
            int depth = 10;
            string query = _basicUrl + _orderBookCommand + $"&currencyPair={currencyPair}&depth={depth}";

            string response = GetResponse(query);

            JObject responseJson = JObject.Parse(response);
            if (isSeller) {
                if (reversePice) {
                    return 1 /(decimal)responseJson["asks"][0][0] * (1 + _feeTaker);
                }
                return (decimal)responseJson["asks"][0][0] * (1 + _feeTaker);
            } else {
                if (reversePice) {
                    return 1 / (decimal)responseJson["bids"][0][0] * (1 - _feeMaker);
                }
                return (decimal)responseJson["bids"][0][0] * (1 - _feeMaker);
            }
        }
    }
}
