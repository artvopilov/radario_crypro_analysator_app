using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Telegram.Bot;
using CryptoAnalysatorWebApp.TelegramBot.Commands.Common;
using CryptoAnalysatorWebApp.TelegramBot.Commands;
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
            _commands.Add(new CreateTradeBotCommand());
            _commands.Add(new TradeCommand());
            _commands.Add(new HelpCommand());
            _commands.Add(new DeleteBotCommand());

            _client = new TelegramBotClient(BotSettings.AccessToken);
            _client.SetWebhookAsync("https://e042dd49.ngrok.io/api/telegrambot").Wait();

            return _client;
        }

        public static void StartChannelPosting() {
            while (_client == null) {
                Console.WriteLine("no client");
                Thread.Sleep(1000);
            }

            DateTime maxDateTimePairs = DateTime.Now;
            DateTime maxDateTimeCrosses = DateTime.Now;
            DateTime maxDateTimeCrossesByMarket = DateTime.Now;
            while (true) {
                string message = "";

                Console.WriteLine($"Bot Works In Channel");
                PairsAnalysator pairsAnalysator = new PairsAnalysator();

                BasicCryptoMarket[] marketsArray = { new PoloniexMarket(), new BittrexMarket(), new ExmoMarket(), new BinanceMarket(), new LivecoinMarket() };
                pairsAnalysator.FindActualPairsAndCrossRates(marketsArray, "bot");

                Dictionary<string, List<ExchangePair>> pairsDic = new Dictionary<string, List<ExchangePair>>();
                pairsDic["crosses"] = pairsAnalysator.CrossPairs.OrderByDescending(p => p.Spread).ToList();
                pairsDic["pairs"] = pairsAnalysator.ActualPairs.OrderByDescending(p => p.Spread).ToList();
                pairsDic["crossesbymarket"] = pairsAnalysator.CrossRatesByMarket.OrderByDescending(p => p.Spread).ToList();

                TimeService.StoreTime(DateTime.Now, pairsDic["pairs"].ToList(), pairsDic["crosses"].ToList(), pairsDic["crossesbymarket"]);

                DateTime timeP = TimeService.TimePairs.Count > 0 ? TimeService.TimePairs.Max(tp => tp.Value) : DateTime.Now;
                DateTime timeC = TimeService.TimeCrosses.Count > 0 ? TimeService.TimeCrosses.Max(tp => tp.Value) : DateTime.Now;
                DateTime timeCbm = TimeService.TimeCrossesByMarket.Count > 0 ? TimeService.TimeCrossesByMarket.Max(tp => tp.Value) : DateTime.Now;

                maxDateTimePairs = timeP > maxDateTimePairs ? timeP : DateTime.Now;
                maxDateTimeCrosses = timeC > maxDateTimeCrosses ? timeC : DateTime.Now;
                maxDateTimeCrossesByMarket = timeCbm > maxDateTimeCrossesByMarket ? timeCbm: DateTime.Now;

                int count = 0;
                foreach (KeyValuePair<ExchangePair, DateTime> kvp in TimeService.TimePairs) {
                    if (kvp.Value == maxDateTimePairs && count < 10) {
                        ExchangePair exchangePair = kvp.Key;

                        if (exchangePair.Spread > 10) {
                            message += $"{count + 1}) {exchangePair.Pair}       {exchangePair.Spread}%\n" +
                                $"{exchangePair.StockExchangeSeller} ({exchangePair.PurchasePrice}) -> " +
                                $"{exchangePair.StockExchangeBuyer} ({exchangePair.SellPrice}) \n";
                            count++;
                        }

                    }
                }

                foreach (KeyValuePair<ExchangePair, DateTime> kvp in TimeService.TimeCrosses) {
                    if (kvp.Value == maxDateTimeCrosses && count < 25) {
                        ExchangePair exchangePair = kvp.Key;

                        if (exchangePair.Spread > 15) {
                            message += $"{count + 1}) {exchangePair.Pair}       {exchangePair.Spread}%\n" +
                                $"{exchangePair.StockExchangeSeller} ({exchangePair.PurchasePrice}) -> " +
                                $"{exchangePair.StockExchangeBuyer} ({exchangePair.SellPrice}) \n";
                            count++;
                        }
                    }
                }

                foreach (KeyValuePair<ExchangePair, DateTime> kvp in TimeService.TimeCrossesByMarket) {
                    if (kvp.Value == maxDateTimeCrossesByMarket && count < 30) {
                        ExchangePair exchangePair = kvp.Key;
                        
                        if (exchangePair.Spread > (decimal)0.00001) {
                            message += $"{count + 1})  -> {exchangePair.PurchasePath}  <- {exchangePair.SellPath}\n" +
                                $"{exchangePair.Market}      ({exchangePair.Spread})%\n";
                            count++;
                        }
                    }
                }

                if (message == "") {
                    Thread.Sleep(2000);
                    continue;
                } else {
                    string channelId = BotSettings.ChannelId;
                    _client.SendTextMessageAsync(channelId, message);

                    Thread.Sleep(2000);
                }
            }
        }
        
        
    }
}
