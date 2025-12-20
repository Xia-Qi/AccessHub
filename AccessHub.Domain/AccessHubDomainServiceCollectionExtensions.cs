using AccessHub.Domain.Users.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AccessHubDomainServiceCollectionExtensions
    {
        public static IServiceCollection AddAccessHubDomain(this IServiceCollection services)
        {
            services.TryAddScoped<UserDomainService>();
            //TODO: 注入默认 passwordHasher
            services.TryAddScoped<IPasswordHasher, DefaultPasswordHasher>();
            return services;
        }
    }
}
