using System;
using System.Linq;
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
            if (apiKey == "" || apiSecret == "") {
                client.SendTextMessageAsync(chatId, "Error: apiKey or/and apiSecret were not provided");
                return;
            }
            
            BittrexTradeBot bittrexTradeBot = new BittrexTradeBot(apiKey, apiSecret);
            TradeBotsStorage.AddTradeBot(chatId, bittrexTradeBot, "bittrex");

            client.SendTextMessageAsync(chatId, $"Trade bot created on Bittrex. Your balancec:\n" +
                                                $"BTC: {bittrexTradeBot.BalanceBtc}\n " +
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