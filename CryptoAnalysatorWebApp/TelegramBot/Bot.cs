using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using CryptoAnalysatorWebApp.TelegramBot.Commands;

namespace CryptoAnalysatorWebApp.TelegramBot
{
    public static class Bot {
        private static TelegramBotClient _client;
        private static List<CommonCommand> _commands;

        public static IReadOnlyList<CommonCommand> Commands { get => _commands.AsReadOnly(); }

        public static TelegramBotClient Get() {
            if (_client != null) {
                return _client;
            }

            int port;
            if (System.IO.File.Exists("production.on")) {
                port = 80;
            } else {
                port = 5000;
            }

            _commands = new List<CommonCommand>();
            _commands.Add(new HelloCommand());
            _commands.Add(new GetTopPairsCommand(port));
            _commands.Add(new GetTopCrossesCommand(port));

            _client = new TelegramBotClient(BotSettings.AccessToken);
            _client.SetWebhookAsync("https://d6ce0ee2.ngrok.io/api/telegrambot").Wait();

            return _client;
        }
    }
}
