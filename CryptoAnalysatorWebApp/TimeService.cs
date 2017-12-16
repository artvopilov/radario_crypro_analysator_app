using System;

namespace CryptoAnalysatorWebApp
{
    public class TimeService
    {
        private TimeSpan _timeUpdated;

        public TimeSpan TimeUpdated { get => _timeUpdated; }

        public void StoreTime(TimeSpan curTime) {
            _timeUpdated = curTime;
        }
    }
}
