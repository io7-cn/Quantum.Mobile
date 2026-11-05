# Installation and updates

[简体中文](../zh-CN/releases.md)

Download the available Windows portable archive or Android [APK](glossary.md#apk) from the [client release page](https://github.com/io7-cn/Quantum.Mobile/releases). Available architectures and versions are listed with each release. The installed app checks this release source. Settings supports automatic and manual checks.

Downloads are verified for declared size and [SHA-256](glossary.md#sha256) digest before opening. A higher semantic version or an allowed higher build number is required. Android does not offer build-number downgrades. Android uses the OS installer with explicit approval. Windows opens the portable archive for manual replacement after closing the app. Apple targets use their configured distribution channel.

## Packaging

Run `.ci/Build-MobileRelease.ps1 -Version X.Y.Z -Target win-x64 -BuildNumber N` for Windows or use `-Target android` for a signed Android build. Build numbers must be positive and increase for Android upgrades. The script produces `update.<channel>.json` and `SHA256SUMS.<channel>.txt` alongside the package.

| Environment variable | Type and requirement | Classification |
| --- | --- | --- |
| `MOBILE_ANDROID_KEYSTORE_PATH` | Required path to the persistent Android signing store for Android packaging | Secret location |
| `MOBILE_ANDROID_KEYSTORE_PASSWORD` | Required signing-store password for Android packaging | Secret |
| `ANDROID_HOME` or `ANDROID_SDK_ROOT` | Android development-kit path for signature verification, `ANDROID_HOME` takes precedence | Non-secret |

The signing alias is `quantum-mobile`. Inject signing inputs externally. Packaging omits debug symbols and includes the project license and attribution. The generated Android package must match the public certificate digest recorded in [.ci/android-signing-certificate.sha256](../../.ci/android-signing-certificate.sha256). Keep the signing store and its password in approved encrypted backup storage outside Git; subsequent Android releases must reuse this identity.

## Independent repository release workflow

[Release Mobile](../../.github/workflows/release-mobile.yml) builds this repository directly. Push a `mobile-vX.Y.Z` tag on verified `main` history to build Android and Windows packages, verify their checksums and publish the complete release. A prerelease suffix is allowed. Manual runs accept a version and upload workflow artifacts only; they do not create a release.

Configure repository secrets `MOBILE_ANDROID_KEYSTORE_BASE64` (the base64-encoded persistent [PKCS12](glossary.md#pkcs12) signing store) and `MOBILE_ANDROID_KEYSTORE_PASSWORD` (the shared store/key password). The workflow restores the store only on the Android runner and removes it after packaging. Normal verification builds do not receive release signing credentials. Both platform packages use the same positive build number, measured in [UTC](../../.standards/docs/glossary.md#utc) seconds since 2020-01-01; later releases must use a higher number.

## Upgrade from the legacy distribution

The independent repository uses a new Android signing certificate. Android installations signed with the previous certificate cannot be upgraded in place. Export or back up recoverable app data, uninstall the old app, then install the newly signed package and sign in again. Uninstalling removes local app data; verify the backup before proceeding. Keep the new signing identity stable for subsequent releases.

Clients configured for the legacy release source need a one-time manual installation from this repository. On Windows, close the app and replace the portable distribution while preserving user data. For Android, follow the uninstall/reinstall procedure above. Moving the source repository does not redirect an installed client's previous update source.
