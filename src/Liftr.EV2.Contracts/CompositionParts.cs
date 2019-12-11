//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    /// Composition Parts class
    /// </summary>
    public class CompositionParts
    {
        /// <summary>
        /// Gets or sets ARM composition part
        /// </summary>
        public ArmCompositionPart Arm { get; set; }

        /// <summary>
        /// Gets or sets Extension composition part
        /// </summary>
        public ExtensionCompositionPart Extension { get; set; }
    }

    /// <summary>
    /// ARM Composition Part
    /// </summary>
    public class ArmCompositionPart
    {
        /// <summary>
        /// Gets or sets the ARM template file path
        /// </summary>
        public string TemplatePath { get; set; }

        /// <summary>
        /// Gets or sets the ARM parameter file path
        /// </summary>
        public string ParametersPath { get; set; }
    }
}