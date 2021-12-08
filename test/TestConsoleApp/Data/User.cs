
using System.Collections.Generic;
using MapTo;


namespace BlueWest.Data
{
    [MapFrom(typeof(UserUpdateDto))]
    public partial struct User 
    {
       public int Id { get;  }
       public string Name { get; set; }
       public string Address { get; set; }
       
       public string BTCAddress { get; set; } 
       public string LTCAddress { get; set; } 

       public double BTCAmount { get; set; }
       public double LTCAmount { get; set; }

        public List<FinanceTransaction> FinanceTransactions { get; set; }

        public User(int id, string name, string address, string btcAddress, string ltcAddress, double btcAmount, double ltcAmount, List<FinanceTransaction> financeTransactions)
        {
            Id = id;
            Name = name;
            Address = address;
            BTCAddress = btcAddress;
            LTCAddress = ltcAddress;
            BTCAmount = btcAmount;
            LTCAmount = ltcAmount;
            FinanceTransactions = financeTransactions;
        }

        public void AddTransaction(FinanceTransaction financeTransaction)
        {
            FinanceTransactions.Add(financeTransaction);
        }

    }
}
    

