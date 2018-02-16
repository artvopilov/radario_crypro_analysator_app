using CryptoAnalysatorWebApp.TelegramBot.Commands.Common;
using CryptoAnalysatorWebApp.TradeBots;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CryptoAnalysatorWebApp.TelegramBot.Commands {
    public class HelpCommand : CommonCommand {
        public override string Name { get; } = "help";
        
        public override void Execute(Message message, TelegramBotClient client, string channelId = null) {
            var chatId = message.Chat.Id;

            const string respMessage = "Commands for trade bot on bittrex:\n" +
                                       "/createBot <apiKey> <apiSecret>\n" +
                                       "/trade bittrex btc:<amount> eth:<amount>\n" +
                                       "/deleteBot bittrex";
            client.SendTextMessageAsync(chatId, respMessage);
        }
    }
}