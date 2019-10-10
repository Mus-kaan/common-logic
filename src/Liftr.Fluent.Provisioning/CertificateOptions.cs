//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class CertificateOptions
    {
        public string CertificateName { get; set; }

        public string SubjectName { get; set; }

        public IList<string> SubjectAlternativeNames { get; set; }
    }
}
