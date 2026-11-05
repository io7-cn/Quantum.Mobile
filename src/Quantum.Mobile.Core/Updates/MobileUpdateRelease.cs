using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Quantum.Mobile.Core.Updates;

public sealed record MobileUpdateRelease(string Version, long BuildNumber, Uri DownloadUri, string FileName, string Sha256, long Size);
