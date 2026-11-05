using Quantum.Mobile.Internal;
using System.Security.Cryptography;

namespace Quantum.Mobile.Core.Attachments;

public sealed class OutgoingAttachmentStore(string directory)
{
    private readonly string _directory = Path.GetFullPath(directory);
    public string PathFor(BridgeAttachment file)
    {
        MobileAttachments.Validate(file);
        return Path.Combine(_directory, file.Id + ".bin");
    }

    public async Task<BridgeAttachment> SaveAsync(Stream source, string name, string contentType, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_directory);
        foreach (var path in Directory.EnumerateFiles(_directory, "*.bin"))
        {
            if (File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddHours(-24))
            {
                File.Delete(path);
            }
        }

        var id = Guid.NewGuid().ToString("N");
        var partial = Path.Combine(_directory, id + ".partial");
        try
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long total = 0;
            await using (var output = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920];
                int count;
                while ((count = await source.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    total += count;
                    if (total > MobileAttachments.MaximumFileBytes)
                    {
                        throw new BridgeException("mobile_attachment_invalid", "Attachments must be 8 MiB or smaller.");
                    }

                    hash.AppendData(buffer, 0, count);
                    await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                }
            }

            var file = new BridgeAttachment(id, name, contentType, total, Convert.ToHexStringLower(hash.GetHashAndReset()));
            MobileAttachments.Validate(file);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(partial, PathFor(file));
            return file;
        }
        finally
        {
            if (File.Exists(partial))
            {
                File.Delete(partial);
            }
        }
    }
}
