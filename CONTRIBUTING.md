# Contributing to dotnet-config-server

Thank you for your interest in contributing! All types of contributions are welcome.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (`net10.0` target in `dotnet-config-server.csproj`)
- Git
- SQL Server is only needed to run the API against a real database (see `appsettings.example.json`); the tests don't require one.

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
2. **Clone** your fork locally and create a branch from `main` (see naming below):
   ```bash
   git checkout -b feature/my-feature
   ```
3. **Make your changes** — keep commits focused and descriptive.
4. **Run tests** to verify nothing is broken.
5. **Push** to your fork and **open a Pull Request** against `main`.

### Branch Naming

Use a type prefix and a short kebab-case description:

- `feature/<description>` — new functionality
- `fix/<description>` — bug fixes
- `docs/<description>` — documentation only
- `refactor/<description>`, `chore/<description>` — everything else

Examples: `feature/config-diff-export`, `fix/batch-operation-error-count`.

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `docs:`, `refactor:`, `chore:`, `style:`), matching the existing history.

### Pull Request Guidelines

- Open the PR against `main`.
- One logical change per PR.
- **CI must pass before review.** The workflows in `.github/workflows/` run:
  - `dotnet restore`, then `dotnet build --configuration Release` (`ci.yml`, `build.yml`)
  - `dotnet test --configuration Release` (`ci.yml`, `build.yml`)
  - `dotnet format --verify-no-changes` and a build with `/p:TreatWarningsAsErrors=true` (`build.yml`, code-quality job)

  Run `make ci` or `make lint` and `make format` locally to catch these before pushing.
- **Describe the change** in the PR body: what changed, why, how you tested it, and any follow-up work. Link related issues (`Closes #123`).
- Update documentation and tests where relevant.

## Code Style

- **C# standard style**; C# language version `latest` on `net10.0`.
- **4-space indentation** for `.cs` files, LF line endings, UTF-8 (enforced by `.editorconfig`).
- **Nullable reference types are enabled** (`<Nullable>enable</Nullable>`); new `.cs` files should start with `#nullable enable`.
- Follow the [`.editorconfig`](.editorconfig) at the root. Key rules: interfaces are prefixed with `I`, private fields use `_camelCase`, braces are required (`csharp_prefer_braces`), and `dotnet format` must produce no changes.
- Namespaces follow the folder: `DotnetConfigServer.<Folder>`.
- Use Serilog structured logging (`ILogger<T>`, message templates with named placeholders).
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
