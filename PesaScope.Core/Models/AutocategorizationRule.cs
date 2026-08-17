using SQLite;

namespace PesaScope.Core.Models;

[Table("AutoCategorizationRules")]
public class AutoCategorizationRule
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [NotNull]
    [Column("rule_type")]
    public RuleType RuleType { get; set; }

    /// <summary>
    /// The value to match against e.g. "888880", "NAIVAS", "JAVA".
    /// Matching is case-insensitive.
    /// </summary>
    [NotNull, Indexed]
    [Column("match_value")]
    public string MatchValue { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key → Categories.id
    /// </summary>
    [NotNull, Indexed]
    [Column("category_id")]
    public int CategoryId { get; set; }

    [NotNull]
    [Column("is_enabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Higher priority rules are evaluated first.
    /// User-created rules should be given a higher priority than system defaults.
    /// </summary>
    [NotNull, Indexed]
    [Column("priority")]
    public int Priority { get; set; } = 0;

    [NotNull]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Navigation (not persisted) ────────────────────────────────────────────
    [Ignore]
    public Category? Category { get; set; }
}

public enum RuleType
{
    MerchantName = 1,
    PaybillNumber = 2,
    TillNumber = 3,
    TransactionType = 4,
    ContainsText = 5,
    Direction = 6
}