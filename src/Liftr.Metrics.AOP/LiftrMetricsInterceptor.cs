//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Castle.DynamicProxy;
using Microsoft.Liftr.Metrics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Metrics.AOP
{
    /// <summary>
    /// Interceptor for emitting latency metrics at Method level
    /// </summary>
    public class LiftrMetricsInterceptor : IInterceptor
    {
        private readonly ILogger _logger;
        private readonly IMetricSender _metricSender;

        public LiftrMetricsInterceptor(IMetricSender metricSender, ILogger logger)
        {
            _metricSender = metricSender ?? throw new ArgumentNullException(nameof(metricSender));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Intercepts a Method
        /// </summary>
        public void Intercept(IInvocation invocation)
        {
            if (invocation == null)
            {
                throw new ArgumentNullException(nameof(invocation));
            }

            if (IsAsyncMethod(invocation.Method))
            {
                InterceptAsyncMethod(invocation);
            }
            else
            {
                InterceptSyncMethod(invocation);
            }
        }

        /// <summary>
        /// Intercepts Asynchronous Method
        /// </summary>
        private void InterceptAsyncMethod(IInvocation invocation)
        {
            var status = Constants.SuccessStatus;
            var stopwatch = new Stopwatch();
            try
            {
                // Before method execution
                stopwatch.Start();

                // Calling the actual method, but execution has not been finished yet
                invocation.Proceed();

                invocation.ReturnValue = InterceptAsync((dynamic)invocation.ReturnValue);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                status = Constants.FailureStatus;
                _logger.Error("Exception in InterceptAsyncMethod : {exception}}", ex.Message);
            }
            finally
            {
                stopwatch.Stop();
                var className = invocation.TargetType.Name;
                var methodName = invocation.MethodInvocationTarget.Name;
                var metricName = GetMetricName(invocation, Constants.DefaultMetricName);
                var dimensions = new Dictionary<string, string>
                {
                    [Constants.Dimension_ClassName] = className,
                    [Constants.Dimension_MethodName] = methodName,
                    [Constants.Dimension_Status] = status,
                };

                _metricSender.Gauge(metricName, (int)stopwatch.ElapsedMilliseconds, dimensions);
                _logger.Debug("Sent Metrics with MetricName: {metricName}, Duration: {duration}, Dimensions: {dimesions}", metricName, stopwatch.ElapsedMilliseconds, dimensions.Values);
            }
        }

        /// <summary>
        /// Intercepts Synchronous Method
        /// </summary>
        private void InterceptSyncMethod(IInvocation invocation)
        {
            var stopwatch = new Stopwatch();
            var status = Constants.SuccessStatus;
            try
            {
                // Before method execution
                stopwatch.Start();

                // Executing the actual method
                invocation.Proceed();

                // After method execution
                stopwatch.Stop();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.Error("Exception: {exception}}", ex.Message);
                status = Constants.FailureStatus;
            }
            finally
            {
                var className = invocation.TargetType.Name;
                var methodName = invocation.MethodInvocationTarget.Name;
                var metricName = GetMetricName(invocation, Constants.DefaultMetricName);
                var dimensions = new Dictionary<string, string>
                {
                   [Constants.Dimension_ClassName] = className,
                   [Constants.Dimension_MethodName] = methodName,
                   [Constants.Dimension_Status] = status,
                };
                _metricSender.Gauge(metricName, (int)stopwatch.ElapsedMilliseconds, dimensions);
                _logger.Debug("Sent Metrics with MetricName: {metricName}, Duration: {duration}, Dimensions: {dimesions}", metricName, stopwatch.ElapsedMilliseconds, dimensions.Values);
            }
        }

        private async Task InterceptAsync(Task task)
        {
            await task;
        }

        private async Task<T> InterceptAsync<T>(Task<T> task)
        {
            T result = await task;
            return result;
        }

        /// <summary>
        /// Checks if Method is Async or not
        /// </summary>
        private static bool IsAsyncMethod(MethodInfo method)
        {
            return method.ReturnType == typeof(Task) ||
                (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
        }

        /// <summary>
        /// Returns the MetricName if passed or else returns default MetricName
        /// </summary>
        private static string GetMetricName(IInvocation invocation, string defaultMetricName)
        {
            var metricName = invocation.MethodInvocationTarget.GetCustomAttribute<LiftrMetricsAttribute>()?.MetricName;
            if (!string.IsNullOrEmpty(metricName))
            {
                return metricName;
            }

            return defaultMetricName;
        }
    }
}
