using BankingAPI.Interfaces;
using BankingAPI.Models;
using BankingAPI.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BankingAPI.Controllers{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : ControllerBase{
        private readonly ITransactionService _transactionService;
        public TransactionController(ITransactionService transactionService){
            _transactionService = transactionService;
        }
        [HttpPost("Transfer")]
        public ActionResult<TransferResponse> Transfer(TransferRequest request){
            try{
                var result = _transactionService.TransferFunds(request);
                return Ok(result);
            }
            catch(Exception ex){
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Deposit")]
        public ActionResult<DepositResponse> Deposit(DepositRequest request){
            try{
                var result = _transactionService.DepositFunds(request);
                return Ok(result);
            }
            catch(Exception ex){
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Withdraw")]
        public ActionResult<WithdrawResponse> Withdraw(WithdrawRequest request){
            try{
                var result = _transactionService.WithdrawFunds(request);
                return Ok(result);
            }
            catch(Exception ex){
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetTransactionHistory")]
        public ActionResult<List<Transaction>> GetTransactionHistory(string accountNumber){
            try{
                var result = _transactionService.GetTransactionHistory(accountNumber);
                return Ok(result);
            }
            catch(Exception ex){
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("GetFilteredTransactions")]
        public ActionResult<PagedResponse<Transaction>> GetFilteredTransactions([FromQuery]TransactionFilterRequest request){
            try{
                var result = _transactionService.GetFilteredTransactions(request);
                return Ok(result);
            }
            catch(Exception ex){
                return BadRequest(ex.Message);
            }
        }
    }
}