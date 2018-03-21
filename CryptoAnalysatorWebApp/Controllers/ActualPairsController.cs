using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using CryptoAnalysatorWebApp.Models.Common;
using CryptoAnalysatorWebApp.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using System.Threading.Tasks;

namespace CryptoAnalysatorWebApp.Controllers {
    [Route("api/[controller]")]
    public class ActualPairsController : Controller {
        private readonly ExmoMarket _exmoMarket;
        private readonly PoloniexMarket _poloniexMarket;
        private readonly BittrexMarket _bittrexMarket;
        private readonly BinanceMarket _binanceMarket;
        private readonly PairsAnalysator _pairsAnalysator;
        private readonly LivecoinMarket _livecoinMarket;

        public ActualPairsController(PoloniexMarket poloniexMarket, BittrexMarket bittrexMarket, ExmoMarket exmoMarket, PairsAnalysator pairsAnalysator, BinanceMarket binanceMarket, LivecoinMarket livecoinMarket) {
            Console.WriteLine("HELLO FROM Controller");
            _exmoMarket = exmoMarket;
            _poloniexMarket = poloniexMarket;
            _bittrexMarket = bittrexMarket;
            _pairsAnalysator = pairsAnalysator;
            _binanceMarket = binanceMarket;
            _livecoinMarket = livecoinMarket;
        }

        // GET api/actualpairs
        [HttpGet]
        [Produces("application/json")]
        public async Task<IActionResult> Get() {

            BasicCryptoMarket[] marketsArray = { _poloniexMarket, _bittrexMarket, _exmoMarket, _binanceMarket, _livecoinMarket};
            await _pairsAnalysator.FindActualPairsAndCrossRates(marketsArray, "contr");

            Dictionary<string, List<ExchangePair>> pairsDic = new Dictionary<string, List<ExchangePair>>();
            pairsDic["crosses"] = _pairsAnalysator.CrossPairs.OrderByDescending(p => p.Spread).ToList();
            pairsDic["pairs"] = _pairsAnalysator.ActualPairs.OrderByDescending(p => p.Spread).ToList();
            pairsDic["crossesbymarket"] = _pairsAnalysator.CrossRatesByMarket.OrderByDescending(p => p.Spread).ToList();

            return Ok(pairsDic);
        }

        //GET api/actualpairs/btc-ltc?seller=poloniex&buyer=bittrex&isCross=false
        [HttpGet("{curPair}")]
        [Produces("application/json")]  
        public async Task<IActionResult> Get(string curPair, [FromQuery]string seller, [FromQuery]string buyer, [FromQuery]bool isCross) {

            decimal resPurchasePrice = 0;
            decimal resSellPrice = 0;
            decimal newSpread = 0;
            ExchangePair exchangePair;

            Dictionary<string, string> resDic = new Dictionary<string, string>();
            exchangePair = TimeService.GetPairOrCross(curPair.ToUpper(), seller, buyer, isCross);
            if (exchangePair == null) {
                Console.WriteLine("NUUUUULLLL");
                resDic["result"] = "Not actual";
                resDic["purchasePrice"] = $"{resPurchasePrice}";
                resDic["sellPrice"] = $"{resSellPrice}";
                return Ok(resDic);
            }


            try {
                if (!isCross) {
                    switch (seller) {
                        case "poloniex":
                            resPurchasePrice = await _poloniexMarket.LoadOrder(curPair.ToUpper(), true);
                            break;
                        case "bittrex":
                            resPurchasePrice = await _bittrexMarket.LoadOrder(curPair.ToUpper(), true);
                            break;
                        case "exmo":
                            resPurchasePrice = await _exmoMarket.LoadOrder(curPair.ToUpper(), true);
                            break;
                        case "binance":
                            resPurchasePrice = await _binanceMarket.LoadOrder(curPair.ToUpper(), true);
                            break;
                        case "livecoin":
                            resPurchasePrice = await _livecoinMarket.LoadOrder(curPair.ToUpper(), true);
                            break;
                    }
                    switch (buyer) {
                        case "poloniex":
                            resSellPrice = await _poloniexMarket.LoadOrder(curPair.ToUpper(), false);
                            break;
                        case "bittrex":
                            resSellPrice = await _bittrexMarket.LoadOrder(curPair.ToUpper(), false);
                            break;
                        case "exmo":
                            resSellPrice = await _exmoMarket.LoadOrder(curPair.ToUpper(), false);
                            break;
                        case "binance":
                            resSellPrice = await _binanceMarket.LoadOrder(curPair.ToUpper(), false);
                            break;
                        case "livecoin":
                            resSellPrice = await _livecoinMarket.LoadOrder(curPair.ToUpper(), false);
                            break;
                    }
                } else {
                    string[] devidedPairs = curPair.Split('-');
                    switch (seller) {
                        case "poloniex":
                            resPurchasePrice = await _poloniexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", true) /
                                               await _poloniexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", false);
                            break;
                        case "bittrex":
                            resPurchasePrice = await _bittrexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", true) /
                                               await _bittrexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", false);
                            break;
                        case "exmo":
                            resPurchasePrice = await _exmoMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", true) /
                                               await _exmoMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", false);
                            break;
                        case "binance":
                            resPurchasePrice = await _binanceMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", true) /
                                               await _binanceMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", false);
                            break;
                        case "livecoin":
                            resPurchasePrice = await _livecoinMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", true) /
                                               await _livecoinMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", false);
                            break;
                    }
                    switch (buyer) {
                        case "poloniex":
                            resSellPrice = await _poloniexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", false) /
                                           await _poloniexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", true);
                            break;
                        case "bittrex":
                            resSellPrice = await _bittrexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", false) /
                                           await _bittrexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", true);
                            break;
                        case "exmo":
                            resSellPrice = await _exmoMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", false) /
                                           await _exmoMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", true);
                            break;
                        case "binance":
                            resSellPrice = await _binanceMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", false) /
                                           await _binanceMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", true);
                            break;
                        case "livecoin":
                            resSellPrice = await _livecoinMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", false) /
                                           await _livecoinMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", true);
                            break;
                    }
                }

                newSpread = Math.Round((resSellPrice - resPurchasePrice) / resPurchasePrice * 100, 4);
            } catch (Exception e) {
                Console.WriteLine("Loading order failed");
                Console.WriteLine(e.Message);
            }

            bool pricesAreOk = newSpread > 0 ? true : false;

            if (pricesAreOk) {
                resDic["result"] = "Ok";
                if (!isCross) {
                    resDic["time"] = $"{(DateTime.Now - TimeService.GetPairTimeUpd(exchangePair))}";
                } else {
                    resDic["time"] = $"{(DateTime.Now - TimeService.GetCrossTimeUpd(exchangePair))}";
                }
                resDic["purchasePrice"] = $"{Math.Round(resPurchasePrice, 11)}";
                resDic["sellPrice"] = $"{Math.Round(resSellPrice, 11)}";
                resDic["spread"] = Math.Round((resSellPrice - resPurchasePrice) / resPurchasePrice * 100, 4).ToString();
                return Ok(resDic);
            } else {
                resDic["result"] = "Not actual (probably no orders)";
                resDic["purchasePrice"] = $"{Math.Round(resPurchasePrice, 11)}";
                resDic["sellPrice"] = $"{Math.Round(resSellPrice, 11)}";
                return Ok(resDic);
            }

        }

        // GET /api/actualpairs/crossMarket/poloniex?purchasepath=btc-ltc&sellpath=btc-eth-ltc
        [HttpGet("crossMarket/{market}")]
        [Produces("application/json")]
        public async Task<IActionResult> GetCrossByMarketRelevance (string market, [FromQuery]string purchasepath, [FromQuery]string sellpath) {
            decimal resPurchasePrice = 1;
            decimal resSellPrice = 1;
            decimal newSpread = 0;
            ExchangePair exchangePair;

            Dictionary<string, string> resDic = new Dictionary<string, string>();
            exchangePair = TimeService.GetCrossByMarket(market, purchasepath, sellpath);
            if (exchangePair == null) {
                resDic["result"] = "Not actual";
                resDic["purchasePrice"] = $"{resPurchasePrice}";
                resDic["sellPrice"] = $"{resSellPrice}";
                return Ok(resDic);
            }
            try {
                string[] devidedPurchasePath = purchasepath.ToUpper().Split('-').ToArray();
                switch (market.ToLower()) {
                    case "poloniex":
                        for (int i = 0; i <= devidedPurchasePath.Length - 2; i++) {
                            resPurchasePrice *= await _poloniexMarket.LoadOrder($"{devidedPurchasePath[i]}-{devidedPurchasePath[i + 1]}", true);
                        }
                        break;
                    case "bittrex":
                        for (int i = 0; i <= devidedPurchasePath.Length - 2; i++) {
                            resPurchasePrice *= await _bittrexMarket.LoadOrder($"{devidedPurchasePath[i]}-{devidedPurchasePath[i + 1]}", true);
                        }
                        break;
                    case "exmo":
                        for (int i = 0; i <= devidedPurchasePath.Length - 2; i++) {
                            resPurchasePrice *= await _exmoMarket.LoadOrder($"{devidedPurchasePath[i]}-{devidedPurchasePath[i + 1]}", true);
                        }
                        break;
                    case "binance":
                        for (int i = 0; i <= devidedPurchasePath.Length - 2; i++) {
                            resPurchasePrice *= await _binanceMarket.LoadOrder($"{devidedPurchasePath[i]}-{devidedPurchasePath[i + 1]}", true);
                        }
                        break;
                    case "livecoin":
                        for (int i = 0; i <= devidedPurchasePath.Length - 2; i++) {
                            decimal newOrder = await _livecoinMarket.LoadOrder($"{devidedPurchasePath[i]}-{devidedPurchasePath[i + 1]}", true);
                            resPurchasePrice *= newOrder;
                            /*using (StreamWriter sw = System.IO.File.AppendText("..\\ControllerCheckPurchase.txt")) {
                                sw.WriteLine($"{devidedPurchasePath[i]}-{devidedPurchasePath[i + 1]}  {newOrder}");
                            }*/
                        }
                        break;
                     
                }
                string[] devidedSellPath = sellpath.ToUpper().Split('-').ToArray();
                switch (market.ToLower()) {
                    case "poloniex":
                        for (int i = devidedSellPath.Length - 1; i > 0; i--) {
                            Console.WriteLine("GO ON");
                            resSellPrice *= await _poloniexMarket.LoadOrder($"{devidedSellPath[i - 1]}-{devidedSellPath[i]}", false, false);
                        }
                        break;
                    case "bittrex":
                        for (int i = devidedSellPath.Length - 1; i > 0; i--) {
                            Console.WriteLine("GO ON");
                            resSellPrice *= await _bittrexMarket.LoadOrder($"{devidedSellPath[i - 1]}-{devidedSellPath[i]}", false, false);
                        }
                        break;
                    case "exmo":
                        for (int i = devidedSellPath.Length - 1; i > 0; i--) {
                            Console.WriteLine("GO ON");
                            resSellPrice *= await _exmoMarket.LoadOrder($"{devidedSellPath[i - 1]}-{devidedSellPath[i]}", false, false);
                        }
                        break;
                    case "binance":
                        for (int i = devidedSellPath.Length - 1; i > 0; i--) {
                            Console.WriteLine("GO ON");
                            resSellPrice *= await _binanceMarket.LoadOrder($"{devidedSellPath[i - 1]}-{devidedSellPath[i]}", false, false);
                        }
                        break;
                    case "livecoin":
                        for (int i = devidedSellPath.Length - 1; i > 0; i--) {
                            decimal newOrder = await _livecoinMarket.LoadOrder($"{devidedSellPath[i - 1]}-{devidedSellPath[i]}", false, false);
                            resSellPrice *= newOrder;
                        }
                        break;
                }

                newSpread = Math.Round((resSellPrice - resPurchasePrice) / resPurchasePrice * 100, 4);
            } catch (Exception e) {
                Console.WriteLine("Loading order failed (crossByMarket)");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }

            bool pricesAreOk = newSpread > 0 ? true : false;
            if (pricesAreOk) {
                resDic["result"] = "Ok";
                resDic["time"] = $"{(DateTime.Now - TimeService.GetCrossByMarketTimeUpd(exchangePair))}";
                resDic["purchasePrice"] = $"{Math.Round(resPurchasePrice, 11)}";
                resDic["sellPrice"] = $"{Math.Round(resSellPrice, 11)}";
                resDic["spread"] = Math.Round((resSellPrice - resPurchasePrice) / resPurchasePrice * 100, 4).ToString();
                return Ok(resDic);
            } else {
                resDic["result"] = "Not actual (probably no orders)";
                resDic["purchasePrice"] = $"{Math.Round(resPurchasePrice, 11)}";
                resDic["sellPrice"] = $"{Math.Round(resSellPrice, 11)}";
                return Ok(resDic);
            }
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
