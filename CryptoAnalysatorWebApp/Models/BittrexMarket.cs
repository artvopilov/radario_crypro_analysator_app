using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using CryptoAnalysatorWebApp.Models.Common;

namespace CryptoAnalysatorWebApp.Models
{
    public class BittrexMarket : BasicCryptoMarket {
        public BittrexMarket(string url = "https://bittrex.com/api/v1.1/public/", string command = "getmarketsummaries",
            decimal feeTaker = (decimal)0.0025, decimal feeMaker = (decimal)0.0025, string orderBookCommand = "getorderbook", string marketName = "Bittrex") 
            : base(url, command, feeTaker, feeMaker, orderBookCommand, marketName) {
        }

        protected override void ProcessResponsePairs(string response) {
            var responseJson = JObject.Parse(response)["result"];

            foreach (JObject pair in responseJson) {
                ExchangePair exPair = new ExchangePair();
                exPair.Pair = (string)pair["MarketName"];
                exPair.PurchasePrice = (decimal)pair["Ask"] * (1 + _feeTaker);
                exPair.SellPrice = (decimal)pair["Bid"] * (1 - _feeMaker);
                exPair.StockExchangeSeller = "Bittrex";

                bool pairIsOk = CheckPairPrices(exPair);
                if (pairIsOk) {
                    _pairs.Add(exPair.Pair, exPair);
                }
            }

            CreateCrossRates();
            Console.WriteLine("[INFO] BittrexMarket is ready");
        }

        public override async Task<decimal> LoadOrder(string currencyPair, bool isSeller, bool reversePice = false) {
            if (!_pairs.ContainsKey(currencyPair)) {
                currencyPair = $"{currencyPair.Split('-')[1]}-{currencyPair.Split('-')[0]}";
                isSeller = isSeller != true;
                reversePice = reversePice != true;
            }

            string query = basicUrl + orderBookCommand + $"?market={currencyPair}&type=both";
            string response = await GetResponse(query);
            
            JToken responseJson = JObject.Parse(response)["result"];
            if (isSeller) {
                if (reversePice) {
                    return 1 / (decimal)responseJson["sell"][0]["Rate"] * (1 + _feeTaker);
                }
                return (decimal)responseJson["sell"][0]["Rate"] * (1 + _feeTaker);
            } else {
                if (reversePice) {
                    return 1 / (decimal)responseJson["buy"][0]["Rate"] * (1 - _feeMaker);
                }
                return (decimal)responseJson["buy"][0]["Rate"] * (1 - _feeMaker);
            }

        }
    }
}
