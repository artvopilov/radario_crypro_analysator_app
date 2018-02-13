using System.Threading.Tasks;
using CryptoAnalysatorWebApp.TradeBots.Common.Objects;


namespace CryptoAnalysatorWebApp.TradeBots.Interfaces {
    public interface ITradeBot {
        Task<ResponseWrapper> GetBalances();
        Task<ResponseWrapper> CreateBuyOrder(string pair, decimal quantity, decimal rate);
        Task<ResponseWrapper> CreateSellORder(string pair, decimal quantity, decimal rate);
        Task<ResponseWrapper> CancelOrder(string orderId);
        Task<ResponseWrapper> GetAllPairs();
        Task<ResponseWrapper> GetOrderBook(string pair);
        Task<ResponseWrapper> GetOpenOrders(string pair);
        void Trade(decimal amountBtc, decimal amountEth);
        void MakeReadyToTrade(decimal amountBtc, decimal amountEth);
    }
}