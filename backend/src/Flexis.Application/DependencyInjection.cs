using Microsoft.Extensions.DependencyInjection;

namespace Flexis.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
