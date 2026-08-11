using System.Text.Json.Serialization;

namespace GrouperLib.Core;

public sealed class GrouperDocumentRule
{
    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("value")]
    [JsonConverter(typeof(StringOrBooleanConverter))]
    public string Value { get; }

    [JsonConstructor]
    public GrouperDocumentRule(string name, string value)
    {
        Name = name ?? string.Empty;
        Value = value ?? string.Empty;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not GrouperDocumentRule rule)
        {
            return false;
        }
        return Name.Equals(rule.Name) && Value.IEquals(rule.Value);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Name);
        hash.Add(Value, StringComparer.OrdinalIgnoreCase);
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        return $"{Name} => {Value}";
    }
}