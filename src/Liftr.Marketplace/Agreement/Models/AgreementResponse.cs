//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Marketplace.Agreement.Models
{
    public class AgreementResponse
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Type { get; set; }

        public AgreementResponseProperties Properties { get; set; }

        public bool IsValid()
        {
            return
                !string.IsNullOrEmpty(Id) &&
                !string.IsNullOrEmpty(Type) &&
                !string.IsNullOrEmpty(Name) &&
                !(Properties is null) &&
                !string.IsNullOrEmpty(Properties.Plan) &&
                !string.IsNullOrEmpty(Properties.Product) &&
                !string.IsNullOrEmpty(Properties.Publisher);
        }
    }
}
