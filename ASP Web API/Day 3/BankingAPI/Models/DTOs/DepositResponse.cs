using System;

namespace BankingAPI.Models.DTOs{
    public class DepositResponse{
        public string Transaction_reference_number { get; set; }
        public string to_account_number { get; set; }
        public float amount { get; set; }
        public DateTime transaction_date { get; set; }
        public string transaction_status { get; set; }
    }
}