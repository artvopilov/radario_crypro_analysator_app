using Telegram.Bot;
using Telegram.Bot.Types;


namespace CryptoAnalysatorWebApp.TelegramBot.Commands
{
    public abstract class CommonCommand {
        public abstract string Name { get; }

        public abstract void Execute(Message message, TelegramBotClient client);
    }
}
