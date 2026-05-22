using BankingAPI.Interfaces;
using BankingAPI.Models.DTOs;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace BankingAPI.Services
{
    public class TokenService : ITokenService
    {
        readonly string _key;
        readonly string _issuer;

        readonly string _duration;
        public TokenService(IConfiguration configuration)
        {
            _key = configuration["JWT:Key"]??"This is the alternate key";
            _issuer = configuration["JWT:Issuer"]??"Any Server";
            _duration = configuration["JWT:DurationInMinutes"]??"00:60:00";
        }

        public string CreateNewToken(TokenRequest request)
        {
            // JWT - HEADER, PAYLOAD, SIGNATURE
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, request.Username),
                new Claim(ClaimTypes.GivenName, request.GivenName),
                new Claim(ClaimTypes.Role, request.Role)
            };
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_duration)),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}