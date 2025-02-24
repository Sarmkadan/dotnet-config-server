# Contributing to Dotnet Config Server

Thank you for your interest in contributing to Dotnet Config Server! This document provides guidelines and instructions for contributing.

## Code of Conduct

This project adheres to a Code of Conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to the maintainers.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the issue list as you might find out that you don't need to create one. When you are creating a bug report, please include as many details as possible:

- **Use a clear and descriptive title** for the issue
- **Describe the exact steps which reproduce the problem**
- **Provide specific examples to demonstrate the steps**
- **Describe the behavior you observed after following the steps**
- **Explain which behavior you expected to see instead and why**
- **Include screenshots and animated GIFs if possible**
- **Include your environment details**:
  - .NET version
  - Operating system and version
  - Browser and version (if applicable)

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, please include:

- **Use a clear and descriptive title** for the issue
- **Provide a step-by-step description of the suggested enhancement**
- **Provide specific examples to demonstrate the steps**
- **Describe the current behavior** and **the expected behavior**
- **Explain why this enhancement would be useful**

### Pull Requests

- Fill in the required template
- Follow the C# style guide
- Include appropriate test cases
- Include documentation updates
- End all files with a newline

## Development Setup

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [Git](https://git-scm.com/)
- [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio](https://visualstudio.microsoft.com/)

### Setting Up Your Development Environment

1. **Fork and clone the repository**
   ```bash
   git clone https://github.com/your-username/dotnet-config-server.git
   cd dotnet-config-server
   ```

2. **Install development tools**
   ```bash
   make dev-setup
   ```

3. **Create a feature branch**
   ```bash
   git checkout -b feature/amazing-feature
   ```

4. **Set up your local database**
   ```bash
   # Update connection string in appsettings.Development.json
   # Then apply migrations
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access Swagger UI**
   Open https://localhost:5001/swagger in your browser

## Development Guidelines

### C# Code Style

We follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

Key principles:

- Use `PascalCase` for class names and public members
- Use `camelCase` for local variables and private fields
- Use 4 spaces for indentation
- Lines should not exceed 120 characters
- Use meaningful variable names

Example:
```csharp
// ✓ Good
public class ConfigurationService
{
    private readonly IRepository<Configuration> _repository;
    
    public async Task<Configuration> GetConfigurationAsync(Guid id)
    {
        var configuration = await _repository.GetByIdAsync(id);
        return configuration;
    }
}

// ✗ Bad
public class ConfigurationService
{
  private IRepository<Configuration> repository;
  
  public Configuration GetConfiguration(Guid id)
  {
    return repository.GetById(id);
  }
}
```

### Method Documentation

All public methods should have XML documentation comments:

```csharp
/// <summary>
/// Creates a new configuration for the specified application.
/// </summary>
/// <param name="applicationId">The ID of the application.</param>
/// <param name="environment">The target environment.</param>
/// <returns>The created configuration.</returns>
/// <exception cref="ArgumentNullException">Thrown when applicationId is null.</exception>
public async Task<Configuration> CreateConfigurationAsync(Guid applicationId, string environment)
{
    if (applicationId == Guid.Empty)
        throw new ArgumentNullException(nameof(applicationId));
    
    // Implementation
}
```

### Testing

All new features must include tests. Run tests with:

```bash
dotnet test
```

Test naming convention: `MethodName_Scenario_ExpectedResult`

```csharp
[Fact]
public async Task CreateConfiguration_WithValidInput_ReturnsCreatedConfiguration()
{
    // Arrange
    var request = new CreateConfigurationRequest { /* ... */ };
    
    // Act
    var result = await _service.CreateConfigurationAsync(request);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(request.Environment, result.Environment);
}
```

### Commit Messages

Use clear, descriptive commit messages:

```
fix: correct null reference exception in ConfigurationService

Fixes #123

The service was not properly null-checking the configuration
before attempting to access its properties. Added guard clause.
```

Format: `<type>: <subject>`

Types:
- `feat`: A new feature
- `fix`: A bug fix
- `docs`: Documentation only
- `style`: Code style changes
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Adding or updating tests

### Git Workflow

1. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature
   ```

2. **Make your changes**
   ```bash
   # Make code changes
   # Run tests
   dotnet test
   
   # Check code style
   dotnet format
   ```

3. **Commit your changes**
   ```bash
   git add .
   git commit -m "feat: add amazing feature"
   ```

4. **Push to your fork**
   ```bash
   git push origin feature/your-feature
   ```

5. **Create a Pull Request**
   - Fill in the PR template
   - Link related issues
   - Request reviewers

## Pull Request Process

1. **Before submitting:**
   - [ ] Tests pass locally (`dotnet test`)
   - [ ] Code follows style guide (`dotnet format`)
   - [ ] Documentation is updated
   - [ ] No breaking changes (or documented)
   - [ ] Commits are logically organized

2. **In your PR description:**
   - Describe the changes
   - Link related issues (#123)
   - Include before/after screenshots if UI changes
   - List breaking changes if any

3. **Address review feedback:**
   - Make requested changes
   - Re-request review
   - Don't force-push after reviewers have commented

## Release Process

1. **Version Bumping**: Uses [Semantic Versioning](https://semver.org/)
   - MAJOR: Breaking changes
   - MINOR: New features (backward compatible)
   - PATCH: Bug fixes

2. **Release Steps**:
   - Update CHANGELOG.md
   - Update version in project file
   - Create release tag
   - Push to main branch
   - GitHub Actions handles the rest

## Project Structure

```
dotnet-config-server/
├── Models/              # Domain models
├── Controllers/         # API endpoints
├── Services/           # Business logic
├── Repositories/       # Data access
├── Data/              # Entity Framework
├── Middleware/        # HTTP middleware
├── Tests/             # Unit tests
├── examples/          # Usage examples
├── docs/              # Documentation
└── .github/workflows/ # CI/CD
```

## Feature Checklist

When adding a new feature:

- [ ] Add models in `Models/`
- [ ] Add service interface in `Services/I*.cs`
- [ ] Implement service in `Services/`
- [ ] Add repository if needed in `Repositories/`
- [ ] Add controller endpoints in `Controllers/`
- [ ] Add unit tests in `Tests/`
- [ ] Update documentation in `docs/`
- [ ] Add usage example in `examples/`
- [ ] Update CHANGELOG.md
- [ ] Update README.md if public API changes

## Getting Help

- **Issues**: [GitHub Issues](https://github.com/sarmkadan/dotnet-config-server/issues)
- **Discussions**: [GitHub Discussions](https://github.com/sarmkadan/dotnet-config-server/discussions)
- **Documentation**: See `docs/` folder
- **Examples**: See `examples/` folder

## Recognition

Contributors are recognized in:
- GitHub Contributors page
- Release notes for significant contributions
- Project documentation

## License

By contributing to this project, you agree that your contributions will be licensed under its MIT License.

---

Thank you for contributing to make Dotnet Config Server better!

**Questions?** Open a discussion or reach out to the maintainers.
