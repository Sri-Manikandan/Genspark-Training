namespace BankingAPI.Models.DTOs{
    public class TransferRequest{
        public string From_Account_Number { get; set; }
        public string To_Account_Number { get; set; }
        public float Amount { get; set; }
    }
}