using System;
using System.Threading;
using CryptoAnalysatorWebApp.TelegramBot.Commands.Common;
using Telegram.Bot;
using Telegram.Bot.Types;
using CryptoAnalysatorWebApp.TradeBots;
using CryptoAnalysatorWebApp.TradeBots.Common;
using SQLitePCL;

namespace CryptoAnalysatorWebApp.TelegramBot.Commands {
    public class TradeCommand : CommonCommand {
        public override string Name { get; } = "trade";

        public override void Execute(Message message, TelegramBotClient client, string channelId = null) {
            var chatId = message.Chat.Id;

            (string market, decimal amountBtc, decimal amountEth) = GetTradeData(message);
            if (amountBtc != 0 && amountBtc < (decimal) 0.0005) {
                client.SendTextMessageAsync(chatId, "Can't trade with < 0.0005 btc");
                return;
            }
            if (amountEth != 0 && amountEth < (decimal) 0.0005) {
                client.SendTextMessageAsync(chatId, "Can't trade with < 0.005 eth");
                return;
            }
            if (amountBtc == 0 && amountEth == 0) {
                client.SendTextMessageAsync(chatId, "Can't trade with 0 btc and 0 eth");
                return;
            } 
            
            if (TradeBotsStorage.Exists(chatId, market)) {
                CommonTradeBot tradeBot = TradeBotsStorage.GetTardeBot(chatId, market);
                tradeBot.MakeReadyToTrade(amountBtc, amountEth);
                if (tradeBot.TradeAmountBtc > tradeBot.BalanceBtc || tradeBot.TradeAmountEth > tradeBot.BalanceEth) {
                    client.SendTextMessageAsync(chatId, string.Format("You don't have enough balance", market));
                    return;
                }

                try {
                    tradeBot.StartTrading(client, chatId);
                } catch (Exception e) {
                    Console.WriteLine($"Exctption while trading: {e.Message}");
                    TradeBotsStorage.DeleteTradeBot(chatId, market);
                    client.SendTextMessageAsync(chatId, e.Message + "\nBot was destryed");
                }

                client.SendTextMessageAsync(chatId, string.Format("Your bot on {0} started trading", market));
            } else {
                client.SendTextMessageAsync(chatId, string.Format("You don't have a bot on {0}", market));
            }
        }

        private (string, decimal, decimal) GetTradeData(Message message) {
            string[] messageWords = message.Text.Split(' ');
            if (messageWords.Length < 3) {
                return ("", 0, 0);
            }

            if (messageWords.Length == 3) {
                string currency = messageWords[2].Split(':')[0].ToLower();
                decimal currencyAmnt = decimal.TryParse(messageWords[2].Split(':')[1], out var amount) ? amount : 0;
                
                return (messageWords[1], currency == "btc" ? currencyAmnt : 0, currency == "eth" ? currencyAmnt : 0);
            } else {
                string currency1 = messageWords[2].Split(':')[0].ToLower();
                string currency2 = messageWords[3].Split(':')[0].ToLower();
                decimal amountBtc = 0;
                decimal amountEth = 0;
                decimal amount;

                switch (currency1) {
                    case "btc":
                        amountBtc = decimal.TryParse(messageWords[2].Split(':')[1], out amount) ? amount : 0;
                        break;
                    case "eth":
                        amountEth = decimal.TryParse(messageWords[2].Split(':')[1], out amount) ? amount : 0;
                        break; 
                }

                switch (currency2) {
                    case "btc":
                        amountBtc = decimal.TryParse(messageWords[3].Split(':')[1], out amount) ? amount : 0;
                        break;
                    case "eth":
                        amountEth = decimal.TryParse(messageWords[3].Split(':')[1], out amount) ? amount : 0;
                        break;
                }
                
                return (messageWords[1], amountBtc, amountEth);
            }
        }
    }
}