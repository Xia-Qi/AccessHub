using AccessHub.Application;
using AccessHub.Application.Behaviors;
using Domain.Base;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AccessHubAppServiceCollectionExtensions
    {
        public static IServiceCollection AddAccessHubApp(this IServiceCollection services)
        {
            // MediatR for Commands
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AccessHubAppServiceCollectionExtensions).Assembly));
            /// Fluent Validators for Commands
            services.AddValidatorsFromAssembly(typeof(AccessHubAppServiceCollectionExtensions).Assembly);
            services.AddScoped<IDomainEventService, DomainEventService>();

            // Behaviors
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

            return services;
        }
    }
}
