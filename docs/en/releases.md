# Installation and updates

[简体中文](../zh-CN/releases.md)

Download the available Windows portable archive or Android [APK](glossary.md#apk) from the [client release page](https://github.com/XmmShp/Quantum/releases). Available architectures and versions are listed with each release. The installed app checks this release source. Settings supports automatic and manual checks.

Downloads are verified for declared size and [SHA-256](glossary.md#sha256) digest before opening. A higher semantic version or an allowed higher build number is required. Android does not offer build-number downgrades. Android uses the OS installer with explicit approval. Windows opens the portable archive for manual replacement after closing the app. Apple targets use their configured distribution channel.

## Packaging

Run `.ci/Build-MobileRelease.ps1 -Version X.Y.Z -Target win-x64 -BuildNumber N` for Windows or use `-Target android` for a signed Android build. Build numbers must be positive and increase for Android upgrades. The script produces `update.<channel>.json` and `SHA256SUMS.<channel>.txt` alongside the package.

| Environment variable | Type and requirement | Classification |
| --- | --- | --- |
| `MOBILE_ANDROID_KEYSTORE_PATH` | Required path to the persistent Android signing store for Android packaging | Secret location |
| `MOBILE_ANDROID_KEYSTORE_PASSWORD` | Required signing-store password for Android packaging | Secret |
| `ANDROID_HOME` or `ANDROID_SDK_ROOT` | Android development-kit path for signature verification, `ANDROID_HOME` takes precedence | Non-secret |

The signing alias is `quantum-mobile`. Preserve the signing identity used by installed clients. Inject signing inputs externally. Packaging omits debug symbols and includes the project license and attribution. The release pipeline that holds signing credentials fetches a pinned commit of this repository. The repository's own [CI](glossary.md#ci) validates builds without production signing credentials.
