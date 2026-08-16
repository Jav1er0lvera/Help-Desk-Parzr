using HelpDesk.Application.Tickets;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HelpDesk.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreateTicketHandler).Assembly));

        return services;
    }
}