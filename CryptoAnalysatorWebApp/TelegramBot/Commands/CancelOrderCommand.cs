using System.Linq;
using System.Threading;
using CryptoAnalysatorWebApp.TelegramBot.Commands.Common;
using CryptoAnalysatorWebApp.TradeBots;
using CryptoAnalysatorWebApp.TradeBots.Common;
using CryptoAnalysatorWebApp.TradeBots.Common.Objects;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CryptoAnalysatorWebApp.TelegramBot.Commands {
    public class CancelOrderCommand : CommonCommand {
        public override string Name { get; } = "cancelOrder";
        public override async void Execute(Message message, TelegramBotClient client, string channelId = null) {
            long chatId = message.Chat.Id;
            if (TradeBotsStorage<ResponseWrapper>.Exists(chatId, "bittrex")) {
                (CommonTradeBot<ResponseWrapper> tradeBot, ManualResetEvent signal) = TradeBotsStorage<ResponseWrapper>.GetTardeBot(chatId, "bittrex");
                string orderUuid = getOrderUuid(message.Text);
                bool canceled = (await tradeBot.CancelOrder(orderUuid)).Success;
                if (canceled) {
                    await client.SendTextMessageAsync(chatId, $"[Canceled] Order ${getOrderUuid(message.Text)}");
                } else {
                    await client.SendTextMessageAsync(chatId, $"You dont have order {getOrderUuid(message.Text)}");
                }

            } else {
                await client.SendTextMessageAsync(chatId, "U dont have bittrex TradeBot");
            }
        }

        private string getOrderUuid(string message) {
            string[] splitedMess = message.Split(' ');
            if (splitedMess.Length > 1) {
                return splitedMess[1];
            }

            return "";
        }
    }
}