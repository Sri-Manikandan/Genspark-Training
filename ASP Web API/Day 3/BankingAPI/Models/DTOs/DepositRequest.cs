namespace BankingAPI.Models.DTOs{
    public class DepositRequest{
        public string To_Account_Number { get; set; }
        public float Amount { get; set; }
    }
}