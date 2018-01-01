using System;
using System.Collections.Generic;
using CryptoAnalysatorWebApp.Models.Common;

namespace CryptoAnalysatorWebApp.Models
{
    public class PairsAnalysator {
        List<ExchangePair> _actualPairs;
        List<ExchangePair> _crossRates;

        public PairsAnalysator() {
            _actualPairs = new List<ExchangePair>();
            _crossRates = new List<ExchangePair>();
        }

        public List<ExchangePair> ActualPairs { get => _actualPairs; set => _actualPairs = value; }
        public List<ExchangePair> CrossPairs { get => _crossRates; set => _crossRates = value; }

        public void FindActualPairsAndCrossRates(BasicCryptoMarket[] marketsArray) {
            _actualPairs.Clear();
            _crossRates.Clear();
            for (int i = 0; i < marketsArray.Length - 1; i++) {
                foreach (ExchangePair thatMarketPair in marketsArray[i].Pairs) {
                    ExchangePair pair = AnalysePairs(thatMarketPair, marketsArray, i);
                    if (pair != null) {
                        _actualPairs.Add(pair);
                    }
                }

                foreach (ExchangePair thatMarketPair in marketsArray[i].Crosses) {
                    ExchangePair crossRate = AnalysePairs(thatMarketPair, marketsArray, i, true);
                    if (crossRate != null) {
                        _crossRates.Add(crossRate);
                    }
                }
            }
        }

        private ExchangePair AnalysePairs(ExchangePair thatMarketPair, BasicCryptoMarket[] marketsArray, int i, bool isCross = false, string keyWord = "") {
            ExchangePair maxSellPricePair = thatMarketPair;
            ExchangePair minPurchasePricePair = thatMarketPair;
            ExchangePair actualPair = new ExchangePair();

            for (int j = i; j < marketsArray.Length; j++) {
                string name = thatMarketPair.Pair;
                ExchangePair anotherMarketPair;

                if (isCross) {
                    anotherMarketPair = marketsArray[j].GetCrossByName(name);
                    if (anotherMarketPair == null) {
                        anotherMarketPair = marketsArray[j].GetCrossByName(name.Substring(name.IndexOf('-') + 1) + '-'
                            + name.Substring(0, name.IndexOf('-'))) ?? thatMarketPair;
                        if (anotherMarketPair != thatMarketPair) {
                            marketsArray[j].DeleteCrossByName(anotherMarketPair.Pair);

                            anotherMarketPair.Pair = name;
                            decimal temp = anotherMarketPair.SellPrice;
                            anotherMarketPair.SellPrice = 1 / anotherMarketPair.PurchasePrice;
                            anotherMarketPair.PurchasePrice = 1 / temp;
                        }
                    }
                } else {
                    anotherMarketPair = marketsArray[j].GetPairByName(name);
                    if (anotherMarketPair == null) {
    
                        anotherMarketPair = marketsArray[j].GetPairByName(name.Substring(name.IndexOf('-') + 1) + '-'
                            + name.Substring(0, name.IndexOf('-'))) ?? thatMarketPair;
                        if (anotherMarketPair != thatMarketPair) {
                            marketsArray[j].DeletePairByName(anotherMarketPair.Pair);

                            anotherMarketPair.Pair = name;
                            decimal temp = anotherMarketPair.SellPrice;
                            anotherMarketPair.SellPrice = 1 / anotherMarketPair.PurchasePrice;
                            anotherMarketPair.PurchasePrice = 1 / temp;
                        }
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
                decimal diff = (maxSellPricePair.SellPrice - minPurchasePricePair.PurchasePrice) / maxSellPricePair.SellPrice;

                actualPair.Pair = diff > (decimal)0.1 ? "" + keyWord + minPurchasePricePair.Pair : keyWord + minPurchasePricePair.Pair;
                actualPair.PurchasePrice = minPurchasePricePair.PurchasePrice;
                actualPair.SellPrice = maxSellPricePair.SellPrice;
                actualPair.StockExchangeBuyer = maxSellPricePair.StockExchangeSeller;
                actualPair.StockExchangeSeller = minPurchasePricePair.StockExchangeSeller;
                //Костыль с делением
                actualPair.Spread = Math.Round((actualPair.SellPrice - actualPair.PurchasePrice) / (actualPair.PurchasePrice > 0 ? actualPair.PurchasePrice : 1)  * 100, 4);

                return actualPair;
            } else {
                return null;
            }
        }

        public void ShowActualPairsAndCrossRates() {
            foreach (ExchangePair pair in _actualPairs) {
                Console.WriteLine($"{pair.Pair}: {pair.StockExchangeSeller} >> {pair.StockExchangeBuyer} || " +
                    $"{pair.PurchasePrice} >> {pair.SellPrice}");
            }
            Console.WriteLine("\n      ___|__|__|__|__|__|___ \n         |  |  |  |  |  |\n");
            foreach (ExchangePair pair in _crossRates) {
                Console.WriteLine($"{pair.Pair}: {pair.StockExchangeSeller} >> {pair.StockExchangeBuyer} || " +
                    $"{pair.PurchasePrice} >> {pair.SellPrice}");
            }
        }
    }
}
