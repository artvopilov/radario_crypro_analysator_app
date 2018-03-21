using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CryptoAnalysatorWebApp.Interfaces;
using Newtonsoft.Json.Linq;

namespace CryptoAnalysatorWebApp.Models.Common
{
    public abstract class BasicCryptoMarket: ICryptoMarket {
        protected readonly string marketName;
        protected readonly string basicUrl;
        public readonly string GetSummariesCommand;
        protected readonly string orderBookCommand;

        protected readonly Dictionary<string, ExchangePair> _pairs;
        protected readonly Dictionary<string, ExchangePair> _crossRates;
        protected readonly Dictionary<string, List<ExchangePair>> _crossRatesGroups;
        private List<string> _currencies;
        private string _resp;

        public string MarketName { get => marketName; }
        public Dictionary<string, ExchangePair> Pairs { get => _pairs; }
        public Dictionary<string, ExchangePair> Crosses { get => _crossRates; }
        public Dictionary<string, List<ExchangePair>> CrossesGroups { get => _crossRatesGroups; }
        public List<string> Currencies { get => _currencies; }
        public string Resp { get => _resp; }

        protected readonly decimal _feeTaker;
        protected readonly decimal _feeMaker;

        protected BasicCryptoMarket(string url, string command, decimal feeTaker, decimal feeMaker, string orderBookCommand, string marketName) {
            this.marketName = marketName;
            _pairs = new Dictionary<string, ExchangePair>();
            _crossRates = new Dictionary<string, ExchangePair>();
            _crossRatesGroups = new Dictionary<string, List<ExchangePair>>();
            basicUrl = url;
            GetSummariesCommand = command;
            this.orderBookCommand = orderBookCommand;
            _feeTaker = feeTaker;
            _feeMaker = feeMaker;
            LoadPairs(command).Wait();
            CollectCurrencies();
        }

        public async Task LoadPairs(string command = null) {
            if (command == null) {
                command = GetSummariesCommand;
            }
            
            _pairs.Clear();
            try {
                string response = await GetResponse(basicUrl + command);
                _resp = response;
                ProcessResponsePairs(response);
            } catch (Exception e) {
                Console.WriteLine("Error in ProcessResponsePairs" + this.MarketName);
                Console.WriteLine(e.Message);
            }
        }

        public abstract Task<decimal> LoadOrder(string currencyPair, bool isSeller, bool reversePice);

        protected abstract void ProcessResponsePairs(string response);

        protected void CreateCrossRates() {
            foreach (ExchangePair pair1 in _pairs.Values.ToArray()) {
                foreach (ExchangePair pair2 in _pairs.Values.ToArray()) {

                    ExchangePair crossRatePair = new ExchangePair();
                    char[] signsSplit = { '-' };
                    string[] splitedPair1 = pair1.Pair.Split(signsSplit);
                    string[] splitedPair2 = pair2.Pair.Split(signsSplit);

                    bool crossFoundBefore = _crossRates.TryGetValue(splitedPair2[1] + '-' + splitedPair2[0] + '-' + splitedPair1[1], out ExchangePair value)  ? true : false;
                    crossFoundBefore = crossFoundBefore == true ? true : _crossRates.TryGetValue(splitedPair1[1] + '-' + splitedPair2[0] + '-' + splitedPair2[1], out ExchangePair value_1) ? true : false;
                    if (crossFoundBefore) {
                        continue;
                    }
                    
                    if (splitedPair1[0] == splitedPair2[0] && splitedPair1[1] != splitedPair2[1]) {
                        crossRatePair.Pair = splitedPair1[1] + '-' + splitedPair2[0] + '-' + splitedPair2[1];
                        crossRatePair.SellPrice = pair2.SellPrice / (pair1.PurchasePrice == 0 ? 1 : pair1.PurchasePrice);
                        crossRatePair.PurchasePrice = 1 / ((pair1.SellPrice == 0 ? 1 : pair1.SellPrice) / (pair2.PurchasePrice == 0 ? 1 : pair2.PurchasePrice));
                        crossRatePair.StockExchangeSeller = pair1.StockExchangeSeller;

                        if (!_crossRatesGroups.ContainsKey(splitedPair1[1] + '-' + splitedPair2[1])) {
                            _crossRatesGroups.Add(splitedPair1[1] + '-' + splitedPair2[1], new List<ExchangePair>());
                        }
                        _crossRatesGroups[splitedPair1[1] + '-' + splitedPair2[1]].Add(crossRatePair);
                        _crossRates.Add(crossRatePair.Pair.ToString(), crossRatePair);
                    }

                    crossFoundBefore = _crossRates.TryGetValue(splitedPair1[0] + '-' + splitedPair2[0] + '-' + splitedPair2[1], out ExchangePair value2) ? true : false;
                    crossFoundBefore = crossFoundBefore == true ? true : _crossRates.TryGetValue(splitedPair2[1] + '-' + splitedPair2[0] + '-' + splitedPair1[0], out ExchangePair value_2) ? true : false;
                    if (crossFoundBefore) {
                        continue;
                    }

                    if (splitedPair1[1] == splitedPair2[0]) {
                        crossRatePair.Pair = splitedPair1[0] + '-' + splitedPair2[0] + '-' + splitedPair2[1];
                        crossRatePair.PurchasePrice = pair1.PurchasePrice * pair2.PurchasePrice;
                        crossRatePair.SellPrice = pair2.SellPrice * pair1.SellPrice;
                        crossRatePair.StockExchangeSeller = pair1.StockExchangeSeller;

                        if (!_crossRatesGroups.ContainsKey(splitedPair1[0] + '-' + splitedPair2[1])) {
                            _crossRatesGroups.Add(splitedPair1[0] + '-' + splitedPair2[1], new List<ExchangePair>());
                        }
                        _crossRatesGroups[splitedPair1[0] + '-' + splitedPair2[1]].Add(crossRatePair);
                        _crossRates.Add(crossRatePair.Pair.ToString(), crossRatePair);
                    }

                }
            }
        }

        protected void CollectCurrencies() {
            _currencies = new List<string>();

            foreach (string pair in _pairs.Keys) {
                string[] splitedPair = pair.Split('-');
                if (!_currencies.Contains(splitedPair[0])) {
                    _currencies.Add(splitedPair[0]);
                }
                if (!_currencies.Contains(splitedPair[1])) {
                    _currencies.Add(splitedPair[1]);
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

        public ExchangePair[] GetSimilarCrosses(string Name) {
            return _crossRatesGroups[Name.Split('-')[0] + '-' + Name.Split('-')[2]].ToArray();
        }

        public void DeletePairByName(string name) {
            _pairs.Remove(name);
        }

        public void DeleteCrossByName(string name) {
            _crossRates.Remove(name);
        }

        public static async Task<string> GetResponse(string url, bool check = false) {
            using (HttpClient client = new HttpClient()) {
                    using (HttpResponseMessage response = await client.GetAsync(check ? $"{url}&check=true" : url)) {
                        using (HttpContent content = response.Content) {
                            string responseStr = content.ReadAsStringAsync().Result;
                            return responseStr;
                        }
                    }
                }
        }
        
        public async Task GetRealPrices(string pairName) {
            if (_pairs.ContainsKey(pairName) && _pairs[pairName] != null) {
                string resp = await GetResponse(basicUrl + $"getorderbook?market={pairName}&type=both");
                var responseJson = JObject.Parse(resp)["result"];
                _pairs[pairName].PurchasePrice = (decimal)responseJson["sell"][0]["Rate"] * (1 + _feeTaker);
                _pairs[pairName].SellPrice = (decimal)responseJson["buy"][0]["Rate"] * (1 - _feeMaker);
            }
        }
    }
}
