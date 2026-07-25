using Microsoft.OpenApi.Models;

namespace Senkora.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = "Senkora API",
                Version     = "v1",
                Description = "WooCommerce - Logo ERP Enterprise Integration Platform",
                Contact     = new OpenApiContact { Name = "Senkora", Email = "dev@senkora.io" }
            });

            var secScheme = new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                In           = ParameterLocation.Header,
                Description  = "Enter: Bearer {token}"
            };
            c.AddSecurityDefinition("Bearer", secScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }},
                    []
                }
            });
        });
        return services;
    }
}
