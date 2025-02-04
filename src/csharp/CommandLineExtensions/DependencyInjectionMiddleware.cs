using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommandLineExtensions;

public static class DependencyInjectionMiddleware
{
    public static CommandLineBuilder UseDependencyInjection(this CommandLineBuilder builder,
        Action<IServiceCollection> configureServices) => builder.UseDependencyInjection((_, services)
        => configureServices(services));

    public static CommandLineBuilder UseDependencyInjection(this CommandLineBuilder builder,
        Action<InvocationContext, IServiceCollection> configureServices) => builder.AddMiddleware(
        async (context, next) =>
        {
            var services = new ServiceCollection();
            configureServices(context, services);
            var uniqueServiceTypes = new HashSet<Type>(services.Select(service => service.ServiceType));
            
            services.TryAddSingleton(context.Console);
            
            await using var serviceProvider = services.BuildServiceProvider();
            
            // ReSharper disable AccessToDisposedClosure
            context.BindingContext.AddService<IServiceProvider>(_ => serviceProvider);

            foreach (var serviceType in uniqueServiceTypes)
            {
                context.BindingContext.AddService(serviceType, _ => serviceProvider.GetRequiredService(serviceType));
                
                var enumerableServiceType = typeof(IEnumerable<>).MakeGenericType(serviceType);
                context.BindingContext.AddService(enumerableServiceType, _ => serviceProvider.GetServices(serviceType));
            }
            // ReSharper restore AccessToDisposedClosure
            
            await next(context);
        });

    public static IServiceCollection AddSystemCommandLine(this IServiceCollection services, Action<CommandLineBuilder> configure)
    {
        services.AddSingleton(sp =>
        {
            var rootCommand = sp.GetService<RootCommand>();
            var builder = new CommandLineBuilder(rootCommand).AddMiddleware((context, next) =>
            {
                context.BindingContext.AddService<IServiceProvider>(_ => sp);
                
                foreach (var service in services.DistinctBy(s => s.ServiceType))
                {
                    context.BindingContext.AddService(service.ServiceType, _ => sp.GetRequiredService(service.ServiceType));
                }

                return next(context);
            });
            configure(builder);
            return builder;
        });
        
        services.AddSingleton(sp => sp.GetRequiredService<CommandLineBuilder>().Build());

        return services;
    }
}