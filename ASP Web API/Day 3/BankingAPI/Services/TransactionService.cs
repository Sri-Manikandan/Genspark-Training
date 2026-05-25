using BankingAPI.Contexts;
using BankingAPI.Interfaces;
using BankingAPI.Models;
using BankingAPI.Models.DTOs;

namespace BankingAPI.Services{
    public class TransactionService : ITransactionService{

        private readonly BankingContext _context;

        public TransactionService(BankingContext context){
            _context = context;
        }

        private Transaction BuildTransaction(string txRef, string? from, string? to, float amount, string status) =>
            new Transaction {
                Transaction_reference_number = txRef,
                from_account_number = from,
                to_account_number = to,
                amount = amount,
                transaction_date = DateTime.UtcNow,
                transaction_status = status
            };

        private string LogFailedTransaction(string? from, string? to, float amount){
            _context.ChangeTracker.Clear();
            var txRef = Guid.NewGuid().ToString();
            using var tx = _context.Database.BeginTransaction();
            _context.Transactions.Add(BuildTransaction(txRef, from, to, amount, "Failed"));
            _context.SaveChanges();
            tx.Commit();
            return txRef;
        }

        public TransferResponse TransferFunds(TransferRequest request){
            var fromAccount = _context.Accounts.Find(request.From_Account_Number);
            var toAccount = _context.Accounts.Find(request.To_Account_Number);
            if(fromAccount == null || toAccount == null)
                throw new Exception("Account not found");

            using var dbTransaction = _context.Database.BeginTransaction();
            try {
                if(fromAccount.Balance < request.Amount)
                    throw new Exception("Insufficient balance");

                fromAccount.Balance -= request.Amount;
                toAccount.Balance += request.Amount;
                var txRef = Guid.NewGuid().ToString();
                _context.Transactions.Add(BuildTransaction(txRef, request.From_Account_Number, request.To_Account_Number, request.Amount, "Success"));
                _context.SaveChanges();
                dbTransaction.Commit();
                return new TransferResponse {
                    Transaction_reference_number = txRef,
                    from_account_number = request.From_Account_Number,
                    to_account_number = request.To_Account_Number,
                    amount = request.Amount,
                    transaction_date = DateTime.UtcNow,
                    transaction_status = "Success"
                };
            }
            catch (Exception) {
                dbTransaction.Rollback();
                var failedRef = LogFailedTransaction(request.From_Account_Number, request.To_Account_Number, request.Amount);
                return new TransferResponse {
                    Transaction_reference_number = failedRef,
                    from_account_number = request.From_Account_Number,
                    to_account_number = request.To_Account_Number,
                    amount = request.Amount,
                    transaction_date = DateTime.UtcNow,
                    transaction_status = "Failed"
                };
            }
        }

        public DepositResponse DepositFunds(DepositRequest request){
            var account = _context.Accounts.Find(request.To_Account_Number);
            if(account == null)
                throw new Exception("Account not found");

            using var dbTransaction = _context.Database.BeginTransaction();
            try {
                account.Balance += request.Amount;
                var txRef = Guid.NewGuid().ToString();
                _context.Transactions.Add(BuildTransaction(txRef, null, request.To_Account_Number, request.Amount, "Success"));
                _context.SaveChanges();
                dbTransaction.Commit();
                return new DepositResponse {
                    Transaction_reference_number = txRef,
                    to_account_number = request.To_Account_Number,
                    amount = request.Amount,
                    transaction_date = DateTime.UtcNow,
                    transaction_status = "Success"
                };
            }
            catch (Exception) {
                dbTransaction.Rollback();
                var failedRef = LogFailedTransaction(null, request.To_Account_Number, request.Amount);
                return new DepositResponse {
                    Transaction_reference_number = failedRef,
                    to_account_number = request.To_Account_Number,
                    amount = request.Amount,
                    transaction_date = DateTime.UtcNow,
                    transaction_status = "Failed"
                };
            }
        }

        public WithdrawResponse WithdrawFunds(WithdrawRequest request){
            var account = _context.Accounts.Find(request.From_Account_Number);
            if(account == null)
                throw new Exception("Account not found");

            using var dbTransaction = _context.Database.BeginTransaction();
            try {
                if(account.Balance < request.Amount)
                    throw new Exception("Insufficient balance");

                account.Balance -= request.Amount;
                var txRef = Guid.NewGuid().ToString();
                _context.Transactions.Add(BuildTransaction(txRef, request.From_Account_Number, null, request.Amount, "Success"));
                _context.SaveChanges();
                dbTransaction.Commit();
                return new WithdrawResponse {
                    Transaction_reference_number = txRef,
                    from_account_number = request.From_Account_Number,
                    amount = request.Amount,
                    transaction_date = DateTime.UtcNow,
                    transaction_status = "Success"
                };
            }
            catch (Exception) {
                dbTransaction.Rollback();
                var failedRef = LogFailedTransaction(request.From_Account_Number, null, request.Amount);
                return new WithdrawResponse {
                    Transaction_reference_number = failedRef,
                    from_account_number = request.From_Account_Number,
                    amount = request.Amount,
                    transaction_date = DateTime.UtcNow,
                    transaction_status = "Failed"
                };
            }
        }

        public List<Transaction> GetTransactionHistory(string accountNumber){
            return _context.Transactions
                .Where(t => t.from_account_number == accountNumber || t.to_account_number == accountNumber)
                .ToList();
        }

        public PagedResponse<Transaction> GetFilteredTransactions(TransactionFilterRequest request){
            IQueryable<Transaction> query = _context.Transactions;
            if(!string.IsNullOrEmpty(request.AccountNumber)){
                query = query.Where(t => t.from_account_number == request.AccountNumber || t.to_account_number == request.AccountNumber);
            }
            if(request.StartDate.HasValue){
                query = query.Where(t => t.transaction_date >= request.StartDate.Value);
            }
            if(request.EndDate.HasValue){
                query = query.Where(t => t.transaction_date <= request.EndDate.Value);
            }
            if(!string.IsNullOrEmpty(request.TransactionStatus)){
                query = query.Where(t => t.transaction_status == request.TransactionStatus);
            }
            if(request.MinAmount.HasValue){
                query = query.Where(t => t.amount >= request.MinAmount.Value);
            }
            if(request.MaxAmount.HasValue){
                query = query.Where(t => t.amount <= request.MaxAmount.Value);
            }

            int totalCount = query.Count();

            var data = query.OrderByDescending(t=> t.transaction_date).Skip((request.Page - 1)* request.PageSize).Take(request.PageSize).ToList();
            return new PagedResponse<Transaction>{
                Data = data,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }
    }
}
