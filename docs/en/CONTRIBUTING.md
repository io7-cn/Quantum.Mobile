# Contributing

[简体中文](../zh-CN/CONTRIBUTING.md)

Branch from `main` and use focused conventional commits. Keep tests in the project that owns the source, with matching relative directories. Maintain both language editions together. Follow `.editorconfig` for naming, layout, braces, var and expression bodies.

After restoring, run `./.ci/Format-Mobile.ps1 -Verify` and `dotnet test .ci/Quantum.Mobile.Checks.slnx -c Release`. Native changes also require a build of the affected target. Set `QuantumMobileTargetFramework` to the native target when restoring and formatting `Quantum.Mobile.slnx`. The full solution includes the native application, while the checks solution is platform independent.

Use the isolated Debug checks for rendering or navigation changes. Verify Release still excludes debug routes. Preserve serialized messages and session storage keys when moving code. Use native platform interfaces and existing framework transports before adding an implementation.

Contributions use [Apache License 2.0](../../LICENSE). Preserve applicable attribution and third-party terms in [NOTICE](../../NOTICE). Explain specialist terms using the [glossary](glossary.md). Report ordinary issues through [GitHub Issues](https://github.com/io7-cn/Quantum.Mobile/issues).
