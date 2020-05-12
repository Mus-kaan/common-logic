//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;

namespace Microsoft.Liftr.AAD
{
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

        /// <summary>
        /// Gets the value associated with the specified key
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="payload">Jwt payload</param>
        /// <param name="key">Key of payload entry</param>
        /// <returns>Value of the payload entry</returns>
        public static T Get<T>(this JwtPayload payload, string key)
        {
            if (payload.TryGet(key, out T value))
            {
                return value;
            }
            else
            {
                throw new InvalidAADTokenException($"Cannot find '{key}' in payload of the JWT.");
            }
        }
    }
}
