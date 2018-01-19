using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoAnalysatorWebApp.Models.Common;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace CryptoAnalysatorWebApp.Models
{
    public class BinanceMarket : BasicCryptoMarket {
        public BinanceMarket(string url = "https://www.binance.com/api/v3/", string command = "ticker/bookTicker",
            decimal feeTaker = (decimal)0.001, decimal feeMaker = (decimal)0.001, string orderBookCommand = "ticker/bookTicker") :
            base(url, command, feeTaker, feeMaker, orderBookCommand) {
        }

        protected override void ProcessResponsePairs(string response) {
            JArray responseJSON = JArray.Parse(response);

            foreach (JObject pair in responseJSON) {
                ExchangePair exPair = new ExchangePair();
                string possibleCryptos = @"(BTC|ETH|LTC|USD|USDT|DOGE|XRP|XEM)";
                Match match = Regex.Match((string)pair["symbol"], possibleCryptos);
                if (match.Success) {
                    string cryptoFound = match.Value;
                    int index = ((string)pair["symbol"]).IndexOf(cryptoFound);
                    if (index == 0) {
                        exPair.Pair = ((string)pair["symbol"]).Substring(cryptoFound.Length) + '-' + ((string)pair["symbol"]).Substring(0, cryptoFound.Length);
                    } else {
                        exPair.Pair = ((string)pair["symbol"]).Substring(index) + '-' + ((string)pair["symbol"]).Substring(0, index);
                    }
                } else {
                    continue;
                }

                exPair.PurchasePrice = (decimal)pair["askPrice"] * (1 + _feeTaker);
                exPair.SellPrice = (decimal)pair["bidPrice"] * (1 - _feeMaker);
                exPair.StockExchangeSeller = "Binance";

                bool pairIsOk = CheckPairPrices(exPair);
                if (pairIsOk) {
                    _pairs.Add(exPair.Pair, exPair);
                }
            }

            CreateCrossRates();
            Console.WriteLine("[INFO] BinanceMarket is ready");
        }

        public decimal LoadOrder(string currencyPair, bool isSeller) {
            string[] currencyPairSplited = currencyPair.Split('-');
            string query = _basicUrl + _orderBookCommand + $"?symbol={currencyPairSplited[1]}{currencyPairSplited[0]}";
            string response = GetResponse(query);

            JObject responseJson = JObject.Parse(response);
            if (isSeller) {
                return (decimal)responseJson["askPrice"] * (1 + _feeTaker);
            } else {
                return (decimal)responseJson["bidPrice"] * (1 - _feeMaker);
            }

        }
    }
}
