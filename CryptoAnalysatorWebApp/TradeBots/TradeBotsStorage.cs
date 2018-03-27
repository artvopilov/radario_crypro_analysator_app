using System.Collections.Generic;
using System.Linq;
using CryptoAnalysatorWebApp.TradeBots.Common;
using System.Threading;

namespace CryptoAnalysatorWebApp.TradeBots {
    public static class TradeBotsStorage<TResult> {
        private static Dictionary<long, CommonTradeBot<TResult>> _bittrexTradeBots;
        private static Dictionary<long, ManualResetEvent> _bittrexTradeSignals;

        static TradeBotsStorage() {
            _bittrexTradeBots = new Dictionary<long, CommonTradeBot<TResult>>();
            _bittrexTradeSignals = new Dictionary<long, ManualResetEvent>();
        }

        public static bool AddTradeBot(long chatId, CommonTradeBot<TResult> tradeBot, string market, ManualResetEvent signal) {
            switch (market) {
                case "bittrex":
                    if (!_bittrexTradeBots.ContainsKey(chatId)) {
                        _bittrexTradeBots.Add(chatId, tradeBot);
                        _bittrexTradeSignals.Add(chatId, signal);
                        return true;
                    }
                    return false;
                    break;
            }

            return false;
        }

        public static bool DeleteTradeBot(long chatId, string market) {
            switch (market) {
                case "bittrex":
                    if (_bittrexTradeBots.ContainsKey(chatId)) {
                        _bittrexTradeBots[chatId].TradeOn = false;
                        _bittrexTradeBots.Remove(chatId);
                        _bittrexTradeSignals.Remove(chatId);
                        return true;
                    }

                    return false;
                    break;
            }

            return false;
        }

        public static bool Exists(long chatId, string market) {
            switch (market) {
                case "bittrex":
                    return _bittrexTradeBots.ContainsKey(chatId);
            }

            return false;
        }

        public static (CommonTradeBot<TResult>, ManualResetEvent) GetTardeBot(long chatId, string market) {
            switch (market) {
                case "bittrex":
                    return (_bittrexTradeBots[chatId], _bittrexTradeSignals[chatId]);
            }

            return (null, null);
        }

        public static ManualResetEvent[] GetMarketSignals(string market) {
            switch (market) {
                case "bittrex":
                    if (_bittrexTradeSignals.Count > 0) {
                        return _bittrexTradeSignals.Values.ToArray();
                    } else {
                        return null;
                    }
            }

            return null;
        }
    }
}