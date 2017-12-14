using System;
using System.Collections.Generic;
using CryptoAnalysatorWebApp.Models;

namespace CryptoAnalysatorWebApp.Interfaces
{
    public interface ICryptoMarket
    {
        List<ExchangePair> Pairs { get;  }
        List<ExchangePair> СrossRates { get; }

        void LoadPairs(string command);

        ExchangePair GetPairByName(string name);
        ExchangePair GetCrossByName(string name);
        void DeletePairByName(string name);
        void DeleteCrossByName(string name);
    }
}
