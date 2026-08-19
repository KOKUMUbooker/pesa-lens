using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.Core.Models;

namespace PesaScope.App.Data.Repositories;

public class AutoCategorizationRuleRepository(DatabaseService databaseService)
    : BaseRepository<AutoCategorizationRule>(databaseService), IAutoCategorizationRuleRepository
{
    public Task<List<AutoCategorizationRule>> GetEnabledOrderedByPriorityAsync() =>
        _db.Table<AutoCategorizationRule>()
           .Where(r => r.IsEnabled)
           .OrderByDescending(r => r.Priority)
           .ToListAsync();

    public Task<List<AutoCategorizationRule>> GetByCategoryAsync(int categoryId) =>
        _db.Table<AutoCategorizationRule>()
           .Where(r => r.CategoryId == categoryId)
           .OrderByDescending(r => r.Priority)
           .ToListAsync();

    public Task<List<AutoCategorizationRule>> GetByTypeAsync(RuleType ruleType) =>
        _db.Table<AutoCategorizationRule>()
           .Where(r => r.RuleType == ruleType)
           .OrderByDescending(r => r.Priority)
           .ToListAsync();

    public async Task<int> SetEnabledAsync(int ruleId, bool isEnabled)
    {
        var rule = await _db.FindAsync<AutoCategorizationRule>(ruleId);
        if (rule is null) return 0;

        rule.IsEnabled = isEnabled;
        return await _db.UpdateAsync(rule);
    }

    public async Task SeedDefaultsAsync()
    {
        // Each tuple: (RuleType, MatchValue, CategoryName, Priority)
        // CategoryName is resolved to an ID at seed time
        var defaults = new List<(RuleType Type, string Value, string CategoryName, int Priority)>
        {
            // Utilities — paybills
            (RuleType.PaybillNumber, "888880", "Utilities", 10), // KPLC Prepaid
            (RuleType.PaybillNumber, "888882", "Utilities", 10), // KPLC Postpaid
            (RuleType.PaybillNumber, "80200",  "Utilities", 10), // Nairobi Water
            (RuleType.ContainsText,  "KPLC",   "Utilities", 9),
            (RuleType.ContainsText,  "WATER",  "Utilities", 9),
            (RuleType.ContainsText,  "ZUKU",   "Utilities", 9),
            (RuleType.ContainsText,  "FAIBA",  "Utilities", 9),

            // Airtime & Data
            (RuleType.PaybillNumber,   "100",             "Airtime & Data", 10), // Safaricom
            (RuleType.TransactionType, "AirtimePurchase", "Airtime & Data", 10),
            (RuleType.ContainsText,    "TUNUKIWA",        "Airtime & Data", 8),

            // Transport
            (RuleType.ContainsText, "UBER",   "Transport", 9),
            (RuleType.ContainsText, "LITTLE", "Transport", 9),
            (RuleType.ContainsText, "BOLT",   "Transport", 9),

            // Health
            (RuleType.PaybillNumber, "808080",    "Health", 10), // NHIF
            (RuleType.ContainsText,  "NHIF",      "Health", 9),
            (RuleType.ContainsText,  "PHARMACY",  "Health", 8),
            (RuleType.ContainsText,  "HOSPITAL",  "Health", 8),

            // Food & Groceries
            (RuleType.ContainsText, "NAIVAS",    "Food & Groceries", 8),
            (RuleType.ContainsText, "QUICKMART", "Food & Groceries", 8),
            (RuleType.ContainsText, "CARREFOUR", "Food & Groceries", 8),
            (RuleType.ContainsText, "JAVA",      "Food & Groceries", 8),
            (RuleType.ContainsText, "KFC",       "Food & Groceries", 8),

            // Shopping
            (RuleType.ContainsText, "SUPERMARKET", "Shopping", 8),

            // Income — anything incoming, unless a more specific rule (e.g. Netflix,
            // GitHub) already claimed it at a higher priority
            (RuleType.Direction, "Incoming", "Income", 5),

            // Software & Dev Tools
            (RuleType.ContainsText, "DIGITALOCEA", "Software & Dev Tools", 9),
            (RuleType.ContainsText, "GITHUB",      "Software & Dev Tools", 9),
            (RuleType.ContainsText, "AWS",         "Software & Dev Tools", 9),
            (RuleType.ContainsText, "AMAZON WEB",  "Software & Dev Tools", 9),
            (RuleType.ContainsText, "GOOGLE CLOUD","Software & Dev Tools", 9),
            (RuleType.ContainsText, "MICROSOFT AZURE", "Software & Dev Tools", 9),
            (RuleType.ContainsText, "VERCEL",      "Software & Dev Tools", 9),
            (RuleType.ContainsText, "NETLIFY",     "Software & Dev Tools", 9),
            (RuleType.ContainsText, "RENDER",      "Software & Dev Tools", 9),
            (RuleType.ContainsText, "NAMECHEAP",   "Software & Dev Tools", 9),
            (RuleType.ContainsText, "GODADDY",     "Software & Dev Tools", 9),
            (RuleType.ContainsText, "JETBRAINS",   "Software & Dev Tools", 9),
            (RuleType.ContainsText, "OPENAI",      "Software & Dev Tools", 9),
            (RuleType.ContainsText, "ANTHROPIC",   "Software & Dev Tools", 9),
            (RuleType.ContainsText, "CLAUDE.AI",   "Software & Dev Tools", 9),
            (RuleType.ContainsText, "OPENROUTER",  "Software & Dev Tools", 9),
            (RuleType.ContainsText, "SUPABASE",    "Software & Dev Tools", 9),

            // Entertainment — streaming via GlobalPay virtual card
            (RuleType.ContainsText, "NETFLIX",       "Entertainment", 9),
            (RuleType.ContainsText, "SPOTIFY",       "Entertainment", 9),
            (RuleType.ContainsText, "YOUTUBE",       "Entertainment", 9),
            (RuleType.ContainsText, "APPLE TV",      "Entertainment", 9),
            (RuleType.ContainsText, "AMAZON PRIME",  "Entertainment", 9),
            (RuleType.ContainsText, "DISNEY",        "Entertainment", 9),
            (RuleType.ContainsText, "SHOWMAX",       "Entertainment", 9),
            (RuleType.ContainsText, "DSTV",          "Entertainment", 9),
            (RuleType.ContainsText, "GOTV",          "Entertainment", 9),
        };

        foreach (var (type, value, categoryName, priority) in defaults)
        {
            // Check if rule already exists
            var existing = await _db.Table<AutoCategorizationRule>()
                .Where(r => r.RuleType == type && r.MatchValue == value)
                .FirstOrDefaultAsync();

            if (existing is not null) continue;

            var category = await _db.Table<Category>()
                .Where(c => c.Name == categoryName)
                .FirstOrDefaultAsync();

            if (category is null) continue;

            await _db.InsertAsync(new AutoCategorizationRule
            {
                RuleType = type,
                MatchValue = value,
                CategoryId = category.Id,
                Priority = priority,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    public Task DeleteByCategoryAsync(int categoryId) =>
        _db.ExecuteAsync(
            "DELETE FROM AutoCategorizationRules WHERE category_id = ?",
            categoryId);
}