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
                    ExchangePair crossRate = AnalysePairs(thatMarketPair, marketsArray, i);
                    if (crossRate != null) {
                        _actualPairs.Add(crossRate);
                    }
                }

                foreach (ExchangePair thatMarketPair in marketsArray[i].СrossRates) {
                    ExchangePair crossRate = AnalysePairs(thatMarketPair, marketsArray, i, "[CROSS] ");
                    if (crossRate != null) {
                        _crossRates.Add(crossRate);
                    }
                }
            }
        }

        private ExchangePair AnalysePairs(ExchangePair thatMarketPair, BasicCryptoMarket[] marketsArray, int i, string keyWord = "") {
            ExchangePair maxSellPricePair = thatMarketPair;
            ExchangePair minPurchasePricePair = thatMarketPair;
            ExchangePair actualPair = new ExchangePair();

            for (int j = i; j < marketsArray.Length; j++) {
                string name = thatMarketPair.Pair;
                ExchangePair anotherMarketPair = marketsArray[j].GetPairByName(name);
                if (anotherMarketPair == null) {
                    anotherMarketPair = marketsArray[j].GetPairByName(name.Substring(name.IndexOf('-') + 1) + '-'
                        + name.Substring(0, name.IndexOf('-'))) ?? thatMarketPair;
                    if (anotherMarketPair != thatMarketPair) {
                        anotherMarketPair.SellPrice = 1 / anotherMarketPair.SellPrice;
                        anotherMarketPair.PurchasePrice = 1 / anotherMarketPair.PurchasePrice;
                    }
                }

                if (maxSellPricePair.SellPrice < anotherMarketPair.SellPrice) {
                    maxSellPricePair = anotherMarketPair;
                }
                if (minPurchasePricePair.PurchasePrice > anotherMarketPair.PurchasePrice) {
                    minPurchasePricePair = anotherMarketPair;
                }

                if (anotherMarketPair != thatMarketPair) {
                    marketsArray[j].DeletePairByName(anotherMarketPair.Pair);
                }

            }

            if (minPurchasePricePair.PurchasePrice < maxSellPricePair.SellPrice) {
                decimal diff = (maxSellPricePair.SellPrice - minPurchasePricePair.PurchasePrice) / maxSellPricePair.SellPrice;

                actualPair.Pair = diff > (decimal)0.1 ? "[WARN] " + keyWord + minPurchasePricePair.Pair : keyWord + minPurchasePricePair.Pair;
                actualPair.PurchasePrice = minPurchasePricePair.PurchasePrice;
                actualPair.SellPrice = maxSellPricePair.SellPrice;
                actualPair.StockExchangeBuyer = maxSellPricePair.StockExchangeSeller;
                actualPair.StockExchangeSeller = minPurchasePricePair.StockExchangeSeller;

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
