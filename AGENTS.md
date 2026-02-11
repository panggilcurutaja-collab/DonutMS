# Repository Guidelines

## Project Structure & Module Organization
The solution root contains `DonutMS.slnx` and a single application project at `DonutMS/`.

- `DonutMS/Views`: WPF windows and user controls (`*Window.xaml`, `*View.xaml`).
- `DonutMS/ViewModels`: MVVM view models (`*ViewModel.cs`) using CommunityToolkit attributes.
- `DonutMS/Services`: business services (auth, navigation, backup, logging integration).
- `DonutMS/Data`: EF Core context, entities, and migrations.
- `DonutMS/Configuration`, `DonutMS/Core`, `DonutMS/Models`, `DonutMS/Validators`, `DonutMS/Resources`: app wiring, shared utilities, DTO/domain models, validation, and styles.
- Runtime artifacts: local SQLite DB (`donutms.db`) and `Logs/` under output folders.

## Build, Test, and Development Commands
Run from repository root:

- `dotnet restore DonutMS/DonutMS.csproj`: restore NuGet packages.
- `dotnet build DonutMS/DonutMS.csproj -c Debug`: compile the WPF app.
- `dotnet run --project DonutMS/DonutMS.csproj`: launch app without Visual Studio.
- `dotnet test DonutMS/DonutMS.csproj -c Debug`: test entrypoint (currently no automated test assemblies).
- `dotnet ef database update --project DonutMS/DonutMS.csproj`: apply EF Core migrations.

## Coding Style & Naming Conventions
- Use 4-space indentation and file-scoped namespaces (`namespace DonutMS;`).
- Keep nullable reference types enabled (`<Nullable>enable</Nullable>`).
- Naming: `PascalCase` for types/methods/properties, `_camelCase` for private fields, `I*` for interfaces.
- Follow existing MVVM naming: `*View`, `*ViewModel`, `*Service`, `*Dto`.
- Prefer small, focused classes and constructor injection through DI.

## Testing Guidelines
There is no dedicated test project yet. For behavior changes:

- Validate manually via login flow, navigation, and affected feature screens.
- Confirm startup stages and failures through `Logs/donutms-YYYYMMDD.log`.
- When adding automated tests, create a separate test project (for example `DonutMS.Tests`) and keep test names descriptive (`MethodName_State_ExpectedResult`).

## Commit & Pull Request Guidelines
- Recent history shows mixed messages; prefer Conventional Commits: `fix:`, `feat:`, `refactor:`, `chore:`.
- Keep commits scoped to one concern and include migration/config changes in the same PR when required.
- PRs should include: summary, changed modules, verification steps, and screenshots for UI/XAML updates.

## Security & Configuration Tips
- Do not commit secrets or machine-specific credentials.
- Use `DONUTMS_SEED_PASSWORD` for seeded user passwords when applicable.
- Keep `appsettings*.json` environment-safe and avoid embedding production credentials.
