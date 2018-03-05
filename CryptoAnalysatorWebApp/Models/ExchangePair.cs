using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CryptoAnalysatorWebApp.Models
{
    public class ExchangePair {
        public string Pair { get; set; }
        public string StockExchangeSeller { get; set; }
        public string StockExchangeBuyer { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SellPrice { get; set; }
        public decimal Spread { get; set; }
        public bool IsCross { get; set; }
        public string PurchasePath { get; set; }
        public string SellPath { get; set; }
        public string Market { get; set; }
        public int InsertCounter { get; set; }
        public string DbOk { get; set; }
        public string TimeInserted { get; set; }
        [BsonId]
        public ObjectId Id { get; set; }
    }
}
