using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoAnalysatorWebApp.Models;

namespace CryptoAnalysatorWebApp.Interfaces
{
    public interface ICryptoMarket
    {
        Dictionary<string, ExchangePair> Pairs { get;  }
        Dictionary<string, ExchangePair> Crosses { get; }

        Task LoadPairs(string command);

        ExchangePair GetPairByName(string name);
        ExchangePair GetCrossByName(string name);
        void DeletePairByName(string name);
        void DeleteCrossByName(string name);
    }
}
