using System;
using System.Collections.Generic;
using System.Text;
using MapTo;

namespace BlueWest.Data
{
    [MapFrom(typeof(FinanceTransaction))]

     partial struct FinanceTransactionReadDto
    {
        public int UserId { get; set; }
        public FinanceTransactionType FinanceTransactionType { get; }
        public FinanceSymbol FinanceSymbol { get; }
        public double Amount { get; } // To Buy
        public double Quantity { get; } // Bought
        public double Fee { get; }
        public DateTime DateTime { get; }

        public string ReadData { get; }
    }
}
