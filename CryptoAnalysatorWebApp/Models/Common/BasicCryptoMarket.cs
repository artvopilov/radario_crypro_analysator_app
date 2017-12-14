using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CryptoAnalysatorWebApp.Interfaces;

namespace CryptoAnalysatorWebApp.Models.Common
{
    public abstract class BasicCryptoMarket: ICryptoMarket {
        protected readonly string _basicUrl;
        protected List<ExchangePair> _pairs;
        protected List<ExchangePair> _usdtPairs;
        protected List<ExchangePair> _crossRates;

        public List<ExchangePair> Pairs { get => _pairs; }
        public List<ExchangePair> СrossRates { get => _crossRates; }

        protected readonly decimal _feeTaker;
        protected readonly decimal _feeMaker;

        public BasicCryptoMarket(string url, string command, decimal feeTaker, decimal feeMaker) {
            _pairs = new List<ExchangePair>();
            _usdtPairs = new List<ExchangePair>();
            _crossRates = new List<ExchangePair>();
            _basicUrl = url;
            _feeTaker = feeTaker;
            _feeMaker = feeMaker;
            LoadPairs(command);
        }

        public void LoadPairs(string command) {
            _pairs.Clear();
            string response = GetResponse(_basicUrl + command);
            ProcessResponse(response);
        }

        protected abstract void ProcessResponse(string response);

        protected void CheckAddUsdtPair(ExchangePair pair) {
            char[] signsSplit = { '-' };
            string[] splitedPair = pair.Pair.Split(signsSplit);

            if (splitedPair[0].ToUpper() == "USDT" || splitedPair[1].ToUpper() == "USDT") {
                _usdtPairs.Add(pair);
            }
        }

        protected void CreateCrossRates() {
            for (int i = 0; i < _usdtPairs.Count - 1; i++) {
                for (int j = i + 1; j < _usdtPairs.Count; j++) {
                    ExchangePair pair1 = _usdtPairs[i];
                    ExchangePair pair2 = _usdtPairs[j];

                    ExchangePair crossRatePair = new ExchangePair();
                    char[] signsSplit = { '-' };
                    string[] splitedPair1 = pair1.Pair.Split(signsSplit);
                    string[] splitedPair2 = pair2.Pair.Split(signsSplit);

                    if (splitedPair1[0] == "USDT" && splitedPair2[0] == "USDT") {
                        crossRatePair.Pair = splitedPair1[1] + '-' + splitedPair2[1];
                        crossRatePair.SellPrice = pair2.SellPrice / pair1.PurchasePrice;
                        crossRatePair.PurchasePrice = 1 / (pair1.SellPrice / pair2.PurchasePrice);
                        crossRatePair.StockExchangeSeller = pair1.StockExchangeSeller;

                        _crossRates.Add(crossRatePair);
                    } else {
                        if (splitedPair1[1] == "USDT" && splitedPair2[1] == "USDT") {
                            crossRatePair.Pair = splitedPair1[0] + '-' + splitedPair2[0];
                            crossRatePair.SellPrice = 1 / (pair1.SellPrice / pair1.PurchasePrice);
                            crossRatePair.PurchasePrice = 1 / (pair1.PurchasePrice / pair2.SellPrice);
                            crossRatePair.StockExchangeSeller = pair1.StockExchangeSeller;

                            _crossRates.Add(crossRatePair);
                        }
                    }
                }
            }
        }

        public ExchangePair GetPairByName(string name) {
            return _pairs.Find(pairEx => pairEx.Pair == name);
        }

        public ExchangePair GetCrossByName(string name) {
            return _crossRates.Find(pairEx => pairEx.Pair == name);
        }

        public void DeletePairByName(string name) {
            _pairs.Remove(_pairs.Find(pairEx => pairEx.Pair == name));
        }

        public void DeleteCrossByName(string name) {
            _crossRates.Remove(_crossRates.Where(pairEx => pairEx.Pair == name).First());
        }

        protected string GetResponse(string url) {
            using (HttpClient client = new HttpClient()) {
                using (HttpResponseMessage response = client.GetAsync(url).Result) {
                    using (HttpContent content = response.Content) {
                        string responseStr = content.ReadAsStringAsync().Result;
                        return responseStr;
                    }
                }
            }
        }
    }
}
