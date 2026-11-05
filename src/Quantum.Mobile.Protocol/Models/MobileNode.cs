using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Quantum.Mobile.Internal;

public sealed record MobileNode(string Type, string Id)
{
    public string? Text { get; init; }
    public string? Placeholder { get; init; }
    public string Style { get; init; } = "body";
    public int MaxLength { get; init; } = 500;
    public bool Enabled { get; init; } = true;
    public string? ActionId { get; init; }
    public string[] InputIds { get; init; } = [];
    public MobileRpcAction? Action { get; init; }
    public MobileNode[] Children { get; init; } = [];

    public MobileNode Label(string text) => this with
    {
        Text = text
    };
    public MobileNode Hint(string text) => this with
    {
        Placeholder = text
    };
    public MobileNode Limit(int length) => this with
    {
        MaxLength = length
    };
    public MobileNode Appearance(string style) => this with
    {
        Style = style
    };
    public MobileNode OnClick(MobileRpcAction action) => this with
    {
        Action = action
    };
}
