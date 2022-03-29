//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    ///     The object representation of the Cloud Service Model of an Azure Service.
    /// </summary>
    public class ScopeBindingsModel : Document
    {
        public ScopeBindingsModel()
        {
            Schema = new Uri("http://schema.express.azure.com/schemas/2015-01-01-alpha/ScopeBindings.json");
            ContentVersion = "0.0.0.1";
        }

        /// <summary>
        ///     The various scope bindings that constitute this service.
        /// </summary>
        public IEnumerable<ScopeBindings> ScopeBindings { get; set; }
    }
}