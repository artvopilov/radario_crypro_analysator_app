using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Timers;
using CryptoAnalysatorWebApp.TelegramBot.Commands.Common;
using CryptoAnalysatorWebApp.TelegramBot.Commands;
using System.Net.Http;
using CryptoAnalysatorWebApp.Models;

namespace CryptoAnalysatorWebApp.TelegramBot
{
    public static class Bot {
        private static TelegramBotClient _client;
        private static List<CommonCommand> _commands;
        private static int _port;

        public static IReadOnlyList<CommonCommand> Commands { get => _commands.AsReadOnly(); }

        public static TelegramBotClient Get() {
            if (_client != null) {
                return _client;
            }

            if (System.IO.File.Exists("production.on")) {
                _port = 80;
            } else {
                _port = 5000;
            }
            GetTopPairsCommand getTopPairsCommand = new GetTopPairsCommand(_port);
            GetTopCrossesCommand getTopCrossesCommand = new GetTopCrossesCommand(_port);

            _commands = new List<CommonCommand>();
            _commands.Add(new HelloCommand());
            _commands.Add(getTopPairsCommand);
            _commands.Add(getTopCrossesCommand);
            _commands.Add(new CheckCensorshipCommand());

            _client = new TelegramBotClient(BotSettings.AccessToken);
            _client.SetWebhookAsync("https://2a62f646.ngrok.io/api/telegrambot").Wait();

            return _client;
        }

        public static void StartChannelPosting() {
            if (_client == null) {
                return;
            }
            Task.Run(async () => {
                DateTime maxDateTimePairs = DateTime.Now;
                DateTime maxDateTimeCrosses = DateTime.Now;
                while (true) {
                    string message = "";

                    Console.WriteLine("Bot Works In Channel");

                    using (HttpClient httpClient = new HttpClient()) {
                        httpClient.GetAsync($"http://localhost:{_port}/api/actualpairs").Wait();
                    }

                    DateTime timeP = TimeService.TimePairs.Max(tp => tp.Value);
                    DateTime timeC = TimeService.TimeCrosses.Max(tp => tp.Value);

                    maxDateTimePairs = timeP > maxDateTimePairs ? timeP : DateTime.Now;
                    maxDateTimeCrosses = timeC > maxDateTimeCrosses ? timeC : DateTime.Now;

                    int count = 0;
                    foreach (KeyValuePair<ExchangePair, DateTime> kvp in TimeService.TimePairs) {
                        if (kvp.Value == maxDateTimePairs && count < 30) {
                            ExchangePair exchangePair = kvp.Key;

                            if (exchangePair.Spread > 5) {
                                message += $"{count + 1}) {exchangePair.Pair} buy: {exchangePair.StockExchangeSeller}({exchangePair.PurchasePrice}) " +
                                                                                     $"sell: {exchangePair.StockExchangeBuyer}({exchangePair.SellPrice})\n";
                                count++;
                            }

                        }
                    }

                    foreach (KeyValuePair<ExchangePair, DateTime> kvp in TimeService.TimeCrosses) {
                        if (kvp.Value == maxDateTimeCrosses && count < 30) {
                            ExchangePair exchangePair = kvp.Key;

                            if (exchangePair.Spread > 5) {
                                message += $"{count + 1}) {exchangePair.Pair} buy: {exchangePair.StockExchangeSeller}({exchangePair.PurchasePrice}) " +
                                                                                    $"sell: {exchangePair.StockExchangeBuyer}({exchangePair.SellPrice})\n";
                                count++;
                            }
                        }
                    }

                    if (message == "") {
                        await Task.Delay(35000);
                        continue;
                    }

                    string channelId = "-1001333185321";
                    await _client.SendTextMessageAsync(channelId, message);

                    await Task.Delay(35000);
                }
            });
        }
    }
}
