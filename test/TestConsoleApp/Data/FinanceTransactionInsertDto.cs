using System;

namespace BlueWest.Data
{
    
    public partial struct FinanceTransactionInsertDto
    {
        public readonly int UserId;
        public readonly FinanceTransactionType FinanceTransactionType;
        public readonly FinanceSymbol FinanceSymbol;
        public readonly double Amount; // To Buy
        public readonly double Quantity; // Bought
        public readonly double Fee;
        public readonly DateTime DateTime;
    }
}