namespace Core.Domain.Requests;

/// <summary>
/// Represents a unique identifier for a request.
/// </summary>
public record RequestId
{
    public string Value { get; }

    private RequestId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new RequestId with a random GUID.
    /// </summary>
    public static RequestId Create() => new(Guid.NewGuid().ToString());

    /// <summary>
    /// Creates a RequestId from an existing string value.
    /// </summary>
    public static RequestId From(string value) => new(value);

    public override string ToString() => Value;

    public override int GetHashCode() => Value.GetHashCode();
}
