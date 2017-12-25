using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;


namespace CryptoAnalysatorWebApp.TelegramBot.Commands
{
    public class HelloCommand : CommonCommand {
        public override string Name { get; } = "hello";

        public override void Execute(Message message, TelegramBotClient client) {

            var chatId = message.Chat.Id;
            var messageId = message.MessageId;

            client.SendTextMessageAsync(chatId, "Hello from bot");
        }
    }
}
