using PesaScope.Core.Models;
using PesaScope.Core.Services.Interfaces;
using System.Text.RegularExpressions;

namespace PesaScope.Core.Services;

/// <summary>
/// Parses raw M-Pesa SMS messages into Transaction objects.
/// Each message type has its own regex pattern. Patterns are compiled once
/// and reused across all calls for performance on large imports.
/// </summary>
public class MpesaSmsParser : IMpesaSmsParser
{
    // ── Public entry point ───────────────────────────────────────────────────
    /// <summary>
    /// Attempts to parse a raw M-Pesa SMS into a Transaction.
    /// Returns null if the message doesn't match any known pattern.
    /// </summary>
    public Transaction? Parse(string smsBody, long smsId, long smsTimestamp)
    {
        if (string.IsNullOrWhiteSpace(smsBody))
            return null;

        // Normalize: collapse whitespace and strip newlines
        var body = NormalizeSms(smsBody);

        var transaction = TryParseSendMoney(body, smsId, smsTimestamp)
            ?? TryParseReceiveMoney(body, smsId, smsTimestamp)
            ?? TryParseGlobalTransferSend(body, smsId, smsTimestamp)
            ?? TryParseGlobalTransferReceive(body, smsId, smsTimestamp)
            ?? TryParseGlobalVirtualCardPayment(body, smsId, smsTimestamp)
            ?? TryParseGlobalPaymentCard(body, smsId, smsTimestamp)
            ?? TryParseDataBundle(body, smsId, smsTimestamp)
            ?? TryParsePayBill(body, smsId, smsTimestamp)
            ?? TryParseBuyGoods(body, smsId, smsTimestamp)
            ?? TryParseWithdrawal(body, smsId, smsTimestamp)
            ?? TryParseAirtime(body, smsId, smsTimestamp)
            ?? TryParseReversal(body, smsId, smsTimestamp)
            ?? TryParseDeposit(body, smsId, smsTimestamp)
            ?? TryParseFuliza(body, smsId, smsTimestamp)
            ?? TryParseMShwariWithdrawal(body, smsId, smsTimestamp)
            ?? TryParseMShwariDeposit(body, smsId, smsTimestamp);

        if (transaction is not null) transaction.OriginalSms = smsBody;

        return transaction;
    }

    // ── Parsers ──────────────────────────────────────────────────────────────

    private static Transaction? TryParseSendMoney(string body, long smsId, long timestamp)
    {
        var m = ParserPatterns.SendMoneyPattern.Match(body);
        if (!m.Success) return null;

        return BuildTransaction(m, TransactionType.SendMoney, smsId, timestamp,
            counterpartyName: m.Groups["name"].Value.Trim(),
            counterpartyNumber: m.Groups["phone"].Value.Trim());
    }

    private static Transaction? TryParseReceiveMoney(string body, long smsId, long timestamp)
    {
        var m = ParserPatterns.ReceiveMoneyPattern.Match(body);
        if (!m.Success) return null;

        return BuildTransaction(m, TransactionType.ReceiveMoney, smsId, timestamp,
            counterpartyName: m.Groups["name"].Value.Trim(),
            counterpartyNumber: m.Groups["phone"].Value.Trim());
    }

    private static Transaction? TryParsePayBill(string body, long smsId, long timestamp)
    {
        var m = ParserPatterns.PayBillPattern.Match(body);
        if (!m.Success) return null;

        return BuildTransaction(m, TransactionType.PayBill, smsId, timestamp,
            counterpartyName: m.Groups["name"].Value.Trim(),
            counterpartyNumber: m.Groups["account"].Value.Trim());
    }

    private static Transaction? TryParseBuyGoods(string body, long smsId, long timestamp)
    {
        var m = ParserPatterns.BuyGoodsPattern.Match(body);
        if (!m.Success) return null;

        return BuildTransaction(m, TransactionType.BuyGoods, smsId, timestamp,
            counterpartyName: m.Groups["name"].Value.Trim());
    }

    private static Transaction? TryParseWithdrawal(string body, long smsId, long timestamp)
    {
        var m = ParserPatterns.WithdrawalPattern.Match(body);
        if (!m.Success) return null;

        var agentName = m.Groups["agent"].Value.Trim();
        var location = m.Groups["location"].Value.Trim();

        return BuildTransaction(m, TransactionType.Withdrawal, smsId, timestamp,
            counterpartyName: $"{agentName} - {location}");
    }

    private static Transaction? TryParseAirtime(string body, long smsId, long timestamp)
    {
        var m = ParserPatterns.AirtimePattern.Match(body);
        if (!m.Success) return null;

        return BuildTransaction(m, TransactionType.AirtimePurchase, smsId, timestamp,
            counterpartyName: "Safaricom Airtime");
    }

    private static Transaction? TryParseDataBundle(string body, long smsId, long timestamp)
    {
        var m = ParserPatterns.DataBundlePattern.Match(body);
        if (!m.Success) return null;

        return BuildTransaction(m, TransactionType.AirtimePurchase, smsId, timestamp,
            counterpartyName: "Safaricom Data Bundles");
    }

    private static Transaction? TryParseReversal(string body, long smsId, long timestamp)
    {
        var m = ParserPatterns.ReversalPattern.Match(body);
        if (!m.Success) return null;

        return BuildTransaction(m, TransactionType.Reversal, smsId, timestamp,
            counterpartyName: "M-PESA Reversal");
    }

    private static Transaction? TryParseDeposit(string body, long smsId, long timestamp)
    {
        var m = ParserPatterns.DepositPattern.Match(body);
        if (!m.Success) return null;

        return BuildTransaction(m, TransactionType.Deposit, smsId, timestamp,
            counterpartyName: "M-PESA Deposit");
    }

    private static Transaction? TryParseFuliza(string body, long smsId, long timestamp)
    {
        var m = ParserPatterns.FulizaPattern.Match(body);
        if (!m.Success) return null;

        return BuildTransaction(m, TransactionType.Fuliza, smsId, timestamp,
            counterpartyName: "Fuliza M-PESA");
    }

    private static Transaction? TryParseMShwariWithdrawal(string body, long smsId, long timestamp)
    {
        var m = ParserPatterns.MShwariWithdrawalPattern.Match(body);
        if (!m.Success) return null;

        return BuildTransaction(m, TransactionType.MShwari, smsId, timestamp,
            counterpartyName: "M-Shwari Withdrawal");
    }

    private static Transaction? TryParseMShwariDeposit(string body, long smsId, long timestamp)
    {
        var m = ParserPatterns.MShwariDepositPattern.Match(body);
        if (!m.Success) return null;

        return BuildTransaction(m, TransactionType.MShwari, smsId, timestamp,
            counterpartyName: "M-Shwari Deposit");
    }

    private static Transaction? TryParseGlobalPaymentCard(string body, long smsId, long timestamp)
    {
        var m = ParserPatterns.GlobalPaymentCardPattern.Match(body);
        if (!m.Success) return null;

        return BuildTransaction(m, TransactionType.GlobalPayment, smsId, timestamp,
            counterpartyName: m.Groups["name"].Value.Trim(),
            counterpartyNumber: m.Groups["account"].Value.Trim());
    }

    private static Transaction? TryParseGlobalTransferSend(string body, long smsId, long timestamp)
    {
        var m = ParserPatterns.GlobalTransferSendPattern.Match(body);
        if (!m.Success) return null;

        return BuildTransaction(m, TransactionType.GlobalPayment, smsId, timestamp,
            counterpartyName: m.Groups["name"].Value.Trim(),
            counterpartyNumber: m.Groups["mtcn"].Value.Trim());
    }

    private static Transaction? TryParseGlobalTransferReceive(string body, long smsId, long timestamp)
    {
        var m = ParserPatterns.GlobalTransferReceivePattern.Match(body);
        if (!m.Success) return null;

        return BuildTransaction(m, TransactionType.GlobalPayment, smsId, timestamp,
            counterpartyName: m.Groups["name"].Value.Trim());
    }

    private static Transaction? TryParseGlobalVirtualCardPayment(string body, long smsId, long timestamp)
    {
        var m = ParserPatterns.GlobalVirtualCardPattern.Match(body);
        if (!m.Success) return null;

        return BuildTransaction(m, TransactionType.GlobalPayment, smsId, timestamp,
            counterpartyName: m.Groups["name"].Value.Trim(),
            counterpartyNumber: $"card *{m.Groups["card_last4"].Value}");
    }

    // ── Builder ──────────────────────────────────────────────────────────────

    private static Transaction BuildTransaction(
        Match match,
        TransactionType type,
        long smsId,
        long smsTimestamp,
        string counterpartyName = "",
        string? counterpartyNumber = null)
    {
        return new Transaction
        {
            MpesaCode = match.Groups["code"].Value.ToUpperInvariant(),
            Type = type,
            Amount = ParseAmount(match.Groups["amount"].Value),
            BalanceAfterTransaction = ParseAmount(match.Groups["balance"].Value),
            CounterpartyName = counterpartyName,
            CounterpartyNumber = counterpartyNumber,
            TransactionDate = ParseDate(match.Groups["date"].Value, match.Groups["time"].Value, smsTimestamp),
            CategoryId = 0,  // Assigned by AutoCategorizationService after insert
            SmsId = smsId,
            SmsTimestamp = smsTimestamp,
            ImportedAt = DateTime.UtcNow,
            IsEdited = false,
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string NormalizeSms(string raw) =>
        Regex.Replace(raw.Trim(), @"\s+", " ");

    private static decimal ParseAmount(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0m;
        var cleaned = raw.Replace(",", "");
        return decimal.TryParse(cleaned, out var result) ? result : 0m;
    }

    /// <summary>
    /// Parses the date + time from the SMS. Falls back to the SMS timestamp
    /// (converted from Android epoch milliseconds) if parsing fails.
    /// </summary>
    private static DateTime ParseDate(string datePart, string timePart, long fallbackTimestampMs)
    {
        var raw = $"{datePart} {timePart}".Trim();

        // M-Pesa formats seen in the wild:
        // "4/6/25 at 10:34 AM"  → d/M/yy
        // "04/06/2025 at 10:34 AM" → dd/MM/yyyy
        string[] formats =
        [
            "d/M/yy h:mm tt",
            "d/M/yyyy h:mm tt",
            "dd/MM/yyyy h:mm tt",
            "d/M/yy HH:mm",
            "d/M/yyyy HH:mm",
        ];

        if (DateTime.TryParseExact(raw, formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var parsed))
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Local).ToUniversalTime();
        }

        // Fallback: use Android SMS timestamp (ms since Unix epoch)
        return DateTimeOffset.FromUnixTimeMilliseconds(fallbackTimestampMs).UtcDateTime;
    }
}