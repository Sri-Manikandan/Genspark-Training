namespace BankingAPI.Models.DTOs
{
    public class TokenRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; }

        public string GivenName { get; set; } = string.Empty;
    }
}