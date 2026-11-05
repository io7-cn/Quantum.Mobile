using Quantum.Mobile.Core.Updates;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Quantum.Mobile.Core.Tests.Updates;

public sealed class MobileAppUpdateTests
{
    [Fact]
    public async Task FindsAndroidPreviewAndVerifiesDownloadedBytes()
    {
        using var fixture = new ReleaseFixture();
        var updates = fixture.Updates();
        await updates.CheckAsync();
        Assert.Equal(MobileUpdateState.Available, updates.State);
        Assert.Equal("0.1.13", updates.Target!.Version);
        await updates.DownloadAsync(fixture.Directory);
        Assert.Equal(MobileUpdateState.Ready, updates.State);
        Assert.Equal(100, updates.Progress);
        Assert.Equal(fixture.Package, await File.ReadAllBytesAsync(updates.DownloadPath!));
    }

    [Theory]
    [InlineData("0.1.12", 101, MobileUpdateState.Available)]
    [InlineData("0.1.12", 100, MobileUpdateState.Current)]
    [InlineData("0.1.12", 99, MobileUpdateState.Current)]
    [InlineData("0.1.11", 200, MobileUpdateState.Current)]
    [InlineData("0.1.13", 99, MobileUpdateState.Current)]
    public async Task UsesBuildFeedWithoutOfferingAndroidDowngrades(string version, int build, MobileUpdateState expected)
    {
        using var fixture = new ReleaseFixture(version, build);
        var updates = fixture.Updates();
        await updates.CheckAsync();
        Assert.Equal(expected, updates.State);
    }

    [Fact]
    public async Task FourPartInstalledVersionStillAcceptsNewerBuildOfSameVersion()
    {
        using var fixture = new ReleaseFixture("0.1.12", 101);
        var updates = new MobileAppUpdateService(fixture.Http, "0.1.12.0", 100, "android");
        await updates.CheckAsync();
        Assert.Equal(MobileUpdateState.Available, updates.State);
    }

    [Fact]
    public async Task LegacyReleaseDoesNotReinstallSameVersion()
    {
        using var fixture = new ReleaseFixture("0.1.12", feed: false);
        var updates = fixture.Updates();
        await updates.CheckAsync();
        Assert.Equal(MobileUpdateState.Current, updates.State);
    }

    [Fact]
    public async Task CorruptedPackageIsNeverExposedToInstaller()
    {
        using var fixture = new ReleaseFixture();
        var updates = fixture.Updates();
        await updates.CheckAsync();
        fixture.Package = Encoding.UTF8.GetBytes("BAD package"); // Same length; digest still must fail.
        await updates.DownloadAsync(fixture.Directory);
        Assert.Equal(MobileUpdateState.Failed, updates.State);
        Assert.Null(updates.DownloadPath);
        Assert.Empty(System.IO.Directory.GetFiles(fixture.Directory));
    }

    [Fact]
    public async Task TamperedFeedAndForeignDownloadHostsAreRejected()
    {
        using var fixture = new ReleaseFixture();
        fixture.CorruptFeed = true;
        var updates = fixture.Updates();
        await updates.CheckAsync();
        Assert.Equal(MobileUpdateState.Failed, updates.State);
        fixture.CorruptFeed = false;
        fixture.DownloadHost = "https://attacker.invalid/package.apk";
        await updates.CheckAsync();
        Assert.Equal(MobileUpdateState.Failed, updates.State);
        Assert.Null(updates.Target);
    }

    [Fact]
    public async Task DraftReleasesAreNotOffered()
    {
        using var fixture = new ReleaseFixture
        {
            Draft = true
        };
        var updates = fixture.Updates();
        await updates.CheckAsync();
        Assert.Equal(MobileUpdateState.Current, updates.State);
    }

    [Fact]
    public async Task UnsupportedPlatformDoesNotMakeNetworkRequests()
    {
        using var fixture = new ReleaseFixture();
        var updates = new MobileAppUpdateService(fixture.Http, "0.1.12", 100, null);
        await updates.CheckAsync();
        Assert.Equal(MobileUpdateState.Unavailable, updates.State);
        Assert.Equal(0, fixture.Requests);
    }

    private sealed class ReleaseFixture : HttpMessageHandler
    {
        public readonly string Directory = Path.Combine(Path.GetTempPath(), "quantum-update-tests-" + Guid.NewGuid().ToString("N"));
        public readonly HttpClient Http;
        public byte[] Package = Encoding.UTF8.GetBytes("APK package");
        public string DownloadHost = "";
        public bool CorruptFeed;
        public bool Draft;
        public int Requests;
        private readonly string _version;
        private readonly bool _feed;
        private readonly string _digest;
        private readonly byte[] _metadata;
        private string PackageUrl => $"https://github.com/XmmShp/Quantum/releases/download/mobile-v{_version}-preview.1/Quantum-Mobile-{_version}-android.apk";
        private string FeedUrl => $"https://github.com/XmmShp/Quantum/releases/download/mobile-v{_version}-preview.1/update.android.json";

        public ReleaseFixture(string version = "0.1.13", int build = 101, bool feed = true)
        {
            _version = version;
            _feed = feed;
            _digest = Convert.ToHexStringLower(SHA256.HashData(Package));
            _metadata = JsonSerializer.SerializeToUtf8Bytes(new { schemaVersion = 1, version, buildNumber = build, channel = "android", fileName = $"Quantum-Mobile-{version}-android.apk", sha256 = _digest });
            Http = new HttpClient(this, false);
        }

        public MobileAppUpdateService Updates() => new(Http, "0.1.12", 100, "android");
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            byte[] bytes;
            if (request.RequestUri!.Host == "api.github.com")
            {
                var assets = new List<object>
                {
                    new
                    {
                        name = $"Quantum-Mobile-{_version}-android.apk",
                        browser_download_url = DownloadHost.Length == 0 ? PackageUrl : DownloadHost,
                        size = 11,
                        digest = "sha256:" + _digest
                    }
                };
                if (_feed)
                {
                    assets.Add(new { name = "update.android.json", browser_download_url = FeedUrl, size = _metadata.Length, digest = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(_metadata)) });
                }

                bytes = JsonSerializer.SerializeToUtf8Bytes(new[] { new { tag_name = $"mobile-v{_version}-preview.1", draft = Draft, published_at = "2026-11-02T00:00:00Z", assets } });
            }
            else if (request.RequestUri.AbsoluteUri == FeedUrl)
            {
                bytes = CorruptFeed ? [1, 2, 3] : _metadata;
            }
            else if (request.RequestUri.AbsoluteUri == PackageUrl)
            {
                bytes = Package;
            }
            else
            {
                throw new InvalidOperationException("Unexpected update endpoint.");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
        }

        protected override void Dispose(bool disposing)
        {
            Http.Dispose();
            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, true);
            }

            base.Dispose(disposing);
        }
    }
}
