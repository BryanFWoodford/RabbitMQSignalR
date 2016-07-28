namespace RabbitMQSignalR.Models
{
    internal struct CurrencyPair
    {
        internal string Key { get; set; }
        internal string Symbol { get; set; }
        internal string Currency { get; set; }
        internal decimal Bid { get; set; }
        internal decimal BidPriceChange { get; set; }
        internal decimal Ask { get; set; }
        internal decimal AskPriceChange { get; set; }
    }
}