param(
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
    [Parameter(Mandatory)][ValidateSet('android', 'win-x64')][string]$Target,
    [ValidateRange(1, 2147483647)][int]$BuildNumber = 1
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$project = 'src/Quantum.Mobile/Quantum.Mobile.csproj'
$publish = "artifacts/mobile-publish/$Target"
$delivery = "artifacts/mobile-delivery/$Target"
$releaseBuild = @('-p:ContinuousIntegrationBuild=true', '-p:DebugType=none', '-p:DebugSymbols=false', '-p:CopyOutputSymbolsToPublishDirectory=false')
New-Item -ItemType Directory -Force $delivery | Out-Null
if ($Target -eq 'android') {
    if (!$env:MOBILE_ANDROID_KEYSTORE_PATH -or !$env:MOBILE_ANDROID_KEYSTORE_PASSWORD) { throw 'Persistent Android signing credentials are required.' }
    # Android emits a universal APK from its Android RuntimeIdentifiers; the build
    # machine's RID must not be added by the SDK's default publish RID inference.
    dotnet clean $project @releaseBuild -v quiet -c Release -f net10.0-android -p:QuantumMobileTargetFramework=net10.0-android
    dotnet publish $project @releaseBuild -c Release -f net10.0-android -p:QuantumMobileTargetFramework=net10.0-android `
        -p:UseDefaultPublishRuntimeIdentifier=false -o $publish `
        -p:ApplicationDisplayVersion=$Version -p:ApplicationVersion=$BuildNumber `
        -p:AndroidPackageFormats=apk -p:AndroidKeyStore=true `
        -p:AndroidSigningKeyStore=$env:MOBILE_ANDROID_KEYSTORE_PATH -p:AndroidSigningKeyAlias=quantum-mobile `
        -p:AndroidSigningStorePass=env:MOBILE_ANDROID_KEYSTORE_PASSWORD -p:AndroidSigningKeyPass=env:MOBILE_ANDROID_KEYSTORE_PASSWORD
    $apk = @(Get-ChildItem $publish -Filter '*-Signed.apk' -File)
    if ($apk.Count -ne 1) { throw 'Expected one signed universal Android APK.' }
    $sdk = if ($env:ANDROID_HOME) { $env:ANDROID_HOME } else { $env:ANDROID_SDK_ROOT }
    $signerName = if ($IsWindows) { 'apksigner.bat' } else { 'apksigner' }
    $signer = Get-ChildItem "$sdk/build-tools/*/$signerName" -File | Sort-Object FullName -Descending | Select-Object -First 1
    if (!$signer) { throw 'Android apksigner is required to verify the release.' }
    $verification = & $signer.FullName verify --verbose --print-certs $apk[0].FullName
    $verification | Write-Output
    $expectedCertificate = (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'android-signing-certificate.sha256') -Raw).Trim()
    $certificateDigests = @($verification | Select-String '^Signer #[0-9]+ certificate SHA-256 digest: ([0-9a-fA-F]{64})$' | ForEach-Object { $_.Matches[0].Groups[1].Value })
    if ($certificateDigests.Count -ne 1 -or $certificateDigests[0] -ne $expectedCertificate) {
        throw 'The APK must use the recorded persistent Android signing certificate.'
    }
    Copy-Item -LiteralPath $apk[0].FullName -Destination "$delivery/Quantum-Mobile-$Version-android.apk"
} else {
    dotnet clean $project @releaseBuild -v quiet -c Release -f net10.0-windows10.0.19041.0 -p:QuantumMobileTargetFramework=net10.0-windows10.0.19041.0 -r win-x64
    dotnet publish $project @releaseBuild -c Release -f net10.0-windows10.0.19041.0 -p:QuantumMobileTargetFramework=net10.0-windows10.0.19041.0 -r win-x64 --self-contained true `
        -p:ApplicationDisplayVersion=$Version -p:ApplicationVersion=$BuildNumber `
        -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -o $publish
    if (Test-Path -LiteralPath (Join-Path $publish 'Quantum.Mobile.Debugging.dll')) {
        throw 'The Debug transport assembly must not be shipped in Release.'
    }
    foreach ($assemblyName in @('Quantum.Mobile.dll', 'Quantum.Mobile.Core.dll', 'Quantum.Mobile.Protocol.dll')) {
        $stream = [IO.File]::OpenRead((Join-Path $publish $assemblyName))
        $reader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
        try {
            foreach ($entry in $reader.ReadDebugDirectory()) {
                if ($entry.Type -eq [System.Reflection.PortableExecutable.DebugDirectoryEntryType]::CodeView) {
                    $debugPath = $reader.ReadCodeViewDebugDirectoryData($entry).Path
                    if ($debugPath -match '^(?:[A-Za-z]:[\\/]|[\\/])') {
                        throw 'Release assemblies must not contain absolute debug paths.'
                    }
                }
            }
        }
        finally {
            $reader.Dispose()
            $stream.Dispose()
        }
    }
    $metadata = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes((Resolve-Path "$publish/Quantum.Mobile.dll")))
    foreach ($debugSymbol in @('PreviewFileAsync', 'RenderPreview', 'MobileDebugServer', 'StartDebugInterface', 'DebugSetInput', 'QUANTUM_MOBILE_DEBUG_PORT')) {
        if ($metadata.Contains($debugSymbol)) { throw 'Debug tools must not be compiled into Release.' }
    }
    Copy-Item -LiteralPath @('LICENSE', 'NOTICE') -Destination $publish
    Compress-Archive -Path "$publish/*" -DestinationPath "$delivery/Quantum-Mobile-$Version-win-x64.zip" -Force
}
$packageName = "Quantum-Mobile-$Version-$Target" + $(if ($Target -eq 'android') { '.apk' } else { '.zip' })
@{
    schemaVersion = 1; version = $Version; buildNumber = $BuildNumber; channel = $Target
    fileName = $packageName
    sha256 = (Get-FileHash -LiteralPath (Join-Path $delivery $packageName) -Algorithm SHA256).Hash.ToLowerInvariant()
} | ConvertTo-Json | Set-Content -LiteralPath "$delivery/update.$Target.json" -Encoding utf8NoBOM
Get-ChildItem $delivery -File | Where-Object Name -NotLike 'SHA256SUMS.*' | ForEach-Object {
    $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
    "$($hash.Hash.ToLowerInvariant())  $($_.Name)"
} | Set-Content "$delivery/SHA256SUMS.$Target.txt"
