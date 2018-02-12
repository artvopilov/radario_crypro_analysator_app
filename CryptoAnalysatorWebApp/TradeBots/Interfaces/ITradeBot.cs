using System.Threading.Tasks;
using CryptoAnalysatorWebApp.TradeBots.Common.Objects;


namespace CryptoAnalysatorWebApp.TradeBots.Interfaces {
    public interface ITradeBot {
        Task<ResponseWrapper> GetBalances();
        Task<ResponseWrapper> CreateBuyOrder(string pair, decimal quantity, decimal rate);
        Task<ResponseWrapper> CreateSellORder(string pair, decimal quantity, decimal rate);
        Task<ResponseWrapper> CancelOrder(string orderId);
        void Trade();
    }
}