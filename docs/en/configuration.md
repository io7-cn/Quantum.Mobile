# Configuration

[简体中文](../zh-CN/configuration.md)

| Setting | Type and default | Persistence and classification |
| --- | --- | --- |
| Platform address | HTTPS root address, `https://quantum.io-vii.com/` | Advanced settings, local preference `quantum.mobile.platform`, non-secret |
| Language | `en` or `zh`, initial system UI culture | Sign-in language selector, `quantum.mobile.language`, non-secret |
| Automatic update check | Boolean, true | Settings, `quantum.mobile.auto-update-check`, non-secret |
| Account session | Serialized account credentials, absent until login | OS [secure storage](glossary.md#secure-storage), scoped to platform, secret |
| Device registration and pending action | Serialized credentials and action content, absent until connection/submission | OS secure storage, scoped to platform and account, secret |
| `QUANTUM_MOBILE_DEBUG_PORT` | Integer 1024–65535, unset disables the server | Debug process environment only, non-secret |
| `QUANTUM_MOBILE_DEBUG_TOKEN` | 32–256 printable non-whitespace ASCII characters, required when enabled | Debug process environment only, secret |
| `QUANTUM_MOBILE_DEBUG_PREVIEW` | `1` enables isolated preview, unset disables it | Debug process environment only, non-secret |
| `QuantumMobileTargetFramework` | Build property, native targets chosen for the build OS | Explicit build argument overrides target selection, non-secret |

The selected platform is saved only after address validation. HTTPS is mandatory except for [loopback](glossary.md#loopback). Use a root address such as `https://platform.example/`. Path, credential and query handling is defined by `PlatformAddress.Parse`. Account secrets are stored after successful authentication. See [login sessions](login-sessions.md).

No release signing credentials belong in source. Packaging uses the environment values documented in [releases](releases.md). Debug environment variables are ignored by Release.
