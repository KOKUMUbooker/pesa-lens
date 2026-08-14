using SQLite;

namespace PesaScope.Core.Models;

[Table("Transactions")]
public class Transaction
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// M-Pesa transaction code e.g. QHX7W8Y9Z. Unique to prevent duplicate imports.
    /// </summary>
    [Unique, NotNull]
    [Column("mpesa_code")]
    public string MpesaCode { get; set; } = string.Empty;

    [NotNull]
    [Column("type")]
    public TransactionType Type { get; set; }

    [NotNull]
    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("balance_after_transaction")]
    public decimal BalanceAfterTransaction { get; set; }

    [NotNull]
    [Column("counterparty_name")]
    public string CounterpartyName { get; set; } = string.Empty;

    [Column("counterparty_number")]
    public string? CounterpartyNumber { get; set; }

    [NotNull, Indexed]
    [Column("transaction_date")]
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Foreign key → Categories.id
    /// </summary>
    [NotNull, Indexed]
    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    /// <summary>
    /// The full original M-Pesa SMS body stored for reference.
    /// </summary>
    [NotNull]
    [Column("original_sms")]
    public string OriginalSms { get; set; } = string.Empty;

    /// <summary>
    /// Android SMS inbox row ID. Used to detect new messages on sync.
    /// </summary>
    [Indexed]
    [Column("sms_id")]
    public long SmsId { get; set; }

    /// <summary>
    /// Android SMS timestamp in milliseconds since epoch. Used to sort and deduplicate.
    /// </summary>
    [Column("sms_timestamp")]
    public long SmsTimestamp { get; set; }

    [NotNull]
    [Column("imported_at")]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// True if the user has manually edited the category or note.
    /// Prevents auto-categorization from overwriting user changes on re-sync.
    /// </summary>
    [Column("is_edited")]
    public bool IsEdited { get; set; } = false;

    // ── Navigation (not persisted, populated in queries) ──────────────────────
    [Ignore]
    public Category? Category { get; set; }
}

public enum TransactionType
{
    Unknown = 0,
    SendMoney = 1,
    ReceiveMoney = 2,
    PayBill = 3,
    BuyGoods = 4,
    AirtimePurchase = 5,
    Withdrawal = 6,
    Deposit = 7,
    Fuliza = 8,
    MShwari = 9,
    Reversal = 10,
    GlobalPayment = 11
}