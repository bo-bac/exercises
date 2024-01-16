using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;

namespace ApiApplication.Internal
{
    //todo: or use for instance https://github.com/khellang/Scrutor

    /// <summary>
    /// DI Decorator Microsoft.Extensions.DependencyInjection
    /// source: https://github.com/valent-cerovecki/decorator/blob/main/Decorator.Service/DecoratorExtensions.cs
    /// </summary>
    internal static class DecoratorExtensions
    {
        public static void Decorate<TInterface, TDecorator>(this IServiceCollection services)
            where TInterface : class
            where TDecorator : class, TInterface
        {
            var interfaceDescriptor = services.SingleOrDefault(
                s => s.ServiceType == typeof(TInterface));

            if (interfaceDescriptor == null)
                throw new InvalidOperationException($"{typeof(TInterface).Name} is not registered in injection container");

            var decoratorFactory = ActivatorUtilities.CreateFactory(
                typeof(TDecorator),
                new[] { typeof(TInterface) });

            if (decoratorFactory == null)
                throw new InvalidOperationException($"Cannot create factory for {typeof(TDecorator).Name}");

            services.Replace(ServiceDescriptor.Describe(
                typeof(TInterface),
                serviceProvider => (TInterface)decoratorFactory(
                    serviceProvider,
                    new[] { serviceProvider.CreateInstance(interfaceDescriptor) }
                ),
                interfaceDescriptor.Lifetime));
        }

        private static object CreateInstance(this IServiceProvider services, ServiceDescriptor descriptor)
        {
            if (descriptor.ImplementationInstance != null)
                return descriptor.ImplementationInstance;

            if (descriptor.ImplementationFactory != null)
                return descriptor.ImplementationFactory(services);

            if (descriptor.ImplementationType == null)
                throw new InvalidOperationException($"Invalid service descriptor implementation type");

            var instance = ActivatorUtilities.GetServiceOrCreateInstance(services, descriptor.ImplementationType!);
            if (instance == null)
                throw new InvalidOperationException($"Cannot create instance of {descriptor.ImplementationType.Name}");

            return instance;
        }
    }
}
