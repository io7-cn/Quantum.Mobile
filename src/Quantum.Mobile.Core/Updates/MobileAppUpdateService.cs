using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Quantum.Mobile.Core.Updates;
/// <summary>Reads only the official client releases; installation remains an explicit OS operation.</summary>
public sealed class MobileAppUpdateService(HttpClient http, string currentVersion, long currentBuild, string? channel)
{
    private const string Repository = "https://api.github.com/repos/io7-cn/Quantum.Mobile/releases?per_page=50";
    private const long MaximumPackageBytes = 512L * 1024 * 1024;
    private readonly SemaphoreSlim _gate = new(1, 1);
    public event Action? Changed;
    public MobileUpdateState State { get; private set; } = channel is null ? MobileUpdateState.Unavailable : MobileUpdateState.Idle;
    public MobileUpdateRelease? Target { get; private set; }
    public string? DownloadPath { get; private set; }
    public int Progress { get; private set; }
    public string CurrentVersion => currentVersion;
    public bool IsBusy => State is MobileUpdateState.Checking or MobileUpdateState.Downloading;

    public async Task CheckAsync(CancellationToken cancellationToken = default)
    {
        if (channel is null || !await _gate.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            if (State == MobileUpdateState.Ready && File.Exists(DownloadPath))
            {
                return;
            }

            SetState(MobileUpdateState.Checking);
            Target = null;
            using var request = new HttpRequestMessage(HttpMethod.Get, Repository);
            request.Headers.UserAgent.ParseAdd("Quantum-Mobile/" + currentVersion);
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var releases = JsonDocument.Parse(await ReadLimitedAsync(response.Content, 2 * 1024 * 1024, cancellationToken));
            var installedVersion = Version.Parse(currentVersion);
            var installed = new Version(installedVersion.Major, installedVersion.Minor, Math.Max(0, installedVersion.Build));
            foreach (var release in releases.RootElement.EnumerateArray().Where(r => !r.GetProperty("draft").GetBoolean() && ReleaseVersion(r) is { } v && v >= installed).OrderByDescending(ReleaseVersion).ThenByDescending(r => r.GetProperty("published_at").GetString()))
            {
                var version = ReleaseVersion(release)!;
                var fileName = $"Quantum-Mobile-{version.ToString(3)}-{channel}" + (channel == "android" ? ".apk" : ".zip");
                var assets = release.GetProperty("assets").EnumerateArray().ToArray();
                var asset = assets.FirstOrDefault(a => a.GetProperty("name").GetString() == fileName);
                if (asset.ValueKind == JsonValueKind.Undefined)
                {
                    continue;
                }

                var sha = AssetDigest(asset);
                var uri = AssetUri(asset);
                var size = asset.GetProperty("size").GetInt64();
                if (size is <= 0 or > MaximumPackageBytes)
                {
                    throw new InvalidDataException("Invalid release package size.");
                }

                long build = 0;
                var feedAsset = assets.FirstOrDefault(a => a.GetProperty("name").GetString() == $"update.{channel}.json");
                if (feedAsset.ValueKind != JsonValueKind.Undefined)
                {
                    using var feedResponse = await http.GetAsync(AssetUri(feedAsset), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    feedResponse.EnsureSuccessStatusCode();
                    var data = await ReadLimitedAsync(feedResponse.Content, 16 * 1024, cancellationToken);
                    if (Convert.ToHexStringLower(SHA256.HashData(data)) != AssetDigest(feedAsset))
                    {
                        throw new InvalidDataException("Update metadata checksum mismatch.");
                    }

                    using var feed = JsonDocument.Parse(data);
                    var manifest = feed.RootElement;
                    if (manifest.GetProperty("schemaVersion").GetInt32() != 1 || manifest.GetProperty("version").GetString() != version.ToString(3) || manifest.GetProperty("channel").GetString() != channel || manifest.GetProperty("fileName").GetString() != fileName || manifest.GetProperty("sha256").GetString() != sha)
                    {
                        throw new InvalidDataException("Update metadata does not match its release.");
                    }

                    build = manifest.GetProperty("buildNumber").GetInt64();
                    if (build <= 0)
                    {
                        throw new InvalidDataException("Invalid update build number.");
                    }
                }

                // Older releases have no build feed; only offer them for a newer semantic version.
                if (version == installed && build <= currentBuild)
                {
                    continue;
                }

                if (channel == "android" && build > 0 && build <= currentBuild)
                {
                    continue;
                }

                Target = new(version.ToString(3), build, uri, fileName, sha, size);
                break;
            }

            SetState(Target is null ? MobileUpdateState.Current : MobileUpdateState.Available);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            SetState(MobileUpdateState.Idle);
            throw;
        }
        catch (Exception)
        {
            SetState(MobileUpdateState.Failed);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DownloadAsync(string directory, CancellationToken cancellationToken = default)
    {
        if (Target is null || !await _gate.WaitAsync(0, cancellationToken))
        {
            return;
        }

        string? partial = null;
        try
        {
            var target = Target;
            if (State == MobileUpdateState.Ready && File.Exists(DownloadPath))
            {
                return;
            }

            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, target.FileName);
            partial = path + ".partial";
            Progress = 0;
            DownloadPath = null;
            SetState(MobileUpdateState.Downloading);
            using var response = await http.GetAsync(target.DownloadUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is { } length && length != target.Size)
            {
                throw new InvalidDataException("Update package size mismatch.");
            }

            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long total = 0;
            await using (var output = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true))
            {
                var buffer = new byte[65536];
                int count;
                while ((count = await input.ReadAsync(buffer, cancellationToken)) != 0)
                {
                    total += count;
                    if (total > target.Size)
                    {
                        throw new InvalidDataException("Update package exceeds its declared size.");
                    }

                    hash.AppendData(buffer, 0, count);
                    await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                    var progress = (int)(total * 100 / target.Size);
                    if (Progress != progress)
                    {
                        Progress = progress;
                        Changed?.Invoke();
                    }
                }
            }

            if (total != target.Size || !CryptographicOperations.FixedTimeEquals(hash.GetHashAndReset(), Convert.FromHexString(target.Sha256)))
            {
                throw new InvalidDataException("Update package checksum mismatch.");
            }

            File.Move(partial, path, true);
            DownloadPath = path;
            SetState(MobileUpdateState.Ready);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            SetState(MobileUpdateState.Available);
            throw;
        }
        catch (Exception)
        {
            SetState(MobileUpdateState.Failed);
        }
        finally
        {
            try
            {
                if (partial is not null && File.Exists(partial))
                {
                    File.Delete(partial);
                }
            }
            finally
            {
                _gate.Release();
            }
        }
    }

    private void SetState(MobileUpdateState state)
    {
        State = state;
        Changed?.Invoke();
    }

    private static Version? ReleaseVersion(JsonElement release)
    {
        var tag = release.GetProperty("tag_name").GetString() ?? "";
        var match = Regex.Match(tag, @"^mobile-v((?:0|[1-9]\d*)\.(?:0|[1-9]\d*)\.(?:0|[1-9]\d*))(?:-[0-9A-Za-z.-]+)?$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        return match.Success && Version.TryParse(match.Groups[1].Value, out var version) ? version : null;
    }

    private static Uri AssetUri(JsonElement asset)
    {
        var uri = new Uri(asset.GetProperty("browser_download_url").GetString()!, UriKind.Absolute);
        if (uri.Scheme != "https" || uri.Host != "github.com" || !uri.IsDefaultPort || uri.UserInfo.Length != 0 || !uri.AbsolutePath.StartsWith("/io7-cn/Quantum.Mobile/releases/download/", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Untrusted update source.");
        }

        return uri;
    }

    private static string AssetDigest(JsonElement asset)
    {
        var digest = asset.GetProperty("digest").GetString() ?? "";
        if (!Regex.IsMatch(digest, "^sha256:[0-9a-f]{64}$", RegexOptions.CultureInvariant))
        {
            throw new InvalidDataException("Missing release checksum.");
        }

        return digest[7..];
    }

    private static async Task<byte[]> ReadLimitedAsync(HttpContent content, int maximum, CancellationToken cancellationToken)
    {
        await content.LoadIntoBufferAsync(maximum, cancellationToken);
        return await content.ReadAsByteArrayAsync(cancellationToken);
    }
}
