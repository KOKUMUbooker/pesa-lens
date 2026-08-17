using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.App.Services.Interfaces;
using PesaScope.Core.Models;

public class AutoCategorizationService : IAutoCategorizationService
{
    private readonly IAutoCategorizationRuleRepository _rulesRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ICategoryRepository _categoryRepo;

    public AutoCategorizationService(
        IAutoCategorizationRuleRepository rulesRepo,
        ITransactionRepository transactionRepo,
        ICategoryRepository categoryRepo)
    {
        _rulesRepo = rulesRepo;
        _transactionRepo = transactionRepo;
        _categoryRepo = categoryRepo;
    }

    public async Task CategorizeAsync(IList<Transaction> transactions)
    {
        // Fetch once, sorted by priority descending — highest priority wins
        var rules = await _rulesRepo.GetEnabledOrderedByPriorityAsync();
        var toUpdate = new List<Transaction>();

        Category? uncategorized = null; // lazily fetched, only if actually needed

        foreach (var tx in transactions)
        {
            if (tx.CategoryId != 0) continue; // already categorized, skip

            var matched = rules.FirstOrDefault(r => Matches(tx, r));

            if (matched is not null)
            {
                tx.CategoryId = matched.CategoryId;
            }
            else
            {
                uncategorized ??= await _categoryRepo.GetUncategorizedAsync();
                tx.CategoryId = uncategorized.Id;
            }

            toUpdate.Add(tx);
        }

        if (toUpdate.Count > 0)
            await _transactionRepo.UpdateManyAsync(toUpdate);
    }

    public async Task<int?> CategorizeAndGetCategoryIdAsync(Transaction transaction)
    {
        if (transaction.CategoryId != 0)
            return transaction.CategoryId; // already categorized

        var rules = await _rulesRepo.GetEnabledOrderedByPriorityAsync();
        var matched = rules.FirstOrDefault(r => Matches(transaction, r));

        var categoryId = matched?.CategoryId
            ?? (await _categoryRepo.GetUncategorizedAsync()).Id;

        transaction.CategoryId = categoryId;
        await _transactionRepo.UpdateManyAsync([transaction]);

        return categoryId;
    }

    private static bool Matches(Transaction tx, AutoCategorizationRule rule) =>
        rule.RuleType switch
        {
            RuleType.ContainsText =>
                tx.CounterpartyName.Contains(rule.MatchValue, StringComparison.OrdinalIgnoreCase),

            RuleType.MerchantName =>
                tx.CounterpartyName.Equals(rule.MatchValue, StringComparison.OrdinalIgnoreCase),

            RuleType.PaybillNumber =>
                tx.CounterpartyNumber?.Equals(rule.MatchValue, StringComparison.OrdinalIgnoreCase) ?? false,

            RuleType.TillNumber =>
                tx.CounterpartyNumber?.Equals(rule.MatchValue, StringComparison.OrdinalIgnoreCase) ?? false,

            RuleType.TransactionType =>
                tx.Type.ToString().Equals(rule.MatchValue, StringComparison.OrdinalIgnoreCase),

            RuleType.Direction =>
                tx.Direction.ToString().Equals(rule.MatchValue, StringComparison.OrdinalIgnoreCase),

            _ => false
        };
}