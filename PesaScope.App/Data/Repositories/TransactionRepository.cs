using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.Core.Models;

namespace PesaScope.App.Data.Repositories;

public class TransactionRepository(DatabaseService databaseService)
    : BaseRepository<Transaction>(databaseService), ITransactionRepository
{
    public async Task<bool> ExistsByMpesaCodeAsync(string mpesaCode) =>
        await _db.Table<Transaction>()
                 .Where(t => t.MpesaCode == mpesaCode)
                 .CountAsync() > 0;

    public async Task<(int Inserted, int Duplicates)> InsertManyAsync(IEnumerable<Transaction> transactions)
    {
        int inserted = 0;
        int duplicates = 0;

        await _db.RunInTransactionAsync(conn =>
        {
            var existingCodes = new HashSet<string>(
                conn.Table<Transaction>().Select(t => t.MpesaCode));

            foreach (var tx in transactions)
            {
                if (existingCodes.Contains(tx.MpesaCode))
                {
                    duplicates++;
                    continue;
                }

                try
                {
                    conn.Insert(tx);
                    existingCodes.Add(tx.MpesaCode);
                    inserted++;
                }
                catch (SQLite.SQLiteException ex) when (ex.Result == SQLite.SQLite3.Result.Constraint)
                {
                    duplicates++;
                }
            }
        });

        return (inserted, duplicates);
    }

    public Task<List<Transaction>> GetByDateRangeAsync(DateTime from, DateTime to) =>
        _db.Table<Transaction>()
           .Where(t => t.TransactionDate >= from && t.TransactionDate <= to)
           .OrderByDescending(t => t.TransactionDate)
           .ToListAsync();

    public Task<List<Transaction>> GetByCategoryAsync(int categoryId) =>
        _db.Table<Transaction>()
           .Where(t => t.CategoryId == categoryId)
           .OrderByDescending(t => t.TransactionDate)
           .ToListAsync();

    public Task<List<Transaction>> GetByTypeAsync(TransactionType type) =>
        _db.Table<Transaction>()
           .Where(t => t.Type == type)
           .OrderByDescending(t => t.TransactionDate)
           .ToListAsync();

    public async Task<List<Transaction>> SearchAsync(string query)
    {
        // sqlite-net-pcl doesn't support LIKE natively in LINQ; use raw query
        var like = $"%{query}%";
        return await _db.QueryAsync<Transaction>(
            @"SELECT * FROM Transactions
              WHERE counterparty_name LIKE ?
                 OR note            LIKE ?
                 OR mpesa_code      LIKE ?
              ORDER BY transaction_date DESC",
            like, like, like);
    }

    public async Task<List<Transaction>> GetFilteredAsync(
        DateTime? from = null,
        DateTime? to = null,
        int? categoryId = null,
        TransactionType? type = null)
    {
        var query = _db.Table<Transaction>().OrderByDescending(t => t.TransactionDate);

        if (from is not null)
            query = query.Where(t => t.TransactionDate >= from.Value);

        if (to is not null)
            query = query.Where(t => t.TransactionDate <= to.Value);

        if (categoryId is not null)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        if (type is not null)
            query = query.Where(t => t.Type == type.Value);

        return await query.ToListAsync();
    }

    public async Task<decimal> GetTotalSpentAsync(DateTime from, DateTime to)
    {
        var outgoingTypes = new[]
        {
            TransactionType.SendMoney,
            TransactionType.PayBill,
            TransactionType.BuyGoods,
            TransactionType.AirtimePurchase,
            TransactionType.Withdrawal,
            TransactionType.Fuliza,
            TransactionType.MShwari
        };

        var transactions = await _db.Table<Transaction>()
            .Where(t => t.TransactionDate >= from && t.TransactionDate <= to)
            .ToListAsync();

        return transactions
            .Where(t => outgoingTypes.Contains(t.Type))
            .Sum(t => t.Amount);
    }

    public async Task<decimal> GetTotalReceivedAsync(DateTime from, DateTime to)
    {
        var transactions = await _db.Table<Transaction>()
            .Where(t => t.TransactionDate >= from
                     && t.TransactionDate <= to
                     && t.Type == TransactionType.ReceiveMoney)
            .ToListAsync();

        return transactions.Sum(t => t.Amount);
    }

    public async Task<Dictionary<int, decimal>> GetSpendingByCategoryAsync(DateTime from, DateTime to)
    {
        var transactions = await GetByDateRangeAsync(from, to);

        return transactions
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
    }

    public async Task<Dictionary<DateTime, decimal>> GetDailySpendingAsync(DateTime from, DateTime to)
    {
        var transactions = await GetByDateRangeAsync(from, to);

        return transactions
            .GroupBy(t => t.TransactionDate.Date)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
    }

    public async Task<int> UpdateCategoryAsync(string mpesaCode, int newCategoryId)
    {
        var tx = await _db.Table<Transaction>()
                          .Where(t => t.MpesaCode == mpesaCode)
                          .FirstOrDefaultAsync();

        if (tx is null) return 0;

        tx.CategoryId = newCategoryId;
        tx.IsEdited = true;

        return await _db.UpdateAsync(tx);
    }

    public async Task<int> UpdateNoteAsync(string mpesaCode, string? note)
    {
        var tx = await _db.Table<Transaction>()
                          .Where(t => t.MpesaCode == mpesaCode)
                          .FirstOrDefaultAsync();

        if (tx is null) return 0;

        tx.Note = note;
        tx.IsEdited = true;

        return await _db.UpdateAsync(tx);
    }

    public async Task<int> UpdateManyAsync(IEnumerable<Transaction> transactions)
    {
        int updated = 0;

        await _db.RunInTransactionAsync(conn =>
        {
            foreach (var tx in transactions)
            {
                updated += conn.Update(tx);
            }
        });

        return updated;
    }

    public Task<List<Transaction>> GetRecentAsync(int count = 5) =>
        _db.Table<Transaction>()
           .OrderByDescending(t => t.TransactionDate)
           .Take(count)
           .ToListAsync();

    public Task<List<Transaction>> GetAfterSmsIdAsync(long lastSmsId) =>
        _db.Table<Transaction>()
           .Where(t => t.SmsId > lastSmsId)
           .OrderBy(t => t.SmsId)
           .ToListAsync();

    public Task<Transaction?> GetByMpesaCodeAsync(string code) =>
        _db.Table<Transaction>()
           .Where(t => t.MpesaCode == code)
           .FirstOrDefaultAsync()!;

    public Task<Transaction?> GetBySmsIdAsync(long smsId) =>
        _db.Table<Transaction>()
           .Where(t => t!.SmsId == smsId)
           .FirstOrDefaultAsync()!;
}