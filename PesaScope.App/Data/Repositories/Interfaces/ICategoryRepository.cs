using PesaScope.Core.Models;

namespace PesaScope.App.Data.Repositories.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>
    /// Returns all non-deleted categories ordered by SortOrder.
    /// </summary>
    Task<List<Category>> GetAllActiveAsync();

    /// <summary>
    /// Returns only user-created (non-system) categories.
    /// </summary>
    Task<List<Category>> GetUserCategoriesAsync();

    /// <summary>
    /// Returns a category by its exact name. Case-insensitive.
    /// </summary>
    Task<Category?> GetByNameAsync(string name);

    /// <summary>
    /// Returns the "Uncategorized" system category.
    /// Used as the fallback when no rule matches.
    /// </summary>
    Task<Category> GetUncategorizedAsync();

    /// <summary>
    /// Seeds the default system categories on first launch.
    /// Skips any category that already exists by name.
    /// </summary>
    Task SeedDefaultsAsync();

    /// <summary>
    /// Soft-deletes a user-created category and reassigns its transactions
    /// to the Uncategorized category.
    /// Throws InvalidOperationException if called on a system category.
    /// </summary>
    Task DeleteAndReassignAsync(int categoryId);

    Task<bool> TryInsertAsync(Category category);

    Task<bool> TryUpdateAsync(Category category);
}