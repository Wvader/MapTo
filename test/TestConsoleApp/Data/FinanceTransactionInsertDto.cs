using System;

namespace BlueWest.Data
{
    
    public partial struct FinanceTransactionInsertDto
    {
        public int UserId { get; set; }
        public FinanceTransactionType FinanceTransactionType { get; }
        public FinanceSymbol FinanceSymbol { get; }
        public double Amount { get; } // To Buy
        public double Quantity { get; } // Bought
        public double Fee { get; }
        public DateTime DateTime { get; }
    }
}