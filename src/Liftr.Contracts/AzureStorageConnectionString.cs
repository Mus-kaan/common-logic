//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Linq;

namespace Microsoft.Liftr.Contracts
{
    /// <summary>
    /// Represents a connection string to Azure Storage.
    /// https://msazure.visualstudio.com/One/_git/Dynamics-LCS?path=%2Fsrc%2FDynamicsOnline%2FLCSTasks%2FStorage%2FAzureStorageConnectionString.cs
    /// </summary>
    public sealed class AzureStorageConnectionString
    {
        private const string c_DefaultEndpointsProtocol = "DefaultEndpointsProtocol";
        private const string c_AccountName = "AccountName";
        private const string c_AccountKey = "AccountKey";
        private const string c_EndpointSuffix = "EndpointSuffix";

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureStorageConnectionString"/> class.
        /// </summary>
        /// <param name="accountName">Name of the account.</param>
        /// <param name="accountKey">The account key.</param>
        /// <param name="storageEndpointSuffix">storageEndpointSuffix, by Azure Environment.</param>
        public AzureStorageConnectionString(string accountName, string accountKey, string storageEndpointSuffix)
            : this(accountName, accountKey, "https", storageEndpointSuffix)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureStorageConnectionString"/> class.
        /// </summary>
        /// <param name="accountName">Name of the account.</param>
        /// <param name="accountKey">The account key.</param>
        /// <param name="protocol">The protocol.</param>
        /// <param name="storageEndpointSuffix">storageEndpointSuffix, by Azure Environment.</param>
        public AzureStorageConnectionString(string accountName, string accountKey, string protocol, string storageEndpointSuffix)
        {
            AccountName = accountName;
            AccountKey = accountKey;
            EndpointsProtocol = protocol;
            EndpointSuffix = storageEndpointSuffix;
        }

        private AzureStorageConnectionString()
        {
        }

        /// <summary>
        /// Gets or sets the name of the account.
        /// </summary>
        /// <value>The name of the account.</value>
        public string AccountName { get; private set; }

        /// <summary>
        /// Gets or sets the account key.
        /// </summary>
        /// <value>The account key.</value>
        public string AccountKey { get; private set; }

        /// <summary>
        /// Gets or sets the endpoints protocol.
        /// </summary>
        /// <value>The endpoints protocol.</value>
        public string EndpointsProtocol { get; private set; }

        /// <summary>
        /// Gets or sets the endpoint suffix for the account.
        /// </summary>
        /// <value>The suffix.</value>
        public string EndpointSuffix { get; private set; }

        public static bool TryParseConnectionString(string connectionString, out AzureStorageConnectionString azStorConnectionString)
        {
            azStorConnectionString = null;
            if (string.IsNullOrEmpty(connectionString))
            {
                return false;
            }

            var elements = connectionString.Split(';');
            if (elements.Length != 3 && elements.Length != 4)
            {
                return false;
            }

            var rst = new AzureStorageConnectionString();

            if (TryExtract(elements, c_DefaultEndpointsProtocol, out var portol))
            {
                rst.EndpointsProtocol = portol;
            }
            else
            {
                return false;
            }

            if (TryExtract(elements, c_AccountName, out var acctName))
            {
                rst.AccountName = acctName;
            }
            else
            {
                return false;
            }

            if (TryExtract(elements, c_AccountKey, out var acctKey))
            {
                rst.AccountKey = acctKey;
            }
            else
            {
                return false;
            }

            if (TryExtract(elements, c_EndpointSuffix, out var suffic))
            {
                rst.EndpointSuffix = suffic;
            }
            else
            {
                return false;
            }

            azStorConnectionString = rst;
            return true;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(EndpointSuffix))
            {
                return $"DefaultEndpointsProtocol={EndpointsProtocol};AccountName={AccountName};AccountKey={AccountKey}";
            }
            else
            {
                return $"DefaultEndpointsProtocol={EndpointsProtocol};AccountName={AccountName};AccountKey={AccountKey};EndpointSuffix={EndpointSuffix}";
            }
        }

        private static bool TryExtract(string[] elements, string keyName, out string value)
        {
            var key = keyName + "=";
            var matched = elements.Where(e => e.StartsWith(key, comparisonType: StringComparison.Ordinal)).FirstOrDefault();
            if (!string.IsNullOrEmpty(matched))
            {
                value = matched.Replace(key, string.Empty);
                return true;
            }

            value = null;
            return false;
        }
    }
}
