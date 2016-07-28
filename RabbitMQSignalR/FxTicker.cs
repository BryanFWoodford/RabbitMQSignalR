using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using RabbitMQ.Client;
using RabbitMQ.Client.MessagePatterns;
using RabbitMQSignalR.Models;
using IConnection = RabbitMQ.Client.IConnection;

namespace RabbitMQSignalR
{
    internal sealed class FxTicker
    {
        private static IConnection Connection { get; set; }
        private static IModel Model { get; set; }
        private static string QueueName { get; set; }
        private static Subscription Subscription { get; set; }

        private readonly static Lazy<FxTicker> _instance = new Lazy<FxTicker>(() => new FxTicker(GlobalHost.ConnectionManager.GetHubContext<FxTickerHub>().Clients));

        private readonly ConcurrentDictionary<string, CurrencyPair> _prices = new ConcurrentDictionary<string, CurrencyPair>();

        private FxTicker(IHubConnectionContext clients)
        {
            Clients = clients;
            _prices.Clear();
            RabbitMq.CurrencyList.ForEach(currencyPair => _prices.TryAdd(currencyPair.Key, currencyPair));

            CreateConnection();
        }

        public static void CreateConnection()
        {
            if (Connection == null)
                Connection = RabbitMq.GetConnection();
            if (Model == null)
                Model = RabbitMq.GetModel(Connection);
            if (string.IsNullOrEmpty(QueueName))
                QueueName = RabbitMq.GetQueue(Model);
            if (Subscription == null)
                Subscription = RabbitMq.GetSubscription(Model, QueueName);
        }

        public static FxTicker Instance
        {
            get { return _instance.Value; }
        }

        private IHubConnectionContext Clients
        {
            get;
            set;
        }

        public IEnumerable<CurrencyPair> GetAllPrices()
        {
            return _prices.Values;
        }
    }
}