using HelpDesk.SDK.Categories;
using HelpDesk.SDK.Comments;
using HelpDesk.SDK.Platforms;
using HelpDesk.SDK.Tickets;
using HelpDesk.SDK.Users;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace HelpDesk.SDK;

public static class DependencyInjection
{
    public static IServiceCollection AddHelpDeskSdk(this IServiceCollection services, string baseUrl)
    {
        services.AddRefitClient<ITicketsApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl));

        services.AddRefitClient<IUsersApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl));

        services.AddRefitClient<ICategoriesApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl));

        services.AddRefitClient<IPlatformsApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl));

        services.AddRefitClient<ICommentsApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl));

        return services;
    }
}