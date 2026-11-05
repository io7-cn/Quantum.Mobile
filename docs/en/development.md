# Local debug interface

[简体中文](../zh-CN/development.md)

Debug builds provide an isolated native preview and an opt-in [loopback](glossary.md#loopback) automation interface. Release excludes the server and preview sources. Use PowerShell 7 at the repository root.

```powershell
$env:QUANTUM_MOBILE_DEBUG_PORT = '9224'
$env:QUANTUM_MOBILE_DEBUG_TOKEN = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$env:QUANTUM_MOBILE_DEBUG_PREVIEW = '1'
dotnet run --project src/Quantum.Mobile/Quantum.Mobile.csproj -c Debug -f net10.0-windows10.0.19041.0 -p:QuantumMobileTargetFramework=net10.0-windows10.0.19041.0
```

Without a configured port the server stays disabled. The token is required and must contain 32–256 printable non-whitespace characters. It is sent in the Authorization header. Keep the same environment token in the calling shell and invoke `.ci/Invoke-MobileDebug.ps1`. The default port is 9224.

```powershell
$state = ./.ci/Invoke-MobileDebug.ps1
$view = $state.value.page.viewId
./.ci/Invoke-MobileDebug.ps1 -Method POST -Path /input -Json (@{
    viewId=$view; nodeId='content'; value='example'
} | ConvertTo-Json)
./.ci/Invoke-MobileDebug.ps1 -Method POST -Path /press -Json (@{
    viewId=$view; nodeId='save'
} | ConvertTo-Json)
```

| Method and path | Behavior |
| --- | --- |
| `GET /state` | Native page tree, input values, preview and pending-action state |
| `GET /devices` | Online desktops for the authenticated account |
| `POST /desktop/select` | Select `desktopId` |
| `GET /pages` | Current desktop page catalog |
| `POST /page/open` | Open `pluginId` and `pageId` |
| `POST /page/pull-refresh` | Run native pull-to-refresh and await the new snapshot |
| `POST /page/refresh` | Refresh the specified `viewId` |
| `POST /input` | Set `nodeId` to a string or Boolean `value` within `viewId` |
| `POST /press` | Press an enabled `nodeId` within `viewId` |
| `POST /menu/open`, `POST /menu/close` | Open or close the account drawer |
| `POST /settings/open`, `POST /settings/close` | Open settings or return without replacing the draft |
| `GET /screenshot` | Windows Debug app capture, disabled on sign-in |
| `POST /navigation/reorder` | Reorder `source` and `target` page keys |
| `POST /navigation/open`, `POST /navigation/close` | Expand or collapse the page list |
| `POST /navigation/select` | Select `pluginId` and `pageId` |
| `POST /preview` | Enter isolated preview |
| `POST /preview/update` | Simulate live page changes |
| `POST /preview/outcome` | Simulate an action result in preview |
| `POST /preview/close` | Leave preview and attempt login restoration |

Stale views, disabled controls and invalid inputs are rejected. Draft conflicts return 409 unless an operation explicitly accepts `discardDraft:true`. An uncertain action must be checked before clearing it. Requests are not automatically retried. The API exposes declared controls and allowed navigation, with no arbitrary plugin invocation or file reading.

After a Windows Debug build, run `.ci/Test-MobileDebug.ps1`. It creates a temporary credential, starts its own isolated preview process, checks native bindings and draft retention, then stops that process and restores the environment. It does not use saved account credentials. Interactive file import uses `tests/Fixtures/mobile-page-v1.json` and is restricted to Debug desktop targets.
