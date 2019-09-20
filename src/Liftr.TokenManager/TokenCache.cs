//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;

namespace Microsoft.Liftr.TokenManager
{
    internal class TokenCache : ITokenCache, IDisposable
    {
        private readonly MemoryCache _memoryCache;
        private readonly TimeSpan _bufferTime;
        private bool _disposed = false;

        internal TokenCache(TimeSpan bufferTime)
        {
            _memoryCache = new MemoryCache(
                new MemoryCacheOptions()
                {
                    ExpirationScanFrequency = TimeSpan.FromSeconds(10),
                });
            _bufferTime = bufferTime;
        }

        ~TokenCache()
        {
            Dispose();
        }

        public AuthenticationResult GetTokenItem(string clientId)
        {
            object result = null;
            if (_memoryCache.TryGetValue(clientId, out result))
            {
                var authResult = result as AuthenticationResult;
                return authResult;
            }

            return null;
        }

        public void SetTokenItem(string clientId, AuthenticationResult token)
        {
            if (!(token.ExpiresOn - DateTime.UtcNow < _bufferTime))
            {
                _memoryCache.Set(clientId, token, token.ExpiresOn - _bufferTime);
            }
        }

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    _memoryCache.Dispose();
                }

                // Note disposing has been done.
                _disposed = true;
            }
        }
    }
}
