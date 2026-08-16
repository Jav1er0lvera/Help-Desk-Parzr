namespace HelpDesk.Domain.Greetings;

public sealed record Greeting(string Message, DateTimeOffset Timestamp)
{
    public static Greeting Create(string message, DateTimeOffset timestamp)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Greeting message must not be blank.", nameof(message));

        return new Greeting(message.Trim(), timestamp);
    }
}
