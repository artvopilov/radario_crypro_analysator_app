using System;
using Newtonsoft.Json.Linq;
using CryptoAnalysatorWebApp.Models.Common;
using CryptoAnalysatorWebApp.Interfaces;

namespace CryptoAnalysatorWebApp.Models
{
    public class ExmoMarket : BasicCryptoMarket {
        public ExmoMarket(string url = "https://api.exmo.me/v1/", string command = "ticker",
            decimal feeTaker = (decimal)0.002, decimal feeMaker = (decimal)0.002, string orderBookCommand = "order_book") :
            base(url, command, feeTaker, feeMaker, orderBookCommand) {
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
                    _pairs.Add(exPair);
                }
            }

            CreateCrossRates();
            Console.WriteLine("[INFO] ExmoMarket is ready");
        }

        public decimal LoadOrder(string currencyPair, bool isSeller) {
            currencyPair = currencyPair.Substring(currencyPair.IndexOf('-') + 1) + '_' + currencyPair.Substring(0, currencyPair.IndexOf('-'));
            string query = _basicUrl + _orderBookCommand + $"/?pair={currencyPair}";
            string response = GetResponse(query);

            JObject responseJson = JObject.Parse(response);
            if (isSeller) {
                return (decimal)responseJson[currencyPair]["ask_top"] * (1 + _feeTaker);
            } else {
                return (decimal)responseJson[currencyPair]["bid_top"] * (1 - _feeMaker);
            }
        }
    }
}
