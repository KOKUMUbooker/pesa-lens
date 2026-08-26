using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.Core.Models;
using SQLite;

namespace PesaScope.App.Data.Repositories;

public class CategoryRepository(DatabaseService databaseService)
    : BaseRepository<Category>(databaseService), ICategoryRepository
{
    public Task<List<Category>> GetAllActiveAsync() =>
        _db.Table<Category>()
           .Where(c => !c.IsSystemCategory || c.Name != "__deleted__")
           .OrderBy(c => c.Name)
           .ToListAsync();

    public Task<List<Category>> GetUserCategoriesAsync() =>
        _db.Table<Category>()
           .Where(c => !c.IsSystemCategory)
           .OrderBy(c => c.Name)
           .ToListAsync();

    public async Task<Category?> GetByNameAsync(string name) =>
        await _db.Table<Category>()
                 .Where(c => c.Name.ToLower() == name.ToLower())
                 .FirstOrDefaultAsync();

    public async Task<bool> TryInsertAsync(Category category)
    {
        try
        {
            await _db.InsertAsync(category);
            return true;
        }
        catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint)
        {
            return false;
        }
    }

    public async Task<bool> TryUpdateAsync(Category category)
    {
        try
        {
            await _db.UpdateAsync(category);
            return true;
        }
        catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint)
        {
            return false;
        }
    }

    public async Task<Category> GetUncategorizedAsync()
    {
        var category = await _db.Table<Category>()
                                .Where(c => c.Name == "Uncategorized")
                                .FirstOrDefaultAsync();

        // Should always exist after seeding but guard just in case
        if (category is null)
        {
            category = new Category
            {
                Name = "Uncategorized",
                Icon = "help",
                Color = "#90A4AE",
                IsSystemCategory = true,
                CreatedAt = DateTime.UtcNow
            };
            await _db.InsertAsync(category);
        }

        return category;
    }

    public async Task SeedDefaultsAsync()
    {
        var defaultCategories = new List<Category>
        {
            new() { Name = "Food & Groceries",     Icon = "Shopping_cart", Color = "#D4522A", IsSystemCategory = true },
            new() { Name = "Transport",            Icon = "Directions_car",Color = "#1976D2", IsSystemCategory = true },
            new() { Name = "Utilities",            Icon = "Bolt",          Color = "#C98A00", IsSystemCategory = true },
            new() { Name = "Airtime & Data",       Icon = "Phone_android", Color = "#5C6BC0", IsSystemCategory = true },
            new() { Name = "Entertainment",        Icon = "Movie",         Color = "#AB47BC", IsSystemCategory = true },
            new() { Name = "Health",               Icon = "Favorite",      Color = "#EF5350", IsSystemCategory = true },
            new() { Name = "Education",            Icon = "School",        Color = "#42A5F5", IsSystemCategory = true },
            new() { Name = "Shopping",             Icon = "Sell",          Color = "#EC407A", IsSystemCategory = true },
            new() { Name = "Rent & Housing",       Icon = "Home",          Color = "#8D6E63", IsSystemCategory = true },
            new() { Name = "Savings & Goals",      Icon = "Savings",       Color = "#26A69A", IsSystemCategory = true },
            new() { Name = "Income",               Icon = "South",         Color = "#1A8C62", IsSystemCategory = true },
            new() { Name = "Uncategorized",        Icon = "Help",          Color = "#90A4AE", IsSystemCategory = true },
            new() { Name = "Software & Dev Tools", Icon = "Code",          Color = "#00897B", IsSystemCategory = true },
        };

        foreach (var category in defaultCategories)
        {
            var existing = await GetByNameAsync(category.Name);
            if (existing is null)
            {
                category.CreatedAt = DateTime.UtcNow;
                await _db.InsertAsync(category);
            }
        }
    }

    public async Task DeleteAndReassignAsync(int categoryId)
    {
        var category = await _db.FindAsync<Category>(categoryId)
            ?? throw new InvalidOperationException($"Category {categoryId} not found.");

        if (category.IsSystemCategory)
            throw new InvalidOperationException("System categories cannot be deleted.");

        var uncategorized = await GetUncategorizedAsync();

        // Reassign all transactions from the deleted category
        await _db.ExecuteAsync(
            "UPDATE Transactions SET category_id = ? WHERE category_id = ?",
            uncategorized.Id, categoryId);

        // Delete the category's rules
        await _db.ExecuteAsync(
            "DELETE FROM AutoCategorizationRules WHERE category_id = ?",
            categoryId);

        // Delete associated budget if any
        await _db.ExecuteAsync(
            "DELETE FROM Budgets WHERE category_id = ?",
            categoryId);

        await _db.DeleteAsync(category);
    }
}