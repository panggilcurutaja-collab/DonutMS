using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DonutMS.Configuration;

public static class ValidationConfiguration
{
    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ValidationConfiguration).Assembly);
        return services;
    }
}
