# Contributing to dotnet-config-server

Thank you for your interest in contributing! All types of contributions are welcome.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A SQL Server instance or LocalDB (for integration tests)
- Git

## Building Locally

```bash
# Clone your fork
git clone https://github.com/<your-username>/dotnet-config-server.git
cd dotnet-config-server

# Restore dependencies
dotnet restore

# Build (Release)
dotnet build --configuration Release

# Or use the convenience script
./build.sh        # Linux / macOS
build.cmd         # Windows
```

## Running Tests

```bash
# Run all tests
dotnet test --configuration Release --verbosity normal

# Run with detailed output and save results
dotnet test --configuration Release --verbosity normal --logger "trx;LogFileName=test-results.trx"

# Run a specific test project
dotnet test tests/dotnet-config-server.Tests/ --configuration Release
```

## Contribution Workflow

1. **Fork** the repository on GitHub.
2. **Clone** your fork locally and create a feature branch:
   ```bash
   git checkout -b feature/my-feature
   ```
3. **Make your changes** — keep commits focused and descriptive.
4. **Run tests** to verify nothing is broken.
5. **Push** to your fork and **open a Pull Request** against `main`.

### Pull Request Guidelines

- One logical change per PR.
- Reference any related issues in the PR description (`Closes #123`).
- Ensure all CI checks pass before requesting a review.
- Update documentation and tests where relevant.

## Code Style

- Follow the existing conventions found in the codebase.
- An `.editorconfig` file is included at the root — make sure your editor respects it.
- Provide **XML documentation** for all public APIs and classes.
- Keep author headers intact on existing files.

## Reporting Issues

Use GitHub Issues and include:
- A clear description of the problem or feature.
- **Reproduction steps** for bugs.
- Expected vs. actual behavior.
- .NET version and OS if relevant.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
