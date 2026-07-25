using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Senkora.Application.Common.Behaviors;
using System.Reflection;

namespace Senkora.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // AutoMapper DI extension is in Senkora.Api — registered there
        // Application layer only registers the mapping profiles assembly reference

        return services;
    }
}
