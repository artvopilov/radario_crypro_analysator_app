using System.Threading;
using System.Threading.Tasks;
using CryptoAnalysatorWebApp.TradeBots.Common.Objects;
using Telegram.Bot;


namespace CryptoAnalysatorWebApp.TradeBots.Interfaces {
    public interface ITradeBot<TResult> {
        Task<TResult> GetBalances();
        Task<TResult> GetBalance(string currency);
        Task<TResult> CreateBuyOrder(string pair, decimal quantity, decimal rate);
        Task<TResult> CreateSellORder(string pair, decimal quantity, decimal rate);
        Task<TResult> CancelOrder(string orderId);
        Task<TResult> GetAllPairs();
        Task<TResult> GetOrderBook(string pair);
        Task<TResult> GetOpenOrders(string pair);
        (decimal, decimal) StartTrading(decimal amountBtc, decimal amountEth, TelegramBotClient client, long chatId, ManualResetEvent signal);
        void Trade(decimal amountBtc, decimal amountEth, TelegramBotClient client, long chatId, ManualResetEvent signal);
    }
}