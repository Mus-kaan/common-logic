//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.Liftr.ImageBuilder
{
    public class RunAzureVMImageBuilderException : Exception
    {
        private const string c_link = "More trouble-shotting guide: https://aka.ms/liftr/aib-tsg";
        private const string c_messagePart = "Please see Azure VM Image Builder logs for details. The logs can be found in the resource group with name starting with";
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
            : base($"{message} {c_messagePart} {PackaerLogLocaionMessage(subscriptionId, resourceGroup, templateName)}. {c_link}")
        {
        }

        public static string PackaerLogLocaionMessage(string subscriptionId, string resourceGroup, string templateName)
        {
            return $"Resource group starting with name '{Truncate($"IT_{resourceGroup}_{templateName}", 43)}' in subscription '{subscriptionId}'";
        }

        private static string Truncate(string input, int maxLength)
        {
            if (input.Length <= maxLength)
            {
                return input;
            }
            else
            {
                return input.Substring(0, maxLength);
            }
        }
    }
}
