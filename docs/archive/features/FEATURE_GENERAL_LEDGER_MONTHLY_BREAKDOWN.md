# Feature Enhancement - General Ledger Monthly Breakdown

## Enhancement Implemented

### **Feature**: Automatic Monthly Breakdown for General Ledger Income

When fetching income from the General Ledger, the system now automatically populates the monthly breakdown with actual ledger data and makes it **read-only** to prevent accidental modifications.

---

## What Changed

### 1. ✅ Fetch Monthly Data from Ledger

**New Behavior**:
- When clicking "📥 Fetch from Ledger", the system now makes TWO API calls:
  1. `/api/ledger/summary` - Gets total income/expenses
  2. `/api/ledger/monthly-summary/{year}` - Gets monthly breakdown

**Code Changes**:
```csharp
// Fetch both total and monthly summaries
var ledgerSummary = await ApiService.GetAsync<LedgerSummaryResponse>(...);
var monthlySummaries = await ApiService.GetAsync<List<MonthlyLedgerSummary>>($"/api/ledger/monthly-summary/{request.TaxYear}");

// Build monthly breakdown
var monthlyBreakdown = monthlySummaries.Select(m => new MonthlyIncomeDto
{
    Month = m.Month,
    MonthName = m.MonthName,
    Amount = m.TotalIncome  // Only income, not expenses
}).ToList();

// Set on income source
newLedgerIncome.UseMonthlyBreakdown = monthlyBreakdown.Any();
newLedgerIncome.MonthlyBreakdown = monthlyBreakdown;
```

---

### 2. ✅ Read-Only Monthly Inputs for General Ledger

**New Behavior**:
- Monthly breakdown fields are **disabled** for General Ledger entries
- Badge shows "Read-only (from ledger)"
- Users cannot edit the monthly amounts
- Reflects actual ledger data

**Visual Changes**:
```html
<!-- Before: Editable for all -->
<input type="number" @bind="month.Amount" class="form-control form-control-sm" />

<!-- After: Disabled for General Ledger -->
<input type="number" 
       @bind="month.Amount" 
       class="form-control form-control-sm"
       readonly="@(income.SourceType == "General Ledger")"
       disabled="@(income.SourceType == "General Ledger")" />
```

---

### 3. ✅ Automatic Monthly Breakdown Toggle

**New Behavior**:
- "Enter income monthly" checkbox is **hidden** for General Ledger entries
- Shows informative message instead
- Monthly breakdown is **automatically enabled** when fetching from ledger

**Before**:
```
☑ Enter income monthly (recommended for variable income)
  - Not available for General Ledger...
```

**After**:
```
┌─────────────────────────────────────────────────────────┐
│ ℹ️ Info Alert                                           │
│ 📊 Monthly breakdown automatically loaded from your     │
│ ledger entries. The amounts shown below reflect your    │
│ actual ledger data and cannot be edited here.           │
└─────────────────────────────────────────────────────────┘
```

---

## User Experience

### Scenario 1: Fetch Ledger Income (First Time)

**User Actions**:
1. Navigates to Submit Income
2. Selects Tax Year: 2025
3. Has existing ledger entries:
   - January: ₦50,000
   - February: ₦60,000
   - March: ₦55,000
4. Clicks "📥 Fetch from Ledger"

**System Response**:
```
✅ Added General Ledger income: ₦165,000.00

Income Source #2
┌─────────────────────────────────────────────────────────┐
│ Source Type: 📒 General Ledger - From ledger entries   │
│ Description: Total income from ledger for 2025         │
│ Amount: ₦165,000.00                                    │
│                                                         │
│ ℹ️ Monthly breakdown automatically loaded from your    │
│ ledger entries...                                       │
│                                                         │
│ Monthly Breakdown [Read-only (from ledger)]            │
│ January:   ₦50,000.00  [disabled]                      │
│ February:  ₦60,000.00  [disabled]                      │
│ March:     ₦55,000.00  [disabled]                      │
│ April:     ₦0.00       [disabled]                      │
│ ...                                                     │
│ Total: ₦165,000.00                                     │
└─────────────────────────────────────────────────────────┘
```

---

### Scenario 2: Update Ledger Income

**User Actions**:
1. Already has General Ledger income fetched
2. Goes to General Ledger, adds more entries
3. Returns to Submit Income
4. Clicks "📥 Fetch from Ledger" again

**System Response**:
```
✅ Updated General Ledger income: ₦200,000.00

[Monthly breakdown automatically updates with new amounts]
January:   ₦50,000.00  [disabled]
February:  ₦75,000.00  [disabled] ← Updated!
March:     ₦55,000.00  [disabled]
April:     ₦20,000.00  [disabled] ← New!
```

---

### Scenario 3: Manual Income Source (Non-Ledger)

**User Actions**:
1. Adds income source manually
2. Selects "Salary"

**System Response**:
```
Income Source #1
┌─────────────────────────────────────────────────────────┐
│ Source Type: 💼 Salary - Regular wages                 │
│ Description: ABC Corp Monthly Salary                   │
│ Amount: ₦500,000.00                                    │
│                                                         │
│ ☑ Enter income monthly (recommended for variable      │
│   income)                                               │
│                                                         │
│ [Can check box to enable editable monthly breakdown]   │
└─────────────────────────────────────────────────────────┘
```

**Difference**: Regular income sources can toggle and edit monthly breakdown ✅

---

## Benefits

### 1. ✅ Data Accuracy
- Monthly amounts reflect **actual ledger data**
- No risk of manual entry errors
- Automatic synchronization with ledger

### 2. ✅ Transparency
- Users can see **exactly** how their ledger income breaks down by month
- Clear indication that data comes from ledger (read-only badge)
- Helpful info message explains the feature

### 3. ✅ Workflow Efficiency
- Automatically populates monthly breakdown (no manual entry needed)
- Updates when ledger is updated
- One-click refresh to sync latest data

### 4. ✅ Data Integrity
- Read-only fields prevent accidental modifications
- Source of truth remains the General Ledger
- Consistent with accounting principles

---

## Technical Implementation

### API Calls

**Endpoint Used**:
```
GET /api/ledger/monthly-summary/{year}
```

**Response Format**:
```json
[
  {
    "month": 1,
    "year": 2025,
    "monthName": "January",
    "totalIncome": 50000.00,
    "totalExpenses": 10000.00,
    "netAmount": 40000.00
  },
  // ... more months
]
```

**What We Use**:
- `month`: To match with MonthlyIncomeDto
- `monthName`: For display
- `totalIncome`: The amount (we ignore expenses here)

---

### DTO Structure

**MonthlyIncomeDto** (already exists):
```csharp
public class MonthlyIncomeDto
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
```

**MonthlyLedgerSummary** (from API):
```csharp
public class MonthlyLedgerSummary
{
    public int Month { get; set; }
    public int Year { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetAmount { get; set; }
}
```

---

## Edge Cases Handled

### 1. ✅ No Monthly Data
- If `/monthly-summary` returns empty array
- `monthlyBreakdown` will be empty list
- `UseMonthlyBreakdown` = false
- Only total amount shown (no breakdown section)

### 2. ✅ Months with Zero Income
- Months with no income entries show ₦0.00
- Still displayed in breakdown (all 12 months)
- Users can see which months had no income

### 3. ✅ Partial Year Data
- If ledger has entries for only Jan-Mar
- Jan-Mar show actual amounts
- Apr-Dec show ₦0.00
- Total is still accurate

### 4. ✅ Manual Edit Attempts
- Inputs are disabled (grayed out)
- Cannot focus or edit
- Tooltip could be added: "Edit in General Ledger"

---

## Testing Instructions

### Test 1: Fetch with Monthly Data
1. Go to General Ledger
2. Add entries for multiple months (e.g., Jan, Feb, Mar)
3. Go to Submit Income, select 2025
4. Click "📥 Fetch from Ledger"
5. **Expected**:
   - ✅ Monthly breakdown appears automatically
   - ✅ Badge shows "Read-only (from ledger)"
   - ✅ Input fields are disabled/grayed out
   - ✅ Amounts match ledger entries
   - ✅ Total matches sum of monthly amounts

### Test 2: Update and Re-fetch
1. Continue from Test 1
2. Go to General Ledger, add more entries
3. Return to Submit Income
4. Click "📥 Fetch from Ledger" again
5. **Expected**:
   - ✅ Monthly amounts update automatically
   - ✅ Still read-only
   - ✅ New total is correct

### Test 3: Compare with Manual Entry
1. Add two income sources:
   - Source #1: Salary (manual)
   - Source #2: General Ledger (fetched)
2. Check "Enter income monthly" for Salary
3. **Expected**:
   - ✅ Salary: Checkbox available, fields editable
   - ✅ General Ledger: No checkbox, fields disabled
   - ✅ Clear visual difference

### Test 4: No Monthly Data
1. Create new year with only a single total entry
2. Fetch from ledger
3. **Expected**:
   - ✅ Shows total amount
   - ✅ No monthly breakdown section (or all zeros)

---

## Files Modified

1. **`SubmitIncome.razor`**:
   - Modified `FetchLedgerIncome()` method
   - Added monthly summary API call
   - Populate monthly breakdown on fetch
   - Made monthly inputs read-only for General Ledger
   - Hide checkbox for General Ledger
   - Show info alert instead

---

## Performance Considerations

### API Calls
- **Before**: 1 API call (summary only)
- **After**: 2 API calls (summary + monthly)
- **Impact**: Minimal (both are fast queries)
- **Optimization**: Could batch both in single endpoint if needed

### Data Size
- Monthly summary: 12 records per year
- Very small data size
- No performance concerns

---

## Future Enhancements

### 1. **Category Breakdown**
Show income breakdown by category within each month:
```
January Total: ₦50,000
  - Salary: ₦40,000
  - Commission: ₦10,000
```

### 2. **Visual Charts**
Add bar chart or line graph showing monthly income trend

### 3. **Export Feature**
Export monthly breakdown to Excel/PDF

### 4. **Income vs Expenses**
Show both income and expenses in monthly breakdown (currently only income)

### 5. **Edit in Place**
Add "Edit in Ledger" button that opens ledger page with filter for that month

---

## Success Criteria

✅ Monthly breakdown fetched from ledger API  
✅ Breakdown displayed in read-only fields  
✅ Clear visual indicators (badge, info message)  
✅ Checkbox hidden for General Ledger entries  
✅ Regular income sources unaffected  
✅ Total amount matches sum of months  
✅ Updates when re-fetching  
✅ No data loss or corruption  

---

**Feature Completed**: October 10, 2025  
**Status**: ✅ READY FOR TESTING  
**Priority**: MEDIUM (Enhancement)  
**Impact**: HIGH (Better UX and data accuracy)  
**Effort**: 45 minutes
