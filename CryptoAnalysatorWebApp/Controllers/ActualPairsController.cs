using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CryptoAnalysatorWebApp.Models.Common;
using CryptoAnalysatorWebApp.Models;
using CryptoAnalysatorWebApp.Interfaces;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using CryptoAnalysatorWebApp.TelegramBot;
using CryptoAnalysatorWebApp.TelegramBot.Commands;
    
namespace CryptoAnalysatorWebApp.Controllers
{
    [Route("api/[controller]")]
    public class ActualPairsController : Controller {
        private ExmoMarket _exmoMarket;
        private PoloniexMarket _poloniexMarket;
        private BittrexMarket _bittrexMarket;
        private PairsAnalysator _pairsAnalysator;

        public ActualPairsController(ExmoMarket exmoMarket, PoloniexMarket poloniexMarket, BittrexMarket bittrexMarket, PairsAnalysator pairsAnalysator) {
            _exmoMarket = exmoMarket;
            _poloniexMarket = poloniexMarket;
            _bittrexMarket = bittrexMarket;
            _pairsAnalysator = pairsAnalysator;
        }

        // GET api/actualpairs
        [HttpGet]
        [Produces("application/json")]
        public IActionResult Get() {

            BasicCryptoMarket[] marketsArray = { _poloniexMarket, _bittrexMarket , _exmoMarket };

            _pairsAnalysator.FindActualPairsAndCrossRates(marketsArray);

            Dictionary<string, List<ExchangePair>> pairsDic = new Dictionary<string, List<ExchangePair>>();
            pairsDic["crosses"] = _pairsAnalysator.CrossPairs.OrderByDescending(p => p.Spread).ToList();
            pairsDic["pairs"] = _pairsAnalysator.ActualPairs.OrderByDescending(p => p.Spread).ToList();

            TimeService.StoreTime(DateTime.Now, pairsDic["pairs"].ToList(), pairsDic["crosses"].ToList());

            return Ok(pairsDic);
        }

        //GET api/actualpairs/btc-ltc?seller=poloniex&buyer=bittrex&isCross=false
        [HttpGet("{curPair}")]
        [Produces("application/json")]
        public IActionResult Get(string curPair, [FromQuery]string seller, [FromQuery]string buyer, [FromQuery]bool isCross) {

            decimal resPurchasePrice = 0;
            decimal resSellPrice = 0;
            ExchangePair exchangePair;

            if (!isCross) {
                exchangePair = _pairsAnalysator.ActualPairs.Find(pair => pair.Pair == curPair.ToUpper());

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
                }
            } else {
                exchangePair = _pairsAnalysator.CrossPairs.Find(pair => pair.Pair == curPair.ToUpper());
                string[] devidedPairs = curPair.Split('-');
                switch (seller) {
                    case "poloniex":
                        resPurchasePrice = _poloniexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", true) /
                            _poloniexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", false);
                        ;
                        break;
                    case "bittrex":
                        resPurchasePrice = _bittrexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", true) /
                            _poloniexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", false);
                        break;
                    case "exmo":
                        resPurchasePrice = _exmoMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", true) /
                            _poloniexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", false);
                        break;
                }
                switch (buyer) {
                    case "poloniex":
                        resSellPrice = _poloniexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", false) /
                            _poloniexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", true);
                        break;
                    case "bittrex":
                        resSellPrice = _bittrexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", false) /
                            _poloniexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", true);
                        break;
                    case "exmo":
                        resSellPrice = _exmoMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[2]}", false) /
                            _poloniexMarket.LoadOrder($"{devidedPairs[1]}-{devidedPairs[0]}", true);
                        break;
                }
            }

            Dictionary<string, string> resDic = new Dictionary<string, string>();

            if (exchangePair == null) {
                resDic["result"] = "Not actual";
                resDic["purchasePrice"] = $"{resPurchasePrice}";
                resDic["sellPrice"] = $"{resSellPrice}";
                return Ok(resDic);
            }

            bool pricesAreOk = true; // ((resSellPrice  >= exchangePair.SellPrice) && (resPurchasePrice <= exchangePair.PurchasePrice)) ? true : false;

            if (pricesAreOk) {
                resDic["result"] = "Ok";
                if (!isCross) {
                    resDic["time"] = $"{(DateTime.Now.TimeOfDay - TimeService.GetPairTimeUpd(exchangePair).TimeOfDay).TotalSeconds}";
                } else {
                    resDic["time"] = $"{(DateTime.Now.TimeOfDay - TimeService.GetCrossTimeUpd(exchangePair).TimeOfDay).TotalSeconds}";
                }
                resDic["purchasePrice"] = $"{exchangePair.PurchasePrice}";
                resDic["sellPrice"] = $"{exchangePair.SellPrice}";
                resDic["spread"] = exchangePair.Spread.ToString();
                return Ok(resDic);
            } else {
                resDic["result"] = "Not actual";
                resDic["purchasePrice"] = $"{resPurchasePrice}";
                resDic["sellPrice"] = $"{resSellPrice}";
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
