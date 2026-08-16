using HelpDesk.Domain.Greetings;

namespace HelpDesk.Application.Greetings;

public sealed class GreetingService(TimeProvider clock) : IGreetingService
{
    public Task<GreetingDto> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var greeting = Greeting.Create("Hello from HelpDesk", clock.GetUtcNow());
        return Task.FromResult(new GreetingDto(greeting.Message, greeting.Timestamp));
    }
}
