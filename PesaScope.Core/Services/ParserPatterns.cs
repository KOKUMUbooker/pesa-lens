using System.Text.RegularExpressions;

namespace PesaScope.Core.Services;

// <summary>
//   Patterns used as set as per the date - 7th June 2026
// </summary>
public static class ParserPatterns
{
    // ── Compiled patterns ────────────────────────────────────────────────────
    // All patterns are case-insensitive. The transaction code is always the
    // first word in every M-Pesa message.

    public static readonly Regex CodePattern = new(
        @"^(?<code>[A-Z0-9]{10})\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly Regex SendMoneyPattern = new(
        @"^(?<code>[A-Z0-9]{8,12}) Confirmed\.\s*" +
        @"Ksh(?<amount>[\d,]+\.?\d*) sent to " +
        @"(?<name>[A-Za-z][A-Za-z\s]*?) " +
        @"(?<phone>\d{9,12}) on " +
        @"(?<date>[\d/]+) at (?<time>[\d:]+\s*[AP]M)\." +
        @"\s*New M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*)\." +
        @".*?Transaction cost,?\s*Ksh(?<cost>[\d,]+\.?\d*)\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);


    public static readonly Regex ReceiveMoneyPattern = new(
        @"^(?<code>[A-Z0-9]{8,12}) Confirmed\." +
        @"\s*You have received Ksh(?<amount>[\d,]+\.?\d*) from " +
        @"(?<name>[A-Za-z][A-Za-z\s]+?) " +
        @"(?<phone>[\d\*]{7,12}) on " +
        @"(?<date>[\d/]+) at (?<time>[\d:]+\s*[AP]M)" +
        @"\s+New M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*)\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex PayBillPattern = new(
        @"^(?<code>[A-Z0-9]{8,12}) Confirmed\.\s*" +
        @"Ksh(?<amount>[\d,]+\.?\d*) sent to " +
        @"(?<name>[A-Z0-9\s&'\-\.]+?) for account " +
        @"(?<account>[A-Z0-9\-\s]+?) on " +
        @"(?<date>[\d/]+) at (?<time>[\d:]+\s*[AP]M) " +
        @"New M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*)\." +
        @"\s*Transaction cost,?\s*Ksh(?<cost>[\d,]+\.?\d*)\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex BuyGoodsPattern = new(
        @"^(?<code>[A-Z0-9]{8,12}) Confirmed\.\s*" +
        @"Ksh(?<amount>[\d,]+\.?\d*) paid to " +
        @"(?<name>[A-Z0-9\s&'\-\.]+?)\.\s*on " +
        @"(?<date>[\d/]+) at (?<time>[\d:]+\s*[AP]M)\." +
        @"New M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*)\." +
        @"\s*Transaction cost,?\s*Ksh(?<cost>[\d,]+\.?\d*)\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex WithdrawalPattern = new(
        @"^(?<code>[A-Z0-9]{8,12}) Confirmed\." +
        @"on (?<date>[\d/]+) at (?<time>[\d:]+\s*[AP]M)" +
        @"Withdraw Ksh(?<amount>[\d,]+\.?\d*) from " +
        @"(?<agent>\d+) - (?<location>.+?) " +
        @"New M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*)\." +
        @"\s*Transaction cost,?\s*Ksh(?<cost>[\d,]+\.?\d*)\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex AirtimePattern = new(
        @"^(?<code>[A-Z0-9]{8,12}) [Cc]onfirmed\." +
        @"\s*You bought Ksh(?<amount>[\d,]+\.?\d*) of airtime on " +
        @"(?<date>[\d/]+) at (?<time>[\d:]+\s*[AP]M)\." +
        @"New M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*)\." +
        @"\s*Transaction cost,?\s*Ksh(?<cost>[\d,]+\.?\d*)\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex DataBundlePattern = new(
        @"^(?<code>[A-Z0-9]{8,12}) Confirmed\.\s*" +
        @"Ksh(?<amount>[\d,]+\.?\d*) sent to SAFARICOM DATA BUNDLES for account SAFARICOM DATA BUNDLES on " +
        @"(?<date>[\d/]+) at (?<time>[\d:]+\s*[AP]M)\." +
        @"\s*New M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*)\." +
        @"\s*Transaction cost,?\s*Ksh(?<cost>[\d,]+\.?\d*)\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex FulizaPattern = new(
       @"^(?<code>[A-Z0-9]{8,12})\s*" +
       @"Confirmed\.\s*" +
       @"Ksh\s*(?<amount>[\d,]+\.?\d*) from your M-PESA has been used to " +
       @"(?<repayment_type>fully|partially) pay your outstanding Fuliza M-PESA\.\s*" +
       @"Available Fuliza M-PESA limit is Ksh\s*(?<limit>[\d,]+\.?\d*)\.",
       RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex ReversalPattern = new(
        @"^(?<code>[A-Z0-9]{8,12}) [Cc]onfirmed\.\s*" +
        @"Reversal of transaction (?<reversed_code>[A-Z0-9]{8,12}) has been successfully reversed\s*" +
        @"on (?<date>[\d/]+)\s*at (?<time>[\d:]+\s*[AP]M) and " +
        @"Ksh(?<amount>[\d,]+\.?\d*) is credited to your M-PESA account\.\s*" +
        @"New M-PESA account balance is Ksh(?<balance>[\d,]+\.?\d*)\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex DepositPattern = new(
        @"^(?<code>[A-Z0-9]{8,12}) Confirmed\.\s*" +
        @"On (?<date>[\d/]+) at (?<time>[\d:]+\s*[AP]M) " +
        @"Give Ksh(?<amount>[\d,]+\.?\d*) cash to " +
        @"(?<agent>.+?) " +
        @"New M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*)\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex MShwariWithdrawalPattern = new(
        @"^(?<code>[A-Z0-9]{8,12}) Confirmed\.\s*" +
        @"Ksh(?<amount>[\d,]+\.?\d*) transferred from M-Shwari account on\s*" +
        @"(?<date>[\d/]+) at (?<time>[\d:]+\s*[AP]M)\.\s*" +
        @"M-Shwari balance is Ksh(?<mshwari_balance>[\d,]+\.?\d*)\s*\.\s*" +
        @"M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*)\s*\.\s*" +
        @"Transaction cost Ksh\.?(?<cost>[\d,]+\.?\d*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static readonly Regex MShwariDepositPattern = new(
        @"^(?<code>[A-Z0-9]{8,12}) Confirmed\.\s*" +
        @"Ksh(?<amount>[\d,]+\.?\d*) transferred to M-Shwari account on\s*" +
        @"(?<date>[\d/]+) at (?<time>[\d:]+\s*[AP]M)\.\s*" +
        @"M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*)\s*\.\s*" +
        @"New M-Shwari saving account balance is Ksh(?<mshwari_balance>[\d,]+\.?\d*)\.\s*" +
        @"Transaction cost Ksh\.?(?<cost>[\d,]+\.?\d*)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    // Card charge, e.g. PayPal/DigitalOcean, routed through M-PESA CARD
    public static readonly Regex GlobalPaymentCardPattern = new(
        @"^(?<code>[A-Z0-9]{8,12}) Confirmed\.\s*" +
        @"Ksh(?<amount>[\d,]+\.?\d*) sent to " +
        @"(?<name>[A-Z0-9\s&'\-\.]+?) for account " +
        @"(?<account>[A-Z0-9\*\s]+?) on " +
        @"(?<date>[\d/]+) at (?<time>[\d:]+\s*[AP]M)\s*" +
        @"New M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*)\." +
        @"\s*Transaction cost,?\s*Ksh(?<cost>[\d,]+\.?\d*)\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    // Western Union (or similar money-transfer agent) send
    public static readonly Regex GlobalTransferSendPattern = new(
        @"^(?<code>[A-Z0-9]{8,12}) Confirmed\.\s*" +
        @"Ksh\s*(?<amount>[\d,]+\.?\d*) sent to " +
        @"(?<name>[A-Za-z][A-Za-z\s]*?) via " +
        @"(?<provider>[A-Za-z\s]+?)\s*\(MTCN:\s*(?<mtcn>\d+)\) on " +
        @"(?<date>[\d/]+) at (?<time>[\d:]+\s*[AP]M)\.\s*" +
        @"Fee:\s*Ksh\s*(?<cost>[\d,]+\.?\d*)\.\s*" +
        @"New M-PESA balance is Ksh\s*(?<balance>[\d,]+\.?\d*)\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    // M-Pesa Global (or similar) receive — foreign currency + Ksh equivalent
    public static readonly Regex GlobalTransferReceivePattern = new(
        @"^(?<code>[A-Z0-9]{8,12}) Confirmed\.\s*" +
        @"You have received (?<currency>[A-Z]{3}) (?<foreign_amount>[\d,]+\.?\d*)\s*" +
        @"\(Ksh\s*(?<amount>[\d,]+\.?\d*)\) from " +
        @"(?<name>[A-Za-z][A-Za-z\s]*?) via .*? on " +
        @"(?<date>[\d/]+) at (?<time>[\d:]+\s*[AP]M)\.\s*" +
        @"New M-PESA balance is Ksh\s*(?<balance>[\d,]+\.?\d*)\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    // GlobalPay virtual card checkout
    public static readonly Regex GlobalVirtualCardPattern = new(
        @"^(?<code>[A-Z0-9]{8,12}) Confirmed\.\s*" +
        @"Ksh\s*(?<amount>[\d,]+\.?\d*) paid to " +
        @"(?<name>[A-Za-z0-9][A-Za-z0-9\s\.\-]*?) using M-PESA GlobalPay virtual card ending \*?(?<card_last4>\d{4}) on " +
        @"(?<date>[\d/]+) at (?<time>[\d:]+\s*[AP]M)\.\s*" +
        @"Exch Rate:.*?\.\s*" +
        @"Fee:\s*Ksh\s*(?<cost>[\d,]+\.?\d*)\.\s*" +
        @"New M-PESA balance is Ksh\s*(?<balance>[\d,]+\.?\d*)\.",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
}
