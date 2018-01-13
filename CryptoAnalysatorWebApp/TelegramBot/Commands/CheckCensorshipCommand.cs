using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.RegularExpressions;

namespace CryptoAnalysatorWebApp.TelegramBot.Commands
{
    public class CheckCensorshipCommand : Common.CommonCommand {
        public override string Name { get; } = "check_censor";

        public override void Execute(Message message, TelegramBotClient client, string channelId = null) {
            string censor = @"(хуй|пидор|дурак)";
            string text = message.Text;

            Match match = Regex.Match(text, censor, RegexOptions.IgnoreCase);
            if (match.Success) {
                client.SendTextMessageAsync(message.Chat.Id, "It's forbidden to tease and razz in the channel. Next time u will be banned");
            }
        }
    }
}
