using System.Collections.Generic;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using RabbitMQSignalR.Models;

namespace RabbitMQSignalR
{
    [HubName("fxTickerMini")]
    internal sealed class FxTickerHub : Hub
    {
        private readonly FxTicker _fxTicker;

        public FxTickerHub() : this(FxTicker.Instance) { }

        public FxTickerHub(FxTicker fxTicker)
        {
            _fxTicker = fxTicker;
        }

        [HubMethodName("GetAllPrices")]
        public IEnumerable<CurrencyPair> GetAllPrices()
        {
            return _fxTicker.GetAllPrices();
        }
    }
}