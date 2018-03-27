using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoAnalysatorWebApp.Models.Common;

namespace CryptoAnalysatorWebApp.Models.AnalyzingAlgorithms {
    public static class FloydWarshell {
        public static void FindCrossesOnMarket(ref List<ExchangePair> crossRatesByMarket,
             Dictionary<string, int> curStartChain, BasicCryptoMarket market, bool needToSaveInTimeService = true) {
             
            double[,] currenciesMatrixPurchaseMin = new double[market.Currencies.Count, market.Currencies.Count];
            double[,] currenciesMatrixSellMax = new double[market.Currencies.Count, market.Currencies.Count];

            int[, ,] visitedPurchaseMin = new int[market.Currencies.Count, market.Currencies.Count, market.Currencies.Count];
            int[, ,] visitedSellMax = new int[market.Currencies.Count, market.Currencies.Count, market.Currencies.Count];

            int[,] nextPurchase = new int[market.Currencies.Count, market.Currencies.Count];
            int[,] nextSell = new int[market.Currencies.Count, market.Currencies.Count];
            int btcIndex = -1;

            for (int i = 0; i < market.Currencies.Count; i++) {
                foreach (string cur in curStartChain.Keys.ToArray()) { 
                    if (market.Currencies[i] == cur) {
                        curStartChain[cur] = i;
                    }
                }

                if (market.Currencies[i] == "BTC") {
                    btcIndex = i;
                }

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

                currenciesMatrixPurchaseMin[index1, index2] = (double)pair.Value.PurchasePrice;
                currenciesMatrixPurchaseMin[index2, index1] = (double)(Math.Round(1 / pair.Value.SellPrice, 20));

                currenciesMatrixSellMax[index1, index2] = (double)pair.Value.SellPrice;
                currenciesMatrixSellMax[index2, index1] = (double)(Math.Round(1 / pair.Value.PurchasePrice, 20));
            }
            
            //Алгоритм Флойда-Уоршелла нахождения кратчайших путей между всеми парами вершин
            
            for (int k = 0; k < market.Currencies.Count; k++) {
                for (int i = 0; i < market.Currencies.Count; i++) {
                    foreach (int curIndex in curStartChain.Values) {
                        if (i != curIndex && visitedPurchaseMin[curIndex, i, nextPurchase[k, i]] != 1 && 
                            currenciesMatrixPurchaseMin[curIndex, i] - currenciesMatrixPurchaseMin[curIndex, k] * 
                            currenciesMatrixPurchaseMin[k, i] > 0.00000001) {
                            currenciesMatrixPurchaseMin[curIndex, i] = currenciesMatrixPurchaseMin[curIndex, k] * currenciesMatrixPurchaseMin[k, i];
                            visitedPurchaseMin[curIndex, i, nextPurchase[k, i]] = 1;
                            nextPurchase[curIndex, i] = nextPurchase[k, i];
                        }
                    }
                }
            }

            for (int k = 0; k < market.Currencies.Count; k++) {
                for (int i = 0; i < market.Currencies.Count; i++) {
                    foreach (int curIndex in curStartChain.Values) {
                        if (i != curIndex && visitedSellMax[curIndex, i, nextSell[k, i]] != 1 && currenciesMatrixSellMax[curIndex, i] - currenciesMatrixSellMax[curIndex, k] * currenciesMatrixSellMax[k, i] < -0.00000001) {
                            currenciesMatrixSellMax[curIndex, i] = currenciesMatrixSellMax[curIndex, k] * currenciesMatrixSellMax[k, i];
                            visitedSellMax[curIndex, i, nextSell[k, i]] = 1;
                            nextSell[curIndex, i] = nextSell[k, i];
                        }
                    }
                }
            }

            for (int i = 0; i < market.Currencies.Count; i++) {
                for (int j = 0; j < market.Currencies.Count; j++) {
                    if (i != j && currenciesMatrixPurchaseMin[i, j] < currenciesMatrixSellMax[needToSaveInTimeService ? i : btcIndex, j] && curStartChain.Values.Contains(i)) {
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
                                if (crossRatePair.Market == "Bittrex" && crossRatePair.PurchasePath.Split('-')[0] == "BTC" && needToSaveInTimeService) {
                                    ShowRates(crossRatePair.PurchasePath.Split('-'), true, market);
                                    ShowRates(crossRatePair.SellPath.Split('-'), false, market);

                                    TimeService.AddCrossRateByMarketBittrex(crossRatePair);
                                }
                                crossRatesByMarket.Add(crossRatePair);
                                
                                
                            }
                        } catch (Exception e) {

                            //Console.WriteLine($"Exception! {e.Message}");
                        }

                    }
                }
            }
        }
        
        private static string GetPath(int start, int curFinish, List<int> visited, int[,] nextArray, List<string> currencies, string result) {
            visited.Add(curFinish);
            if (visited.Contains(nextArray[start, curFinish])) {
                throw new Exception("Incorrect path");
            }
    
            if (nextArray[start, curFinish] == start) {
                return $"{currencies[start]}-{result}";
            }
            
            
            return GetPath(start, nextArray[start, curFinish], visited, nextArray, currencies, $"{currencies[nextArray[start, curFinish]]}-{result}");
        }
        
        private static void ShowRates(string[] devidedPath, bool purchase, BasicCryptoMarket market) {
            if (purchase) {
                for (int i = 0; i < devidedPath.Length - 1; i++) {
                    purchase = true;
                    string pair = devidedPath[i] + '-' + devidedPath[i + 1];
                    if (!market.Pairs.ContainsKey(pair)) {
                        pair = devidedPath[i + 1] + '-' + devidedPath[i];
                        purchase = false;
                    }

                    Console.WriteLine(purchase
                        ? $"{pair}  {market.Pairs[pair].PurchasePrice}  Buy"
                        : $"{pair}  {market.Pairs[pair].SellPrice}  Sell");
                }
            } else {
                Console.WriteLine("SellPathHere");
                for (int i = devidedPath.Length - 1; i > 0; i--) {
                    purchase = false;
                    string pair = devidedPath[i - 1] + '-' + devidedPath[i];
                    if (!market.Pairs.ContainsKey(pair)) {
                        pair = devidedPath[i] + '-' + devidedPath[i - 1];
                        purchase = true; 
                    }

                    Console.WriteLine(purchase
                        ? $"{pair}  {market.Pairs[pair].PurchasePrice}  Buy"
                        : $"{pair}  {market.Pairs[pair].SellPrice}  Sell");
                }
                Console.WriteLine("SellPathComleted");
            }
        }
    }
}