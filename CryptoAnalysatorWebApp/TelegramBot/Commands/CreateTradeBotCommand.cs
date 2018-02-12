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
        private readonly int _port;

        public CreateTradeBotCommand(int port) {
            _port = port;
        }

        public override void Execute(Message message, TelegramBotClient client, string channelId = null) {
            var chatId = message.Chat.Id;
            var messageId = message.MessageId;

            string[] messageWords = message.Text.Split(' ');
            if (messageWords.Length < 3) {
                client.SendTextMessageAsync(chatId, "Error: apiKey or/and apiSecret were not provided");
                return;
            }

            string apiKey = messageWords[1];
            string apiSecret = messageWords[2];
            
            BittrexTradeBot bittrexTradeBot = new BittrexTradeBot(apiKey, apiSecret);
            ResponseWrapper responseBalances = bittrexTradeBot.GetBalances().Result;
            JArray currenciesOnBalance = (JArray) responseBalances.Result;
            client.SendTextMessageAsync(chatId, $"Trade bot created on Bittrex. Your balance:\n" +
                                                $"BTC: {currenciesOnBalance.First(cur => (string)cur["Currency"] == "BTC")}\n " +
                                                $"ETH: {currenciesOnBalance.First(cur => (string)cur["Currency"] == "ETH")}");
        }
    }
}