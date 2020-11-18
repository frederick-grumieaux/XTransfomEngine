using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XTransformEngine.Core.Installers
{
    public static class LibraryInstaller
    {
        public static void Install(IServiceCollection services)
        {
            services.AddTransient<XTransformEngineFactory>();
            services.AddFactory<XTransformEngine>();
        }
    }

    public static class IServiceCollectionExtension
    {
        public static IServiceCollection AddFactory<TService>(this IServiceCollection serviceCollection)
            where TService : class
        {
            return serviceCollection.AddFactory<TService, TService>();
        }
        public static IServiceCollection AddFactory<TService, TServiceImplementation>(this IServiceCollection serviceCollection)
            where TService : class
            where TServiceImplementation : class, TService
        {
            return serviceCollection
                .AddTransient<TService, TServiceImplementation>()
                .AddTransient<Func<TService>>(sp => sp.GetRequiredService<TService>);
        }
    }
}
