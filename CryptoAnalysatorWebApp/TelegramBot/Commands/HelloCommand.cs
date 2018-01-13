using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using CryptoAnalysatorWebApp.TelegramBot.Commands.Common;

namespace CryptoAnalysatorWebApp.TelegramBot.Commands
{
    public class HelloCommand : CommonCommand {
        public override string Name { get; } = "hello";

        public override void Execute(Message message, TelegramBotClient client, string channelId = null) {

            var chatId = message.Chat.Id;
            var messageId = message.MessageId;

            client.SendTextMessageAsync(chatId, "Hello from bot");
        }
    }
}
