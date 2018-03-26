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
    public class GetOpenOrdersCommand : CommonCommand {
        public override string Name { get; } = "showOpenOrders";
        public override async void Execute(Message message, TelegramBotClient client, string channelId = null) {
            long chatId = message.Chat.Id;
            
            if (TradeBotsStorage<ResponseWrapper>.Exists(chatId, "bittrex")) {
                (CommonTradeBot<ResponseWrapper> tradeBot, ManualResetEvent signal) = TradeBotsStorage<ResponseWrapper>.GetTardeBot(chatId, "bittrex");
                JArray openOrders = (JArray)(await tradeBot.GetOpenOrders()).Result;
                string respMess = "NoOrders";
                if (openOrders.Count > 0) {
                    respMess = string.Join("\n",
                        openOrders.Select(o =>
                                "Uuid: " + o["OrderUuid"] + "Type: " + o["OrderType"] + "Pair: " + o["Exchange"])
                            .ToArray());
                }

                await client.SendTextMessageAsync(chatId, respMess);
            } else {
                await client.SendTextMessageAsync(chatId, "U dont have bittrex TradeBot");
            }
        }
    }
}