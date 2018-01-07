﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace CryptoAnalysatorWebApp.TelegramBot.Commands
{
    public class GetTopCrossesCommand : CommonCommand {
        private int _port;
        public override string Name { get; } = "crosses";

        public GetTopCrossesCommand(int port) {
            _port = port;
        }

        public override void Execute(Message message, TelegramBotClient client) {
            var chatId = message.Chat.Id;
            var messageId = message.MessageId;

            string numStr = message.Text.Split(' ')[1];
            int numWanted = int.TryParse(numStr, out numWanted) ? numWanted : 0;

            if (numWanted > 0) {
                string responseStr = GetResponse();
                string botMess = ProcessResponse(responseStr, numWanted);

                client.SendTextMessageAsync(chatId, botMess);
            }
        }

        private string GetResponse() {
            using (HttpClient httpClient = new HttpClient()) {
                using (HttpResponseMessage respMessage = httpClient.GetAsync($"http://localhost:{_port}/api/actualpairs").Result) {
                    using (HttpContent httpContent = respMessage.Content) {
                        string responseStr = httpContent.ReadAsStringAsync().Result;
                        return responseStr;
                    }
                }
            }
        }

        private string ProcessResponse(string responseStr, int numWanted) {
            string botMess = "";

            JArray responseJson = (JArray)JObject.Parse(responseStr)["crosses"];

            int count = 0;
            int pairsAmount = responseJson.Count;
            while (count < pairsAmount && count < numWanted) {
                botMess += $"{count + 1}){responseJson[count]["pair"]} $buy$: {responseJson[count]["stockExchangeSeller"]}({responseJson[count]["purchasePrice"]}) " +
                    $"$sell$: {responseJson[count]["stockExchangeBuyer"]}({responseJson[count]["sellPrice"]})\n";
                count++;
            }

            return botMess;
        }
    }
}
