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

namespace CryptoAnalysatorWebApp.Controllers
{
    [Route("api/[controller]")]
    public class ActualPairsController : Controller
    {
        private ExmoMarket _exmoMarket;
        private PoloniexMarket _poloniexMarket;
        private BittrexMarket _bittrexMarket;
        private PairsAnalysator _pairsAnalysator;
        private TimeService _timeService;

        public ActualPairsController(ExmoMarket exmoMarket, PoloniexMarket poloniexMarket, BittrexMarket bittrexMarket, PairsAnalysator pairsAnalysator, TimeService timeService) {
            _exmoMarket = exmoMarket;
            _poloniexMarket = poloniexMarket;
            _bittrexMarket = bittrexMarket;
            _pairsAnalysator = pairsAnalysator;
            _timeService = timeService;
        }

        // GET api/actualpairs
        [HttpGet]
        [Produces("application/json")]
        public IActionResult Get()
        {
            BasicCryptoMarket[] marketsArray = { _poloniexMarket, _bittrexMarket, _exmoMarket };

            _pairsAnalysator.FindActualPairsAndCrossRates(marketsArray);

            Dictionary<string, List<ExchangePair>> pairsDic = new Dictionary<string, List<ExchangePair>>();
            pairsDic["crosses"] = _pairsAnalysator.CrossPairs.OrderByDescending(p => p.Spread).ToList();
            pairsDic["pairs"] = _pairsAnalysator.ActualPairs.OrderByDescending(p => p.Spread).ToList();

            _timeService.StoreTime(DateTime.Now.TimeOfDay);

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
                switch (seller) {
                    case "poloniex":
                        resPurchasePrice = _poloniexMarket.LoadOrder($"USDT-{curPair.ToUpper().Substring(curPair.IndexOf('-') + 1)}", true) /
                            _poloniexMarket.LoadOrder($"USDT-{curPair.ToUpper().Substring(0, curPair.IndexOf('-'))}", false);
                            ;
                        break;
                    case "bittrex":
                        resPurchasePrice = _bittrexMarket.LoadOrder($"USDT-{curPair.ToUpper().Substring(curPair.IndexOf('-') + 1)}", true) /
                            _bittrexMarket.LoadOrder($"USDT-{curPair.ToUpper().Substring(0, curPair.IndexOf('-'))}", false);
                        break;
                    case "exmo":
                        resPurchasePrice = _exmoMarket.LoadOrder($"USDT-{curPair.ToUpper().Substring(curPair.IndexOf('-') + 1)}", true) /
                            _exmoMarket.LoadOrder($"USDT-{curPair.ToUpper().Substring(0, curPair.IndexOf('-'))}", false);
                        break;
                }
                switch (buyer) {
                    case "poloniex":
                        resSellPrice = _poloniexMarket.LoadOrder($"USDT-{curPair.ToUpper().Substring(curPair.IndexOf('-') + 1)}", false) /
                            _poloniexMarket.LoadOrder($"USDT-{curPair.ToUpper().Substring(0, curPair.IndexOf('-'))}", true);
                        break;
                    case "bittrex":
                        resSellPrice = _bittrexMarket.LoadOrder($"USDT-{curPair.ToUpper().Substring(curPair.IndexOf('-') + 1)}", false) /
                            _bittrexMarket.LoadOrder($"USDT-{curPair.ToUpper().Substring(0, curPair.IndexOf('-'))}", true);
                        break;
                    case "exmo":
                        resSellPrice = _exmoMarket.LoadOrder($"USDT-{curPair.ToUpper().Substring(curPair.IndexOf('-') + 1)}", false) /
                            _exmoMarket.LoadOrder($"USDT-{curPair.ToUpper().Substring(0, curPair.IndexOf('-'))}", true);
                        break;
                }
            }

            bool pricesAreOk = resSellPrice >= exchangePair.SellPrice && resPurchasePrice <= exchangePair.PurchasePrice ? true : false;
            Dictionary<string, string> resDic = new Dictionary<string, string>();

            if (pricesAreOk) {
                resDic["result"] = "Ok";
                resDic["time"] = $"{(DateTime.Now.TimeOfDay - _timeService.TimeUpdated).TotalSeconds}";
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
