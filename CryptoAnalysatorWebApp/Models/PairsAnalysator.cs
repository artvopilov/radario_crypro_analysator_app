using System;
using System.Collections.Generic;
using CryptoAnalysatorWebApp.Models.Common;
using System.Diagnostics;

namespace CryptoAnalysatorWebApp.Models
{
    public class PairsAnalysator {
        List<ExchangePair> _actualPairs;
        List<ExchangePair> _crossRates;
        List<ExchangePair> _crossRatesByMarket;

        public PairsAnalysator() {
            _actualPairs = new List<ExchangePair>();
            _crossRates = new List<ExchangePair>();
            _crossRatesByMarket = new List<ExchangePair>();
        }

        public List<ExchangePair> ActualPairs { get => _actualPairs; set => _actualPairs = value; }
        public List<ExchangePair> CrossPairs { get => _crossRates; set => _crossRates = value; }
        public List<ExchangePair> CrossRatesByMarket { get => _crossRatesByMarket; set => _crossRatesByMarket = value; }

        public void FindActualPairsAndCrossRates(BasicCryptoMarket[] marketsArray, string caller) {
            _actualPairs.Clear();
            _crossRates.Clear();
            _crossRatesByMarket.Clear();

            for (int i = 0; i < marketsArray.Length; i++) {
                FindCrossesOnMarket(marketsArray[i]);
            }

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
        }

        private ExchangePair AnalysePairs(ExchangePair thatMarketPair, BasicCryptoMarket[] marketsArray, int i, bool isCross = false, string keyWord = "") {
            ExchangePair maxSellPricePair = thatMarketPair;
            ExchangePair minPurchasePricePair = thatMarketPair;
            ExchangePair actualPair = new ExchangePair();
            string name = thatMarketPair.Pair;

            if (isCross && marketsArray[i].GetSimilarCrosses(name).Length > 0) {
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
            }
            for (int j = i; j < marketsArray.Length; j++) {
                ExchangePair anotherMarketPair;

                if (isCross) {
                    anotherMarketPair = marketsArray[j].GetCrossByName(name);
                    if (anotherMarketPair == null) {
                        continue;
                        //anotherMarketPair = marketsArray[j].GetCrossByName(name.Substring(name.IndexOf('-') + 1) + '-'
                        //    + name.Substring(0, name.IndexOf('-'))) ?? thatMarketPair;
                        //if (anotherMarketPair != thatMarketPair) {

                        //    anotherMarketPair.Pair = name;
                        //    decimal temp = anotherMarketPair.SellPrice;
                        //    anotherMarketPair.SellPrice = 1 / anotherMarketPair.PurchasePrice;
                        //    anotherMarketPair.PurchasePrice = 1 / temp;
                        //}
                    }
                } else {
                    anotherMarketPair = marketsArray[j].GetPairByName(name);
                    if (anotherMarketPair == null) {
                        continue;
                        //anotherMarketPair = marketsArray[j].GetPairByName(name.Substring(name.IndexOf('-') + 1) + '-'
                        //    + name.Substring(0, name.IndexOf('-'))) ?? thatMarketPair;
                        //if (anotherMarketPair != thatMarketPair) {

                        //    anotherMarketPair.Pair = name;
                        //    decimal temp = anotherMarketPair.SellPrice;
                        //    anotherMarketPair.SellPrice = 1 / anotherMarketPair.PurchasePrice;
                        //    anotherMarketPair.PurchasePrice = 1 / temp;
                        //}
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

        private void FindCrossesOnMarket(BasicCryptoMarket market) {
            Console.WriteLine($"Market: {market.MarketName}");

            double[,] currenciesMatrixPurchaseMin = new double[market.Currencies.Count, market.Currencies.Count];
            double[,] currenciesMatrixSellMax = new double[market.Currencies.Count, market.Currencies.Count];

            int[, ,] visitedPurchaseMin = new int[market.Currencies.Count, market.Currencies.Count, market.Currencies.Count];
            int[, ,] visitedSellMax = new int[market.Currencies.Count, market.Currencies.Count, market.Currencies.Count];

            int[,] nextPurchase = new int[market.Currencies.Count, market.Currencies.Count];
            int[,] nextSell = new int[market.Currencies.Count, market.Currencies.Count];

            for (int i = 0; i < market.Currencies.Count; i++) {
                for (int j = 0; j < market.Currencies.Count; j++) {
                    nextPurchase[i, j] = i;
                    nextSell[i, j] = i;

                    visitedPurchaseMin[i, j, i] = 1;
                    visitedPurchaseMin[i, j, j] = 1;
                    visitedSellMax[i, j, i] = 1;
                    visitedSellMax[i, j, j] = 1;

                    if (i == j) {
                        currenciesMatrixPurchaseMin[i, j] = 1;
                        currenciesMatrixSellMax[i, j] = 1;
                        continue;
                    }
                    currenciesMatrixPurchaseMin[i, j] = int.MaxValue;
                    currenciesMatrixSellMax[i, j] = 0;
                }
            }

            foreach (KeyValuePair<string, ExchangePair> pair in market.Pairs) {
                string[] currencies = pair.Key.Split('-');
                int index1 = market.Currencies.IndexOf(currencies[0]);
                int index2 = market.Currencies.IndexOf(currencies[1]);

                Console.WriteLine($"first:  {pair.Value.Pair}  {pair.Value.PurchasePrice} {pair.Value.SellPrice}");
                currenciesMatrixPurchaseMin[index1, index2] = (double)pair.Value.PurchasePrice;
                currenciesMatrixPurchaseMin[index2, index1] = (double)(1 / pair.Value.SellPrice);
                Console.WriteLine($"{currencies[0]} {currencies[1]}  {currenciesMatrixPurchaseMin[index1, index2]}  {currenciesMatrixPurchaseMin[index2, index1]}");

                currenciesMatrixSellMax[index1, index2] = (double)pair.Value.SellPrice;
                currenciesMatrixSellMax[index2, index1] = (double)(1 / pair.Value.PurchasePrice);
            }

            //Алгоритм Флойда-Уоршелла нахождения кратчайших путей между всеми парами вершин
            for (int k = 0; k < market.Currencies.Count; k++) {
                for (int i = 0; i < market.Currencies.Count; i++) {
                    for (int j = 0; j < market.Currencies.Count; j++) {
                        if (i != j && visitedPurchaseMin[i, j, k] != 1 && currenciesMatrixPurchaseMin[i, j] - currenciesMatrixPurchaseMin[i, k] * currenciesMatrixPurchaseMin[k, j] > 0.000000000000000001) {
                            currenciesMatrixPurchaseMin[i, j] = currenciesMatrixPurchaseMin[i, k] * currenciesMatrixPurchaseMin[k, j];
                            if (currenciesMatrixPurchaseMin[i, j] == 1) {
                            }
                            visitedPurchaseMin[i, j, k] = 1;
                            nextPurchase[i, j] = nextPurchase[k, j];
                        }
                    }
                }
            }

            for (int k = 0; k < market.Currencies.Count; k++) {
                for (int i = 0; i < market.Currencies.Count; i++) {
                    for (int j = 0; j < market.Currencies.Count; j++) {
                        if (i != j && visitedSellMax[i, j, k] != 1 && currenciesMatrixSellMax[i, j] - currenciesMatrixSellMax[i, k] * currenciesMatrixSellMax[k, j] < -0.000000000000000001) {
                            currenciesMatrixSellMax[i, j] = currenciesMatrixSellMax[i, k] * currenciesMatrixSellMax[k, j];
                            visitedSellMax[i, j, k] = 1;
                            nextSell[i, j] = nextSell[k, j];
                        }
                    }
                }
            }

            for (int i = 0; i < market.Currencies.Count; i++) {
                for (int j = 0; j < market.Currencies.Count; j++) {
                    if (currenciesMatrixPurchaseMin[i, j] < currenciesMatrixSellMax[i, j]) {
                        ExchangePair crossRatePair = new ExchangePair();
                        try {
                            crossRatePair.PurchasePath = GetPath(i, j, new List<int>(), nextPurchase, market.Currencies, market.Currencies[j]);
                            crossRatePair.SellPath = GetPath(i, j, new List<int>(), nextSell, market.Currencies, market.Currencies[j]);
                        } catch (Exception e) {
                            //Console.WriteLine($"Exception: {e.Message}");
                            continue;
                        }
                        crossRatePair.Market = market.MarketName;
                        try {
                            crossRatePair.PurchasePrice = (decimal)currenciesMatrixPurchaseMin[i, j];
                            crossRatePair.SellPrice = (decimal)currenciesMatrixSellMax[i, j];
                            if (crossRatePair.PurchasePrice > 0 && crossRatePair.SellPrice > 0) {
                                crossRatePair.IsCross = true;
                                crossRatePair.Spread = Math.Round((crossRatePair.SellPrice - crossRatePair.PurchasePrice) / crossRatePair.PurchasePrice * 100, 4);
                                //Console.WriteLine($"{crossRatePair.PurchasePath}  {crossRatePair.SellPath}  {crossRatePair.Spread}");
                                _crossRatesByMarket.Add(crossRatePair);
                            }
                        } catch (Exception e) {

                            //Console.WriteLine($"Exception! {e.Message}");
                        }

                        //Console.WriteLine($"{market.Currencies[i]} - {market.Currencies[j]} : {crossRatePair.PurchasePrice }  {crossRatePair.PurchasePath}");
                        //Console.WriteLine($"{market.Currencies[i]} - {market.Currencies[j]} : {crossRatePair.SellPrice}   {crossRatePair.SellPath}");
                    }
                }
            }

        }

        private string GetPath(int start, int curFinish, List<int> visited, int[,] nextArray, List<string> currencies, string result) {
            visited.Add(curFinish);
            if (start == curFinish) {
                return $"{currencies[start]} - {currencies[curFinish]}";
            }

            if (nextArray[start, curFinish] == start) {
                return $"{currencies[start]}-{result}";
            }
            if (visited.Contains(nextArray[start, curFinish])) {
                throw new Exception("Incorrect path");
            }
            
            return GetPath(start, nextArray[start, curFinish], visited, nextArray, currencies, $"{currencies[nextArray[start, curFinish]]}-{result}");
        }
    }
}
