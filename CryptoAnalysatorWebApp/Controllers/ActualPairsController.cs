using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CryptoAnalysatorWebApp.Models.Common;
using CryptoAnalysatorWebApp.Models;
using CryptoAnalysatorWebApp.Interfaces;

namespace CryptoAnalysatorWebApp.Controllers
{
    [Route("api/[controller]")]
    public class ActualPairsController : Controller
    {
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

        // GET api/values
        [HttpGet]
        [Produces("application/json")]
        public IActionResult Get()
        {
            BasicCryptoMarket[] marketsArray = { _poloniexMarket, _bittrexMarket, _exmoMarket };

            _pairsAnalysator.FindActualPairsAndCrossRates(marketsArray);

            Dictionary<string, List<ExchangePair>> pairsDic = new Dictionary<string, List<ExchangePair>>();
            pairsDic["crosses"] = _pairsAnalysator.CrossPairs;
            pairsDic["pairs"] = _pairsAnalysator.ActualPairs;

            return Ok(pairsDic);
        }

        // GET api/values/5
        [HttpGet("{curPair}")]
        [Produces("application/json")]
        public string Get(string curPair, [FromQuery]string seller, [FromQuery]string buyer, [FromQuery]bool isCross)
        {
            

            return $"{curPair.ToUpper()} {seller} {buyer}";
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
