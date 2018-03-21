using System;
using CryptoAnalysatorWebApp.Models.Common;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CryptoAnalysatorWebApp.Models
{
    public class BinanceMarket : BasicCryptoMarket {
        public BinanceMarket(string url = "https://www.binance.com/api/v3/", string command = "ticker/bookTicker",
            decimal feeTaker = (decimal)0.001, decimal feeMaker = (decimal)0.001, string orderBookCommand = "ticker/bookTicker", string marketName = "Binance") :
            base(url, command, feeTaker, feeMaker, orderBookCommand, marketName) {
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

        public override async Task<decimal> LoadOrder(string currencyPair, bool isSeller, bool reversePice = false) {
            if (!_pairs.ContainsKey(currencyPair)) {
                currencyPair = $"{currencyPair.Split('-')[1]}-{currencyPair.Split('-')[0]}";
                isSeller = isSeller != true;
                reversePice = reversePice == true ? false : true;
            }

            string[] currencyPairSplited = currencyPair.Split('-');
            string query = basicUrl + orderBookCommand + $"?symbol={currencyPairSplited[1]}{currencyPairSplited[0]}";
            string response = await GetResponse(query);

            JObject responseJson = JObject.Parse(response);
            if (isSeller) {
                if (reversePice) {
                    return 1 / (decimal)responseJson["askPrice"] * (1 + _feeTaker);
                }
                return (decimal)responseJson["askPrice"] * (1 + _feeTaker);
            } else {
                if (reversePice) {
                    return 1 / (decimal)responseJson["bidPrice"] * (1 - _feeMaker);
                }
                return (decimal)responseJson["bidPrice"] * (1 - _feeMaker);
            }

        }
    }
}
