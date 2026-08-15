using PesaScope.Core.Models;

namespace PesaScope.App.Data.Repositories.Interfaces;

public interface ITransactionRepository : IRepository<Transaction>
{
    /// <summary>
    /// Checks if a transaction with the given M-Pesa code already exists.
    /// Used during sync to skip duplicates.
    /// </summary>
    Task<bool> ExistsByMpesaCodeAsync(string mpesaCode);

    /// <summary>
    /// Bulk insert a list of transactions. Skips any whose MpesaCode already exists.
    /// Returns the count of newly inserted transactions.
    /// </summary>
    Task<(int Inserted, int Duplicates)> InsertManyAsync(IEnumerable<Transaction> transactions);

    /// <summary>
    /// Returns all transactions within the given date range, newest first.
    /// </summary>
    Task<List<Transaction>> GetByDateRangeAsync(DateTime from, DateTime to);

    /// <summary>
    /// Returns all transactions for a specific category.
    /// </summary>
    Task<List<Transaction>> GetByCategoryAsync(int categoryId);

    /// <summary>
    /// Returns all transactions of a specific type.
    /// </summary>
    Task<List<Transaction>> GetByTypeAsync(TransactionType type);

    /// <summary>
    /// Full-text search across counterparty name, note, and mpesa code.
    /// </summary>
    Task<List<Transaction>> SearchAsync(string query);

    /// <summary>
    /// Returns transactions filtered by date range, category, and/or type.
    /// Any null parameter is ignored (not applied as a filter).
    /// </summary>
    Task<List<Transaction>> GetFilteredAsync(
        DateTime? from = null,
        DateTime? to = null,
        int? categoryId = null,
        TransactionType? type = null);

    /// <summary>
    /// Returns the total amount spent (outgoing) within a date range.
    /// </summary>
    Task<decimal> GetTotalSpentAsync(DateTime from, DateTime to);

    /// <summary>
    /// Returns the total amount received (incoming) within a date range.
    /// </summary>
    Task<decimal> GetTotalReceivedAsync(DateTime from, DateTime to);

    /// <summary>
    /// Returns the total amount spent per category within a date range.
    /// Key = CategoryId, Value = total spent.
    /// </summary>
    Task<Dictionary<int, decimal>> GetSpendingByCategoryAsync(DateTime from, DateTime to);

    /// <summary>
    /// Returns daily totals (spent) within a date range for charting.
    /// Key = date (time stripped), Value = total spent that day.
    /// </summary>
    Task<Dictionary<DateTime, decimal>> GetDailySpendingAsync(DateTime from, DateTime to);

    /// <summary>
    /// Updates only the category and IsEdited flag of a transaction.
    /// Used when the user manually re-categorizes a transaction.
    /// </summary>
    Task<int> UpdateCategoryAsync(string mpesaCode, int newCategoryId);

    /// <summary>
    /// Updates only the note field of a transaction.
    /// </summary>
    Task<int> UpdateNoteAsync(string mpesaCode, string? note);

    /// <summary>
    /// Returns the N most recent transactions. Used for the dashboard preview list.
    /// </summary>
    Task<List<Transaction>> GetRecentAsync(int count = 5);

    /// <summary>
    /// Returns transaction by its code
    /// </summary>
    Task<Transaction?> GetByMpesaCodeAsync(string code);

    /// <summary>
    /// Returns all transactions with an Android SmsId greater than the given value.
    /// Used for incremental sync.
    /// </summary>
    Task<List<Transaction>> GetAfterSmsIdAsync(long lastSmsId);

    /// <summary>
    /// Updates list of transactions passed as argument
    /// </summary>
    Task<int> UpdateManyAsync(IEnumerable<Transaction> transactions);

    /// <summary>
    /// Gets sms messages by Id
    /// </summary>
    Task<Transaction?> GetBySmsIdAsync(long smsId);
}