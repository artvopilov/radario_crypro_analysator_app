using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot; 


namespace CryptoAnalysatorWebApp.TelegramBot
{
    public static class Bot {
        private static TelegramBotClient _client;

        public static TelegramBotClient Get() {
            if (_client != null) {
                return _client;
            }

            _client = new TelegramBotClient(BotSettings.AccessToken);

            return _client;
        }
    }
}
