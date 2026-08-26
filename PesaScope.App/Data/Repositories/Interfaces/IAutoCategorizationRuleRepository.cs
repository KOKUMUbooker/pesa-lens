using PesaScope.Core.Models;

namespace PesaScope.App.Data.Repositories.Interfaces;

public interface IAutoCategorizationRuleRepository : IRepository<AutoCategorizationRule>
{
    /// <summary>
    /// Returns all enabled rules ordered by priority descending (highest first).
    /// This is the list used by the categorization engine at sync time.
    /// </summary>
    Task<List<AutoCategorizationRule>> GetEnabledOrderedByPriorityAsync();

    /// <summary>
    /// Returns all rules assigned to a specific category.
    /// </summary>
    Task<List<AutoCategorizationRule>> GetByCategoryAsync(int categoryId);

    /// <summary>
    /// Returns all rules of a specific type e.g. all PaybillNumber rules.
    /// </summary>
    Task<List<AutoCategorizationRule>> GetByTypeAsync(RuleType ruleType);

    /// <summary>
    /// Enables or disables a rule without deleting it.
    /// </summary>
    Task<int> SetEnabledAsync(int ruleId, bool isEnabled);

    /// <summary>
    /// Seeds built-in system rules on first launch.
    /// Skips rules that already exist by type + matchValue combination.
    /// </summary>
    Task SeedDefaultsAsync();

    /// <summary>
    /// Deletes all user-created rules for a category.
    /// Called when a category is deleted.
    /// </summary>
    Task DeleteByCategoryAsync(int categoryId);

    Task<bool> ExistsAsync(RuleType ruleType, string matchValue);

    Task<bool> TryInsertAsync(AutoCategorizationRule rule);

    Task<bool> TryUpdateAsync(AutoCategorizationRule rule);
}