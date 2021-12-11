
using System.Collections.Generic;
using MapTo;


namespace BlueWest.Data
{
    [MapFrom(typeof(UserUpdateDto))]
    public partial class User 
    {
        public readonly int Id;
        public string Name;
        public string Address;

        public string BTCAddress;
        public string LTCAddress;

        public double BTCAmount;
        public double LTCAmount;

        public readonly List<FinanceTransaction> FinanceTransactions;

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
    

