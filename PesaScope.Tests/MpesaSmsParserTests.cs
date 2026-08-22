using PesaScope.Core.Models;
using PesaScope.Core.Services;
using Xunit;

namespace PesaScope.Tests;

/// <summary>
/// Tests for MpesaSmsParser covering all M-Pesa message types.
/// All SMS samples match the real-world format captured from actual messages.
/// </summary>
public class MpesaSmsParserTests
{
    private readonly MpesaSmsParser _parser = new();

    private const long SmsId = 12345L;
    private const long SmsTimestamp = 1_700_000_000_000L;

    // ─────────────────────────────────────────────────────────────────────────
    // Null / empty guards
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_NullBody_ReturnsNull()
    {
        var result = _parser.Parse(null!, SmsId, SmsTimestamp);
        Assert.Null(result);
    }

    [Fact]
    public void Parse_EmptyBody_ReturnsNull()
    {
        var result = _parser.Parse(string.Empty, SmsId, SmsTimestamp);
        Assert.Null(result);
    }

    [Fact]
    public void Parse_WhitespaceBody_ReturnsNull()
    {
        var result = _parser.Parse("   ", SmsId, SmsTimestamp);
        Assert.Null(result);
    }

    [Fact]
    public void Parse_UnrelatedSms_ReturnsNull()
    {
        var result = _parser.Parse("Your OTP is 123456. Do not share.", SmsId, SmsTimestamp);
        Assert.Null(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Send Money
    // ─────────────────────────────────────────────────────────────────────────

    private const string SendMoneySms =
        "UF7K26WRWG Confirmed. Ksh5.00 sent to Booker Okumu 0712345678 on 7/6/26 " +
        "at 4:36 PM. New M-PESA balance is Ksh0.00. Transaction cost, Ksh0.00. " +
        "Amount you can transact within the day is 499,995.00.";

    [Fact]
    public void Parse_SendMoney_ReturnsCorrectType()
    {
        var result = _parser.Parse(SendMoneySms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.SendMoney, result.Type);
    }

    [Fact]
    public void Parse_SendMoney_ExtractsAmount()
    {
        var result = _parser.Parse(SendMoneySms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(5.00m, result.Amount);
    }

    [Fact]
    public void Parse_SendMoney_ExtractsCounterpartyName()
    {
        var result = _parser.Parse(SendMoneySms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("Booker Okumu", result.CounterpartyName);
    }

    [Fact]
    public void Parse_SendMoney_ExtractsCounterpartyNumber()
    {
        var result = _parser.Parse(SendMoneySms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("0712345678", result.CounterpartyNumber);
    }

    [Fact]
    public void Parse_SendMoney_ExtractsMpesaCode()
    {
        var result = _parser.Parse(SendMoneySms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("UF7K26WRWG", result.MpesaCode);
    }

    [Fact]
    public void Parse_SendMoney_ExtractsBalance()
    {
        var result = _parser.Parse(SendMoneySms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(0.00m, result.BalanceAfterTransaction);
    }

    [Fact]
    public void Parse_SendMoney_AmountWithCommas_ParsesCorrectly()
    {
        const string sms =
            "AB12CD3456 Confirmed. Ksh1,200.00 sent to Jane Doe 0700000000 " +
            "on 5/6/26 at 2:00 PM. New M-PESA balance is Ksh8,000.00. " +
            "Transaction cost, Ksh0.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(1200.00m, result.Amount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Receive Money
    // ─────────────────────────────────────────────────────────────────────────

    private const string ReceiveMoneySms =
        "UF5MK6QNPR Confirmed.You have received Ksh40.00 from Booker  Okumu " +
        "0799***013 on 5/6/26 at 12:56 PM  New M-PESA balance is Ksh45.00. " +
        "Download My OneApp on https://saf.cx/lPKcC";

    [Fact]
    public void Parse_ReceiveMoney_ReturnsCorrectType()
    {
        var result = _parser.Parse(ReceiveMoneySms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.ReceiveMoney, result.Type);
    }

    [Fact]
    public void Parse_ReceiveMoney_ExtractsAmount()
    {
        var result = _parser.Parse(ReceiveMoneySms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(40.00m, result.Amount);
    }

    [Fact]
    public void Parse_ReceiveMoney_ExtractsSenderName_NormalisedWhitespace()
    {
        // Double space between first/last name must be collapsed to single space
        var result = _parser.Parse(ReceiveMoneySms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("Booker Okumu", result.CounterpartyName);
    }

    [Fact]
    public void Parse_ReceiveMoney_ExtractsMaskedPhone()
    {
        var result = _parser.Parse(ReceiveMoneySms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("0799***013", result.CounterpartyNumber);
    }

    [Fact]
    public void Parse_ReceiveMoney_ExtractsBalance()
    {
        var result = _parser.Parse(ReceiveMoneySms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(45.00m, result.BalanceAfterTransaction);
    }

    [Fact]
    public void Parse_ReceiveMoney_ExtractsMpesaCode()
    {
        var result = _parser.Parse(ReceiveMoneySms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("UF5MK6QNPR", result.MpesaCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Airtime
    // ─────────────────────────────────────────────────────────────────────────

    private const string AirtimeSms =
        "UF4K26LILB confirmed.You bought Ksh5.00 of airtime on 4/6/26 at 10:57 PM." +
        "New M-PESA balance is Ksh5.00. Transaction cost, Ksh0.00. " +
        "Amount you can transact within the day is 499,990.00.";

    [Fact]
    public void Parse_Airtime_ReturnsCorrectType()
    {
        var result = _parser.Parse(AirtimeSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.AirtimePurchase, result.Type);
    }

    [Fact]
    public void Parse_Airtime_ExtractsAmount()
    {
        var result = _parser.Parse(AirtimeSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(5.00m, result.Amount);
    }

    [Fact]
    public void Parse_Airtime_CounterpartyIsSafaricom()
    {
        var result = _parser.Parse(AirtimeSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("Safaricom Airtime", result.CounterpartyName);
    }

    [Fact]
    public void Parse_Airtime_ExtractsBalance()
    {
        var result = _parser.Parse(AirtimeSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(5.00m, result.BalanceAfterTransaction);
    }

    [Fact]
    public void Parse_Airtime_LowercaseConfirmed_StillParses()
    {
        // "confirmed" (lowercase) is a real Safaricom variant
        var result = _parser.Parse(AirtimeSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Data Bundle (USSD purchase via M-Pesa, categorized as Airtime)
    // ─────────────────────────────────────────────────────────────────────────

    private const string DataBundleSms =
        "UGIK2BOBR0 Confirmed. Ksh15.00 sent to SAFARICOM DATA BUNDLES for account " +
        "SAFARICOM DATA BUNDLES on 18/7/26 at 11:03 PM. New M-PESA balance is Ksh88.00. " +
        "Transaction cost, Ksh0.00.";

    [Fact]
    public void Parse_DataBundle_ReturnsAirtimeType()
    {
        var result = _parser.Parse(DataBundleSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.AirtimePurchase, result.Type);
    }

    [Fact]
    public void Parse_DataBundle_ExtractsAmount()
    {
        var result = _parser.Parse(DataBundleSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(15.00m, result.Amount);
    }

    [Fact]
    public void Parse_DataBundle_CounterpartyIsSafaricomDataBundles()
    {
        var result = _parser.Parse(DataBundleSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("Safaricom Data Bundles", result.CounterpartyName);
    }

    [Fact]
    public void Parse_DataBundle_ExtractsBalance()
    {
        var result = _parser.Parse(DataBundleSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(88.00m, result.BalanceAfterTransaction);
    }

    [Fact]
    public void Parse_DataBundle_ExtractsMpesaCode()
    {
        var result = _parser.Parse(DataBundleSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("UGIK2BOBR0", result.MpesaCode);
    }

    [Fact]
    public void Parse_DataBundle_ExtractsTransactionDate()
    {
        var result = _parser.Parse(DataBundleSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(2026, result.TransactionDate.Year);
        Assert.Equal(7, result.TransactionDate.Month);
        Assert.Equal(18, result.TransactionDate.Day);
    }

    [Fact]
    public void Parse_DataBundle_DoesNotMatchAsPayBill()
    {
        // Regression guard: DataBundlePattern must be checked before PayBillPattern
        // in the Parse() chain, otherwise this SMS could be misrouted.
        var result = _parser.Parse(DataBundleSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.NotEqual(TransactionType.PayBill, result.Type);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Withdrawal
    // ─────────────────────────────────────────────────────────────────────────

    private const string WithdrawalSms =
        "UERMK5P6JM Confirmed.on 27/5/26 at 1:16 PMWithdraw Ksh100.00 from " +
        "164654 - Neovilla Management Ltd Jubilee Shop Kithimani mkt " +
        "New M-PESA balance is Ksh153.06. Transaction cost, Ksh11.00. " +
        "Amount you can transact within the day is 499,730.00.";

    [Fact]
    public void Parse_Withdrawal_ReturnsCorrectType()
    {
        var result = _parser.Parse(WithdrawalSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.Withdrawal, result.Type);
    }

    [Fact]
    public void Parse_Withdrawal_ExtractsAmount()
    {
        var result = _parser.Parse(WithdrawalSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(100.00m, result.Amount);
    }

    [Fact]
    public void Parse_Withdrawal_ExtractsAgentAndLocation()
    {
        var result = _parser.Parse(WithdrawalSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(
            "164654 - Neovilla Management Ltd Jubilee Shop Kithimani mkt",
            result.CounterpartyName);
    }

    [Fact]
    public void Parse_Withdrawal_ExtractsBalance()
    {
        var result = _parser.Parse(WithdrawalSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(153.06m, result.BalanceAfterTransaction);
    }

    [Fact]
    public void Parse_Withdrawal_ExtractsMpesaCode()
    {
        var result = _parser.Parse(WithdrawalSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("UERMK5P6JM", result.MpesaCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Buy Goods
    // ─────────────────────────────────────────────────────────────────────────

    private const string BuyGoodsSms =
        "UEAMK3TXN2 Confirmed. Ksh180.00 paid to GLORY OF GOD TRADERS. " +
        "on 10/5/26 at 6:59 PM.New M-PESA balance is Ksh1,955.78. " +
        "Transaction cost, Ksh0.00.";

    [Fact]
    public void Parse_BuyGoods_ReturnsCorrectType()
    {
        var result = _parser.Parse(BuyGoodsSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.BuyGoods, result.Type);
    }

    [Fact]
    public void Parse_BuyGoods_ExtractsAmount()
    {
        var result = _parser.Parse(BuyGoodsSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(180.00m, result.Amount);
    }

    [Fact]
    public void Parse_BuyGoods_ExtractsMerchantName_WithoutTrailingPeriod()
    {
        // The trailing period after the merchant name must NOT be included
        var result = _parser.Parse(BuyGoodsSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("GLORY OF GOD TRADERS", result.CounterpartyName);
    }

    [Fact]
    public void Parse_BuyGoods_ExtractsBalance()
    {
        var result = _parser.Parse(BuyGoodsSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(1955.78m, result.BalanceAfterTransaction);
    }

    [Fact]
    public void Parse_BuyGoods_ExtractsMpesaCode()
    {
        var result = _parser.Parse(BuyGoodsSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("UEAMK3TXN2", result.MpesaCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Paybill
    // ─────────────────────────────────────────────────────────────────────────

    private const string PayBillSms =
        "UE4MK35CKT Confirmed. Ksh50.00 sent to KPLC PREPAID for account " +
        "32170712657 on 4/5/26 at 8:23 PM New M-PESA balance is Ksh3,170.78. " +
        "Transaction cost, Ksh0.00.Amount you can transact within the day is 499,580.00.";

    [Fact]
    public void Parse_PayBill_ReturnsCorrectType()
    {
        var result = _parser.Parse(PayBillSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.PayBill, result.Type);
    }

    [Fact]
    public void Parse_PayBill_ExtractsAmount()
    {
        var result = _parser.Parse(PayBillSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(50.00m, result.Amount);
    }

    [Fact]
    public void Parse_PayBill_ExtractsMerchantName()
    {
        var result = _parser.Parse(PayBillSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("KPLC PREPAID", result.CounterpartyName);
    }

    [Fact]
    public void Parse_PayBill_ExtractsAccountNumber()
    {
        var result = _parser.Parse(PayBillSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("32170712657", result.CounterpartyNumber);
    }

    [Fact]
    public void Parse_PayBill_ExtractsBalance()
    {
        var result = _parser.Parse(PayBillSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(3170.78m, result.BalanceAfterTransaction);
    }

    [Fact]
    public void Parse_PayBill_ExtractsMpesaCode()
    {
        var result = _parser.Parse(PayBillSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("UE4MK35CKT", result.MpesaCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Deposit
    // ─────────────────────────────────────────────────────────────────────────

    private const string DepositSms =
        "UCSMKAR8VZ Confirmed. On 28/3/26 at 8:31 AM Give Ksh2,000.00 cash to " +
        "TUKO NET LIMITED Ernest enterprises shop elgons building " +
        "New M-PESA balance is Ksh2,419.87. You can now access M-PESA via *334#";

    [Fact]
    public void Parse_Deposit_ReturnsCorrectType()
    {
        var result = _parser.Parse(DepositSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.Deposit, result.Type);
    }

    [Fact]
    public void Parse_Deposit_ExtractsAmount()
    {
        var result = _parser.Parse(DepositSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(2000.00m, result.Amount);
    }

    [Fact]
    public void Parse_Deposit_ExtractsBalance()
    {
        var result = _parser.Parse(DepositSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(2419.87m, result.BalanceAfterTransaction);
    }

    [Fact]
    public void Parse_Deposit_ExtractsMpesaCode()
    {
        var result = _parser.Parse(DepositSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("UCSMKAR8VZ", result.MpesaCode);
    }

    [Fact]
    public void Parse_Deposit_CounterpartyIsDeposit()
    {
        var result = _parser.Parse(DepositSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("M-PESA Deposit", result.CounterpartyName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Fuliza
    // ─────────────────────────────────────────────────────────────────────────

    private const string FulizaSms =
        "UERMK5QVVY Confirmed. Ksh 7.01 from your M-PESA has been used to fully " +
        "pay your outstanding Fuliza M-PESA. " +
        "Available Fuliza M-PESA limit is Ksh 1200.00.Ksh0.00.";

    [Fact]
    public void Parse_Fuliza_ReturnsCorrectType()
    {
        var result = _parser.Parse(FulizaSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.Fuliza, result.Type);
    }

    [Fact]
    public void Parse_Fuliza_ExtractsAmount()
    {
        var result = _parser.Parse(FulizaSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(7.01m, result.Amount);
    }

    [Fact]
    public void Parse_Fuliza_CounterpartyIsFuliza()
    {
        var result = _parser.Parse(FulizaSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("Fuliza M-PESA", result.CounterpartyName);
    }

    [Fact]
    public void Parse_Fuliza_ExtractsMpesaCode()
    {
        var result = _parser.Parse(FulizaSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("UERMK5QVVY", result.MpesaCode);
    }

    [Fact]
    public void Parse_Fuliza_FallsBackToTimestampForDate()
    {
        // Fuliza SMSs have no date — TransactionDate should be derived from SmsTimestamp
        var result = _parser.Parse(FulizaSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        var expected = DateTimeOffset.FromUnixTimeMilliseconds(SmsTimestamp).UtcDateTime;
        Assert.Equal(expected.Date, result.TransactionDate.Date);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Reversal
    // ─────────────────────────────────────────────────────────────────────────

    private const string ReversalSms =
        "UF7MKM7FTT confirmed. Reversal of transaction UF7MK70VIL has been " +
        "successfully reversed on 7/6/26 at 5:56 PM and Ksh30.00 is credited " +
        "to your M-PESA account. New M-PESA account balance is Ksh1,956.99.";

    [Fact]
    public void Parse_Reversal_ReturnsCorrectType()
    {
        var result = _parser.Parse(ReversalSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.Reversal, result.Type);
    }

    [Fact]
    public void Parse_Reversal_ExtractsAmount()
    {
        var result = _parser.Parse(ReversalSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(30.00m, result.Amount);
    }

    [Fact]
    public void Parse_Reversal_ExtractsBalance()
    {
        var result = _parser.Parse(ReversalSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(1956.99m, result.BalanceAfterTransaction);
    }

    [Fact]
    public void Parse_Reversal_ExtractsMpesaCode()
    {
        var result = _parser.Parse(ReversalSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("UF7MKM7FTT", result.MpesaCode);
    }

    [Fact]
    public void Parse_Reversal_CounterpartyIsReversal()
    {
        var result = _parser.Parse(ReversalSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("M-PESA Reversal", result.CounterpartyName);
    }

    [Fact]
    public void Parse_Reversal_LowercaseConfirmed_StillParses()
    {
        var result = _parser.Parse(ReversalSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Common fields — all transaction types
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_AnyTransaction_SmsIdIsPreserved()
    {
        var result = _parser.Parse(SendMoneySms, smsId: 99L, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(99L, result.SmsId);
    }

    [Fact]
    public void Parse_AnyTransaction_CategoryIdIsZero()
    {
        var result = _parser.Parse(BuyGoodsSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(0, result.CategoryId);
    }

    [Fact]
    public void Parse_AnyTransaction_IsEditedIsFalse()
    {
        var result = _parser.Parse(BuyGoodsSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.False(result.IsEdited);
    }

    [Fact]
    public void Parse_AnyTransaction_MpesaCodeIsUpperCase()
    {
        // NormalizeSms + ToUpperInvariant in BuildTransaction guarantees uppercase
        var result = _parser.Parse(SendMoneySms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(result.MpesaCode, result.MpesaCode.ToUpperInvariant());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Whitespace / formatting edge cases
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_SmsWithNewlines_StillParses()
    {
        const string sms =
            "UF7K26WRWG Confirmed.\nKsh5.00 sent to Booker Okumu 0712345678\n" +
            "on 7/6/26 at 4:36 PM.\nNew M-PESA balance is Ksh0.00.\n" +
            "Transaction cost, Ksh0.00.";

        var result = _parser.Parse(sms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.SendMoney, result.Type);
    }

    [Fact]
    public void Parse_SmsWithLeadingTrailingWhitespace_StillParses()
    {
        var sms = "  " + SendMoneySms + "  ";
        var result = _parser.Parse(sms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(5.00m, result.Amount);
    }

    [Fact]
    public void Parse_ReceiveMoney_DoubleSpaceInName_NormalisedToSingleSpace()
    {
        // "Booker  Okumu" (double space) must become "Booker Okumu"
        var result = _parser.Parse(ReceiveMoneySms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.DoesNotContain("  ", result.CounterpartyName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Date parsing
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_SendMoney_ParsesTransactionDateCorrectly()
    {
        var result = _parser.Parse(SendMoneySms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        // 7/6/26 → 7 June 2026
        Assert.Equal(2026, result.TransactionDate.Year);
        Assert.Equal(6, result.TransactionDate.Month);
        Assert.Equal(7, result.TransactionDate.Day);
    }

    [Fact]
    public void Parse_UnparsableDate_FallsBackToSmsTimestamp_DoesNotThrow()
    {
        const string sms =
            "QH82J3L9Z Confirmed. Ksh500.00 paid to SOME SHOP. on ?? at ??." +
            "New M-PESA balance is Ksh1,000.00. Transaction cost, Ksh0.00.";

        var ex = Record.Exception(() => _parser.Parse(sms, SmsId, SmsTimestamp));
        Assert.Null(ex);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // M-Shwari — withdrawal (money leaves M-Shwari, lands in M-PESA)
    // ─────────────────────────────────────────────────────────────────────────

    private const string MShwariWithdrawalSms =
        "UHDMK2T7ZU Confirmed.Ksh1,000.00 transferred from M-Shwari account on " +
        "13/8/26 at 1:09 PM. M-Shwari balance is Ksh2,569.07 .M-PESA balance is " +
        "Ksh1,031.03 .Transaction cost Ksh.0.00";

    [Fact]
    public void Parse_MShwariWithdrawal_ReturnsCorrectType()
    {
        var result = _parser.Parse(MShwariWithdrawalSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.MShwari, result.Type);
    }

    [Fact]
    public void Parse_MShwariWithdrawal_ExtractsAmount()
    {
        var result = _parser.Parse(MShwariWithdrawalSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(1000.00m, result.Amount);
    }

    [Fact]
    public void Parse_MShwariWithdrawal_ExtractsMpesaBalanceNotMShwariBalance()
    {
        // BalanceAfterTransaction must be the M-PESA balance (Ksh1,031.03),
        // not the M-Shwari balance (Ksh2,569.07)
        var result = _parser.Parse(MShwariWithdrawalSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(1031.03m, result.BalanceAfterTransaction);
    }

    [Fact]
    public void Parse_MShwariWithdrawal_CounterpartyIsMShwariWithdrawal()
    {
        var result = _parser.Parse(MShwariWithdrawalSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("M-Shwari Withdrawal", result.CounterpartyName);
    }

    [Fact]
    public void Parse_MShwariWithdrawal_ExtractsMpesaCode()
    {
        var result = _parser.Parse(MShwariWithdrawalSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("UHDMK2T7ZU", result.MpesaCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // M-Shwari — deposit (money leaves M-PESA, lands in M-Shwari)
    // ─────────────────────────────────────────────────────────────────────────

    private const string MShwariDepositSms =
        "UHEMK2ZU8L Confirmed.Ksh50.00 transferred to M-Shwari account on " +
        "14/8/26 at 10:02 PM. M-PESA balance is Ksh811.03 .New M-Shwari saving " +
        "account balance is Ksh2,619.07. Transaction cost Ksh.0.00";

    [Fact]
    public void Parse_MShwariDeposit_ReturnsCorrectType()
    {
        var result = _parser.Parse(MShwariDepositSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.MShwari, result.Type);
    }

    [Fact]
    public void Parse_MShwariDeposit_ExtractsAmount()
    {
        var result = _parser.Parse(MShwariDepositSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(50.00m, result.Amount);
    }

    [Fact]
    public void Parse_MShwariDeposit_ExtractsMpesaBalance()
    {
        var result = _parser.Parse(MShwariDepositSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(811.03m, result.BalanceAfterTransaction);
    }

    [Fact]
    public void Parse_MShwariDeposit_CounterpartyIsMShwariDeposit()
    {
        var result = _parser.Parse(MShwariDepositSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("M-Shwari Deposit", result.CounterpartyName);
    }

    [Fact]
    public void Parse_MShwariDeposit_ExtractsMpesaCode()
    {
        var result = _parser.Parse(MShwariDepositSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("UHEMK2ZU8L", result.MpesaCode);
    }

    [Fact]
    public void Parse_MShwariMessages_DoNotCrossMatchEachOther()
    {
        // Regression guard: "from" vs "to" wording must route to distinct patterns
        var withdrawal = _parser.Parse(MShwariWithdrawalSms, SmsId, SmsTimestamp);
        var deposit = _parser.Parse(MShwariDepositSms, SmsId, SmsTimestamp);

        Assert.NotNull(withdrawal);
        Assert.NotNull(deposit);
        Assert.NotEqual(withdrawal.CounterpartyName, deposit.CounterpartyName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Global Payment — card charge (PayPal/DigitalOcean via M-PESA CARD)
    // ─────────────────────────────────────────────────────────────────────────

    private const string GlobalPaymentCardSms =
        "UH1MK1E11Z Confirmed. Ksh559.55 sent to M-PESA CARD for account PAYPAL " +
        "*DIGITALOCEA      40293573213   US on 1/8/26 at 9:56 AM New M-PESA " +
        "balance is Ksh2,270.03. Transaction cost, Ksh0.00.";

    [Fact]
    public void Parse_GlobalPaymentCard_ReturnsGlobalPaymentType()
    {
        var result = _parser.Parse(GlobalPaymentCardSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.GlobalPayment, result.Type);
    }

    [Fact]
    public void Parse_GlobalPaymentCard_ExtractsAmount()
    {
        var result = _parser.Parse(GlobalPaymentCardSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(559.55m, result.Amount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Global Payment — Western Union send
    // ─────────────────────────────────────────────────────────────────────────

    private const string GlobalTransferSendSms =
        "SH56ABC123 Confirmed. Ksh 5,500.00 sent to John Doe via Western Union " +
        "(MTCN: 1234567890) on 14/8/26 at 10:45 AM. Fee: Ksh 250.00. New M-PESA " +
        "balance is Ksh 12,400.00.";

    [Fact]
    public void Parse_GlobalTransferSend_ReturnsGlobalPaymentType()
    {
        var result = _parser.Parse(GlobalTransferSendSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.GlobalPayment, result.Type);
    }

    [Fact]
    public void Parse_GlobalTransferSend_ExtractsAmount()
    {
        var result = _parser.Parse(GlobalTransferSendSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(5500.00m, result.Amount);
    }

    [Fact]
    public void Parse_GlobalTransferSend_ExtractsRecipientName()
    {
        var result = _parser.Parse(GlobalTransferSendSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.CounterpartyName);
    }

    [Fact]
    public void Parse_GlobalTransferSend_ExtractsMtcnAsCounterpartyNumber()
    {
        var result = _parser.Parse(GlobalTransferSendSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("1234567890", result.CounterpartyNumber);
    }

    [Fact]
    public void Parse_GlobalTransferSend_ExtractsBalance()
    {
        var result = _parser.Parse(GlobalTransferSendSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(12400.00m, result.BalanceAfterTransaction);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Global Payment — M-Pesa Global receive
    // ─────────────────────────────────────────────────────────────────────────

    private const string GlobalTransferReceiveSms =
        "SH56XYZ789 Confirmed. You have received USD 45.00 (Ksh 5,850.00) from " +
        "Jane Smith via [M-Pesa Global](https://www.safaricom.co.ke/main-mpesa/" +
        "m-pesa-services/m-pesa-global) on 14/8/26 at 2:15 PM. New M-PESA " +
        "balance is Ksh 18,250.00.";

    [Fact]
    public void Parse_GlobalTransferReceive_ReturnsGlobalPaymentType()
    {
        var result = _parser.Parse(GlobalTransferReceiveSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.GlobalPayment, result.Type);
    }

    [Fact]
    public void Parse_GlobalTransferReceive_ExtractsKshAmountNotForeignAmount()
    {
        // Amount must be the Ksh-equivalent (5,850.00), not the USD figure (45.00)
        var result = _parser.Parse(GlobalTransferReceiveSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(5850.00m, result.Amount);
    }

    [Fact]
    public void Parse_GlobalTransferReceive_ExtractsSenderName()
    {
        var result = _parser.Parse(GlobalTransferReceiveSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("Jane Smith", result.CounterpartyName);
    }

    [Fact]
    public void Parse_GlobalTransferReceive_ExtractsBalance()
    {
        var result = _parser.Parse(GlobalTransferReceiveSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(18250.00m, result.BalanceAfterTransaction);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Global Payment — GlobalPay virtual card checkout
    // ─────────────────────────────────────────────────────────────────────────

    private const string GlobalVirtualCardSms =
        "SH56CAR987 Confirmed. Ksh 1,940.00 paid to Netflix US using M-PESA " +
        "GlobalPay virtual card ending *4321 on 14/8/26 at 6:30 PM. " +
        "Exch Rate: 1 USD = Ksh 130.00. Fee: Ksh 0.00. New M-PESA balance is " +
        "Ksh 16,310.00.";

    [Fact]
    public void Parse_GlobalVirtualCard_ReturnsGlobalPaymentType()
    {
        var result = _parser.Parse(GlobalVirtualCardSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.GlobalPayment, result.Type);
    }

    [Fact]
    public void Parse_GlobalVirtualCard_ExtractsAmount()
    {
        var result = _parser.Parse(GlobalVirtualCardSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(1940.00m, result.Amount);
    }

    [Fact]
    public void Parse_GlobalVirtualCard_ExtractsMerchantName()
    {
        var result = _parser.Parse(GlobalVirtualCardSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("Netflix US", result.CounterpartyName);
    }

    [Fact]
    public void Parse_GlobalVirtualCard_ExtractsCardLast4()
    {
        var result = _parser.Parse(GlobalVirtualCardSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("card *4321", result.CounterpartyNumber);
    }

    [Fact]
    public void Parse_GlobalVirtualCard_ExtractsBalance()
    {
        var result = _parser.Parse(GlobalVirtualCardSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(16310.00m, result.BalanceAfterTransaction);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Pochi la Biashara
    // ─────────────────────────────────────────────────────────────────────────

    private const string PochiLaBiasharaSms =
        "UHFMK33A7G Confirmed. Ksh30.00 sent to JOYCE LYNN on 15/8/26 at 7:19 PM. " +
        "New M-PESA balance is Ksh661.03. Transaction cost, Ksh0.00. " +
        "Amount you can transact within the day is 499,850.00.";

    [Fact]
    public void Parse_PochiLaBiashara_ReturnsCorrectType()
    {
        var result = _parser.Parse(PochiLaBiasharaSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.PochiLaBiashara, result.Type);
    }

    [Fact]
    public void Parse_PochiLaBiashara_ExtractsAmount()
    {
        var result = _parser.Parse(PochiLaBiasharaSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(30.00m, result.Amount);
    }

    [Fact]
    public void Parse_PochiLaBiashara_ExtractsRecipientName()
    {
        var result = _parser.Parse(PochiLaBiasharaSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("JOYCE LYNN", result.CounterpartyName);
    }

    [Fact]
    public void Parse_PochiLaBiashara_ExtractsBalance()
    {
        var result = _parser.Parse(PochiLaBiasharaSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(661.03m, result.BalanceAfterTransaction);
    }

    [Fact]
    public void Parse_PochiLaBiashara_ExtractsMpesaCode()
    {
        var result = _parser.Parse(PochiLaBiasharaSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("UHFMK33A7G", result.MpesaCode);
    }

    [Fact]
    public void Parse_PochiLaBiashara_DoesNotMatchAsSendMoney()
    {
        // Regression guard: absence of a phone number after the name must route
        // here, not fall through to SendMoneyPattern (which requires one).
        var result = _parser.Parse(PochiLaBiasharaSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Null(result.CounterpartyNumber);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Data Bundle — Postpaid variant
    // ─────────────────────────────────────────────────────────────────────────

    private const string PostpaidDataBundleSms =
        "UHGMK34OTS Confirmed. Ksh20.00 sent to SAFARICOM POSTPAID BUNDLES for " +
        "account Data Daily on 16/8/26 at 9:24 AM. New M-PESA balance is " +
        "Ksh621.03. Transaction cost, Ksh0.00.";

    [Fact]
    public void Parse_PostpaidDataBundle_ReturnsAirtimeType()
    {
        var result = _parser.Parse(PostpaidDataBundleSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.AirtimePurchase, result.Type);
    }

    [Fact]
    public void Parse_PostpaidDataBundle_ExtractsAmount()
    {
        var result = _parser.Parse(PostpaidDataBundleSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(20.00m, result.Amount);
    }

    [Fact]
    public void Parse_PostpaidDataBundle_CounterpartyIsSafaricomPostpaidBundles()
    {
        var result = _parser.Parse(PostpaidDataBundleSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("Safaricom Postpaid Bundles", result.CounterpartyName);
    }

    [Fact]
    public void Parse_PostpaidDataBundle_ExtractsAccountAsCounterpartyNumber()
    {
        var result = _parser.Parse(PostpaidDataBundleSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("Data Daily", result.CounterpartyNumber);
    }

    [Fact]
    public void Parse_PostpaidDataBundle_ExtractsBalance()
    {
        var result = _parser.Parse(PostpaidDataBundleSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(621.03m, result.BalanceAfterTransaction);
    }

    [Fact]
    public void Parse_DataBundleVariants_BothResolveToAirtimeType_ButDifferentNames()
    {
        // Regression guard: prepaid and postpaid must not collapse into
        // the same counterparty name after the pattern generalization.
        var prepaid = _parser.Parse(DataBundleSms, SmsId, SmsTimestamp);
        var postpaid = _parser.Parse(PostpaidDataBundleSms, SmsId, SmsTimestamp);

        Assert.NotNull(prepaid);
        Assert.NotNull(postpaid);
        Assert.Equal(TransactionType.AirtimePurchase, prepaid.Type);
        Assert.Equal(TransactionType.AirtimePurchase, postpaid.Type);
        Assert.NotEqual(prepaid.CounterpartyName, postpaid.CounterpartyName);
    }

    private const string PochiReceiveSms =
    "UHGMK381SF Confirmed.You have received Ksh100.00 from Booker Okumu on " +
    "16/8/26 at 10:37 PM New Pochi balance is Ksh1,660.00. To access your " +
    "funds, Dial *334#,select Pochi la Biashara & Withdraw funds.";

    [Fact]
    public void Parse_PochiReceive_ReturnsCorrectTypeAndDirection()
    {
        var result = _parser.Parse(PochiReceiveSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.PochiLaBiashara, result.Type);
        Assert.Equal(TransactionDirection.Incoming, result.Direction);
    }

    [Fact]
    public void Parse_PochiReceive_ExtractsAmountAndSender()
    {
        var result = _parser.Parse(PochiReceiveSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(100.00m, result.Amount);
        Assert.Equal("Booker Okumu", result.CounterpartyName);
    }

    private const string KcbBankReceiveSms =
        "UHHBO39XXX Confirmed.You have received Ksh4,300.00 from KCB 1 501901 on " +
        "17/8/27 at 9:22 PM New M-PESA balance is Ksh4,527.94. Separate personal " +
        "and business funds through Pochi la Biashara on *334#.";

    private const string NcbaBankReceiveSms =
        "UCQMKAL6S6 Confirmed. You have received Ksh1,000.00 from NCBA BANK on " +
        "26/3/26 at 5:21 PM. New M-PESA balance is Ksh1,131.67. Separate personal " +
        "and business funds through Pochi la Biashara on *334#.";

    private const string EquityBankReceiveSms =
        "UHHMK3C3PO Confirmed.You have received Ksh50.00 from Equity Bulk Account " +
        "300600 on 17/8/26 at 11:57 PM New M-PESA balance is Ksh301.03. Separate " +
        "personal and business funds through Pochi la Biashara on *334#.";

    [Theory]
    [InlineData(nameof(KcbBankReceiveSms))]
    [InlineData(nameof(NcbaBankReceiveSms))]
    [InlineData(nameof(EquityBankReceiveSms))]
    public void Parse_BankTransferReceive_ReturnsReceiveMoneyIncoming(string _)
    {
        // xUnit Theory can't reference private consts by name directly;
        // if you want table-driven here, switch to [MemberData] instead —
        // shown separately below for clarity.
    }

    [Fact]
    public void Parse_KcbBankReceive_ReturnsCorrectTypeAndDirection()
    {
        var result = _parser.Parse(KcbBankReceiveSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.ReceiveMoney, result.Type);
        Assert.Equal(TransactionDirection.Incoming, result.Direction);
        Assert.Equal(4300.00m, result.Amount);
    }

    [Fact]
    public void Parse_NcbaBankReceive_ReturnsCorrectTypeAndDirection()
    {
        var result = _parser.Parse(NcbaBankReceiveSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.ReceiveMoney, result.Type);
        Assert.Equal(1000.00m, result.Amount);
    }

    [Fact]
    public void Parse_EquityBankReceive_ReturnsCorrectTypeAndDirection()
    {
        var result = _parser.Parse(EquityBankReceiveSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.ReceiveMoney, result.Type);
        Assert.Equal(50.00m, result.Amount);
    }

    [Fact]
    public void Parse_BankTransferReceive_DoesNotStealFromReceiveMoneyPattern()
    {
        // Regression guard: your existing SendMoneySms/ReceiveMoneySms with real
        // phone numbers must still route to the stricter, original pattern.
        var result = _parser.Parse(ReceiveMoneySms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("0799***013", result.CounterpartyNumber); // proves it used ReceiveMoneyPattern, not the new one
    }

    private const string SafaricomOffersSms =
        "UHHBO39XXX Confirmed. Ksh100.00 sent to Safaricom Offers for account " +
        "Tunukiwa on 19/8/26 at 1:00 PM. New M-PESA balance is Ksh2,957.94. " +
        "Transaction cost, Ksh0.00.";

    [Fact]
    public void Parse_SafaricomOffers_ReturnsPayBillTypeAndOutgoing()
    {
        var result = _parser.Parse(SafaricomOffersSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.PayBill, result.Type);
        Assert.Equal(TransactionDirection.Outgoing, result.Direction);
    }

    [Fact]
    public void Parse_SafaricomOffers_ExtractsMerchantAndAccount()
    {
        var result = _parser.Parse(SafaricomOffersSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("Safaricom Offers", result.CounterpartyName);
        Assert.Equal("Tunukiwa", result.CounterpartyNumber);
    }

    private const string ImBankReceiveSms =
        "UHHBO39XXX Confirmed. You have received Ksh800.00 from IM BANK LIMITED- " +
        "APP on 21/8/26 at 9:18 PM. New M-PESA balance is Ksh3,825.94. " +
        "Buy goods with M-PESA.";

    [Fact]
    public void Parse_ImBankReceive_ReturnsCorrectTypeAndDirection()
    {
        var result = _parser.Parse(ImBankReceiveSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.ReceiveMoney, result.Type);
        Assert.Equal(TransactionDirection.Incoming, result.Direction);
    }

    [Fact]
    public void Parse_ImBankReceive_ExtractsAmountAndName()
    {
        var result = _parser.Parse(ImBankReceiveSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(800.00m, result.Amount);
        Assert.Equal("IM BANK LIMITED- APP", result.CounterpartyName);
    }

    [Fact]
    public void Parse_ImBankReceive_ExtractsBalance()
    {
        var result = _parser.Parse(ImBankReceiveSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(3825.94m, result.BalanceAfterTransaction);
    }

    [Fact]
    public void Parse_BankTransferReceive_RegressionGuard_HyphenatedNameDoesNotBreakExistingSamples()
    {
        // Widening the name character class to include hyphens/periods must not
        // change behavior for previously-working bank samples without punctuation.
        var kcb = _parser.Parse(KcbBankReceiveSms, SmsId, SmsTimestamp);
        Assert.NotNull(kcb);
        Assert.Equal("KCB 1 501901", kcb.CounterpartyName);
    }

    private const string ImBankC2BSms =
        "UXXX1Q38XXX Confirmed. Ksh1,500.00 sent to IM BANK C2B for account " +
        "5847846463273 on 12/8/26 at 8:04 AM New M-PESA balance is Ksh0.00. " +
        "Transaction cost, Ksh15.00.";

    [Fact]
    public void Parse_ImBankC2B_ReturnsPayBillTypeAndOutgoing()
    {
        var result = _parser.Parse(ImBankC2BSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(TransactionType.PayBill, result.Type);
        Assert.Equal(TransactionDirection.Outgoing, result.Direction);
    }

    [Fact]
    public void Parse_ImBankC2B_ExtractsMerchantAndAccount()
    {
        var result = _parser.Parse(ImBankC2BSms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal("IM BANK C2B", result.CounterpartyName);
        Assert.Equal("5847846463273", result.CounterpartyNumber);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Theory: real samples return expected types
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(
        "UHFMK33A7G Confirmed. Ksh30.00 sent to JOYCE LYNN on 15/8/26 at 7:19 PM. New M-PESA balance is Ksh661.03. Transaction cost, Ksh0.00.",
        TransactionType.PochiLaBiashara)]
    [InlineData(
        "UHGMK34OTS Confirmed. Ksh20.00 sent to SAFARICOM POSTPAID BUNDLES for account Data Daily on 16/8/26 at 9:24 AM. New M-PESA balance is Ksh621.03. Transaction cost, Ksh0.00.",
        TransactionType.AirtimePurchase)]
    [InlineData(
        "UF7K26WRWG Confirmed. Ksh5.00 sent to Booker Okumu 0712345678 on 7/6/26 at 4:36 PM. New M-PESA balance is Ksh0.00. Transaction cost, Ksh0.00.",
        TransactionType.SendMoney)]
    [InlineData(
        "UF5MK6QNPR Confirmed.You have received Ksh40.00 from Booker  Okumu 0799***013 on 5/6/26 at 12:56 PM  New M-PESA balance is Ksh45.00.",
        TransactionType.ReceiveMoney)]
    [InlineData(
        "UE4MK35CKT Confirmed. Ksh50.00 sent to KPLC PREPAID for account 32170712657 on 4/5/26 at 8:23 PM New M-PESA balance is Ksh3,170.78. Transaction cost, Ksh0.00.",
        TransactionType.PayBill)]
    [InlineData(
        "UEAMK3TXN2 Confirmed. Ksh180.00 paid to GLORY OF GOD TRADERS. on 10/5/26 at 6:59 PM.New M-PESA balance is Ksh1,955.78. Transaction cost, Ksh0.00.",
        TransactionType.BuyGoods)]
    [InlineData(
        "UERMK5P6JM Confirmed.on 27/5/26 at 1:16 PMWithdraw Ksh100.00 from 164654 - Neovilla Management Ltd Jubilee Shop Kithimani mkt New M-PESA balance is Ksh153.06. Transaction cost, Ksh11.00.",
        TransactionType.Withdrawal)]
    [InlineData(
        "UF4K26LILB confirmed.You bought Ksh5.00 of airtime on 4/6/26 at 10:57 PM.New M-PESA balance is Ksh5.00. Transaction cost, Ksh0.00.",
        TransactionType.AirtimePurchase)]
    [InlineData(
        "UGIK2BOBR0 Confirmed. Ksh15.00 sent to SAFARICOM DATA BUNDLES for account SAFARICOM DATA BUNDLES on 18/7/26 at 11:03 PM. New M-PESA balance is Ksh88.00. Transaction cost, Ksh0.00.",
        TransactionType.AirtimePurchase)]
    [InlineData(
        "UCSMKAR8VZ Confirmed. On 28/3/26 at 8:31 AM Give Ksh2,000.00 cash to TUKO NET LIMITED Ernest enterprises shop elgons building New M-PESA balance is Ksh2,419.87.",
        TransactionType.Deposit)]
    [InlineData(
        "UERMK5QVVY Confirmed. Ksh 7.01 from your M-PESA has been used to fully pay your outstanding Fuliza M-PESA. Available Fuliza M-PESA limit is Ksh 1200.00.Ksh0.00.",
        TransactionType.Fuliza)]
    [InlineData(
        "UF7MKM7FTT confirmed. Reversal of transaction UF7MK70VIL has been successfully reversed on 7/6/26 at 5:56 PM and Ksh30.00 is credited to your M-PESA account. New M-PESA account balance is Ksh1,956.99.",
        TransactionType.Reversal)]
    [InlineData(
        "UHDMK2T7ZU Confirmed.Ksh1,000.00 transferred from M-Shwari account on 13/8/26 at 1:09 PM. M-Shwari balance is Ksh2,569.07 .M-PESA balance is Ksh1,031.03 .Transaction cost Ksh.0.00",
        TransactionType.MShwari)]
    [InlineData(
        "UHEMK2ZU8L Confirmed.Ksh50.00 transferred to M-Shwari account on 14/8/26 at 10:02 PM. M-PESA balance is Ksh811.03 .New M-Shwari saving account balance is Ksh2,619.07. Transaction cost Ksh.0.00",
        TransactionType.MShwari)]
    [InlineData(
        "SH56ABC123 Confirmed. Ksh 5,500.00 sent to John Doe via Western Union (MTCN: 1234567890) on 14/8/26 at 10:45 AM. Fee: Ksh 250.00. New M-PESA balance is Ksh 12,400.00.",
        TransactionType.GlobalPayment)]
    [InlineData(
        "SH56CAR987 Confirmed. Ksh 1,940.00 paid to Netflix US using M-PESA GlobalPay virtual card ending *4321 on 14/8/26 at 6:30 PM. Exch Rate: 1 USD = Ksh 130.00. Fee: Ksh 0.00. New M-PESA balance is Ksh 16,310.00.",
        TransactionType.GlobalPayment)]
    [InlineData(
        "UHHBO39XXX Confirmed. Ksh100.00 sent to Safaricom Offers for account Tunukiwa on 19/8/26 at 1:00 PM. New M-PESA balance is Ksh2,957.94. Transaction cost, Ksh0.00.",
        TransactionType.PayBill)]
    [InlineData(
        "UHHBO39XXX Confirmed. You have received Ksh800.00 from IM BANK LIMITED- APP on 21/8/26 at 9:18 PM. New M-PESA balance is Ksh3,825.94. Buy goods with M-PESA.",
        TransactionType.ReceiveMoney)]
    [InlineData(
        "UXXX1Q38XXX Confirmed. Ksh1,500.00 sent to IM BANK C2B for account 5847846463273 on 12/8/26 at 8:04 AM New M-PESA balance is Ksh0.00. Transaction cost, Ksh15.00.",
        TransactionType.PayBill)]
    public void Parse_RealSamples_ReturnsCorrectType(string sms, TransactionType expectedType)
    {
        var result = _parser.Parse(sms, SmsId, SmsTimestamp);
        Assert.NotNull(result);
        Assert.Equal(expectedType, result.Type);
    }
}