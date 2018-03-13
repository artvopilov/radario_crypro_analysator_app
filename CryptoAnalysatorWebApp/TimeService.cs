using System;
using System.Collections.Generic;
using System.IO;
using CryptoAnalysatorWebApp.Models;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using CryptoAnalysatorWebApp.TradeBots;
using CryptoAnalysatorWebApp.TradeBots.Common.Objects;

namespace CryptoAnalysatorWebApp {
    public static class TimeService {
        private static Dictionary<ExchangePair, DateTime> _timeUpdatedPairs = new Dictionary<ExchangePair, DateTime>();
        private static Dictionary<ExchangePair, DateTime> _timeUpdatedCrosses = new Dictionary<ExchangePair, DateTime>();
        private static Dictionary<ExchangePair, DateTime> _timeUpdatedCrossesByMarket = new Dictionary<ExchangePair, DateTime>();

        public static Dictionary<ExchangePair, DateTime> TimePairs { get => _timeUpdatedPairs; }
        public static Dictionary<ExchangePair, DateTime> TimeCrosses { get => _timeUpdatedCrosses; }
        public static Dictionary<ExchangePair, DateTime> TimeCrossesByMarket { get => _timeUpdatedCrossesByMarket; }

        public static DateTime GetPairTimeUpd (ExchangePair pair) {
            return _timeUpdatedPairs.TryGetValue(pair, out DateTime value) ? value : DateTime.Now;
        }

        public static DateTime GetCrossTimeUpd(ExchangePair cross) {
            return _timeUpdatedCrosses.TryGetValue(cross, out DateTime value) ? value : DateTime.Now;
        }

        public static DateTime GetCrossByMarketTimeUpd (ExchangePair crossByMarket) {
            return _timeUpdatedCrossesByMarket.TryGetValue(crossByMarket, out DateTime value) ? value : DateTime.Now;
        }

        public static ExchangePair GetPairOrCross(string pairArg, string seller, string buyer, bool isCross) {
            if (!isCross) {
                foreach (ExchangePair pair in _timeUpdatedPairs.Keys) {
                    if (pair.Pair == pairArg && pair.StockExchangeSeller.ToLower() == seller && pair.StockExchangeBuyer.ToLower() == buyer) {
                        return pair;
                    }
                }
            } else {
                foreach (ExchangePair cross in _timeUpdatedCrosses.Keys) {
                    if (cross.Pair == pairArg && cross.StockExchangeSeller.ToLower() == seller && cross.StockExchangeBuyer.ToLower() == buyer) {
                        return cross;
                    }
                }
            }
            return null;
        }

        public static ExchangePair GetCrossByMarket(string market, string purchasePath, string sellPath) {
            foreach (ExchangePair crossByMarket in _timeUpdatedCrossesByMarket.Keys) {
                if (crossByMarket.Market == market && crossByMarket.PurchasePath == purchasePath && 
                        crossByMarket.SellPath == sellPath) {
                    return crossByMarket;
                }
            }
            return null;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void StoreTime(DateTime curTime, List<ExchangePair> pairsToStore, List<ExchangePair> crossesToStore, List<ExchangePair> crossesByMarketToStore) {
            lock (_timeUpdatedPairs) lock (_timeUpdatedCrosses) lock (_timeUpdatedCrossesByMarket) {
                ManualResetEvent[] signalsBittrex = TradeBotsStorage<ResponseWrapper>.GetMarketSignals("bittrex");
                if (signalsBittrex != null) {
                    foreach (var signal in signalsBittrex) {
                        signal.Reset();
                    }
                }
                
                List<ExchangePair> pairsToRemove = new List<ExchangePair>();
                foreach (ExchangePair pairS in _timeUpdatedPairs.Keys) {
                    ExchangePair pairFound = pairsToStore.Find(p => p.Pair == pairS.Pair && p.StockExchangeSeller == pairS.StockExchangeSeller &&
                                                                    p.StockExchangeBuyer == pairS.StockExchangeBuyer);
                    if (pairFound == null) {
                        pairsToRemove.Add(pairS);
                    } else {
                        pairsToStore.Remove(pairFound);
                    }
                }
                foreach (ExchangePair pairRemained in pairsToStore) {
                    _timeUpdatedPairs[pairRemained] = curTime;
                }
                foreach (ExchangePair pairRm in pairsToRemove) {
                    _timeUpdatedPairs.Remove(pairRm);
                }

                List<ExchangePair> crossesToRemove = new List<ExchangePair>();
                foreach (ExchangePair crossS in _timeUpdatedCrosses.Keys) {
                    ExchangePair crossFound = crossesToStore.Find(c => c.Pair == crossS.Pair && c.StockExchangeSeller == crossS.StockExchangeSeller &&
                                                                       c.StockExchangeBuyer == crossS.StockExchangeBuyer);
                    if (crossFound == null) {
                        crossesToRemove.Add(crossS);
                    } else {
                        crossesToStore.Remove(crossFound);
                    }
                }
                foreach (ExchangePair crossRemained in crossesToStore) {
                    _timeUpdatedCrosses[crossRemained] = curTime;
                }
                foreach (ExchangePair crossRm in crossesToRemove) {
                    _timeUpdatedCrosses.Remove(crossRm);
                }

                bool bittrexSignalsOn = false;
                List<ExchangePair> crossesByMarketToRemove = new List<ExchangePair>();
                foreach (ExchangePair crossByMarketS in _timeUpdatedCrossesByMarket.Keys) {
                    ExchangePair crossByMarketFound = crossesByMarketToStore.Find(c => c.Market == crossByMarketS.Market &&
                                                                                       c.PurchasePath == crossByMarketS.PurchasePath && c.SellPath == crossByMarketS.SellPath);
                    if (crossByMarketFound == null) {
                        crossesByMarketToRemove.Add(crossByMarketS);
                    } else {
                        if (crossByMarketFound.Market == "Bittrex") {
                            bittrexSignalsOn = true;
                        }
                        crossesByMarketToStore.Remove(crossByMarketFound);
                    }
                }
                foreach (ExchangePair crossByMarketRemained in crossesByMarketToStore) {
                    if (crossByMarketRemained.Market == "Bittrex") {
                        bittrexSignalsOn = true;
                    }
                    _timeUpdatedCrossesByMarket[crossByMarketRemained] = curTime;
                }
                foreach (ExchangePair crossByMarketRm in crossesByMarketToRemove) {
                    _timeUpdatedCrossesByMarket.Remove(crossByMarketRm);
                }

                if (bittrexSignalsOn && signalsBittrex != null) {
                    foreach (var signal in signalsBittrex) {
                        signal.Set();
                    }
                }
            }
        }

        public static DateTime GetTimeUpdBy (ExchangePair pairArg, bool isCross) {
            if (!isCross) {
                ExchangePair curPair = _timeUpdatedPairs.Keys.First(p => p.Pair == pairArg.Pair);
                return _timeUpdatedPairs[curPair];
            } else {
                ExchangePair curCross = _timeUpdatedCrosses.Keys.First(c => c.Pair == pairArg.Pair);
                return _timeUpdatedCrosses[curCross];
            }
        }

        public static void AddCrossRateByMarketBittrex(ExchangePair crossRateByMarketToStore) {
            lock (_timeUpdatedCrossesByMarket) {
                bool toAdd = true;
                foreach (ExchangePair crossByMarketS in _timeUpdatedCrossesByMarket.Keys) {
                    if (crossRateByMarketToStore.Market == crossByMarketS.Market &&
                        crossRateByMarketToStore.PurchasePath == crossByMarketS.PurchasePath &&
                        crossRateByMarketToStore.SellPath == crossByMarketS.SellPath) {
                        toAdd = false;
                        break;
                    }
                }

                if (toAdd) {
                    _timeUpdatedCrossesByMarket[crossRateByMarketToStore] = DateTime.Now;
                    ManualResetEvent[] signalsBittrex = TradeBotsStorage<ResponseWrapper>.GetMarketSignals("bittrex");
                    if (signalsBittrex != null) {
                        foreach (var signal in signalsBittrex) {
                            signal.Set();
                        }
                    }
                }
            }
        }
    }
}
