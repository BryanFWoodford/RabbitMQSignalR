namespace RabbitMQSignalR.Models
{
    public struct CurrencyPair
    {
        public string Key { get; set; }
        public string Symbol { get; set; }
        public string Currency { get; set; }
        public decimal Bid { get; set; }
        public decimal BidPriceChange { get; set; }
        public decimal Ask { get; set; }
        public decimal AskPriceChange { get; set; }
    }
}