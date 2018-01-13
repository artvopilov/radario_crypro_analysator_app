using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using CryptoAnalysatorWebApp.TelegramBot.Commands.Common;

namespace CryptoAnalysatorWebApp.TelegramBot.Commands
{
    public class GetTopCrossesCommand : CommonCommand {
        public override string Name { get; } = "crosses";
        private int _port;

        public GetTopCrossesCommand(int port) {
            _port = port;
        }

        public override void Execute(Message message, TelegramBotClient client, string channelId = null) {
            if (message == null) {
                string responseStr = GetResponse(_port);
                string botMess = ProcessResponse(responseStr, -1);

                client.SendTextMessageAsync(channelId, botMess);
                return;
            }

            var chatId = message.Chat.Id;
            var messageId = message.MessageId;

            string numStr = message.Text.Split(' ').Length > 1 ? message.Text.Split(' ')[1] : "-1";
            int numWanted = int.TryParse(numStr, out numWanted) ? numWanted : 0;

            if (numWanted != 0) {
                string responseStr = GetResponse(_port);
                string botMess = ProcessResponse(responseStr, numWanted);

                client.SendTextMessageAsync(chatId, botMess);
            } else {
                client.SendTextMessageAsync(chatId, "Smth wrong");
            }
        }

        private string ProcessResponse(string responseStr, int numWanted) {
            string botMess = "Crosses\n";

            JArray responseJson = (JArray)JObject.Parse(responseStr)["crosses"];

            numWanted = numWanted != -1 ? numWanted : responseJson.Count;

            int count = 0;
            int pairsAmount = responseJson.Count;
            while (count < pairsAmount && count < numWanted && count < 30) {
                botMess += $"{count + 1}){responseJson[count]["pair"]} buy: {responseJson[count]["stockExchangeSeller"]}({responseJson[count]["purchasePrice"]}) " +
                    $"sell: {responseJson[count]["stockExchangeBuyer"]}({responseJson[count]["sellPrice"]})\n";
                count++;
            }

            return botMess;
        }
    }
}
