namespace HelpDesk.Application.Greetings;

public interface IGreetingService
{
    Task<GreetingDto> GetCurrentAsync(CancellationToken cancellationToken = default);
}
