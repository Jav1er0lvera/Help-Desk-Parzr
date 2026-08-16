using HelpDesk.Domain.Greetings;

namespace HelpDesk.Domain.Tests.Greetings;

public class GreetingTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankMessage_Throws(string message)
        => Assert.Throws<ArgumentException>(() => Greeting.Create(message, DateTimeOffset.UnixEpoch));

    [Fact]
    public void Create_TrimsMessage()
    {
        var greeting = Greeting.Create("  hi  ", DateTimeOffset.UnixEpoch);
        Assert.Equal("hi", greeting.Message);
    }
}
