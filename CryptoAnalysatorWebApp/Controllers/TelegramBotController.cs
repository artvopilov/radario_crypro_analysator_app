using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using CryptoAnalysatorWebApp.TelegramBot;
using CryptoAnalysatorWebApp.TelegramBot.Commands;

namespace CryptoAnalysatorWebApp.Controllers
{
    [Route("api/telegrambot")]
    public class TelegramBotController : Controller {
        private TelegramBotClient _client;
        private IReadOnlyList<CommonCommand> _commands;

        public TelegramBotController() {
            _client = Bot.Get();
            _commands = Bot.Commands;
        }

        // POST api/telegrambot
        [HttpPost]
        public TelegramBotClient Post([FromBody]Update update) {
            Message message = update.Message;

            foreach (CommonCommand command in _commands) {
                if (message.Text.Contains('/' + command.Name)) {
                    command.Execute(message, _client);
                    break;
                }
            }

            return _client;

        }
    }
}
