using System.Threading;
using System.Threading.Tasks;
using CryptoAnalysatorWebApp.TradeBots.Common.Objects;
using Telegram.Bot;


namespace CryptoAnalysatorWebApp.TradeBots.Interfaces {
    public interface ITradeBot<TResult> {
        Task<TResult> GetBalances();
        Task<TResult> CreateBuyOrder(string pair, decimal quantity, decimal rate);
        Task<TResult> CreateSellORder(string pair, decimal quantity, decimal rate);
        Task<TResult> CancelOrder(string orderId);
        Task<TResult> GetAllPairs();
        Task<TResult> GetOrderBook(string pair);
        Task<TResult> GetOpenOrders(string pair);
        void StartTrading(TelegramBotClient client, long chatId, ManualResetEvent signal);
        void Trade(decimal amountBtc, decimal amountEth, TelegramBotClient client, long chatId, ManualResetEvent signal);
        void MakeReadyToTrade(decimal amountBtc, decimal amountEth);
        //Key:8a36bde72234442ca543f86696a1ecc1 
        //Secret:ce4e67b6e9654e358c6da9677750d9e9
    }
}