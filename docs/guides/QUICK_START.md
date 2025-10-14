# Quick Start Guide - NasosoTax

## 🚀 Getting Started in 5 Minutes

### Step 1: Prerequisites
- .NET 9.0 SDK installed
- Any modern web browser

### Step 2: Clone and Run
```bash
git clone https://github.com/KelvinEsiri/NasosoTax.git
cd NasosoTax
cd NasosoTax.Web
dotnet run
```

### Step 3: Access the Application
Open your browser and navigate to:
- **URL:** http://localhost:5000
- **Alternative HTTPS:** https://localhost:5001

---

## 📋 First Time User Flow

### 1. Register an Account
- Click **Register** in the navigation menu
- Fill in:
  - Full Name: `John Doe`
  - Email: `john@example.com`
  - Username: `johndoe`
  - Password: `password123`
- Click **Register**

### 2. Login
- Click **Login** in the navigation menu
- Enter your username and password
- Click **Login**

### 3. Calculate Tax (No Login Required)
- Click **Tax Calculator** in the menu
- Enter your annual income (e.g., `5000000` for ₦5M)
- Add deductions:
  - Select type: `Pension Contribution`
  - Enter amount: `400000`
  - Click **+ Add Deduction**
- Click **Calculate Tax**
- View your tax summary on the right

### 4. Submit Income Data (Login Required)
- Click **Submit Income**
- Select tax year: `2025`
- Add income sources:
  - Type: `Employment`
  - Description: `Salary from ABC Corp`
  - Amount: `5000000`
- Add deductions:
  - Type: `Pension`
  - Amount: `400000`
- Click **Submit Tax Information**

### 5. View Reports (Login Required)
- Click **Reports** in the menu
- View your yearly summaries
- Click **View Details** to see breakdown

---

## 🔌 Using the API

### Get JWT Token
```bash
# Register
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"johndoe","email":"john@example.com","password":"password123","fullName":"John Doe"}'

# Login and get token
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"johndoe","password":"password123"}'
```

### Submit Tax Data
```bash
# Use the token from login response
TOKEN="your-jwt-token-here"

curl -X POST http://localhost:5000/api/tax/submit \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "taxYear": 2025,
    "incomeSources": [{"sourceType":"Employment","description":"Salary","amount":5000000}],
    "deductions": [{"deductionType":"Pension","description":"Pension","amount":400000}]
  }'
```

### Get Tax Summary
```bash
curl -X GET http://localhost:5000/api/tax/summary/2025 \
  -H "Authorization: Bearer $TOKEN"
```

---

## 💡 Sample Tax Scenarios

### Scenario 1: Low Income (₦2M)
- **Income:** ₦2,000,000
- **Deductions:** ₦100,000
- **Taxable Income:** ₦1,900,000
- **Tax:** ₦165,000 (8.25% effective rate)

### Scenario 2: Middle Income (₦5M)
- **Income:** ₦5,000,000
- **Deductions:** ₦450,000
- **Taxable Income:** ₦4,550,000
- **Tax:** ₦609,000 (12.18% effective rate)

### Scenario 3: High Income (₦15M)
- **Income:** ₦15,000,000
- **Deductions:** ₦1,000,000
- **Taxable Income:** ₦14,000,000
- **Tax:** ₦2,172,000 (14.48% effective rate)

---

## 📚 Understanding Nigerian Tax

### Tax Brackets (2025)
1. **₦0 - ₦800,000:** 0% (Tax-free)
2. **₦800,001 - ₦3,000,000:** 15%
3. **₦3,000,001 - ₦12,000,000:** 18%
4. **₦12,000,001 - ₦25,000,000:** 21%
5. **₦25,000,001 - ₦50,000,000:** 23%
6. **Above ₦50,000,000:** 25%

### Eligible Deductions
1. **National Housing Fund (NHF)** - Contributions
2. **National Health Insurance (NHIS)** - Premiums
3. **Pension Contributions** - As per Pension Reform Act
4. **Life Insurance Premiums** - Annual premiums paid
5. **Rent Relief** - 20% of annual rent, max ₦500,000
6. **Mortgage Interest** - Interest on owner-occupied residential house

---

## 🎯 Tips for Accurate Tax Calculation

### DO:
✅ Include ALL income sources (employment, business, investment, rental)
✅ Keep receipts for all deductible expenses
✅ Claim all eligible deductions to reduce tax liability
✅ Submit tax data annually for proper record-keeping
✅ Review tax summary before finalizing

### DON'T:
❌ Forget to include side income or bonuses
❌ Claim deductions without proper documentation
❌ Miss the rent relief deduction if you're renting
❌ Overlook pension contributions (often auto-deducted)

---

## 🔧 Configuration Options

### Database Location
Default: `NasosoTax.Web/nasosotax.db`

To change, edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=your-path/nasosotax.db"
  }
}
```

### JWT Settings
Edit `appsettings.json`:
```json
{
  "Jwt": {
    "Key": "YourSecretKey",
    "Issuer": "NasosoTax",
    "Audience": "NasosoTaxUsers"
  }
}
```

---

## 🐛 Troubleshooting

### Issue: Database Error
**Solution:** Delete `nasosotax.db` and restart the application. The database will be recreated automatically.

### Issue: Port Already in Use
**Solution:** Run with a different port:
```bash
dotnet run --urls "http://localhost:5050"
```

### Issue: JWT Token Expired
**Solution:** Login again to get a new token. Tokens expire after 8 hours.

### Issue: Build Errors
**Solution:** Ensure .NET 9.0 SDK is installed:
```bash
dotnet --version  # Should show 9.x.x
```

---

## 📞 Need Help?

1. Check **README.md** for comprehensive documentation
2. Review **API_DOCUMENTATION.md** for API details
3. Read **Nigeria-Tax-Act-2025.pdf** for tax law reference
4. Open an issue on GitHub for bugs or feature requests

---

## 🎉 You're All Set!

Your NasosoTax portal is now ready to:
- Calculate taxes in real-time
- Track income and deductions
- Generate comprehensive reports
- Manage multiple tax years

**Happy Tax Calculating! 🎯**