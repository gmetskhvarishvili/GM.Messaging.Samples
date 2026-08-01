# Contributing to GM.Messaging Samples

This repository is a **usage sample** for the [`GM.Messaging`](https://www.nuget.org/packages/GM.Messaging)
stack. It is not published to NuGet — there is no versioning or release workflow here. The goal is
to keep the sample building, tested, and easy to follow.

## Prerequisites

- **.NET 10 SDK**
- **RabbitMQ** + **PostgreSQL** to run the services end-to-end (the unit tests need neither).

```bash
dotnet build -c Release
dotnet test  -c Release
```

## Branch & PR flow

1. Branch off `master`: `git switch -c fix/consumer-inbox`
2. Open a PR into `master`. CI (`build` + tests) must pass.
3. Keep changes focused and the README in sync with what the sample does.

## Guidelines

- Reference the published **GM.*** packages — don't add project references into the library repos.
- Handler/validator changes should come with a test in `tests/GM.Messaging.Sample.Tests`.
- Bump a `GM.*` package version when a new release adds something the sample should show.

## Where releases happen

Package versioning, tags, changelog and nuget.org publishing live in the library repository
([`GM.Messaging`](https://github.com/gmetskhvarishvili/GM.Messaging)), driven by Conventional Commits.
Nothing is published from this samples repo.

## Code style

Enforced by [`.editorconfig`](.editorconfig). Run `dotnet format` before pushing if unsure.
