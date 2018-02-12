using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoAnalysatorWebApp.Models.Common;
using Newtonsoft.Json.Linq;

namespace CryptoAnalysatorWebApp.Models
{
    public class LivecoinMarket : BasicCryptoMarket {
        public LivecoinMarket(string url = "https://api.livecoin.net/", string command = "exchange/maxbid_minask",
            decimal feeTaker = (decimal)0.0018, decimal feeMaker = (decimal)0.0018, string orderBookCommand = "exchange/maxbid_minask", string marketName = "Livecoin") : 
            base(url, command, feeTaker, feeMaker, orderBookCommand, marketName) {
        }

        protected override void ProcessResponsePairs(string response) {
            JToken responseJSON = JObject.Parse(response)["currencyPairs"];
            foreach (JObject pair in responseJSON) {
                ExchangePair exPair = new ExchangePair();
                string[] splitedPair = ((string)pair["symbol"]).Split('/');
                if (splitedPair[0] == "BCC" || splitedPair[1] == "BCC") {
                    continue;
                }
                exPair.Pair = splitedPair[1] + '-' + splitedPair[0];
                try {
                    exPair.PurchasePrice = (decimal)pair["minAsk"] * (1 + _feeTaker);
                    exPair.SellPrice = (decimal)pair["maxBid"] * (1 - _feeMaker);
                } catch (Exception e) {
                    continue;
                }
                exPair.StockExchangeSeller = this.MarketName;

                bool pairIsOk = CheckPairPrices(exPair);
                if (pairIsOk) {
                    _pairs.Add(exPair.Pair, exPair);
                }
            }

            CreateCrossRates();
            Console.WriteLine("[INFO] LivecoinMarket is ready");
        }

        public override decimal LoadOrder(string currencyPair, bool isSeller, bool reversePice = false) {
            if (!_pairs.ContainsKey(currencyPair)) {
                currencyPair = $"{currencyPair.Split('-')[1]}-{currencyPair.Split('-')[0]}";
                isSeller = isSeller == true ? false : true;
                reversePice = reversePice == true ? false : true;
            }

            currencyPair = currencyPair.Substring(currencyPair.IndexOf('-') + 1) + '/' + currencyPair.Substring(0, currencyPair.IndexOf('-'));
            string query = basicUrl + orderBookCommand + $"?currencyPair={currencyPair}";
            string response = GetResponse(query);

            JObject responseJson = (JObject)JObject.Parse(response)["currencyPairs"][0];
            if (isSeller) {
                if (reversePice) {
                    return 1 / (decimal)responseJson["minAsk"] * (1 + _feeTaker);
                }
                return (decimal)responseJson["minAsk"] * (1 + _feeTaker);
            } else {
                if (reversePice) {
                    return 1 / (decimal)responseJson["maxBid"] * (1 - _feeMaker);
                }
                return (decimal)responseJson["maxBid"] * (1 - _feeMaker);
            }
        }
    }
}
