# Quantum Mobile

[简体中文](../zh-CN/README.md)

Quantum Mobile is a native companion for [Quantum Desktop](https://github.com/io7-cn/Quantum.Desktop), maintained by the product engineering team of 艾欧柒（杭州）智能科技有限公司. It renders declared plugin pages and sends actions to a desktop signed in to the same platform account. Business operations run in Desktop plugins. The app uses [.NET MAUI](glossary.md#maui) for Android, iOS, Windows and Mac Catalyst.

## Source layout

| Directory | Responsibility |
| --- | --- |
| `src/Quantum.Mobile/` | Native controls, page orchestration, platform APIs and storage adapters |
| `src/Quantum.Mobile.Core/` | Platform networking, navigation state, attachment storage and updates |
| `src/Quantum.Mobile.Protocol/` | Mobile messages, page models, serialization and validation |
| `tests/` | Corresponding host, Core and protocol suites, plus page fixtures |
| `docs/en/` and `docs/zh-CN/` | Maintained documentation |
| `.ci/` | Verification and packaging helpers |

The source projects restore their own dependencies. The protocol and Core are built from this repository. Plugin authors use the separately published [Quantum SDK](https://github.com/io7-cn/Quantum.Sdk), where [SDK](glossary.md#sdk) means Software Development Kit.

## Build and verify

Use a .NET 10 SDK selected by `global.json`, the native workload for the target, and its platform tools. Android requires an Android development kit and Java Development Kit 21. Apple targets require macOS and a compatible Xcode installation.

```powershell
dotnet test .ci/Quantum.Mobile.Checks.slnx -c Release
dotnet build src/Quantum.Mobile/Quantum.Mobile.csproj -c Debug -f net10.0-windows10.0.19041.0 -p:QuantumMobileTargetFramework=net10.0-windows10.0.19041.0
dotnet build src/Quantum.Mobile/Quantum.Mobile.csproj -c Debug -f net10.0-android -p:QuantumMobileTargetFramework=net10.0-android
```

Use `dotnet run` with the same project, target and property to launch a native target. Windows and Android builds run in [CI](glossary.md#ci). Apple targets require validation on the corresponding hardware and toolchain.

## Connect

Start Desktop, sign in to the Quantum Platform account, and install a plugin that declares mobile pages. Sign in to the same account in Mobile, then select an online desktop. The account menu switches desktops and opens settings. The bottom navigation opens plugin pages. Pull the page down to refresh. A pending operation must be checked before switching desktops.

The default platform is `https://quantum.io-vii.com/`. Advanced settings accepts a self-hosted platform root address. Transport requires HTTPS except for [loopback](glossary.md#loopback) development addresses. Account and device credentials use [secure storage](glossary.md#secure-storage). See [login sessions](login-sessions.md) and [configuration](configuration.md).

Debug builds provide an isolated preview and local automation routes. Their [developer guide](development.md) describes setup. Release builds exclude these tools.

See [architecture](architecture.md), [installation and updates](releases.md), [contributing](CONTRIBUTING.md) and [security reporting](security.md). Report ordinary bugs and requests through [Issues](https://github.com/io7-cn/Quantum.Mobile/issues).

Licensed under [Apache License 2.0](../../LICENSE). Attribution and third-party terms are provided in [NOTICE](../../NOTICE).

## Development standards

Follow the [English development standards](../../.standards/docs/en/README.md). The .standards submodule records the exact reviewed revision; upstream changes apply only after an explicit update of that recorded commit. Initialize it before development:

```sh
git submodule update --init --recursive
python .standards/.ci/check-adoption.py
```

Keep both language editions aligned. AGENTS.md links to the adopted English entry point; project-specific instructions supplement the shared requirements.
