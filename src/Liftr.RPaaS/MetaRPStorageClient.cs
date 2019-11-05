//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Liftr.Contracts.ARM;
using Microsoft.Liftr.Contracts.Exceptions;
using Microsoft.Liftr.TokenManager;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Liftr.RPaaS
{
    public class MetaRPStorageClient : IMetaRPStorageClient, IDisposable
    {
        private readonly RPaaSConfiguration _configuration;
        private readonly ITokenManager _tokenManager;
        private readonly IKeyVaultClient _keyVaultClient;
        private readonly HttpClient _httpClient;
        private readonly MemoryCache _memoryCache;
        private static readonly TimeSpan s_bufferTime = TimeSpan.FromHours(1);
        private bool _disposed = false;

        public MetaRPStorageClient(
            IOptions<RPaaSConfiguration> configuration,
            ITokenManager tokenManager,
            IKeyVaultClient keyVaultClient,
            HttpClient httpClient)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _configuration = configuration.Value;
            _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
            _keyVaultClient = keyVaultClient ?? throw new ArgumentNullException(nameof(keyVaultClient));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _httpClient.BaseAddress = new Uri(_configuration.MetaRPEndpoint);

            _memoryCache = new MemoryCache(
                new MemoryCacheOptions()
                {
                    ExpirationScanFrequency = TimeSpan.FromSeconds(10),
                });
        }

        public async Task<T> GetResourceAsync<T>(string resourceId, string apiVersion) where T : ARMResource
        {
            var url = GetMetaRPResourceUrl(resourceId, apiVersion);
            _httpClient.DefaultRequestHeaders.Authorization = await GetAuthHeaderAsync();
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(
                    await response.Content.ReadAsStringAsync());
            }
            else
            {
                throw HttpResponseException.Create(response, resourceId);
            }
        }

        public async Task UpdateResourceAsync<T>(T resource, string apiVersion) where T : ARMResource
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            using (var content = new StringContent(JsonConvert.SerializeObject(resource), Encoding.UTF8, "application/json"))
            {
                var url = GetMetaRPResourceUrl(resource.Id, apiVersion);
                _httpClient.DefaultRequestHeaders.Authorization = await GetAuthHeaderAsync();
                var response = await _httpClient.PutAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw HttpResponseException.Create(response, resource.Id);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient.Dispose();
                    _memoryCache.Dispose();
                    _keyVaultClient.Dispose();
                }

                _disposed = true;
            }
        }

        private string GetMetaRPResourceUrl(string resourceId, string apiVersion)
        {
            return resourceId + "?api-version=" + apiVersion;
        }

        private async Task<AuthenticationHeaderValue> GetAuthHeaderAsync()
        {
            var certificate = await LoadCertificateAsync();

            var authenticationHeader = new AuthenticationHeaderValue(
                        "Bearer",
                        await _tokenManager.GetTokenAsync(_configuration.MetaRPAccessorClientId, certificate));

            certificate.Dispose();
            return authenticationHeader;
        }

        private async Task<X509Certificate2> LoadCertificateAsync()
        {
            var localCertificate = LoadCertificateFromCache();

            if (localCertificate == null)
            {
                var secretBundle = await _keyVaultClient.GetSecretAsync(
                    _configuration.MetaRPAccessorVaultEndpoint, _configuration.MetaRPAccessorCertificateName);
                var privateKeyBytes = Convert.FromBase64String(secretBundle.Value);
                var certificate = new X509Certificate2(privateKeyBytes);
                _memoryCache.Set(
                    _configuration.MetaRPAccessorCertificateName, certificate, DateTimeOffset.UtcNow + s_bufferTime);
                return certificate;
            }

            return localCertificate;
        }

        private X509Certificate2 LoadCertificateFromCache()
        {
            X509Certificate2 result = null;
            _memoryCache.TryGetValue(_configuration.MetaRPAccessorCertificateName, out result);
            return result;
        }
    }
}
