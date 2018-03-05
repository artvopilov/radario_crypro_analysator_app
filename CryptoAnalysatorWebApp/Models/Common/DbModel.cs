using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoAnalysatorWebApp.Models.Common;
using MongoDB.Driver;
using MongoDB.Driver.Core;
using MongoDB.Bson;
using CryptoAnalysatorWebApp.Models;
using Microsoft.CodeAnalysis.CSharp;

namespace CryptoAnalysatorWebApp.Models.Common {
    public class DbModel {
        private IMongoCollection<ExchangePair> _collection;
        private int _currentDbId;

        public int CurrentDbId {
            get => _currentDbId;
            set => _currentDbId = value;
        }

        protected DbModel(string collectionName) {
            const string connectionString = "mongodb://localhost:27017";
            var client = new MongoClient(connectionString);
            IMongoDatabase bittrexCrossratesStatisticsDatabase = client.GetDatabase("BittrexCrossratesStatistics");
            _collection = bittrexCrossratesStatisticsDatabase.GetCollection<ExchangePair>(collectionName);
            _currentDbId = collectionName == "PairsAfterAnalysis" ? GenerateId(client, 0) : GenerateId(client);
            
        }

        private int GenerateId(MongoClient client, int idBias = 1) {
            List<ExchangePair> data = client.GetDatabase("BittrexCrossratesStatistics")
                .GetCollection<ExchangePair>("PairsBeforeAnalysis").Find(x => true)
                .Sort(Builders<ExchangePair>.Sort.Descending("InsertCounter")).Limit(1).ToList();
            return data[0].InsertCounter + idBias;
        }

        public async Task Insert(ExchangePair item) {
            item.InsertCounter = _currentDbId;
            await _collection.InsertOneAsync(item);
        }

        public async Task InsertMany(ExchangePair[] items) {
            foreach (var item in items) {
                item.InsertCounter = _currentDbId;
            }

            await _collection.InsertManyAsync(items);
        }

        public async Task<ExchangePair> UpdateDbOk(ExchangePair item, string update) {
            Console.WriteLine($"update: {update} Cross: item.SellPath {item.SellPath} item.PurchasePath {item.PurchasePath} item.InsertCounter {item.InsertCounter}");
            var filter = Builders<ExchangePair>.Filter.And(
                Builders<ExchangePair>.Filter.Eq(i => i.SellPath, item.SellPath),
                Builders<ExchangePair>.Filter.Eq(i => i.PurchasePath, item.PurchasePath),
                Builders<ExchangePair>.Filter.Eq(i => i.InsertCounter, item.InsertCounter));
            return await _collection.FindOneAndUpdateAsync(filter, Builders<ExchangePair>.Update.Set("DbOk", update));
        }
    }
}