using PesaScope.Core.Models;

namespace PesaScope.App.Services.Interfaces;

public interface IAutoCategorizationService
{
    public Task CategorizeAsync(IList<Transaction> transactions);

    /// <summary>
    /// Categorizes a single transaction and returns the CategoryId that was
    /// applied, or Uncategorized rule id if no rule matched. Use this when you need the resolved
    /// CategoryId immediately after categorization (e.g. for budget notifications).
    /// </summary>
    Task<int?> CategorizeAndGetCategoryIdAsync(Transaction transaction);
}
