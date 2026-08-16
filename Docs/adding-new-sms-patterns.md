# Adding New M-Pesa SMS Patterns

This guide walks through the process of adding support for a new M-Pesa SMS
message type to `PesaScope.Core.Services.MpesaSmsParser`.

The parser works as a chain of pattern-matching attempts (`Parse()` tries
each `TryParseX` method in order via `??` until one succeeds). Getting a new
pattern right means getting three things right together: the **regex**, the
**parser method**, and its **position in the chain** — plus tests that prove
all three.

---

## 1. Collect a real sample

Before writing any regex, get the *exact* raw SMS text — copy-pasted, not
retyped, not "cleaned up." M-Pesa message formatting is inconsistent across
transaction types and even within the same type over time:

- Some messages put a space after `Ksh` (`Ksh 5,500.00`), others don't
  (`Ksh5,500.00`).
- Some end a sentence with a period before the next clause
  (`... at 11:03 PM.  New M-PESA balance...`), others don't
  (`... at 8:23 PM New M-PESA balance...`).
- Cost/fee lines appear in different positions and under different labels
  (`Transaction cost, Ksh...`, `Fee: Ksh...`, `Transaction cost Ksh.0.00`).
- Some samples are stripped of markdown/HTML if you pulled them from a web
  page rather than an actual phone — watch out for stray link syntax like
  `[M-Pesa Global](https://...)` that would never appear in a real SMS.

If you have more than one real sample of the *same* transaction type, check
them against each other before assuming a single pattern will cover both —
small wording differences (e.g. "transferred **from** M-Shwari" vs.
"transferred **to** M-Shwari") usually mean you need two separate patterns,
not one looser one.

---

## 2. Check whether it's really a new pattern

Ask two questions before writing a new regex:

1. **Does an existing pattern already match this, just with a minor format
   quirk?** (e.g. a missing space, an extra period). If so, consider whether
   loosening the existing pattern is safer than adding a near-duplicate one
   — but only if you're confident it won't start accidentally matching
   messages it shouldn't (see Step 4).
2. **Is this structurally a new shape?** (different clause order, a field
   the others don't have, a different cost-line format). If so, it earns
   its own pattern.

When in doubt, prefer a new, narrower pattern over widening an existing one.
Broad character classes are the single biggest source of bugs in this
parser — see Step 4.

---

## 3. Write the regex pattern

Add the new pattern to `ParserPatterns.cs`, following the existing style:

```csharp
public static readonly Regex YourNewPattern = new(
    @"^(?<code>[A-Z0-9]{8,12}) Confirmed\.\s*" +
    @"Ksh(?<amount>[\d,]+\.?\d*) ..." +
    // ...
    @"New M-PESA balance is Ksh(?<balance>[\d,]+\.?\d*)\." +
    @"\s*Transaction cost,?\s*Ksh(?<cost>[\d,]+\.?\d*)\.",
    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
```

Guidelines:

- **Name capture groups consistently** with existing patterns:
  `code`, `amount`, `name`, `phone`, `account`, `date`, `time`, `balance`,
  `cost`. Add new group names only for genuinely new fields (e.g. `mtcn`,
  `card_last4`, `bundle_type`).
- **Keep `RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline`**
  — `Singleline` lets `.` match newlines, which matters because
  `NormalizeSms()` collapses whitespace but the raw SMS may still span
  multiple logical sentences.
- **Anchor to literal text wherever a field is a known fixed value**,
  rather than using a generic character class. For example, a card-payment
  pattern that's *specifically* about `M-PESA CARD` should capture
  `(?<name>M-PESA CARD)` literally, not a generic
  `(?<name>[A-Z0-9\s&'\-\.]+?)` — otherwise it can accidentally swallow
  unrelated messages (see Step 4).
- **Non-greedy (`+?`) over greedy (`+`)** for any group followed by literal
  text it needs to stop at (e.g. a name followed by `" on "`) — greedy
  matching can overshoot into the next clause.

---

## 4. Guard against cross-matching (the #1 source of bugs here)

This is the most common way new patterns break existing ones. Because
`Parse()` is a chain of `??`, **whichever pattern matches first wins — even
if a later, more correct pattern also would have matched.**

A character class that's broader than it needs to be can accidentally
match SMS text meant for a *different* transaction type. Two real examples
from this project:

- A `PochiLaBiashara` pattern using `[A-Za-z\s]*?` for the recipient name
  ended up swallowing PayBill and Data Bundle messages, because
  `"SAFARICOM DATA BUNDLES for account SAFARICOM DATA BUNDLES"` is *also*
  just letters and spaces with no digits to stop the match early.
- A `GlobalPaymentCard` pattern using a generic merchant-name character
  class matched **any** PayBill-shaped message, not just ones actually
  routed through `M-PESA CARD`, because it never anchored to that literal
  text.

**Before merging a new pattern, check it against every existing test
fixture, not just your new sample.** Ask: *"could this character class also
match something already in the test suite?"* If a field has a known fixed
value (a provider name, a fixed phrase like `for account`), anchor to it
literally or use a negative lookahead (e.g. `(?!.*for account)`) to
explicitly exclude shapes that belong to other patterns.

---

## 5. Add the parser method

In `MpesaSmsParser.cs`, add a `private static Transaction? TryParseX(...)`
method following the existing pattern:

```csharp
private static Transaction? TryParseYourNewType(string body, long smsId, long timestamp)
{
    var m = ParserPatterns.YourNewPattern.Match(body);
    if (!m.Success) return null;

    return BuildTransaction(m, TransactionType.YourType, smsId, timestamp,
        counterpartyName: m.Groups["name"].Value.Trim(),
        counterpartyNumber: m.Groups["account"].Value.Trim()); // if applicable
}
```

If the transaction type doesn't already exist in `TransactionType`
(`Transaction.cs`), add it as a new value **at the end of the enum** —
never renumber or insert in the middle, since existing rows in deployed
databases store the underlying integer value.

---

## 6. Wire it into the parse chain

Add your new `TryParseX` call to the `??` chain in `Parse()`. Placement
matters:

- Put **more specific / narrower** patterns before **more general** ones
  they could otherwise be mistaken for.
- If your pattern is fully self-contained (e.g. anchored to unique literal
  text that nothing else could produce), its position in the chain is less
  critical — but keep it near conceptually related patterns for
  readability anyway (e.g. group all `GlobalPayment` variants together).

```csharp
var transaction = TryParseSendMoney(body, smsId, smsTimestamp)
    ?? TryParseReceiveMoney(body, smsId, smsTimestamp)
    ?? TryParseYourNewType(body, smsId, smsTimestamp)   // ← new
    ?? TryParsePayBill(body, smsId, smsTimestamp)
    // ...
```

---

## 7. Write tests

Add a new section to `MpesaSmsParserTests.cs` for the new type, following
the existing structure. At minimum:

- **Type test** — asserts `result.Type` is correct.
- **Amount test** — asserts `result.Amount` parses correctly, including
  comma-separated values if applicable.
- **Counterparty tests** — name and/or number, whichever fields apply.
- **Balance test**.
- **M-Pesa code test**.
- **A cross-match regression guard** — an explicit test proving this
  pattern does *not* misfire on, or get misfired on by, any structurally
  similar existing pattern. This is the test that would have caught the
  `PochiLaBiashara`/`GlobalPaymentCard` bugs described in Step 4.

Also add a real-sample entry to the `[Theory]`/`[InlineData]` block at the
bottom of the file, which cross-checks every known SMS sample against its
expected `TransactionType` in one place — this is your fastest early
warning if a new pattern starts stealing matches from an old one.

```csharp
[InlineData(
    "<the exact real SMS text>",
    TransactionType.YourType)]
```

---

## 8. Run the full test suite — every time

> **You must run `dotnet test` after adding or changing any pattern —
> not just the tests for the new type, but the entire suite.**

This is not optional. Because patterns share the same chain and can
structurally overlap (Step 4), a change that looks correct in isolation can
silently break an unrelated, already-working transaction type. The full
suite is what catches that — a change is not done until:

```bash
dotnet test
```

reports **all** tests passing, with no regressions in previously-green
tests. If you see failures in tests you didn't intend to touch, that's a
signal your new pattern's character classes are too broad — go back to
Step 4 before adjusting anything else.

---

## 9. If this is a new `TransactionType` value on an already-deployed app

Adding a new enum value doesn't retroactively reclassify existing rows. If
older SMS messages of this new type were previously either:

- **falling through and returning `null`** (never inserted) — nothing to
  migrate; the user just needs to re-sync/re-import those SMSs, or
- **being miscategorized under an existing type** (e.g. `PayBill`) — this
  needs a one-time, versioned migration step that runs once per user on
  next app launch, matching against the stored `OriginalSms` text (the
  only reliable signal for what a row *used to be*) and updating `Type`
  accordingly.

Keep any such migration idempotent and gated behind a version flag so it
never re-runs on users who've already had it applied.
