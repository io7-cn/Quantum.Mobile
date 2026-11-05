using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Quantum.Mobile.Internal;

public static partial class MobileUiValidator
{
    public const int MaximumPageBytes = 128 * 1024;
    private static readonly HashSet<string> Types = ["VStack", "HStack", "Scroll", "Text", "TextField", "Attachments", "Toggle", "Button", "Divider", "Spacer", "Progress"];
    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_.-]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdPattern();
    public static void Id(string value)
    {
        if (value is null || !IdPattern().IsMatch(value))
        {
            throw new ArgumentException("Invalid mobile identifier.");
        }
    }

    public static IReadOnlyDictionary<string, MobileNode> Validate(MobilePage page, bool wire = false)
    {
        ArgumentNullException.ThrowIfNull(page);
        Id(page.Id);
        if (page.SchemaVersion is not (1 or 2) || string.IsNullOrWhiteSpace(page.Title) || page.Title.Length > 200)
        {
            throw new ArgumentException("Unsupported mobile page or title.");
        }

        var nodes = new Dictionary<string, MobileNode>(StringComparer.Ordinal);
        visit(page.Root, 1);
        foreach (var node in nodes.Values)
        {
            if (wire && node.Action is not null)
            {
                throw new ArgumentException("RPC definitions cannot cross the mobile wire.");
            }

            if (!wire && node.ActionId is not null)
            {
                throw new ArgumentException("Action IDs are assigned by the host.");
            }

            if (node.InputIds is null || !wire && node.InputIds.Length != 0 || node.InputIds.Any(id => !nodes.TryGetValue(id, out var input) || input.Type is not ("TextField" or "Attachments" or "Toggle")))
            {
                throw new ArgumentException("Invalid wire input IDs.");
            }

            if (node.Type != "Button" && (node.Action is not null || node.ActionId is not null))
            {
                throw new ArgumentException("Only buttons can declare actions.");
            }

            if (node.Action is not { } action)
            {
                continue;
            }

            if (action.CapabilityName is not { Length: <= 200 } name || name.StartsWith("quantum.", StringComparison.Ordinal) || !Regex.IsMatch(name, @"\A[a-z0-9_-]+(?:\.[a-z0-9_-]+)+\z", RegexOptions.CultureInvariant))
            {
                throw new ArgumentException("Expected a plugin capability name.");
            }

            if (action.Constants is null || action.Inputs is null || action.Constants.Count + action.Inputs.Count > 32)
            {
                throw new ArgumentException("Invalid action fields.");
            }

            foreach (var field in action.Constants.Keys.Concat(action.Inputs.Keys))
            {
                Id(field);
            }

            foreach (var (field, source) in action.Inputs)
            {
                if (action.Constants.ContainsKey(field) || !nodes.TryGetValue(source, out var input) || input.Type is not ("TextField" or "Attachments" or "Toggle"))
                {
                    throw new ArgumentException("Invalid or overlapping input mapping.");
                }
            }
        }

        if (JsonSerializer.SerializeToUtf8Bytes(page, MobileJson.Options).Length > MaximumPageBytes)
        {
            throw new ArgumentException("Mobile page exceeds 128 KiB.");
        }

        return nodes;
        void visit(MobileNode node, int depth)
        {
            if (node is null || depth > 16 || nodes.Count >= 500)
            {
                throw new ArgumentException("Mobile tree limit exceeded.");
            }

            Id(node.Id);
            if (node.Type == "Attachments" && page.SchemaVersion < 2)
            {
                throw new ArgumentException("Attachments require mobile schema 2.");
            }

            if (!Types.Contains(node.Type) || !nodes.TryAdd(node.Id, node) || node.Children is null || node.Text?.Length > 2000 || node.Placeholder?.Length > 2000 || node.MaxLength is < 1 or > 2000 || node.Style is not ("body" or "title" or "caption" or "error"))
            {
                throw new ArgumentException("Invalid mobile node.");
            }

            if (node.Type == "Scroll" && node.Children.Length != 1 || node.Type is not ("VStack" or "HStack" or "Scroll") && node.Children.Length != 0)
            {
                throw new ArgumentException("Invalid mobile children.");
            }

            foreach (var child in node.Children)
            {
                visit(child, depth + 1);
            }
        }
    }
}
