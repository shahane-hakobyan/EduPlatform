# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

EduPlatform is an ASP.NET Core MVC application currently at the default project-template stage (`dotnet new mvc`-equivalent scaffold). There is no custom domain logic, database, or test project yet — `HomeController` and its views are the only application code. Treat any architectural conventions below as the starting point, not an established pattern to preserve at all costs.

## Commands

Run all commands from the repo root (`EduPlatform.slnx`) or from `EduPlatform/` (the single project, `EduPlatform.csproj`).

```
dotnet build                    # build the solution
dotnet run --project EduPlatform   # run the app (see launchSettings.json for ports/profiles)
dotnet watch --project EduPlatform run   # run with hot reload
```

There is no test project in the solution yet. If one is added, prefer `dotnet test` and note the convention here.

## Architecture

- **Framework**: ASP.NET Core MVC targeting `net10.0`, nullable reference types and implicit usings enabled (`EduPlatform/EduPlatform.csproj`).
- **Entry point**: [Program.cs](EduPlatform/Program.cs) — minimal hosting model. Request pipeline: exception handler + HSTS in non-Development, HTTPS redirection, routing, authorization, then `MapStaticAssets()` and the default `{controller=Home}/{action=Index}/{id?}` route.
- **Controllers**: `EduPlatform/Controllers/` — currently just `HomeController` (Index, Privacy, Error).
- **Views**: Razor views under `EduPlatform/Views/`, following standard MVC conventions (`Views/{Controller}/{Action}.cshtml`, shared layout in `Views/Shared/_Layout.cshtml`).
- **Static assets**: `EduPlatform/wwwroot/` (css, js, lib — client-side libraries). The app uses the newer `MapStaticAssets()`/`WithStaticAssets()` static asset pipeline (not the older `UseStaticFiles()`), so new static assets should go through this convention.
- **Config**: `appsettings.json` / `appsettings.Development.json` for configuration; `Properties/launchSettings.json` for local run profiles.
