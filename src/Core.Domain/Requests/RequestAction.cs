namespace Core.Domain.Requests;

/// <summary>
/// Represents a hierarchical, dot-separated request action identifier.
/// Actions compose with the / operator (e.g. <c>McpServer / "instance" / "invoke-tool"</c>).
/// </summary>
public readonly struct RequestAction : IEquatable<RequestAction>, IComparable<RequestAction>
{
    /// <summary>
    /// The separator used between segments in the action path.
    /// </summary>
    public const char Separator = '.';

    private readonly string _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestAction"/> struct.
    /// </summary>
    /// <param name="value">The action value. Must not be null or whitespace.</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is empty or whitespace.</exception>
    public RequestAction(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Request action value cannot be empty or whitespace.", nameof(value));
        }

        _value = value;
    }

    /// <summary>
    /// Gets the string value of the request action.
    /// </summary>
    public string Value => _value ?? string.Empty;

    /// <summary>
    /// Gets the number of segments in the action path.
    /// </summary>
    public int Depth
    {
        get
        {
            if (string.IsNullOrEmpty(_value))
            {
                return 0;
            }

            var count = 1;
            for (var i = 0; i < _value.Length; i++)
            {
                if (_value[i] == Separator)
                {
                    count++;
                }
            }
            return count;
        }
    }

    /// <summary>
    /// Gets the last segment of the action path.
    /// </summary>
    public string Leaf
    {
        get
        {
            if (string.IsNullOrEmpty(_value))
            {
                return string.Empty;
            }

            var lastSeparatorIndex = _value.LastIndexOf(Separator);
            return lastSeparatorIndex < 0 ? _value : _value.Substring(lastSeparatorIndex + 1);
        }
    }

    /// <summary>
    /// Gets the parent action, or null if this is a root-level action.
    /// </summary>
    public RequestAction? Parent
    {
        get
        {
            if (string.IsNullOrEmpty(_value))
            {
                return null;
            }

            var lastSeparatorIndex = _value.LastIndexOf(Separator);
            if (lastSeparatorIndex < 0)
            {
                return null;
            }

            return new RequestAction(_value.Substring(0, lastSeparatorIndex));
        }
    }

    /// <summary>
    /// Determines whether this action is a child of (or equal to) the specified ancestor action.
    /// </summary>
    /// <param name="ancestor">The potential ancestor action.</param>
    /// <returns>true if this action equals the ancestor or starts with the ancestor followed by the separator; otherwise, false.</returns>
    public bool IsChildOf(RequestAction ancestor)
    {
        if (string.IsNullOrEmpty(_value) || string.IsNullOrEmpty(ancestor._value))
        {
            return false;
        }

        if (_value.Equals(ancestor._value, StringComparison.Ordinal))
        {
            return true;
        }

        return _value.Length > ancestor._value.Length
            && _value[ancestor._value.Length] == Separator
            && _value.StartsWith(ancestor._value, StringComparison.Ordinal);
    }

    /// <summary>
    /// Combines this action with a child segment using the / operator.
    /// </summary>
    /// <param name="parent">The parent action.</param>
    /// <param name="child">The child segment to append.</param>
    /// <returns>A new action representing the combined path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when child is null.</exception>
    /// <exception cref="ArgumentException">Thrown when child is empty, whitespace, or contains the separator character.</exception>
    public static RequestAction operator /(RequestAction parent, string child)
    {
        if (child == null)
        {
            throw new ArgumentNullException(nameof(child));
        }

        if (string.IsNullOrWhiteSpace(child))
        {
            throw new ArgumentException("Child segment cannot be empty or whitespace.", nameof(child));
        }

        if (child.IndexOf(Separator) >= 0)
        {
            throw new ArgumentException($"Child segment cannot contain the separator character '{Separator}'. Use chained / operators to add multiple segments.", nameof(child));
        }

        if (string.IsNullOrEmpty(parent._value))
        {
            return new RequestAction(child);
        }

        return new RequestAction(parent._value + Separator + child);
    }

    /// <summary>
    /// Implicitly converts a request action to its string representation.
    /// </summary>
    /// <param name="action">The action to convert.</param>
    public static implicit operator string(RequestAction action) => action.Value;

    /// <summary>
    /// Returns the string representation of this action.
    /// </summary>
    public override string ToString() => Value;

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    public override int GetHashCode()
    {
        return _value?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// Determines whether this instance equals another RequestAction instance.
    /// </summary>
    public bool Equals(RequestAction other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether this instance equals another object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is RequestAction other && Equals(other);
    }

    /// <summary>
    /// Compares this instance to another RequestAction.
    /// </summary>
    public int CompareTo(RequestAction other)
    {
        return string.Compare(_value ?? string.Empty, other._value ?? string.Empty, StringComparison.Ordinal);
    }

    public static bool operator ==(RequestAction left, RequestAction right) => left.Equals(right);
    public static bool operator !=(RequestAction left, RequestAction right) => !left.Equals(right);
    public static bool operator <(RequestAction left, RequestAction right) => left.CompareTo(right) < 0;
    public static bool operator <=(RequestAction left, RequestAction right) => left.CompareTo(right) <= 0;
    public static bool operator >(RequestAction left, RequestAction right) => left.CompareTo(right) > 0;
    public static bool operator >=(RequestAction left, RequestAction right) => left.CompareTo(right) >= 0;
}
