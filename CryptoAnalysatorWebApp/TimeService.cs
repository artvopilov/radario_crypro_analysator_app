using System;
using System.Collections.Generic;
using CryptoAnalysatorWebApp.Models;
using System.Linq;


namespace CryptoAnalysatorWebApp
{
    public class TimeService
    {
        private Dictionary<ExchangePair, TimeSpan> _timeUpdatedPairs = new Dictionary<ExchangePair, TimeSpan>();
        private Dictionary<ExchangePair, TimeSpan> _timeUpdatedCrosses = new Dictionary<ExchangePair, TimeSpan>();

        public Dictionary<ExchangePair, TimeSpan> TimePairs { get => _timeUpdatedPairs; }
        public Dictionary<ExchangePair, TimeSpan> TimeCrosses { get => _timeUpdatedCrosses; }

        public TimeSpan GetPairTimeUpd (ExchangePair pair) {
            return _timeUpdatedPairs.Where(p => p.Key.Pair == pair.Pair && p.Key.StockExchangeBuyer == pair.StockExchangeBuyer &&
                p.Key.StockExchangeSeller == pair.StockExchangeSeller).First().Value;
        }

        public TimeSpan GetCrossTimeUpd (ExchangePair cross) {
            return _timeUpdatedCrosses.Where(c => c.Key.Pair == cross.Pair && c.Key.StockExchangeBuyer == cross.StockExchangeBuyer &&
                c.Key.StockExchangeSeller == cross.StockExchangeSeller).First().Value;
        }

        public void StoreTime(TimeSpan curTime, List<ExchangePair> pairsToStore, List<ExchangePair> crossesToStore) {
            List<ExchangePair> pairsToRemove = new List<ExchangePair>();
            foreach (ExchangePair pairS in _timeUpdatedPairs.Keys) {
                ExchangePair pairFound = pairsToStore.Find(p => p.Pair == pairS.Pair && p.StockExchangeSeller == pairS.StockExchangeSeller &&
                    p.StockExchangeBuyer == pairS.StockExchangeBuyer);
                if (pairFound == null) {
                    pairsToRemove.Add(pairS);
                } else if (pairFound.Spread > pairS.Spread + (decimal)0.5 || pairFound.Spread < pairS.Spread) {
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

            List<ExchangePair> crosseToRemove = new List<ExchangePair>();
            foreach (ExchangePair crossS in _timeUpdatedCrosses.Keys) {
                ExchangePair crossFound = crossesToStore.Find(c => c.Pair == crossS.Pair && c.StockExchangeSeller == crossS.StockExchangeSeller &&
                    c.StockExchangeBuyer == crossS.StockExchangeBuyer);
                if (crossFound == null) {
                    crosseToRemove.Add(crossS);
                } else if (crossFound.Spread < crossS.Spread || crossFound.Spread > (decimal)0.5 + crossS.Spread) {
                    crosseToRemove.Add(crossS);
                } else {
                    crossesToStore.Remove(crossFound);
                }
            }
            foreach (ExchangePair crossRemained in crossesToStore) {
                _timeUpdatedCrosses[crossRemained] = curTime;
            }
            foreach (ExchangePair crossRm in crosseToRemove) {
                _timeUpdatedCrosses.Remove(crossRm);
            }

        }

        public TimeSpan GetTimeUpBy (ExchangePair pairArg, bool isCross) {
            if (!isCross) {
                ExchangePair curPair = _timeUpdatedPairs.Keys.Where(p => p.Pair == pairArg.Pair).First();
                return _timeUpdatedPairs[curPair];
            } else {
                ExchangePair curCross = _timeUpdatedCrosses.Keys.Where(c => c.Pair == pairArg.Pair).First();
                return _timeUpdatedCrosses[curCross];
            }
        }
    }
}
