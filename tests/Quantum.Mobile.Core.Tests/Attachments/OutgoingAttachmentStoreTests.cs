using Quantum.Mobile.Core.Attachments;
using Quantum.Mobile.Internal;
using System.Security.Cryptography;

namespace Quantum.Mobile.Core.Tests.Attachments;

public sealed class OutgoingAttachmentStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "quantum-attachments-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SavesValidatedContentWithItsDigest()
    {
        var store = new OutgoingAttachmentStore(_directory);
        var bytes = "Attachment content"u8.ToArray();
        await using var input = new MemoryStream(bytes);
        var file = await store.SaveAsync(input, "example.txt", "text/plain");
        Assert.Equal(bytes.Length, file.Size);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), file.Sha256);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(store.PathFor(file)));
        Assert.Single(Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task OversizedInputRemovesTheIncompleteFile()
    {
        var store = new OutgoingAttachmentStore(_directory);
        await using var input = new MemoryStream(new byte[MobileAttachments.MaximumFileBytes + 1]);
        await Assert.ThrowsAsync<BridgeException>(() => store.SaveAsync(input, "example.bin", "application/octet-stream"));
        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task CancellationRemovesTheIncompleteFile()
    {
        var store = new OutgoingAttachmentStore(_directory);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await using var input = new MemoryStream("example"u8.ToArray());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(input, "example.txt", "text/plain", cancellation.Token));
        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task EmptyInputIsNotPublished()
    {
        var store = new OutgoingAttachmentStore(_directory);
        await using var input = new MemoryStream();
        await Assert.ThrowsAsync<BridgeException>(() => store.SaveAsync(input, "example.txt", "text/plain"));
        Assert.Empty(Directory.GetFiles(_directory));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
