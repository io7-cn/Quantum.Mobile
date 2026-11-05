using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Quantum.Mobile.Core.Updates;

public enum MobileUpdateState
{
    Unavailable,
    Idle,
    Checking,
    Current,
    Available,
    Downloading,
    Ready,
    Failed
}
