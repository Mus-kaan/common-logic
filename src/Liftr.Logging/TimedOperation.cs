//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Liftr.DiagnosticSource;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Liftr.Logging
{
    internal class TimedOperation : ITimedOperation
    {
        private readonly Serilog.ILogger _logger;
        private readonly string _operationName;
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();
        private readonly IOperationHolder<RequestTelemetry> _appInsightsOperation;
        private readonly string _operationId;
        private readonly string _startTime = DateTime.UtcNow.ToZuluString();
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private readonly LogContextPropertyScope _correlationIdScope;
        private bool _isSuccessful = true;

        public TimedOperation(Serilog.ILogger logger, string operationName, string operationId = null)
        {
            if (string.IsNullOrEmpty(operationId))
            {
                operationId = "liftr-" + Guid.NewGuid().ToString();
            }

            if (string.IsNullOrEmpty(CallContextHolder.CorrelationId.Value))
            {
                CallContextHolder.CorrelationId.Value = operationId;
                _correlationIdScope = new LogContextPropertyScope("LiftrCorrelationId", operationId);
            }

            _logger = logger;
            _operationName = operationName;
            _appInsightsOperation = AppInsightsHelper.AppInsightsClient?.StartOperation<RequestTelemetry>(operationName);
            _operationId = operationId;
            SetContextProperty("LiftrTimedOperationId", operationId);
            _logger.Debug("Start TimedOperation '{TimedOperationName}' with '{TimedOperationId}' at StartTime {StartTime}.", _operationName, _operationId, _startTime);
        }

        public void Dispose()
        {
            if (_correlationIdScope != null)
            {
                // If the correlation Id is added by this instance, it should clear it after it is going out of scope.
                CallContextHolder.CorrelationId.Value = string.Empty;
            }

            _sw.Stop();
            _logger.Information("Finished TimedOperation '{TimedOperationName}' with '{TimedOperationId}'. Successful: {isSuccessful}. Duration: {DurationMs} ms. Properties: {Properties}. StartTime: {StartTime}, StopTime: {StopTime}", _operationName, _operationId, _isSuccessful, _sw.ElapsedMilliseconds, _properties, _startTime, DateTime.UtcNow.ToZuluString());
            _appInsightsOperation?.Dispose();
            _correlationIdScope?.Dispose();
        }

        public void SetProperty(string name, string value)
        {
            _properties[name] = value;
            if (_appInsightsOperation != null)
            {
                _appInsightsOperation.Telemetry.Properties[name] = value;
            }
        }

        public void SetProperty(string name, int value)
        {
            _properties[name] = value;
            if (_appInsightsOperation != null)
            {
                _appInsightsOperation.Telemetry.Metrics[name] = value;
            }
        }

        public void SetProperty(string name, double value)
        {
            _properties[name] = value;
            if (_appInsightsOperation != null)
            {
                _appInsightsOperation.Telemetry.Metrics[name] = value;
            }
        }

        public void SetContextProperty(string name, string value)
        {
            LogContext.PushProperty(name, value);
            SetProperty(name, value);
        }

        public void SetContextProperty(string name, int value)
        {
            LogContext.PushProperty(name, value);
            SetProperty(name, value);
        }

        public void SetContextProperty(string name, double value)
        {
            LogContext.PushProperty(name, value);
            SetProperty(name, value);
        }

        public void SetResultDescription(string resultDescription)
        {
            SetProperty("ResultDescription", resultDescription);
        }

        public void FailOperation(string message = null)
        {
            _isSuccessful = false;
            if (_appInsightsOperation != null)
            {
                _appInsightsOperation.Telemetry.Success = false;
                _appInsightsOperation.Telemetry.ResponseCode = "500";
            }

            if (!string.IsNullOrEmpty(message))
            {
                SetProperty("FailureMessage", message);
            }
        }
    }
}
