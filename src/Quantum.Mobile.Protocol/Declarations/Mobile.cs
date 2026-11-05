using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Quantum.Mobile.Internal;
/// <summary>Executed only in the desktop plugin, never interpreted by the mobile client.</summary>
public static class Mobile
{
    public static MobilePage Page(string id, string title, MobileNode root) => new(id, title, root);
    public static MobileNode VStack(string id, params MobileNode[] children) => new("VStack", id)
    {
        Children = children
    };
    public static MobileNode HStack(string id, params MobileNode[] children) => new("HStack", id)
    {
        Children = children
    };
    public static MobileNode Scroll(string id, MobileNode child) => new("Scroll", id)
    {
        Children = [child]
    };
    public static MobileNode Text(string id, string text) => new("Text", id)
    {
        Text = text
    };
    public static MobileNode TextField(string id) => new("TextField", id);
    public static MobileNode Toggle(string id, string text) => new("Toggle", id)
    {
        Text = text
    };
    public static MobileNode Button(string id, string text) => new("Button", id)
    {
        Text = text
    };
    public static MobileNode Divider(string id) => new("Divider", id);
    public static MobileNode Spacer(string id) => new("Spacer", id);
    public static MobileNode Progress(string id) => new("Progress", id);
    public static MobileRpcAction Rpc(string capabilityName) => new(capabilityName);
}
