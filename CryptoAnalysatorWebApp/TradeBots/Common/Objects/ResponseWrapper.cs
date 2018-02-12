using Newtonsoft.Json.Linq;

namespace CryptoAnalysatorWebApp.TradeBots.Common.Objects {
    public class ResponseWrapper {
        public bool Success { get; set; }
        public string Message { get; set; }
        public JToken Result { get; set; }
    }
}