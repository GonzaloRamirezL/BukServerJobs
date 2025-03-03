using API.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ServiceRegistration;
using System;

namespace Startup.Utils
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddServiceRegistration(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            ServiceConfiguration.ConfigureServices(services);
            return services;
        }

        public static IApplicationBuilder UseServiceLocator(this IApplicationBuilder builder)
        {
            ServiceLocator.ServiceProvider = builder.ApplicationServices;
            return builder;
        }
    }
}
