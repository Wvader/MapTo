using System;
using System.Collections.Generic;
using System.Text;
using MapTo;

namespace BlueWest.Data
{
    [MapFrom(typeof(FinanceTransaction))]

     partial struct FinanceTransactionReadDto
    {
        public readonly int UserId;
        public readonly FinanceTransactionType FinanceTransactionType;
        public readonly FinanceSymbol FinanceSymbol;
        public readonly double Amount; // To Buy
        public readonly double Quantity; // Bought
        public readonly double Fee;
        public readonly DateTime DateTime;

        public readonly string ReadData;
    }
}
