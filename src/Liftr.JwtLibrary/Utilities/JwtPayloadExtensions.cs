//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;

namespace Microsoft.Liftr.JwtLibrary.Utilities
{
    /// This is mostly from ACR implementation
    /// https://msazure.visualstudio.com/One/_git/DevServices-ContainerRegistry-Service?path=%2Fsrc%2Ftokenservice%2FACR.JwtLibrary%2FUtilities
    public static class JwtPayloadExtensions
    {
        /// <summary>
        /// Gets the value associated with the specified key
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="payload">Jwt payload</param>
        /// <param name="key">Key of payload entry</param>
        /// <param name="value">Value of the payload entry</param>
        /// <returns>True if correctly gets the value, otherwise false</returns>
        public static bool TryGet<T>(this JwtPayload payload, string key, out T value)
        {
            if (payload != null && payload.TryGetValue(key, out object obj))
            {
                if (obj is T typed)
                {
                    value = typed;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public static void AddAccess(this JwtPayload payload, List<ResourceScope> access)
        {
            if (payload != null && access != null)
            {
                payload.Add("access", access);
            }
        }

        /// <summary>
        /// ValidateAccess check the payload permissions
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="type">type of the token permission</param>
        /// <param name="permission">the expected permissions. Empty means skip action validation</param>
        /// <returns></returns>
        public static bool ValidateAccess(this JwtPayload payload, TokenRequestTypes type, string permission)
        {
            payload.TryGet("access", out object access);
            var accessList = (access as JArray)?.ToObject<List<ResourceScope>>();
            if (accessList == null)
            {
                return false;
            }

            foreach (var item in accessList)
            {
                if (item.Type == type)
                {
                    if (string.IsNullOrEmpty(permission))
                    {
                        return true;
                    }

                    foreach (var act in item.Actions)
                    {
                        if (act == permission)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
