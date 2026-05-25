using BankingAPI.Models;
using System.Collections.Generic;
using BankingAPI.Models.DTOs;

namespace BankingAPI.Interfaces{
    public interface ITransactionService{
        public TransferResponse TransferFunds(TransferRequest request);
        public DepositResponse DepositFunds(DepositRequest request);
        public WithdrawResponse WithdrawFunds(WithdrawRequest request);
        public List<Transaction> GetTransactionHistory(string accountNumber);
    }
}