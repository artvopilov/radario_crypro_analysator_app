using System;
using System.Collections.Generic;
using CryptoAnalysatorWebApp.Models.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using MongoDB.Driver.Core;
using MongoDB.Driver;
using MongoDB.Bson;
using CryptoAnalysatorWebApp.Models.Db;
using Microsoft.EntityFrameworkCore.Query.Internal;
using CryptoAnalysatorWebApp.Models.AnalyzingAlgorithms;

namespace CryptoAnalysatorWebApp.Models
{
    public class PairsAnalysator {
        private List<ExchangePair> _actualPairs;
        private List<ExchangePair> _crossRates;
        private List<ExchangePair> _crossRatesByMarket;

        public PairsAnalysator() {
            _actualPairs = new List<ExchangePair>();
            _crossRates = new List<ExchangePair>();
            _crossRatesByMarket = new List<ExchangePair>();
        }

        public List<ExchangePair> ActualPairs { get => _actualPairs; set => _actualPairs = value; }
        public List<ExchangePair> CrossPairs { get => _crossRates; set => _crossRates = value; }
        public List<ExchangePair> CrossRatesByMarket { get => _crossRatesByMarket; set => _crossRatesByMarket = value; }

        public async Task FindActualPairsAndCrossRates(BasicCryptoMarket[] marketsArray, string caller) {
            _actualPairs.Clear();
            _crossRates.Clear();
            _crossRatesByMarket.Clear();

            for (int i = 0; i < marketsArray.Length - 1; i++) {
                foreach (ExchangePair thatMarketPair in marketsArray[i].Pairs.Values) {
                    ExchangePair pair = AnalysePairs(thatMarketPair, marketsArray, i);
                    if (pair != null && _actualPairs.Find(p => p.Pair == pair.Pair && p.StockExchangeBuyer == pair.StockExchangeBuyer && p.StockExchangeSeller == pair.StockExchangeSeller) == null && pair.Spread > 4) {
                        pair.IsCross = false;
                        _actualPairs.Add(pair);
                    }
                }

                foreach (ExchangePair thatMarketPair in marketsArray[i].Crosses.Values) {
                    
                    ExchangePair crossRate = AnalysePairs(thatMarketPair, marketsArray, i, true);
                    if (crossRate != null && _crossRates.Find(c => c.Pair == crossRate.Pair && c.StockExchangeBuyer == crossRate.StockExchangeBuyer && c.StockExchangeSeller == crossRate.StockExchangeSeller) == null && crossRate.Spread > 4) {
                        crossRate.IsCross = true;
                        _crossRates.Add(crossRate);
                    }
                }
            }
            
            for (int i = 0; i < marketsArray.Length; i++) {
                BasicCryptoMarket market = marketsArray[i];
                Console.WriteLine($"Market: {market.MarketName}  Time: {DateTime.Now}  TimeMlscnds: {DateTime.Now.Millisecond}");
                if (market.MarketName == "Bittrex") {
                    market = new BittrexMarket();
                } else {
                    await market.LoadPairs();   
                }

                FloydWarshell.FindCrossesOnMarket(market.MarketName, market.Currencies, market.Pairs,
                    _crossRatesByMarket, new Dictionary<string, int> { ["BTC"] = 0, ["ETH"] = 0 });
            }
        }

        private ExchangePair AnalysePairs(ExchangePair thatMarketPair, BasicCryptoMarket[] marketsArray, int i, bool isCross = false, string keyWord = "") {
            ExchangePair maxSellPricePair = thatMarketPair;
            ExchangePair minPurchasePricePair = thatMarketPair;
            ExchangePair actualPair = new ExchangePair();
            string name = thatMarketPair.Pair;
            
            /*if (isCross && marketsArray[i].GetSimilarCrosses(name).Length > 0 && (name.Split('-')[0] == "BTC" || name.Split('-')[0] == "ETH")) {
                foreach (ExchangePair crossRate in marketsArray[i].GetSimilarCrosses(name)) {
                    if (thatMarketPair.PurchasePrice < crossRate.SellPrice) {
                        ExchangePair crossRatePair = new ExchangePair();
                        crossRatePair.PurchasePath = thatMarketPair.Pair;
                        crossRatePair.SellPath = crossRate.Pair;
                        crossRatePair.Market = thatMarketPair.StockExchangeSeller;
                        try {
                            crossRatePair.PurchasePrice = thatMarketPair.PurchasePrice;
                            crossRatePair.SellPrice = crossRate.SellPrice;
                            if (crossRatePair.PurchasePrice > 0 && crossRatePair.SellPrice > 0) {
                                crossRatePair.IsCross = true;
                                crossRatePair.Spread = Math.Round((crossRatePair.SellPrice - crossRatePair.PurchasePrice) / crossRatePair.PurchasePrice * 100, 4);
                                if (_crossRatesByMarket.Find(c => c.PurchasePath == crossRatePair.PurchasePath && c.SellPath == crossRatePair.SellPath && c.Market == crossRatePair.Market) == null) {
                                    _crossRatesByMarket.Add(crossRatePair);
                                }
                            }
                        } catch (Exception e) {
                            //Console.WriteLine($"Exception! {e.Message}");
                        }
                    }
                    if (crossRate.PurchasePrice < thatMarketPair.SellPrice) {
                        ExchangePair crossRatePair = new ExchangePair();
                        crossRatePair.PurchasePath = crossRate.Pair;
                        crossRatePair.SellPath = thatMarketPair.Pair;
                        crossRatePair.Market = thatMarketPair.StockExchangeSeller;
                        try {
                            crossRatePair.PurchasePrice = crossRate.PurchasePrice;
                            crossRatePair.SellPrice = thatMarketPair.SellPrice;
                            if (crossRatePair.PurchasePrice > 0 && crossRatePair.SellPrice > 0) {
                                crossRatePair.IsCross = true;
                                crossRatePair.Spread = Math.Round((crossRatePair.SellPrice - crossRatePair.PurchasePrice) / crossRatePair.PurchasePrice * 100, 4);
                                if (_crossRatesByMarket.Find(c => c.PurchasePath == crossRatePair.PurchasePath && c.SellPath == crossRatePair.SellPath && c.Market == crossRatePair.Market) == null) {
                                    _crossRatesByMarket.Add(crossRatePair);
                                }
                            }
                        } catch (Exception e) {
                            //Console.WriteLine($"Exception! {e.Message}");
                        }
                    }
                }

            }*/
            for (int j = i; j < marketsArray.Length; j++) {
                ExchangePair anotherMarketPair;

                if (isCross) {
                    anotherMarketPair = marketsArray[j].GetCrossByName(name);
                    if (anotherMarketPair == null) {
                        continue;
                    }
                } else {
                    anotherMarketPair = marketsArray[j].GetPairByName(name);
                    if (anotherMarketPair == null) {
                        continue;
                    }
                }

                
                

                if (maxSellPricePair.SellPrice < anotherMarketPair.SellPrice) {
                    maxSellPricePair = anotherMarketPair;
                }
                if (minPurchasePricePair.PurchasePrice > anotherMarketPair.PurchasePrice) {
                    minPurchasePricePair = anotherMarketPair;
                }

            }

            if (minPurchasePricePair.PurchasePrice < maxSellPricePair.SellPrice) {
                decimal diff = (maxSellPricePair.SellPrice - minPurchasePricePair.PurchasePrice) / minPurchasePricePair.PurchasePrice;

                actualPair.Pair = diff > (decimal)0.1 ? "" + keyWord + minPurchasePricePair.Pair : keyWord + minPurchasePricePair.Pair;
                actualPair.PurchasePrice = Math.Round(minPurchasePricePair.PurchasePrice, 11);
                actualPair.SellPrice = Math.Round(maxSellPricePair.SellPrice, 11);
                actualPair.StockExchangeBuyer = maxSellPricePair.StockExchangeSeller;
                actualPair.StockExchangeSeller = minPurchasePricePair.StockExchangeSeller;
                actualPair.Spread = Math.Round((actualPair.SellPrice - actualPair.PurchasePrice) / actualPair.PurchasePrice * 100, 4);

                return actualPair;
            } else {
                return null;
            }
        }
    }
}
