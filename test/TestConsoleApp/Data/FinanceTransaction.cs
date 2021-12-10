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

    public partial struct FinanceTransaction
    {
       public int Id { get;  }
       public int UserId { get; set; }
       public FinanceTransactionType FinanceTransactionType { get; }
       public FinanceSymbol FinanceSymbol { get; }
       public double Amount { get; } // To Buy
       public double Quantity { get; } // Bought
       public double Fee { get; }
       public DateTime DateTime { get; }


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