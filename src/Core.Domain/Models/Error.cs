namespace Core.Domain.Models;

/// <summary>
/// Represents an error with a code and message.
/// </summary>
public record Error
{
    public ErrorCode Code { get; }
    public string Message { get; }

    public Error(ErrorCode code, string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentException("Message cannot be null or empty.", nameof(message));
        }

        Code = code;
        Message = message;
    }
}
