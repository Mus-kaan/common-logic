//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Liftr.PerformanceTelemetry.Models.Enums;
using Microsoft.Liftr.PerformanceTelemetry.Models.Operations;
using Serilog;
using System;

namespace Microsoft.Liftr.PerformanceTelemetry
{
    /// <summary>
    /// Static class to capture stateless telemetry of an operation.
    /// </summary>
    public static class TelemetryCapture
    {
        /// <summary>
        /// This method will be called from each RP as they import the package.
        /// </summary>
        /// <param name="mainOperationName">Pass a valid operation name everytime the RP calls the method</param>
        /// <param name="partner">Pass the RP name from which the method was called</param>
        /// <param name="uniqueIdentifierType"></param>
        /// <param name="uniqueIdentifierId"></param>
        /// <param name="operationType">Pass Start when it's the start of the operation, or else pass Stop</param>
        /// <param name="subOperationName">Pass the sub operation, if it's an operation within an operation. Doc for more details - //todo. Please pass null object if it's the main operation calling this method and there is no suboperation</param>
        /// <param name="status">Pass 'None' while passing 'Start' as the argument to operationType</param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void OperationDetails(MainOperationNameBaseType mainOperationName, Partners partner, UniqueIdentifierTypes uniqueIdentifierType, string uniqueIdentifierId, OperationTypes operationType, SubOperationNameBaseType subOperationName, Statuses status, ILogger logger)
        {
            if(mainOperationName is null)
            {
                throw new ArgumentNullException(nameof(mainOperationName), $"Please provide valid {nameof(mainOperationName)}");
            }

            if (string.IsNullOrWhiteSpace(uniqueIdentifierId))
            {
                throw new ArgumentNullException(nameof(uniqueIdentifierId), $"Please provide valid {nameof(uniqueIdentifierId)}");
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger), $"Please provide valid {nameof(logger)}");
            }


            logger.Information($"Performance insight details captured for Operation: {mainOperationName}, Partner: {partner}, UniqueIdentifierType: {uniqueIdentifierType}, UniqueIdentifierId: {uniqueIdentifierId}, OperationType: {operationType}, SubOperationName: {subOperationName}, Status: {status}");
        }
    }
}