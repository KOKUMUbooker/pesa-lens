using PesaScope.Core.Models;
using SQLite;

namespace PesaScope.App.Data;

/// <summary>
/// Owns the single SQLiteAsyncConnection for the app.
/// Registered as a singleton in MauiProgram.cs.
/// Call InitializeAsync() once at startup before any repository is used.
/// </summary>
public class DatabaseService
{
    private readonly SQLiteAsyncConnection _db;
    private bool _dbInitialized;
    public SQLiteAsyncConnection Connection => _db;

    public DatabaseService(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath, SQLiteOpenFlags.ReadWrite |
                                                SQLiteOpenFlags.Create |
                                                SQLiteOpenFlags.SharedCache);
    }

    /// <summary>
    /// Creates all tables (if they don't exist) and returns the shared connection.
    /// Must be awaited before any repository operation.
    /// </summary>
    public async Task<SQLiteAsyncConnection> InitializeAsync()
    {
        if (_dbInitialized)
            return _db;

        await _db.CreateTableAsync<Transaction>();
        await _db.CreateTableAsync<Category>();
        // Make the index case-insensitive for category names
        await _db.ExecuteAsync(
            @"CREATE UNIQUE INDEX IF NOT EXISTS idx_categories_name_unique
              ON Categories(name COLLATE NOCASE)");
        await _db.CreateTableAsync<AutoCategorizationRule>();
        // To set DB-level combined uniqueness for rule_type and match_value fields in AutoCategorizationRule
        await _db.ExecuteAsync(
            @"CREATE UNIQUE INDEX IF NOT EXISTS idx_rules_type_value_unique
          ON AutoCategorizationRules(rule_type, match_value COLLATE NOCASE)");
        await _db.CreateTableAsync<Budget>();
        await _db.CreateTableAsync<OverallBudget>();
        await _db.CreateTableAsync<SyncMetadata>();
        await _db.CreateTableAsync<AppSettings>();
        await _db.CreateTableAsync<ExportHistory>();
        await _db.CreateTableAsync<BudgetSnapshot>();

        _dbInitialized = true;

        return _db;
    }

    /// <summary>
    /// Drops and recreates all tables. Called when the user clears all app data.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await _db.DropTableAsync<Transaction>();
        await _db.DropTableAsync<Category>();
        await _db.DropTableAsync<AutoCategorizationRule>();
        await _db.DropTableAsync<Budget>();
        await _db.DropTableAsync<OverallBudget>();
        await _db.DropTableAsync<SyncMetadata>();
        await _db.DropTableAsync<AppSettings>();
        await _db.DropTableAsync<ExportHistory>();
        await _db.DropTableAsync<BudgetSnapshot>();

        _dbInitialized = false;   // ← allow InitializeAsync to actually run again
        await InitializeAsync();
    }
}