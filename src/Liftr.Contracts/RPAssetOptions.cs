//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr
{
    public sealed class RPAssetOptions
    {
        public string CosmosConnectionString { get; set; }

        public string StorageConnectionString { get; set; }

        public string DataPlaneStorageConnectionStrings { get; set; }

        public string DataPlaneSubscriptions { get; set; }

        public IEnumerable<string> GetStorageConnectionStrings()
        {
            var strings = DataPlaneStorageConnectionStrings.FromJson<ListItemHolder>();
            return strings.StringItems;
        }

        public IEnumerable<string> GetSubscriptions()
        {
            var strings = DataPlaneSubscriptions.FromJson<ListItemHolder>();
            return strings.StringItems;
        }

        public void SetStorageConnectionStrings(IEnumerable<string> values)
        {
            var holder = new ListItemHolder()
            {
                StringItems = values,
            };
            DataPlaneStorageConnectionStrings = holder.ToJson();
        }

        public void SetSubscriptions(IEnumerable<string> values)
        {
            var holder = new ListItemHolder()
            {
                StringItems = values,
            };
            DataPlaneSubscriptions = holder.ToJson();
        }
    }

    internal class ListItemHolder
    {
        public IEnumerable<string> StringItems { get; set; }
    }
}
