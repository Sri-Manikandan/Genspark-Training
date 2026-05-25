namespace BankingAPI.Models.DTOs{
    public class WithdrawRequest{
        public string From_Account_Number { get; set; }
        public float Amount { get; set; }
    }
}