using System.Collections.Generic;
using CryptoAnalysatorWebApp.TradeBots.Common;

namespace CryptoAnalysatorWebApp.TradeBots {
    public static class TradeBotsStorage {
        private static Dictionary<long, CommonTradeBot> _bittrexTradeBots;

        static TradeBotsStorage() {
            _bittrexTradeBots = new Dictionary<long, CommonTradeBot>();
        }

        public static bool AddTradeBot(long chatId, CommonTradeBot tradeBot, string market) {
            switch (market) {
                case "bittrex":
                    if (!_bittrexTradeBots.ContainsKey(chatId)) {
                        _bittrexTradeBots.Add(chatId, tradeBot);
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
                        _bittrexTradeBots.Remove(chatId);
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
                    return _bittrexTradeBots.ContainsKey(chatId) ? true : false;
            }

            return false;
        }

        public static CommonTradeBot GetTardeBot(long chatId, string market) {
            switch (market) {
                case "bittrex":
                    return _bittrexTradeBots[chatId];
            }

            return null;
        }
    }
}