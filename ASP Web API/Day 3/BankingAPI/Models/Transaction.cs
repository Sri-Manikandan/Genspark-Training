using System;

namespace BankingAPI.Models{
    public class Transaction{
        public string Transaction_reference_number { get; set; }
        public string? from_account_number { get; set; }
        public string? to_account_number { get; set; }
        public float amount { get; set; }
        public DateTime transaction_date { get; set; }
        public string transaction_status { get; set; }

        public Account? FromAccount { get; set; }
        public Account? ToAccount { get; set; }
    }
}