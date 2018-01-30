using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using CryptoAnalysatorWebApp.Models.Common;
using CryptoAnalysatorWebApp.Models;

    
namespace CryptoAnalysatorWebApp.Controllers
{
    [Route("api/[controller]")]
    public class ActualPairsController : Controller {
        private ExmoMarket _exmoMarket;
        private PoloniexMarket _poloniexMarket;
        private BittrexMarket _bittrexMarket;
        private BinanceMarket _binanceMarket;
        private PairsAnalysator _pairsAnalysator;

        public ActualPairsController(PoloniexMarket poloniexMarket, BittrexMarket bittrexMarket, ExmoMarket exmoMarket, PairsAnalysator pairsAnalysator, BinanceMarket binanceMarket) {
            Console.WriteLine("HELLO FROM Controller");
            _exmoMarket = exmoMarket;
            _poloniexMarket = poloniexMarket;
            _bittrexMarket = bittrexMarket;
            _pairsAnalysator = pairsAnalysator;
            _binanceMarket = binanceMarket;
        }

        // GET api/actualpairs
        [HttpGet]
        [Produces("application/json")]
        public IActionResult Get() {

            BasicCryptoMarket[] marketsArray = { _poloniexMarket, _bittrexMarket, _exmoMarket, _binanceMarket};
            _pairsAnalysator.FindActualPairsAndCrossRates(marketsArray, "contr");

            Dictionary<string, List<ExchangePair>> pairsDic = new Dictionary<string, List<ExchangePair>>();
            pairsDic["crosses"] = _pairsAnalysator.CrossPairs.OrderByDescending(p => p.Spread).ToList();
            pairsDic["pairs"] = _pairsAnalysator.ActualPairs.OrderByDescending(p => p.Spread).ToList();
            pairsDic["crossesbymarket"] = _pairsAnalysator.CrossRatesByMarket.OrderByDescending(p => p.Spread).ToList();

            return Ok(pairsDic);
        }

        //GET api/actualpairs/btc-ltc?seller=poloniex&buyer=bittrex&isCross=false
        [HttpGet("{curPair}")]
        [Produces("application/json")]  
        public IActionResult Get(string curPair, [FromQuery]string seller, [FromQuery]string buyer, [FromQuery]bool isCross) {

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
                            resPurchasePrice = _poloniexMarket.LoadOrder(curPair.ToUpper(), true);
                            break;
                        case "bittrex":
                            resPurchasePrice = _bittrexMarket.LoadOrder(curPair.ToUpper(), true);
                            break;
                        case "exmo":
                            resPurchasePrice = _exmoMarket.LoadOrder(curPair.ToUpper(), true);
                            break;
                        case "binance":
                            resPurchasePrice = _binanceMarket.LoadOrder(curPair.ToUpper(), true);
                            break;
                    }
                    switch (buyer) {
                        case "poloniex":
                            resSellPrice = _poloniexMarket.LoadOrder(curPair.ToUpper(), false);
                            break;
                        case "bittrex":
                            resSellPrice = _bittrexMarket.LoadOrder(curPair.ToUpper(), false);
                            break;
                        case "exmo":
                            resSellPrice = _exmoMarket.LoadOrder(curPair.ToUpper(), false);
                            break;
                        case "binance":
                            resSellPrice = _binanceMarket.LoadOrder(curPair.ToUpper(), false);
                            break;
                    }
                } else {
                    string[] devidedPairs = curPair.Split('-');
                    switch (seller) {
                        case "poloniex":
                            resPurchasePrice = _poloniexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", true) /
                                _poloniexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", false);
                            break;
                        case "bittrex":
                            resPurchasePrice = _bittrexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", true) /
                                _bittrexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", false);
                            break;
                        case "exmo":
                            resPurchasePrice = _exmoMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", true) /
                                _exmoMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", false);
                            break;
                        case "binance":
                            resPurchasePrice = _binanceMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", true) /
                                _binanceMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", false);
                            break;
                    }
                    switch (buyer) {
                        case "poloniex":
                            resSellPrice = _poloniexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", false) /
                                _poloniexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", true);
                            break;
                        case "bittrex":
                            resSellPrice = _bittrexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", false) /
                                _bittrexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", true);
                            break;
                        case "exmo":
                            resSellPrice = _exmoMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", false) /
                                _exmoMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", true);
                            break;
                        case "binance":
                            resSellPrice = _binanceMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", false) /
                                _binanceMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", true);
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
                return Ok(resDic);
            }

        }

        // GET /api/actualpairs/crossMarket/poloniex?purchasepath=btc-ltc&sellpath=btc-eth-ltc
        [HttpGet("crossMarket/{market}")]
        [Produces("application/json")]
        public IActionResult GetCrossByMarketRelevance (string market, [FromQuery]string purchasepath, [FromQuery]string sellpath) {
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
            Console.WriteLine($" {market}  {purchasepath}  {sellpath}");
            try {
                string[] devidedPurchasePath = purchasepath.ToUpper().Split('-').ToArray();
                switch (market.ToLower()) {
                    case "poloniex":
                        for (int i = 0; i <= devidedPurchasePath.Length - 2; i++) {
                            resPurchasePrice *= _poloniexMarket.LoadOrder($"{devidedPurchasePath[i]}-{devidedPurchasePath[i + 1]}", true);
                        }
                        break;
                    case "bittrex":
                        for (int i = 0; i <= devidedPurchasePath.Length - 2; i++) {
                            resPurchasePrice *= _bittrexMarket.LoadOrder($"{devidedPurchasePath[i]}-{devidedPurchasePath[i + 1]}", true);
                        }
                        break;
                    case "exmo":
                        for (int i = 0; i <= devidedPurchasePath.Length - 2; i++) {
                            resPurchasePrice *= _exmoMarket.LoadOrder($"{devidedPurchasePath[i]}-{devidedPurchasePath[i + 1]}", true);
                        }
                        break;
                    case "binance":
                        for (int i = 0; i <= devidedPurchasePath.Length - 2; i++) {
                            resPurchasePrice *= _binanceMarket.LoadOrder($"{devidedPurchasePath[i]}-{devidedPurchasePath[i + 1]}", true);
                        }
                        break;
                }
                string[] devidedSellPath = sellpath.ToUpper().Split('-').ToArray();
                switch (market.ToLower()) {
                    case "poloniex":
                        for (int i = devidedPurchasePath.Length - 1; i > 0; i--) {
                            Console.WriteLine("GO ON");
                            resSellPrice *= _poloniexMarket.LoadOrder($"{devidedPurchasePath[i - 1]}-{devidedPurchasePath[i]}", false, false);
                        }
                        break;
                    case "bittrex":
                        for (int i = devidedPurchasePath.Length - 1; i > 0; i--) {
                            Console.WriteLine("GO ON");
                            resSellPrice *= _binanceMarket.LoadOrder($"{devidedPurchasePath[i - 1]}-{devidedPurchasePath[i]}", false, false);
                        }
                        break;
                    case "exmo":
                        for (int i = devidedPurchasePath.Length - 1; i > 0; i--) {
                            Console.WriteLine("GO ON");
                            resSellPrice *= _exmoMarket.LoadOrder($"{devidedPurchasePath[i - 1]}-{devidedPurchasePath[i]}", false, false);
                        }
                        break;
                    case "binance":
                        for (int i = devidedPurchasePath.Length - 1; i > 0; i--) {
                            Console.WriteLine("GO ON");
                            resSellPrice *= _binanceMarket.LoadOrder($"{devidedPurchasePath[i - 1]}-{devidedPurchasePath[i]}", false, false);
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
