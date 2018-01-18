using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Timers;
using CryptoAnalysatorWebApp.TelegramBot.Commands.Common;
using CryptoAnalysatorWebApp.TelegramBot.Commands;
using System.Net.Http;
using CryptoAnalysatorWebApp.Models;
using CryptoAnalysatorWebApp.Models.Common;

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
            _client.SetWebhookAsync("https://add7b70b.ngrok.io/api/telegrambot").Wait();

            return _client;
        }

        public static void StartChannelPosting() {
            while (_client == null) {
                Console.WriteLine("no client");
                Thread.Sleep(1000);
            }

            DateTime maxDateTimePairs = DateTime.Now;
            DateTime maxDateTimeCrosses = DateTime.Now;
            while (true) {
                string message = "";

                Console.WriteLine($"Bot Works In Channel");
                PairsAnalysator pairsAnalysator = new PairsAnalysator();

                BasicCryptoMarket[] marketsArray = { new PoloniexMarket(), new BittrexMarket(), new ExmoMarket() };
                pairsAnalysator.FindActualPairsAndCrossRates(marketsArray, "bot");

                Console.WriteLine("ANALYSED");

                Dictionary<string, List<ExchangePair>> pairsDic = new Dictionary<string, List<ExchangePair>>();
                pairsDic["crosses"] = pairsAnalysator.CrossPairs.OrderByDescending(p => p.Spread).ToList();
                pairsDic["pairs"] = pairsAnalysator.ActualPairs.OrderByDescending(p => p.Spread).ToList();

                TimeService.StoreTime(DateTime.Now, pairsDic["pairs"].ToList(), pairsDic["crosses"].ToList());


                DateTime timeP = TimeService.TimePairs.Max(tp => tp.Value);
                DateTime timeC = TimeService.TimeCrosses.Max(tp => tp.Value);

                Console.WriteLine($"maxDateTimePairs: {maxDateTimePairs}  maxDateTimeCrosses: {maxDateTimeCrosses}");
                Console.WriteLine($"TIMEPAIR: {timeP}  TIMECROSSES: {timeC}");

                maxDateTimePairs = timeP > maxDateTimePairs ? timeP : DateTime.Now;
                maxDateTimeCrosses = timeC > maxDateTimeCrosses ? timeC : DateTime.Now;

                int count = 0;
                foreach (KeyValuePair<ExchangePair, DateTime> kvp in TimeService.TimePairs) {
                    if (kvp.Value == maxDateTimePairs && count < 15) {
                        ExchangePair exchangePair = kvp.Key;

                        if (exchangePair.Spread > 3) {
                            message += $"{count + 1}) {exchangePair.Pair} buy: {exchangePair.StockExchangeSeller}({exchangePair.PurchasePrice}) " +
                                                                                 $"sell: {exchangePair.StockExchangeBuyer}({exchangePair.SellPrice})\n";
                            count++;
                        }

                    }
                }

                foreach (KeyValuePair<ExchangePair, DateTime> kvp in TimeService.TimeCrosses) {
                    if (kvp.Value == maxDateTimeCrosses && count < 30) {
                        ExchangePair exchangePair = kvp.Key;

                        if (exchangePair.Spread > 3) {
                            message += $"{count + 1}) {exchangePair.Pair} buy: {exchangePair.StockExchangeSeller}({exchangePair.PurchasePrice}) " +
                                                                                $"sell: {exchangePair.StockExchangeBuyer}({exchangePair.SellPrice})\n";
                            count++;
                        }
                    }
                }

                if (message == "") {
                    Console.WriteLine("ANY MESSAGE");
                    Thread.Sleep(2000);
                    continue;
                } else {
                    //Console.WriteLine($"MESSAGE: {message}");
                    string channelId = "-1001333185321";
                    _client.SendTextMessageAsync(channelId, message);

                    Thread.Sleep(2000);
                }
            }
        }
    }
}
