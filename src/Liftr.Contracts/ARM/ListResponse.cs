//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Contracts.ARM
{
    /// <summary>
    /// Response of a list operation.
    /// </summary>
    /// <typeparam name="T">Type of the list result.</typeparam>
    public class ListResponse<T>
    {
        /// <summary>
        /// Results of a list operation.
        /// </summary>
        public IEnumerable<T> Value { get; set; }

        /// <summary>
        /// Link to the next set of results, if any.
        /// </summary>
        public string NextLink { get; set; }
    }
}
