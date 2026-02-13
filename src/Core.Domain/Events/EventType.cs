namespace Core.Domain.Events;

/// <summary>
/// Represents a hierarchical, dot-separated event type identifier.
/// Event types compose with the / operator (e.g. <c>McpServer / "instance" / "started"</c>).
/// </summary>
public readonly struct EventType : IEquatable<EventType>, IComparable<EventType>
{
    /// <summary>
    /// The separator used between segments in the event type path.
    /// </summary>
    public const char Separator = '.';

    private readonly string _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventType"/> struct.
    /// </summary>
    /// <param name="value">The event type value. Must not be null or whitespace.</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is empty or whitespace.</exception>
    public EventType(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Event type value cannot be empty or whitespace.", nameof(value));
        }

        _value = value;
    }

    /// <summary>
    /// Gets the string value of the event type.
    /// </summary>
    public string Value => _value ?? string.Empty;

    /// <summary>
    /// Gets the number of segments in the event type path.
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
    /// Gets the last segment of the event type path.
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
    /// Gets the parent event type, or null if this is a root-level type.
    /// </summary>
    public EventType? Parent
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

            return new EventType(_value.Substring(0, lastSeparatorIndex));
        }
    }

    /// <summary>
    /// Determines whether this event type is a child of (or equal to) the specified ancestor type.
    /// </summary>
    /// <param name="ancestor">The potential ancestor event type.</param>
    /// <returns>true if this type equals the ancestor or starts with the ancestor followed by the separator; otherwise, false.</returns>
    public bool IsChildOf(EventType ancestor)
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
    /// Combines this event type with a child segment using the / operator.
    /// </summary>
    /// <param name="parent">The parent event type.</param>
    /// <param name="child">The child segment to append.</param>
    /// <returns>A new event type representing the combined path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when child is null.</exception>
    /// <exception cref="ArgumentException">Thrown when child is empty, whitespace, or contains the separator character.</exception>
    public static EventType operator /(EventType parent, string child)
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
            return new EventType(child);
        }

        return new EventType(parent._value + Separator + child);
    }

    /// <summary>
    /// Implicitly converts an event type to its string representation.
    /// </summary>
    /// <param name="eventType">The event type to convert.</param>
    public static implicit operator string(EventType eventType) => eventType.Value;

    /// <summary>
    /// Returns the string representation of this event type.
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
    /// Determines whether this instance equals another EventType instance.
    /// </summary>
    public bool Equals(EventType other)
    {
        return string.Equals(_value, other._value, StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether this instance equals another object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is EventType other && Equals(other);
    }

    /// <summary>
    /// Compares this instance to another EventType.
    /// </summary>
    public int CompareTo(EventType other)
    {
        return string.Compare(_value ?? string.Empty, other._value ?? string.Empty, StringComparison.Ordinal);
    }

    public static bool operator ==(EventType left, EventType right) => left.Equals(right);
    public static bool operator !=(EventType left, EventType right) => !left.Equals(right);
    public static bool operator <(EventType left, EventType right) => left.CompareTo(right) < 0;
    public static bool operator <=(EventType left, EventType right) => left.CompareTo(right) <= 0;
    public static bool operator >(EventType left, EventType right) => left.CompareTo(right) > 0;
    public static bool operator >=(EventType left, EventType right) => left.CompareTo(right) >= 0;
}
