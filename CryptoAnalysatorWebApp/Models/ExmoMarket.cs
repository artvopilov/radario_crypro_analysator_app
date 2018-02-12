using System;
using Newtonsoft.Json.Linq;
using CryptoAnalysatorWebApp.Models.Common;

namespace CryptoAnalysatorWebApp.Models
{
    public class ExmoMarket : BasicCryptoMarket {
        public ExmoMarket(string url = "https://api.exmo.me/v1/", string command = "ticker",
            decimal feeTaker = (decimal)0.002, decimal feeMaker = (decimal)0.002, string orderBookCommand = "order_book", string marketName = "Exmo") :
            base(url, command, feeTaker, feeMaker, orderBookCommand, marketName) {
        }

        protected override void ProcessResponsePairs(string response) {
            var responseJSON = JObject.Parse(response);

            foreach (var pair in responseJSON) {
                ExchangePair exPair = new ExchangePair();
                char[] signsSplit = { '_' };
                string[] splitedPair = pair.Key.Split(signsSplit);
                exPair.Pair = splitedPair[1] + '-' + splitedPair[0];

                exPair.PurchasePrice = ((decimal)pair.Value["sell_price"]) * (1 + _feeTaker);
                exPair.SellPrice = ((decimal)pair.Value["buy_price"]) * (1 - _feeMaker);
                exPair.StockExchangeSeller = "Exmo";

                bool pairIsOk = CheckPairPrices(exPair);
                if (pairIsOk) {
                    _pairs.Add(exPair.Pair, exPair);
                }
            }

            CreateCrossRates();
            Console.WriteLine("[INFO] ExmoMarket is ready");
        }

        public override decimal LoadOrder(string currencyPair, bool isSeller, bool reversePice = false) {
            if (!_pairs.ContainsKey(currencyPair)) {
                currencyPair = $"{currencyPair.Split('-')[1]}-{currencyPair.Split('-')[0]}";
                isSeller = isSeller == true ? false : true;
                reversePice = reversePice == true ? false : true;
            }

            currencyPair = currencyPair.Substring(currencyPair.IndexOf('-') + 1) + '_' + currencyPair.Substring(0, currencyPair.IndexOf('-'));
            string query = basicUrl + orderBookCommand + $"/?pair={currencyPair}";
            string response = GetResponse(query);

            JObject responseJson = JObject.Parse(response);
            if (isSeller) {
                if (reversePice) {
                    return 1 / (decimal)responseJson[currencyPair]["ask_top"] * (1 + _feeTaker);
                }
                return (decimal)responseJson[currencyPair]["ask_top"] * (1 + _feeTaker);
            } else {
                if (reversePice) {
                    return 1 / (decimal)responseJson[currencyPair]["bid_top"] * (1 - _feeMaker);
                }
                return (decimal)responseJson[currencyPair]["bid_top"] * (1 - _feeMaker);
            }
        }
    }
}
