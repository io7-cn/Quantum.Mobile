namespace Quantum.MobileApp.Storage;

internal static class MobileStorage
{
    public static OutgoingAttachmentStore Attachments { get; } = new(Path.Combine(FileSystem.AppDataDirectory, "OutgoingAttachments"));
}
