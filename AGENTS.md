# Quantum Mobile instructions

- Follow the [English development standards](.standards/docs/en/README.md) at the commit recorded by this repository. Use the contributor-approved Git identity for both author and committer; never invent a placeholder identity.

- Read the [English documentation](docs/en/README.md). Maintain matching files under `docs/en/` and `docs/zh-CN/`, with direct links between each pair. Keep this file in English.
- Follow the repository EditorConfig. Prefer var when inferable, PascalCase for static fields and constants, _camelCase for private instance fields, braces for control blocks, and expression bodies for single-line methods.
- Group host and Core source by technical responsibility. Keep the corresponding test project directories aligned with source. Keep CI and development helpers under `.ci/`.
- Keep [MAUI](docs/en/glossary.md#maui) controls, platform APIs, secure storage adapters and lifecycle integration in `Quantum.Mobile`. Keep platform-independent services in `Quantum.Mobile.Core`. Keep `Quantum.Mobile.Protocol` independent of MAUI, [NOF](docs/en/glossary.md#nof) and the plugin SDK.
- Preserve existing serialized fields and the `Quantum.Mobile.Internal` namespace when reorganizing protocol code. Native clients call the platform transport. Plugin execution belongs to Desktop.
- Run `dotnet test .ci/Quantum.Mobile.Checks.slnx -c Release` and the configured formatting checks. Build affected native targets and use the isolated Debug checks for UI changes. Keep Debug routes and previews out of Release.
- Explain specialist terms using the [glossary](docs/en/glossary.md). Keep public examples fictional and repository-relative.
