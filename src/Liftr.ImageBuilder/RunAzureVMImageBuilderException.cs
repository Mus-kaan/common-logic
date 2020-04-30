//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.ImageBuilder
{
    public class RunAzureVMImageBuilderException : Exception
    {
        private const string c_link = "More trouble-shotting guide: https://aka.ms/liftr/aib-tsg";
        private const string c_messagePart = "Please see Azure VM Image Builder logs for details. The logs can be found in the resource group with name";
        private const string c_basicErrorMessage = c_messagePart + " 'IT_<ImageResourceGroupName>_<TemplateName>'. " + c_link;

        public RunAzureVMImageBuilderException()
            : base(c_basicErrorMessage)
        {
        }

        public RunAzureVMImageBuilderException(string message)
            : base(message + " " + c_basicErrorMessage)
        {
        }

        public RunAzureVMImageBuilderException(string message, Exception innerException)
            : base(message + " " + c_basicErrorMessage, innerException)
        {
        }

        public RunAzureVMImageBuilderException(string message, string subscriptionId, string resourceGroup, string templateName)
            : base($"{message} {c_messagePart} 'IT_{resourceGroup}_{templateName}' in subscription '{subscriptionId}'. {c_link}")
        {
        }
    }
}
