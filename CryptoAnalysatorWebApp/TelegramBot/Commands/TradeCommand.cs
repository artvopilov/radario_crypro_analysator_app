using System;
using CryptoAnalysatorWebApp.TelegramBot.Commands.Common;
using Telegram.Bot;
using Telegram.Bot.Types;
using CryptoAnalysatorWebApp.TradeBots;
using CryptoAnalysatorWebApp.TradeBots.Common;

namespace CryptoAnalysatorWebApp.TelegramBot.Commands {
    public class TradeCommand : CommonCommand {
        public override string Name { get; } = "trade";

        public override void Execute(Message message, TelegramBotClient client, string channelId = null) {
            var chatId = message.Chat.Id;

            (string market, decimal amountBtc, decimal amountEth) = GetTradeData(message);
            if (amountBtc == 0 && amountEth == 0) {
                client.SendTextMessageAsync(chatId, "Can't trade with 0 btc and 0 eth");
                return;
            }
            
            if (TradeBotsStorage.Exists(chatId, market)) {
                CommonTradeBot tradeBot = TradeBotsStorage.GetTardeBot(chatId, market);
                tradeBot.Ready = true;
                tradeBot.TradeAmountBtc = amountBtc;
                tradeBot.TradeAmountEth = amountEth;
                if (tradeBot.TradeAmountBtc > tradeBot.BalanceBtc || tradeBot.TradeAmountEth > tradeBot.BalanceEth) {
                    client.SendTextMessageAsync(chatId, string.Format("You don't have enough balance", market));
                    return;
                }
                tradeBot.Trade(amountBtc, amountEth);
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
                return (messageWords[1], decimal.TryParse(messageWords[2].Split(':')[1], out var amountBtc)
                    ? amountBtc : 0, 0);
            } else {
                return (messageWords[1], decimal.TryParse(messageWords[2].Split(':')[1], out var amountBtc)
                    ? amountBtc : 0, decimal.TryParse(messageWords[3].Split(':')[1], out var amountEth)
                    ? amountEth : 0);
            }
        }
    }
}