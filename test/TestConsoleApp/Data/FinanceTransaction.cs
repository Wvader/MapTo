using System;
using MapTo;

namespace BlueWest.Data
{
    public enum FinanceSymbol
    {
        BTC_EUR,
        BTC_BUSD,
        BTC_USD,
        BTC_USDT,
        LTC_EUR,
        LTC_BUSD,
        LTC_USDT









    }

    public enum FinanceTransactionType
    {
        Buy,
        Sell
    }

    [JsonExtension]
    [MapFrom(typeof(FinanceTransactionInsertDto))]
    public partial struct FinanceTransaction
    {
       public readonly int  Id;
       public readonly int UserId;
        public readonly FinanceTransactionType FinanceTransactionType;
        public readonly FinanceSymbol FinanceSymbol;
        public readonly double Amount; // To Buy
        public readonly double Quantity; // Bought
        public readonly double Fee;
        public readonly DateTime DateTime;


        public FinanceTransaction(int id, int userId, FinanceTransactionType financeTransactionType,
            FinanceSymbol financeSymbol, double amount, double quantity, double fee, DateTime dateTime)
        {
            Id = id;
            UserId = userId;
            FinanceTransactionType = financeTransactionType;
            FinanceSymbol = financeSymbol;
            Amount = amount;
            Quantity = quantity;
            Fee = fee;
            DateTime = dateTime;
        }
    }
}