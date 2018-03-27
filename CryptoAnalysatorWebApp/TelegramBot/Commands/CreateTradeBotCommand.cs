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

        public override async void Execute(Message message, TelegramBotClient client, string channelId = null) {
            var chatId = message.Chat.Id;

            (string apiKey, string apiSecret) = GetAuthData(message, client, chatId);
            if (apiKey == "" || apiSecret == "") {
                client.SendTextMessageAsync(chatId, "Error: apiKey or/and apiSecret were not provided");
                return;
            }

            BittrexTradeBot bittrexTradeBot = new BittrexTradeBot(apiKey, apiSecret);
            ManualResetEvent signal = new ManualResetEvent(false);
            
            bool exists = TradeBotsStorage<ResponseWrapper>.Exists(chatId, "bittrex");
            if (exists) {
                client.SendTextMessageAsync(chatId, $"You already have bot on bittrex");
                return;
            }

            bool created = TradeBotsStorage<ResponseWrapper>.AddTradeBot(chatId, bittrexTradeBot, "bittrex", signal);

            if (created) {
                try {
                    await bittrexTradeBot.UpdateWalletBalances();
                    client.SendTextMessageAsync(chatId, $"Trade bot created on Bittrex. Your balances:\n" +
                                                        $"{await bittrexTradeBot.ShowBalances()}\n");
                } catch (Exception e) {
                    client.SendTextMessageAsync(chatId, "Error in creating TradeBot");
                    TradeBotsStorage<ResponseWrapper>.DeleteTradeBot(chatId, "bittrex");
                }
                
            } else {
                client.SendTextMessageAsync(chatId, "Error in creating TradeBot");
            }
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