using System;
using System.Collections.Generic;
using System.Text;
using MapTo;

namespace BlueWest.Data
{
    [MapFrom(typeof(FinanceTransaction))]

    public partial struct FinanceTransactionReadDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public FinanceTransactionType FinanceTransactionType { get; }
        public FinanceSymbol FinanceSymbol { get; }
        public double Amount { get; } // To Buy
        public double Quantity { get; } // Bought
        public double Fee { get; }
        public DateTime DateTime { get; }
    }
}
