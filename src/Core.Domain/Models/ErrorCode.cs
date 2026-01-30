namespace Core.Domain.Models;

/// <summary>
/// Represents an error code value object.
/// </summary>
public record ErrorCode
{
    public string Value { get; }

    public ErrorCode(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("Error code cannot be null or empty.", nameof(value));
        }

        Value = value;
    }
}
