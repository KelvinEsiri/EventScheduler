# NasosoTax - Documentation Index

**Project:** NasosoTax - Tax Management Portal  
**Last Updated:** October 2025

Welcome to the NasosoTax documentation! This index will help you find the information you need.

---

## 📚 Quick Navigation

### 🚀 Getting Started
- [README](../README.md) - Project overview and quick start
- [Quick Start Guide](guides/QUICK_START.md) - Get up and running quickly
- [Running the Application](guides/RUNNING_SEPARATED_ARCHITECTURE.md) - How to run frontend and backend

### 🏗️ Architecture & Design
- [Architecture Documentation](ARCHITECTURE.md) - Complete architecture guide
- [Features Documentation](FEATURES.md) - All features explained
- [Project Improvements](PROJECT_IMPROVEMENTS.md) - Recommendations and roadmap

### 👨‍💻 For Developers
- [Contributing Guidelines](CONTRIBUTING.md) - How to contribute
- [Code of Conduct](CODE_OF_CONDUCT.md) - Community guidelines
- [API Documentation](guides/API_DOCUMENTATION.md) - REST API reference
- [Authentication Guide](guides/AUTHENTICATION.md) - Auth system details

### 🧪 Testing & Deployment
- [Testing Guide](guides/TESTING_GUIDE.md) - How to test the application
- [Deployment Guide](guides/DEPLOYMENT_GUIDE.md) - Production deployment instructions

---

## 📖 Documentation by Topic

### Core Concepts

#### Tax Calculation
The system implements the **Nigeria Tax Act 2025** with progressive tax brackets:

| Income Range | Tax Rate |
|-------------|----------|
| First ₦800,000 | 0% |
| Next ₦2,200,000 | 15% |
| Next ₦9,000,000 | 18% |
| Next ₦13,000,000 | 21% |
| Next ₦25,000,000 | 23% |
| Above ₦50,000,000 | 25% |

📄 **Related Documents:**
- [Features - Tax Calculation](FEATURES.md#tax-calculation-features)
- [Architecture - Tax Calculation Service](ARCHITECTURE.md#2-application-layer-nasosotaxapplication-)

#### User Authentication
JWT-based authentication with secure password hashing and session management.

📄 **Related Documents:**
- [Authentication Guide](guides/AUTHENTICATION.md)
- [API Documentation - Auth Endpoints](guides/API_DOCUMENTATION.md)
- [Features - User Authentication](FEATURES.md#1-user-authentication--authorization-)

#### Income Management
Track income from multiple sources with monthly breakdown support.

📄 **Related Documents:**
- [Features - Income Management](FEATURES.md#income-management-features)
- [Features - Monthly Income Breakdown](FEATURES.md#7-monthly-income-breakdown-)

#### Deductions
Support for all major Nigerian tax deductions (NHF, NHIS, Pension, etc.).

📄 **Related Documents:**
- [Features - Deduction Features](FEATURES.md#deduction-features)
- [Features - Comprehensive Deduction Support](FEATURES.md#9-comprehensive-deduction-support-)

#### General Ledger
Financial transaction tracking with filtering and reporting.

📄 **Related Documents:**
- [Features - General Ledger Features](FEATURES.md#general-ledger-features)
- [Features - Ledger Filtering](FEATURES.md#12-ledger-filtering--searching-)

---

## 🎯 Documentation by Role

### For New Users

**Start Here:**
1. [README](../README.md) - Understand what NasosoTax does
2. [Quick Start Guide](guides/QUICK_START.md) - Set up and run the application
3. [Features Documentation](FEATURES.md) - Learn about available features

### For Developers

**Start Here:**
1. [Architecture Documentation](ARCHITECTURE.md) - Understand the system design
2. [Contributing Guidelines](CONTRIBUTING.md) - Learn how to contribute
3. [API Documentation](guides/API_DOCUMENTATION.md) - API endpoints and usage

**Then Explore:**
- [Running the Application](guides/RUNNING_SEPARATED_ARCHITECTURE.md)
- [Authentication Guide](guides/AUTHENTICATION.md)
- [Testing Guide](guides/TESTING_GUIDE.md)

### For DevOps / Administrators

**Start Here:**
1. [Deployment Guide](guides/DEPLOYMENT_GUIDE.md) - Deploy to production
2. [Architecture Documentation](ARCHITECTURE.md) - Understand infrastructure needs
3. [API Documentation](guides/API_DOCUMENTATION.md) - Health checks and monitoring

### For Tax Professionals

**Start Here:**
1. [Features Documentation](FEATURES.md) - Learn all features
2. [Quick Start Guide](guides/QUICK_START.md) - Get started quickly
3. [README](../README.md) - Overview and usage examples

---

## 📂 Document Organization

### `/docs` - Main Documentation
```
docs/
├── ARCHITECTURE.md           # Architecture guide
├── FEATURES.md               # Features documentation
├── CONTRIBUTING.md           # Contribution guidelines
├── CODE_OF_CONDUCT.md        # Community guidelines
├── DOCUMENTATION_INDEX.md    # This file
├── guides/                   # User and developer guides
└── archive/                  # Historical documents
```

### `/docs/guides` - Guides and How-Tos
```
guides/
├── API_DOCUMENTATION.md                    # REST API reference
├── AUTHENTICATION.md                       # Auth system guide
├── DEPLOYMENT_GUIDE.md                     # Deployment instructions
├── TESTING_GUIDE.md                        # Testing guide
├── QUICK_START.md                          # Quick start
└── RUNNING_SEPARATED_ARCHITECTURE.md       # Running guide
```

### `/docs/archive` - Historical Documents
Archived documentation for reference (moved from `NasosoTax.Doc/`).

---

## 🔍 Find Documentation by Feature

### Tax Calculator
- **Main Doc:** [Features - Real-Time Tax Calculator](FEATURES.md#3-real-time-tax-calculator-)
- **Architecture:** [Architecture - Application Layer](ARCHITECTURE.md#2-application-layer-nasosotaxapplication-)
- **API:** [API Documentation - Tax Endpoints](guides/API_DOCUMENTATION.md)

### Income Submission
- **Main Doc:** [Features - Multiple Income Sources](FEATURES.md#6-multiple-income-sources-)
- **API:** [API Documentation - Tax Submit](guides/API_DOCUMENTATION.md)

### Reports
- **Main Doc:** [Features - Reporting Features](FEATURES.md#reporting-features)
- **API:** [API Documentation - Reports Endpoints](guides/API_DOCUMENTATION.md)

### General Ledger
- **Main Doc:** [Features - General Ledger Features](FEATURES.md#general-ledger-features)
- **API:** [API Documentation - Ledger Endpoints](guides/API_DOCUMENTATION.md)

### Authentication
- **Main Doc:** [Authentication Guide](guides/AUTHENTICATION.md)
- **API:** [API Documentation - Auth Endpoints](guides/API_DOCUMENTATION.md)
- **Architecture:** [Architecture - Security Features](ARCHITECTURE.md)

---

## 🆕 Recently Updated Documents

| Document | Last Updated | Changes |
|----------|-------------|---------|
| ARCHITECTURE.md | Oct 2025 | Created comprehensive architecture guide |
| FEATURES.md | Oct 2025 | Created complete features documentation |
| PROJECT_IMPROVEMENTS.md | Oct 2025 | Added recommendations and roadmap |
| CONTRIBUTING.md | Oct 2025 | Added contribution guidelines |
| CODE_OF_CONDUCT.md | Oct 2025 | Added community guidelines |
| DOCUMENTATION_INDEX.md | Oct 2025 | Created this index |

---

## 📋 Documentation Checklist

### For End Users ✅
- [x] Quick start guide
- [x] Features overview
- [x] Usage examples in README
- [x] FAQ (in README)

### For Developers ✅
- [x] Architecture documentation
- [x] API documentation
- [x] Authentication guide
- [x] Contributing guidelines
- [x] Code of conduct
- [x] Testing guide

### For DevOps ✅
- [x] Deployment guide
- [x] Running instructions
- [x] Configuration guide (in deployment docs)
- [x] Health check documentation

### Future Additions ⏳
- [ ] Troubleshooting guide
- [ ] Performance tuning guide
- [ ] Security best practices
- [ ] API versioning strategy
- [ ] Database migration guide
- [ ] Monitoring and logging guide

---

## 🔗 External Resources

### Nigeria Tax Act 2025
- Tax brackets and rates are based on the Nigeria Tax Act 2025
- See `Nigeria-Tax-Act-2025.pdf` in the root directory

### .NET Resources
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Blazor Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)

### Development Tools
- [Visual Studio](https://visualstudio.microsoft.com/)
- [VS Code](https://code.visualstudio.com/)
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

---

## 💡 How to Use This Documentation

### If You're Looking For...

**"How do I install and run the application?"**
→ Start with [Quick Start Guide](guides/QUICK_START.md)

**"How does the tax calculation work?"**
→ See [Features - Tax Calculation](FEATURES.md#tax-calculation-features)

**"How is the application structured?"**
→ Read [Architecture Documentation](ARCHITECTURE.md)

**"How do I contribute?"**
→ Check [Contributing Guidelines](CONTRIBUTING.md)

**"What API endpoints are available?"**
→ Review [API Documentation](guides/API_DOCUMENTATION.md)

**"How do I deploy to production?"**
→ Follow [Deployment Guide](guides/DEPLOYMENT_GUIDE.md)

**"How does authentication work?"**
→ Read [Authentication Guide](guides/AUTHENTICATION.md)

**"What features are available?"**
→ Browse [Features Documentation](FEATURES.md)

---

## 📝 Documentation Standards

All documentation in this project follows these standards:

### Structure
- Clear table of contents for long documents
- Consistent heading hierarchy
- Code examples where applicable
- Visual diagrams when helpful

### Style
- Clear, concise language
- Active voice
- Step-by-step instructions
- Real-world examples

### Maintenance
- Keep documentation up-to-date with code changes
- Review and update quarterly
- Mark outdated sections clearly
- Archive historical documents

---

## 🤝 Contributing to Documentation

Found an error or want to improve the documentation?

1. **Small fixes:** Edit directly via GitHub
2. **Large changes:** Follow the [Contributing Guidelines](CONTRIBUTING.md)
3. **Questions:** Open an issue on GitHub

Documentation contributions are highly valued! Clear documentation helps everyone.

---

## 📮 Feedback

Have feedback on the documentation?

- **Missing information?** Open an issue
- **Unclear sections?** Let us know
- **Suggestions?** We'd love to hear them

Your feedback helps us improve the documentation for everyone.

---

## 🎯 Documentation Goals

Our documentation aims to:

1. ✅ **Be Accessible** - Easy to find and understand
2. ✅ **Be Comprehensive** - Cover all aspects of the system
3. ✅ **Be Current** - Stay up-to-date with the codebase
4. ✅ **Be Practical** - Include real-world examples
5. ✅ **Be Organized** - Well-structured and easy to navigate

---

## Summary

**Total Documents:** 10+ core documents  
**Coverage:** Architecture, Features, Guides, API, Contributing  
**Status:** ✅ Core documentation complete

---

**Happy Reading! 📖**

For questions or suggestions, please open an issue on GitHub.

---

**Document Version:** 1.0  
**Last Updated:** October 2025  
**Maintained By:** Development Team
