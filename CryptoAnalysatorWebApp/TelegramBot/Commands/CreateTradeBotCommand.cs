using System;
using System.Linq;
using System.Threading;
using CryptoAnalysatorWebApp.TelegramBot.Commands.Common;
using Telegram.Bot;
using Telegram.Bot.Types;
using CryptoAnalysatorWebApp.TradeBots;
using CryptoAnalysatorWebApp.TradeBots.Common.Objects;
using Newtonsoft.Json.Linq;

namespace CryptoAnalysatorWebApp.TelegramBot.Commands {
    public class CreateTradeBotCommand : CommonCommand {
        public override string Name { get; } = "createBot";

        public override void Execute(Message message, TelegramBotClient client, string channelId = null) {
            var chatId = message.Chat.Id;

            (string apiKey, string apiSecret) = GetAuthData(message, client, chatId);
            /*if (apiKey == "" || apiSecret == "") {
                client.SendTextMessageAsync(chatId, "Error: apiKey or/and apiSecret were not provided");
                BittrexTradeBot bittrexTradeBot1 = new BittrexTradeBot();
                ManualResetEvent signal1 = new ManualResetEvent(false);
                //return;
            }*/

            BittrexTradeBot bittrexTradeBot = new BittrexTradeBot();// = new BittrexTradeBot(apiKey, apiSecret);
            ManualResetEvent signal = new ManualResetEvent(false);// = new ManualResetEvent(false);
            
            bool created = TradeBotsStorage.AddTradeBot(chatId, bittrexTradeBot, "bittrex", signal);
            if (!created) {
                client.SendTextMessageAsync(chatId, $"You already have bot on bittrex");
            }

            client.SendTextMessageAsync(chatId, $"Trade bot created on Bittrex. Your balances:\n" +
                                                $"BTC: {bittrexTradeBot.BalanceBtc}\n" +
                                                $"ETH: {bittrexTradeBot.BalanceEth}");
        }

        private (string, string) GetAuthData(Message message, TelegramBotClient client, long chatId) {
            string[] messageWords = message.Text.Split(' ');
            if (messageWords.Length < 3) {
                return ("", "");
            }

            string apiKey = messageWords[1];
            string apiSecret = messageWords[2];
            return (apiKey, apiSecret);
        }
    }
}