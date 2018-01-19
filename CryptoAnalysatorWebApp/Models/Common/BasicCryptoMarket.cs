using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CryptoAnalysatorWebApp.Interfaces;
using Newtonsoft.Json.Linq;

namespace CryptoAnalysatorWebApp.Models.Common
{
    public abstract class BasicCryptoMarket: ICryptoMarket {
        protected readonly string _basicUrl;
        protected readonly string _orderBookCommand;

        protected Dictionary<string, ExchangePair> _pairs;
        protected Dictionary<string, ExchangePair> _crossRates;

        public Dictionary<string, ExchangePair> Pairs { get => _pairs; }
        public Dictionary<string, ExchangePair> Crosses { get => _crossRates; }

        protected readonly decimal _feeTaker;
        protected readonly decimal _feeMaker;

        public BasicCryptoMarket(string url, string command, decimal feeTaker, decimal feeMaker, string orderBookCommand) {
            _pairs = new Dictionary<string, ExchangePair>();
            _crossRates = new Dictionary<string, ExchangePair>();
            _basicUrl = url;
            _orderBookCommand = orderBookCommand;
            _feeTaker = feeTaker;
            _feeMaker = feeMaker;
            LoadPairs(command);
        }

        public void LoadPairs(string command) {
            _pairs.Clear();
            try {
                string response = GetResponse(_basicUrl + command);
                ProcessResponsePairs(response);
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        protected abstract void ProcessResponsePairs(string response);

        protected void CreateCrossRates() {
            foreach (ExchangePair pair1 in _pairs.Values.ToArray()) {
                foreach (ExchangePair pair2 in _pairs.Values.ToArray()) {

                    ExchangePair crossRatePair = new ExchangePair();
                    char[] signsSplit = { '-' };
                    string[] splitedPair1 = pair1.Pair.Split(signsSplit);
                    string[] splitedPair2 = pair2.Pair.Split(signsSplit);

                    bool crossFoundBefore = _crossRates.TryGetValue(splitedPair2[1] + '-' + splitedPair2[0] + '-' + splitedPair1[1], out ExchangePair value) ? true : false;
                    if (crossFoundBefore) {
                        continue;
                    }

                    if (splitedPair1[0] == splitedPair2[0] && splitedPair1[1] != splitedPair2[1]) {
                        crossRatePair.Pair = splitedPair1[1] + '-' + splitedPair2[0] + '-' + splitedPair2[1];
                        crossRatePair.SellPrice = pair2.SellPrice / (pair1.PurchasePrice == 0 ? 1 : pair1.PurchasePrice);
                        crossRatePair.PurchasePrice = 1 / ((pair1.SellPrice == 0 ? 1 : pair1.SellPrice) / (pair2.PurchasePrice == 0 ? 1 : pair2.PurchasePrice));
                        crossRatePair.StockExchangeSeller = pair1.StockExchangeSeller;

                        _crossRates.Add(crossRatePair.Pair.ToString(), crossRatePair);
                    }
                }
            }
        }

        protected bool CheckPairPrices(ExchangePair pair) {
            return (pair.SellPrice == 0 && pair.PurchasePrice == 0) ? false : true;
        }

        public ExchangePair GetPairByName(string name) {
            return _pairs.TryGetValue(name, out ExchangePair value) ? value : null;
        }

        public ExchangePair GetCrossByName(string name) {
            return _crossRates.TryGetValue(name, out ExchangePair value) ? value : null;
        }

        public void DeletePairByName(string name) {
            _pairs.Remove(name);
        }

        public void DeleteCrossByName(string name) {
            _crossRates.Remove(name);
        }

        protected string GetResponse(string url) {
            Console.WriteLine($"Trying to get url {url}");
            using (HttpClient client = new HttpClient()) {
                    using (HttpResponseMessage response = client.GetAsync(url).Result) {
                        Console.WriteLine($"Got {response.StatusCode}.");
                        using (HttpContent content = response.Content) {
                            string responseStr = content.ReadAsStringAsync().Result;
                            Console.WriteLine("Success.");
                            return responseStr;
                        }
                    }
                }
        }
    }
}
