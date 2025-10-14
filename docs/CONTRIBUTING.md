# Contributing to NasosoTax

First off, thank you for considering contributing to NasosoTax! It's people like you that make NasosoTax such a great tool for tax management.

## Table of Contents

1. [Code of Conduct](#code-of-conduct)
2. [Getting Started](#getting-started)
3. [How Can I Contribute?](#how-can-i-contribute)
4. [Development Process](#development-process)
5. [Coding Standards](#coding-standards)
6. [Commit Guidelines](#commit-guidelines)
7. [Pull Request Process](#pull-request-process)
8. [Testing Guidelines](#testing-guidelines)

---

## Code of Conduct

This project and everyone participating in it is governed by the [NasosoTax Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

---

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Git
- A code editor (Visual Studio, VS Code, or Rider recommended)
- Basic knowledge of C#, ASP.NET Core, and Blazor

### Setting Up Your Development Environment

1. **Fork the repository**
   ```bash
   # Click the 'Fork' button on GitHub
   ```

2. **Clone your fork**
   ```bash
   git clone https://github.com/YOUR-USERNAME/NasosoTax.git
   cd NasosoTax
   ```

3. **Add upstream remote**
   ```bash
   git remote add upstream https://github.com/KelvinEsiri/NasosoTax.git
   ```

4. **Install dependencies**
   ```bash
   dotnet restore
   ```

5. **Build the project**
   ```bash
   dotnet build
   ```

6. **Run the application**
   ```bash
   # Terminal 1 - API
   cd NasosoTax.Api
   dotnet run

   # Terminal 2 - Web
   cd NasosoTax.Web
   dotnet run
   ```

---

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the existing issues to avoid duplicates. When you create a bug report, include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps to reproduce the problem**
- **Provide specific examples** (code snippets, screenshots, etc.)
- **Describe the behavior you observed and what you expected**
- **Include your environment details** (.NET version, OS, browser, etc.)

**Bug Report Template:**
```markdown
## Description
[Clear description of the bug]

## Steps to Reproduce
1. Go to '...'
2. Click on '...'
3. Scroll down to '...'
4. See error

## Expected Behavior
[What you expected to happen]

## Actual Behavior
[What actually happened]

## Screenshots
[If applicable, add screenshots]

## Environment
- OS: [e.g., Windows 11, macOS 13]
- .NET Version: [e.g., 9.0]
- Browser: [e.g., Chrome 118]
- Project Version: [e.g., 1.1.0]

## Additional Context
[Any other relevant information]
```

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, include:

- **Use a clear and descriptive title**
- **Provide a detailed description** of the suggested enhancement
- **Explain why this enhancement would be useful**
- **Provide examples** of how the feature would be used
- **Consider the impact** on existing features

**Enhancement Template:**
```markdown
## Feature Description
[Clear description of the feature]

## Problem It Solves
[What problem does this solve?]

## Proposed Solution
[How should this feature work?]

## Alternatives Considered
[What other solutions did you consider?]

## Additional Context
[Any other relevant information]
```

### Your First Code Contribution

Unsure where to begin? Look for issues labeled:
- `good first issue` - Simple issues for beginners
- `help wanted` - Issues where we need community help
- `documentation` - Documentation improvements

### Pull Requests

1. Follow the [Development Process](#development-process)
2. Follow the [Coding Standards](#coding-standards)
3. Update documentation as needed
4. Add tests for new features
5. Ensure all tests pass
6. Follow the [Pull Request Process](#pull-request-process)

---

## Development Process

### Branching Strategy

- `main` - Production-ready code
- `develop` - Development branch (if used)
- Feature branches - `feature/feature-name`
- Bug fix branches - `fix/bug-name`
- Hotfix branches - `hotfix/issue-name`

### Creating a Feature Branch

```bash
# Update your local repository
git checkout main
git pull upstream main

# Create a feature branch
git checkout -b feature/your-feature-name
```

### Making Changes

1. **Make your changes** in your feature branch
2. **Test your changes** thoroughly
3. **Commit your changes** following commit guidelines
4. **Push to your fork**
   ```bash
   git push origin feature/your-feature-name
   ```

### Keeping Your Branch Updated

```bash
# Fetch upstream changes
git fetch upstream

# Rebase your branch
git rebase upstream/main

# Force push if needed (only for your own branches)
git push origin feature/your-feature-name --force
```

---

## Coding Standards

### C# Coding Conventions

Follow Microsoft's [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

**Key Points:**
- Use PascalCase for class names, method names, and properties
- Use camelCase for local variables and parameters
- Use meaningful and descriptive names
- One class per file
- Use `var` when the type is obvious
- Use `async`/`await` for asynchronous operations
- Avoid unnecessary complexity

**Example:**
```csharp
// Good
public class TaxCalculationService
{
    private readonly ILogger<TaxCalculationService> _logger;

    public TaxCalculationService(ILogger<TaxCalculationService> logger)
    {
        _logger = logger;
    }

    public async Task<TaxCalculationResult> CalculateTaxAsync(decimal income)
    {
        // Implementation
    }
}

// Avoid
public class taxcalculation
{
    public decimal calc(decimal i)
    {
        // Implementation
    }
}
```

### Project Structure

- Place entities in `NasosoTax.Domain/Entities`
- Place DTOs in `NasosoTax.Application/DTOs`
- Place services in `NasosoTax.Application/Services`
- Place repositories in `NasosoTax.Infrastructure/Repositories`
- Place controllers in `NasosoTax.Api/Controllers`
- Place Blazor pages in `NasosoTax.Web/Components/Pages`

### Dependency Injection

- Always use constructor injection
- Register services in `Program.cs`
- Use appropriate lifetimes (Transient, Scoped, Singleton)

```csharp
// Good
public class TaxController : ControllerBase
{
    private readonly ITaxCalculationService _taxService;

    public TaxController(ITaxCalculationService taxService)
    {
        _taxService = taxService;
    }
}
```

### Error Handling

- Use try-catch blocks where appropriate
- Log errors with sufficient context
- Return meaningful error messages
- Use proper HTTP status codes

```csharp
try
{
    var result = await _service.ProcessAsync(request);
    return Ok(result);
}
catch (ValidationException ex)
{
    _logger.LogWarning(ex, "Validation failed for request");
    return BadRequest(new { error = ex.Message });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error processing request");
    return StatusCode(500, new { error = "An error occurred" });
}
```

### Logging

- Use structured logging with Serilog
- Include relevant context in log messages
- Use appropriate log levels

```csharp
_logger.LogInformation("Calculating tax for user {UserId} with income {Income}", 
    userId, income);

_logger.LogError(ex, "Failed to calculate tax for user {UserId}", userId);
```

---

## Commit Guidelines

### Commit Message Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks
- `perf`: Performance improvements

**Example:**
```
feat(tax-calc): add monthly income toggle feature

- Added IsMonthlyIncome property to TaxCalculationRequest
- Updated calculator UI with toggle switch
- Added automatic annualization (monthly × 12)
- Added visual indicators for mode

Closes #123
```

### Good Commit Practices

- **Make atomic commits** - One logical change per commit
- **Write clear messages** - Explain what and why, not how
- **Reference issues** - Use `Closes #123` or `Fixes #456`
- **Keep commits focused** - Don't mix unrelated changes

---

## Pull Request Process

### Before Submitting

1. **Ensure your code builds** without errors
   ```bash
   dotnet build
   ```

2. **Run existing tests** (when available)
   ```bash
   dotnet test
   ```

3. **Update documentation** if needed
   - README.md
   - API_DOCUMENTATION.md
   - Feature documentation

4. **Check for code quality**
   - No compiler warnings
   - Follows coding standards
   - Properly formatted

### Creating a Pull Request

1. **Push your branch** to your fork
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create a Pull Request** on GitHub
   - Use a clear and descriptive title
   - Fill out the PR template
   - Link related issues
   - Add screenshots for UI changes

3. **PR Description Template:**
   ```markdown
   ## Description
   [Brief description of changes]

   ## Type of Change
   - [ ] Bug fix
   - [ ] New feature
   - [ ] Breaking change
   - [ ] Documentation update

   ## Changes Made
   - [List of changes]
   - [Another change]

   ## Testing
   - [ ] Tested locally
   - [ ] All existing tests pass
   - [ ] Added new tests (if applicable)

   ## Screenshots
   [If applicable, add screenshots]

   ## Related Issues
   Closes #123

   ## Checklist
   - [ ] Code follows project coding standards
   - [ ] Documentation updated
   - [ ] No new warnings
   - [ ] Tested thoroughly
   ```

### Review Process

1. **Automated Checks** - CI/CD pipeline runs (when configured)
2. **Code Review** - Maintainers review your code
3. **Address Feedback** - Make requested changes
4. **Approval** - Get approval from maintainers
5. **Merge** - Maintainers merge your PR

### After Your PR is Merged

1. **Delete your feature branch** (optional)
   ```bash
   git branch -d feature/your-feature-name
   git push origin --delete feature/your-feature-name
   ```

2. **Update your local repository**
   ```bash
   git checkout main
   git pull upstream main
   ```

---

## Testing Guidelines

### Unit Tests

- Test individual components in isolation
- Use mocking for dependencies
- Follow Arrange-Act-Assert pattern
- Write meaningful test names

```csharp
[Fact]
public async Task CalculateTax_WithValidIncome_ReturnsCorrectTax()
{
    // Arrange
    var service = new TaxCalculationService();
    var income = 5000000m;

    // Act
    var result = service.CalculateTax(income, new List<DeductionDetail>());

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expected, result.TotalTax);
}
```

### Integration Tests

- Test API endpoints end-to-end
- Use test database
- Clean up test data
- Test error scenarios

### Manual Testing

Before submitting:
1. Test in different browsers (Chrome, Firefox, Edge)
2. Test on different screen sizes (desktop, tablet, mobile)
3. Test error scenarios
4. Test edge cases

---

## Documentation

### Code Documentation

- Add XML comments to public APIs
- Document complex algorithms
- Explain non-obvious code
- Keep documentation up-to-date

```csharp
/// <summary>
/// Calculates tax based on Nigeria Tax Act 2025 progressive brackets.
/// </summary>
/// <param name="income">Total annual income before deductions</param>
/// <param name="deductions">List of eligible deductions</param>
/// <returns>Detailed tax calculation result with bracket breakdown</returns>
public TaxCalculationResult CalculateTax(decimal income, List<DeductionDetail> deductions)
{
    // Implementation
}
```

### Documentation Files

Keep these files updated:
- `README.md` - Project overview and setup
- `docs/ARCHITECTURE.md` - Architecture documentation
- `docs/FEATURES.md` - Feature documentation
- `docs/guides/*.md` - User guides

---

## Questions or Need Help?

- **GitHub Issues** - Ask questions in issues
- **GitHub Discussions** - Start a discussion
- **Email** - Contact maintainers directly

---

## Recognition

Contributors will be recognized in:
- README.md contributors section
- Release notes
- Project documentation

---

Thank you for contributing to NasosoTax! Your efforts help make tax management easier for everyone. 🎉

---

**Document Version:** 1.0  
**Last Updated:** October 2025  
**Maintained By:** Development Team
