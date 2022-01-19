//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Microsoft.Liftr.Metrics.AOP
{
    /// <summary>
    /// Extension Methods for the <see cref="IServiceCollection"/>
    /// </summary>
    public static class LiftrMetricsServiceExtension
    {
        /// <summary>
        /// Adds a Singleton Service to the ServiceCollection that is wrapped in a Proxy
        /// </summary>
        /// <typeparam name="TInterface">Interface Type</typeparam>
        /// <typeparam name="TImplementation">Implementation Type</typeparam>
        /// <param name="services">Services Collection</param>
        /// <returns><see cref="IServiceCollection"/></returns>
        public static void AddSingletonWithProxy<TInterface, TImplementation>(this IServiceCollection services)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            services.AddSingleton<TImplementation>();
            services.AddSingleton(new ProxyGenerator());
            services.AddSingleton<IInterceptor, LiftrMetricsInterceptor>();

            services.AddSingleton(typeof(TInterface), serviceProvider =>
            {
                var proxyGenerator = serviceProvider.GetRequiredService<ProxyGenerator>();
                var actual = serviceProvider.GetRequiredService<TImplementation>();
                var interceptors = serviceProvider.GetServices<IInterceptor>().ToArray();
                return proxyGenerator.CreateInterfaceProxyWithTarget(typeof(TInterface), actual, interceptors);
            });
        }
    }
}
