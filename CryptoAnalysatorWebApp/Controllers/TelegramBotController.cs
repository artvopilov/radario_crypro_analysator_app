using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using CryptoAnalysatorWebApp.TelegramBot;
using CryptoAnalysatorWebApp.TelegramBot.Commands.Common;

namespace CryptoAnalysatorWebApp.Controllers
{
    [Route("api/[controller]")]
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
            Message message = update.Message ?? update.EditedMessage;

            if (message == null) {
                message = update.ChannelPost;
                _commands.First(cmnd => cmnd.Name == "check_censor").Execute(message, _client);
                return _client;
            }

            bool ok = false;
            foreach (CommonCommand command in _commands) {
                if (message.Text.Contains('/' + command.Name)) {
                    command.Execute(message, _client);
                    ok = true;
                    break;
                }
            }

            if (!ok) {
                _client.SendTextMessageAsync(message.Chat.Id, "Check your command (or type /help)");
            }
            return _client;
        }
    }
}
