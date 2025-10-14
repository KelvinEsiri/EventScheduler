# NasosoTax API Documentation

## Base URL
```
http://localhost:5000/api
```

## Authentication

Most endpoints require JWT Bearer token authentication. Include the token in the Authorization header:
```
Authorization: Bearer {your-jwt-token}
```

## API Endpoints

### Authentication Endpoints

#### 1. Register User
**POST** `/auth/register`

Register a new user account.

**Request Body:**
```json
{
  "username": "johndoe",
  "email": "john@example.com",
  "password": "securePassword123",
  "fullName": "John Doe"
}
```

**Response (200 OK):**
```json
{
  "message": "User registered successfully"
}
```

**Error Response (409 Conflict):**
```json
{
  "message": "Username already exists"
}
```

#### 2. Login
**POST** `/auth/login`

Authenticate user and receive JWT token.

**Request Body:**
```json
{
  "username": "johndoe",
  "password": "securePassword123"
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "johndoe",
  "email": "john@example.com",
  "fullName": "John Doe"
}
```

**Error Response (401 Unauthorized):**
```json
{
  "message": "Invalid username or password"
}
```

---

### Tax Management Endpoints

#### 3. Submit Income and Deductions
**POST** `/tax/submit`

Submit income sources and deductions for a specific tax year.

**Authentication:** Required

**Request Body:**
```json
{
  "taxYear": 2025,
  "incomeSources": [
    {
      "sourceType": "Employment",
      "description": "Salary from ABC Corp",
      "amount": 5000000
    },
    {
      "sourceType": "Business",
      "description": "Consulting income",
      "amount": 2000000
    }
  ],
  "deductions": [
    {
      "deductionType": "Pension",
      "description": "Pension contribution",
      "amount": 400000
    },
    {
      "deductionType": "NHIS",
      "description": "Health insurance",
      "amount": 50000
    },
    {
      "deductionType": "Rent",
      "description": "Rent relief",
      "amount": 500000
    }
  ]
}
```

**Income Source Types:**
- Employment
- Business
- Investment
- Rental
- Other

**Deduction Types:**
- NHF (National Housing Fund)
- NHIS (National Health Insurance Scheme)
- Pension
- Insurance (Life Insurance Premium)
- Rent (Rent Relief)
- Mortgage (Mortgage Interest)
- Other

**Response (200 OK):**
```json
{
  "message": "Tax information submitted successfully",
  "taxRecordId": 1
}
```

#### 4. Get Tax Summary
**GET** `/tax/summary/{taxYear}`

Retrieve tax summary for a specific year.

**Authentication:** Required

**Parameters:**
- `taxYear` (path parameter): The tax year (e.g., 2025)

**Response (200 OK):**
```json
{
  "taxYear": 2025,
  "totalIncome": 5000000.0,
  "totalDeductions": 450000.0,
  "taxableIncome": 4550000.0,
  "totalTax": 609000.0,
  "effectiveTaxRate": 12.18,
  "netIncome": 4391000.0,
  "lastUpdated": "2025-10-10T13:37:11.83669"
}
```

**Error Response (404 Not Found):**
```json
{
  "message": "No tax record found for the specified year"
}
```

#### 5. Get All Tax Records
**GET** `/tax/records`

Retrieve all tax records for the authenticated user.

**Authentication:** Required

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "userId": 1,
    "taxYear": 2025,
    "totalIncome": 5000000.0,
    "taxableIncome": 4550000.0,
    "totalTax": 609000.0,
    "effectiveTaxRate": 12.18,
    "createdAt": "2025-10-10T13:37:11.83669",
    "updatedAt": null,
    "incomeSources": [...],
    "deductions": [...]
  }
]
```

#### 6. Get Tax Brackets
**GET** `/tax/brackets`

Retrieve current Nigeria Tax Act 2025 tax brackets.

**Authentication:** Not Required

**Response (200 OK):**
```json
[
  {
    "minIncome": 0,
    "maxIncome": 800000,
    "taxRate": 0.0,
    "description": "First N800,000 at 0%"
  },
  {
    "minIncome": 800000,
    "maxIncome": 3000000,
    "taxRate": 0.15,
    "description": "Next N2,200,000 at 15%"
  },
  {
    "minIncome": 3000000,
    "maxIncome": 12000000,
    "taxRate": 0.18,
    "description": "Next N9,000,000 at 18%"
  },
  {
    "minIncome": 12000000,
    "maxIncome": 25000000,
    "taxRate": 0.21,
    "description": "Next N13,000,000 at 21%"
  },
  {
    "minIncome": 25000000,
    "maxIncome": 50000000,
    "taxRate": 0.23,
    "description": "Next N25,000,000 at 23%"
  },
  {
    "minIncome": 50000000,
    "maxIncome": 79228162514264337593543950335,
    "taxRate": 0.25,
    "description": "Above N50,000,000 at 25%"
  }
]
```

---

### General Ledger Endpoints

#### 7. Add Ledger Entry
**POST** `/ledger/entry`

Add a new income, expense, or deduction entry to the general ledger.

**Authentication:** Required

**Request Body:**
```json
{
  "entryDate": "2025-10-01T00:00:00Z",
  "description": "October Salary",
  "category": "Salary",
  "amount": 500000,
  "entryType": "Income"
}
```

**Entry Types:**
- Income
- Expense  
- Deduction (Tax-deductible expenses)

**Response (200 OK):**
```json
{
  "id": 1,
  "entryDate": "2025-10-01T00:00:00Z",
  "description": "October Salary",
  "category": "Salary",
  "amount": 500000,
  "entryType": "Income"
}
```

#### 8. Update Ledger Entry
**PUT** `/ledger/entry/{entryId}`

Update an existing ledger entry.

**Authentication:** Required

**Parameters:**
- `entryId` (path parameter): The ID of the entry to update

**Request Body:** Same as Add Ledger Entry

**Response (200 OK):** Same as Add Ledger Entry

#### 9. Delete Ledger Entry
**DELETE** `/ledger/entry/{entryId}`

Delete a ledger entry.

**Authentication:** Required

**Parameters:**
- `entryId` (path parameter): The ID of the entry to delete

**Response (200 OK):**
```json
true
```

#### 10. Get Ledger Summary
**GET** `/ledger/summary`

Get summary of all ledger entries for the authenticated user.

**Authentication:** Required

**Query Parameters:**
- `startDate` (optional): Filter entries from this date
- `endDate` (optional): Filter entries until this date

**Response (200 OK):**
```json
{
  "totalIncome": 650000.0,
  "totalExpenses": 225000.0,
  "netAmount": 425000.0,
  "entries": [
    {
      "id": 1,
      "entryDate": "2025-10-01T00:00:00Z",
      "description": "October Salary",
      "category": "Salary",
      "amount": 500000,
      "entryType": "Income"
    }
  ]
}
```

#### 11. Get Monthly Ledger Summaries
**GET** `/ledger/monthly-summary/{year}`

Get monthly summaries for a specific year.

**Authentication:** Required

**Parameters:**
- `year` (path parameter): The year to get summaries for

**Response (200 OK):**
```json
[
  {
    "month": 1,
    "year": 2025,
    "monthName": "January",
    "totalIncome": 500000.0,
    "totalExpenses": 200000.0,
    "netAmount": 300000.0
  },
  {
    "month": 2,
    "year": 2025,
    "monthName": "February",
    "totalIncome": 550000.0,
    "totalExpenses": 220000.0,
    "netAmount": 330000.0
  }
]
```

#### 12. Calculate Tax from Ledger
**POST** `/ledger/calculate-tax`

Calculate tax using ledger income for a specific month, with optional additional income and deductions.

**Authentication:** Required

**Request Body:**
```json
{
  "taxYear": 2025,
  "month": 10,
  "additionalIncome": 4500000,
  "deductions": [
    {
      "deductionType": "Pension",
      "description": "Pension contribution (8%)",
      "amount": 400000
    },
    {
      "deductionType": "NHIS",
      "description": "Health insurance",
      "amount": 50000
    }
  ]
}
```

**Response (200 OK):**
```json
{
  "totalIncome": 5150000.0,
  "totalDeductions": 450000,
  "taxableIncome": 4700000.0,
  "totalTax": 636000.000,
  "effectiveTaxRate": 12.35,
  "netIncome": 4514000.000,
  "bracketCalculations": [
    {
      "bracketDescription": "First N800,000 at 0%",
      "incomeInBracket": 800000,
      "taxRate": 0.00,
      "taxAmount": 0.00
    },
    {
      "bracketDescription": "Next N2,200,000 at 15%",
      "incomeInBracket": 2200000,
      "taxRate": 0.15,
      "taxAmount": 330000.00
    },
    {
      "bracketDescription": "Next N9,000,000 at 18%",
      "incomeInBracket": 1700000.0,
      "taxRate": 0.18,
      "taxAmount": 306000.000
    }
  ],
  "deductionDetails": [
    {
      "type": "Pension",
      "description": "Pension contribution (8%)",
      "amount": 400000
    },
    {
      "type": "NHIS",
      "description": "Health insurance",
      "amount": 50000
    }
  ]
}
```

---

### Reports Endpoints

#### 13. Get User Report
**GET** `/reports/user`

Generate comprehensive tax report for the authenticated user across all years.

**Authentication:** Required

**Response (200 OK):**
```json
{
  "userId": 1,
  "username": "johndoe",
  "yearlySummaries": [
    {
      "taxYear": 2025,
      "totalIncome": 5000000.0,
      "totalTax": 609000.0,
      "effectiveTaxRate": 12.18
    },
    {
      "taxYear": 2024,
      "totalIncome": 4500000.0,
      "totalTax": 525000.0,
      "effectiveTaxRate": 11.67
    }
  ]
}
```

#### 14. Get Yearly Summaries
**GET** `/reports/yearly-summaries`

Get summaries for all tax years for the authenticated user.

**Authentication:** Required

**Response (200 OK):**
```json
[
  {
    "taxYear": 2025,
    "totalIncome": 5000000.0,
    "totalTax": 609000.0,
    "effectiveTaxRate": 12.18
  }
]
```

---

## Error Handling

All endpoints may return the following error responses:

### 400 Bad Request
```json
{
  "message": "Username and password are required"
}
```

### 401 Unauthorized
```json
{
  "message": "Invalid username or password"
}
```
or
```
Unauthorized
```
(when JWT token is missing or invalid)

### 404 Not Found
```json
{
  "message": "No tax record found for the specified year"
}
```

### 409 Conflict
```json
{
  "message": "Username already exists"
}
```

### 500 Internal Server Error
```json
{
  "message": "An error occurred"
}
```

---

## Example Usage with cURL

### Register and Login
```bash
# Register
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "johndoe",
    "email": "john@example.com",
    "password": "securePassword123",
    "fullName": "John Doe"
  }'

# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "johndoe",
    "password": "securePassword123"
  }'
```

### Submit Tax Data
```bash
# Save token from login response
TOKEN="your-jwt-token-here"

# Submit income and deductions
curl -X POST http://localhost:5000/api/tax/submit \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "taxYear": 2025,
    "incomeSources": [
      {
        "sourceType": "Employment",
        "description": "Salary",
        "amount": 5000000
      }
    ],
    "deductions": [
      {
        "deductionType": "Pension",
        "description": "Pension contribution",
        "amount": 400000
      }
    ]
  }'
```

### Get Reports
```bash
# Get tax summary for specific year
curl -X GET http://localhost:5000/api/tax/summary/2025 \
  -H "Authorization: Bearer $TOKEN"

# Get user report
curl -X GET http://localhost:5000/api/reports/user \
  -H "Authorization: Bearer $TOKEN"
```

---

## Rate Limiting

Currently, there are no rate limits applied. Consider implementing rate limiting in production.

## CORS

CORS is enabled for all origins in development. Configure appropriately for production.

## Security Notes

1. Always use HTTPS in production
2. Store JWT secret securely (use environment variables)
3. Implement password strength requirements
4. Consider implementing refresh tokens for better security
5. Add request validation and sanitization
6. Implement rate limiting to prevent abuse
7. Use secure password hashing (current implementation uses SHA-256, consider bcrypt for production)