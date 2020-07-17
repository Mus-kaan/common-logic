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
using System.Globalization;
using System.Net;

namespace Microsoft.Liftr.Logging
{
    internal class TimedOperation : ITimedOperation
    {
        private const string ResultDescription = nameof(ResultDescription);
        private const string FailureMessage = nameof(FailureMessage);
        private const string SucceededOperationCount = nameof(SucceededOperationCount);
        private const string FailedOperationCount = nameof(FailedOperationCount);
        private const string StatusCode = nameof(StatusCode);
        private readonly Serilog.ILogger _logger;
        private readonly string _operationName;
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();
        private readonly IOperationHolder<RequestTelemetry> _appInsightsOperation;
        private readonly string _operationId;
        private readonly bool _generateMetrics;
        private readonly string _startTime = DateTime.UtcNow.ToZuluString();
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private readonly LogContextPropertyScope _correlationIdScope;
        private readonly LogContextPropertyScope _parentCorrelationIdScope;
        private bool _isSuccessful = true;
        private int? _statusCode = null;
        private string _environmentType = null;

        public TimedOperation(Serilog.ILogger logger, string operationName, string operationId = null, bool generateMetrics = false, bool newCorrelationId = false)
        {
            if (string.IsNullOrEmpty(operationId))
            {
                operationId = Guid.NewGuid().ToString();
            }

            if (string.IsNullOrEmpty(CallContextHolder.CorrelationId.Value))
            {
                CallContextHolder.CorrelationId.Value = operationId;
                _correlationIdScope = new LogContextPropertyScope("LiftrCorrelationId", operationId);
            }
            else if (newCorrelationId)
            {
                var parentId = CallContextHolder.CorrelationId.Value;
                _parentCorrelationIdScope = new LogContextPropertyScope("ParentLiftrCorrelationId", parentId);
                CallContextHolder.CorrelationId.Value = operationId;
            }

            _logger = logger;
            _operationName = operationName;
            _appInsightsOperation = AppInsightsHelper.AppInsightsClient?.StartOperation<RequestTelemetry>(operationName);
            _operationId = operationId;
            _generateMetrics = generateMetrics;
            SetContextProperty("LiftrTimedOperationId", operationId);
            if (LoggerExtensions.Options.LogTimedOperation)
            {
                _logger.Information($"Start TimedOperation '{_operationName}' with '{{TimedOperationId}}' at StartTime {{StartTime}}.", _operationId, _startTime);
            }
        }

        public void Dispose()
        {
            if (_correlationIdScope != null)
            {
                // If the correlation Id is added by this instance, it should clear it after it is going out of scope.
                CallContextHolder.CorrelationId.Value = string.Empty;
            }

            _sw.Stop();
            if (LoggerExtensions.Options.LogTimedOperation)
            {
                if (_statusCode == null)
                {
                    _logger.Information("Finished TimedOperation '" + _operationName + "' with '{TimedOperationId}'. Successful: {isSuccessful}. Duration: {DurationMs} ms. Properties: {Properties}. StartTime: {StartTime}, StopTime: {StopTime}", _operationId, _isSuccessful, _sw.ElapsedMilliseconds, _properties, _startTime, DateTime.UtcNow.ToZuluString());
                }
                else
                {
                    _logger.Information("Finished TimedOperation '" + _operationName + "' with '{TimedOperationId}'. Successful: {isSuccessful}. StatusCode: {statusCode}. Duration: {DurationMs} ms. Properties: {Properties}. StartTime: {StartTime}, StopTime: {StopTime}", _operationId, _isSuccessful, _statusCode.Value, _sw.ElapsedMilliseconds, _properties, _startTime, DateTime.UtcNow.ToZuluString());
                }
            }

            if (_appInsightsOperation != null && _generateMetrics)
            {
                if (_isSuccessful)
                {
                    _appInsightsOperation.Telemetry.Metrics[SucceededOperationCount] = 1;
                    _appInsightsOperation.Telemetry.Metrics[FailedOperationCount] = 0;
                    _appInsightsOperation.Telemetry.Metrics[_operationName + SucceededOperationCount] = 1;
                    _appInsightsOperation.Telemetry.Metrics[_operationName + FailedOperationCount] = 0;

                    if (!string.IsNullOrEmpty(_environmentType))
                    {
                        _appInsightsOperation.Telemetry.Metrics[_environmentType + SucceededOperationCount] = 1;
                        _appInsightsOperation.Telemetry.Metrics[_environmentType + FailedOperationCount] = 0;
                        _appInsightsOperation.Telemetry.Metrics[_environmentType + _operationName + SucceededOperationCount] = 1;
                        _appInsightsOperation.Telemetry.Metrics[_environmentType + _operationName + FailedOperationCount] = 0;
                    }
                }
                else
                {
                    _appInsightsOperation.Telemetry.Metrics[SucceededOperationCount] = 0;
                    _appInsightsOperation.Telemetry.Metrics[FailedOperationCount] = 1;
                    _appInsightsOperation.Telemetry.Metrics[_operationName + SucceededOperationCount] = 0;
                    _appInsightsOperation.Telemetry.Metrics[_operationName + FailedOperationCount] = 1;

                    if (!string.IsNullOrEmpty(_environmentType))
                    {
                        _appInsightsOperation.Telemetry.Metrics[_environmentType + SucceededOperationCount] = 0;
                        _appInsightsOperation.Telemetry.Metrics[_environmentType + FailedOperationCount] = 1;
                        _appInsightsOperation.Telemetry.Metrics[_environmentType + _operationName + SucceededOperationCount] = 0;
                        _appInsightsOperation.Telemetry.Metrics[_environmentType + _operationName + FailedOperationCount] = 1;
                    }
                }
            }

            _appInsightsOperation?.Dispose();
            _correlationIdScope?.Dispose();
            _parentCorrelationIdScope?.Dispose();
        }

        public void SetEnvironmentType(string environmentType)
        {
            _environmentType = environmentType;
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
            SetProperty(ResultDescription, resultDescription);
        }

        public void SetResult(int statusCode, string resultDescription = null)
        {
            if (statusCode == 0)
            {
                statusCode = 200;
            }

            if (!string.IsNullOrEmpty(resultDescription))
            {
                SetProperty(ResultDescription, resultDescription);
            }

            _statusCode = statusCode;

            if (statusCode < 200 || statusCode > 299)
            {
                _isSuccessful = false;
            }

            if (_appInsightsOperation != null)
            {
                _appInsightsOperation.Telemetry.Success = _isSuccessful;
                _appInsightsOperation.Telemetry.ResponseCode = ((int)statusCode).ToString(CultureInfo.InvariantCulture);
            }
        }

        public void FailOperation(string message = null)
        {
            _isSuccessful = false;
            _statusCode = 500;

            if (_appInsightsOperation != null)
            {
                _appInsightsOperation.Telemetry.Success = false;
                _appInsightsOperation.Telemetry.ResponseCode = "500";
            }

            if (!string.IsNullOrEmpty(message))
            {
                SetProperty(FailureMessage, message);
            }
        }

        public void FailOperation(HttpStatusCode statusCode, string message = null)
        {
            _isSuccessful = false;
            _statusCode = (int)statusCode;

            var statusCodeStr = ((int)statusCode).ToString(CultureInfo.InvariantCulture);
            if (_appInsightsOperation != null)
            {
                _appInsightsOperation.Telemetry.Success = false;
                _appInsightsOperation.Telemetry.ResponseCode = statusCodeStr;
            }

            if (!string.IsNullOrEmpty(message))
            {
                SetProperty(FailureMessage, message);
            }
        }
    }
}
