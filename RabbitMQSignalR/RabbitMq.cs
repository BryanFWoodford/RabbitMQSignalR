using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Elmah;
using Microsoft.AspNet.SignalR.Hubs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.MessagePatterns;
using RabbitMQSignalR.Models;
using IConnection = RabbitMQ.Client.IConnection;

namespace RabbitMQSignalR
{
    internal sealed class RabbitMq
    {
        public static List<CurrencyPair> Feed = new List<CurrencyPair>();

        public static IConnection GetConnection()
        {
            var connectionFactory = new ConnectionFactory
            {
                HostName = ConfigurationManager.AppSettings["RabbitHostName"]
            };

            return connectionFactory.CreateConnection();
        }

        public static IModel GetModel(IConnection connection)
        {
            return connection.CreateModel();
        }

        public static string GetQueue(IModel model)
        {
            return CreateQueue(model);
        }

        public static Subscription GetSubscription(IModel model, string queueName)
        {
            return new Subscription(model, queueName, true);
        }

        public void ConnectAndReceiveMessages(IModel model, string queueName, Subscription subscription)
        {
            try
            {
                lock (model)
                {
                    using (subscription)
                    {
                        BlockingReceiveMessage(subscription);
                    }
                }
            }
            catch (Exception ex)
            {
                var error = new Error(ex);
                ErrorLog.GetDefault(null).Log(error);
            }
        }

        static string CreateQueue(IModel model)
        {
            try
            {
                String queuename = model.QueueDeclare();
                model.QueueBind(queuename, ConfigurationManager.AppSettings["RabbitExchange"], "RoutingKey");
                return queuename;
            }
            catch (Exception ex)
            {
                var error = new Error(ex);
                ErrorLog.GetDefault(null).Log(error);
                return String.Empty;
            }
        }

        private void BlockingReceiveMessage(Subscription fSubs)
        {
            try
            {
                int errorCount = 0;

                foreach (BasicDeliverEventArgs indexArgs in fSubs)
                {
                    try
                    {
                        OnUpdateMessage(Deserialize<Dictionary<String, String>>(indexArgs.Body));
                        fSubs.Ack();
                    }
                    catch (Exception ex)
                    {
                        if (errorCount % 100 == 0)
                        {
                            var error = new Error(ex)
                            {
                                Detail = "Error in the FX FEED Rabbit Message Queue foreach Subscription loop"
                            };

                            ErrorLog.GetDefault(null).Log(error);
                        }

                        errorCount++;
                    }
                }
            }
            catch (OperationInterruptedException ex)
            {
                var error = new Error(ex);
                ErrorLog.GetDefault(null).Log(error);
            }
        }

        public IHubConnectionContext Clients
        {
            get;
            set;
        }

        private readonly List<CurrencyPair> _previousItems = new List<CurrencyPair>();

        private void OnUpdateMessage(IReadOnlyDictionary<string, string> data)
        {
            var list = GetCurrencyPairList();

            // only select items from the pre-defined dictionary
            var currencyPairs = list as CurrencyPair[] ?? list.ToArray();
            if (!currencyPairs.Select(m => m.Key).Contains(data["FKEY"])) return;
            var fxItem = currencyPairs.Where(m => m.Key == data["FKEY"]).SingleOrDefault();

            fxItem.Ask = Convert.ToDecimal(data["ASK"]);
            fxItem.Bid = Convert.ToDecimal(data["BID"]);

            int index = Feed.FindIndex(m => m.Key == fxItem.Key);
            if (index >= 0)
            {
                var previousIndex = _previousItems.FindIndex(m => m.Key == fxItem.Key);

                if (previousIndex >= 0)
                {
                    var previousItem = _previousItems.SingleOrDefault(m => m.Key == fxItem.Key);
                    fxItem.BidPriceChange = fxItem.Bid - previousItem.Bid;
                    fxItem.AskPriceChange = fxItem.Ask - previousItem.Ask;

                    // we're done with the old price, so remove it.
                    _previousItems.Remove(previousItem);
                }

                // add the previous price to list
                _previousItems.Add(fxItem);
            }
            else
                Feed.Add(fxItem);

            Clients.All.updateFxPrice(fxItem);
        }

        private static T Deserialize<T>(byte[] data)
        {
            using (var m = new MemoryStream(data, false))
            {
                return (T)(new BinaryFormatter()).Deserialize(m);
            }
        }

        public static List<CurrencyPair> CurrencyList
        {
            get
            {
                return GetCurrencyPairList().Select(item => new CurrencyPair
                {
                    Ask = (decimal)0.0,
                    Bid = (decimal)0.0,
                    Key = item.Key,
                    Symbol = item.Symbol
                }).ToList();
            }
        }

        private static List<CurrencyPair> _list = new List<CurrencyPair>();

        private static IEnumerable<CurrencyPair> GetCurrencyPairList()
        {
            if (_list.Count == 0)
            {
                _list = new List<CurrencyPair>
                {
                    new CurrencyPair {Key = "150852", Symbol = "GBP/USD", Currency = "USD"},
                    new CurrencyPair {Key = "150853", Symbol = "EUR/USD", Currency = "USD"},
                    new CurrencyPair {Key = "150864", Symbol = "EUR/GBP", Currency = "USD"}
                };
            }

            return _list;
        }
    }
}