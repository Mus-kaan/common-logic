//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Utilities
{
    public static class InstanceMetaHelper
    {
        private static readonly TimeSpan s_cacheTime = TimeSpan.FromMinutes(30);
        private static MetaInfo s_metaInfo;
        private static DateTime? s_loadTime = null;

        public static async Task<MetaInfo> GetMetaInfoAsync()
        {
            try
            {
                if (s_loadTime.HasValue && (DateTime.UtcNow - s_loadTime.Value) < s_cacheTime)
                {
                    return s_metaInfo;
                }

                if (s_metaInfo == null)
                {
                    var entryAssembly = Assembly.GetEntryAssembly();
                    var assemblyName = entryAssembly.GetName().Name;
                    var assemblyProductVersion = FileVersionInfo.GetVersionInfo(entryAssembly.Location).ProductVersion;

                    var currentAssembly = Assembly.GetExecutingAssembly();
                    var currentAssemblyProductVersion = FileVersionInfo.GetVersionInfo(currentAssembly.Location).ProductVersion;

                    s_metaInfo = new MetaInfo()
                    {
                        AssemblyName = assemblyName,
                        Version = assemblyProductVersion,
                        LiftrLibraryVersion = currentAssemblyProductVersion,
                        Timestamp = DateTime.UtcNow.ToZuluString(),
                    };
                }

                s_metaInfo.InstanceMeta = await InstanceMetadata.LoadAsync();
                s_metaInfo.OutboundIP = await MetadataHelper.GetPublicIPAddressAsync(noThrow: true);
                s_loadTime = DateTime.UtcNow;
                return s_metaInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{nameof(GetMetaInfoAsync)} failed. Exception: {ex}");
                return null;
            }
        }
    }
}
