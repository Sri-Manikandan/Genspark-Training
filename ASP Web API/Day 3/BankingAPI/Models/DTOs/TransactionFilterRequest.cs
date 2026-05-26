using System;

namespace BankingAPI.Models.DTOs{
    public class TransactionFilterRequest{
        public string? AccountNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? TransactionStatus { get; set; }
        public float? MinAmount { get; set; }
        public float? MaxAmount { get; set; }

        public int? Page { get; set; } = 1;
        public int? PageSize { get; set; } = 10;

    }
}