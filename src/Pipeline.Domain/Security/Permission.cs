using System.Text.RegularExpressions;

namespace Pipeline.Domain.Security;

public readonly partial record struct Permission
{
    [GeneratedRegex(@"^[a-z]+(\.[a-z]+){1,2}$", RegexOptions.Compiled)]
    private static partial Regex FormatRegex();

    public string Value { get; }

    public Permission(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var trimmed = value.Trim();
        if (!FormatRegex().IsMatch(trimmed))
        {
            throw new ArgumentException(
                $"Permission '{value}' is invalid. Format must be lowercase matching ^[a-z]+(\\.[a-z]+){{1,2}}$, e.g. 'jobs.read' or 'sheets.values.read'.",
                nameof(value));
        }

        Value = trimmed;
    }

    public override string ToString() => Value;

    public static implicit operator string(Permission permission) => permission.Value;
    public static explicit operator Permission(string value) => new(value);
}
