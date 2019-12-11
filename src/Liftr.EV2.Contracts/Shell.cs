//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.EV2.Contracts
{
    /// <summary>
    /// The Shell.
    /// </summary>
    public class Shell
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Justification = "Property names are chosen for brevity.")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the shell properties.
        /// </summary>
        public ShellProperties Properties { get; set; }

        /// <summary>
        /// Gets or sets the shell package.
        /// </summary>
        public ShellPackage Package { get; set; }

        /// <summary>
        /// Gets or sets the launch.
        /// </summary>
        public ShellLaunch Launch { get; set; }

        /// <summary>
        /// Gets or sets the image name.
        /// </summary>
        internal string ImageName { get; set; }

        /// <summary>
        /// Gets or sets the image version.
        /// </summary>
        internal string ImageVersion { get; set; }

        /// <summary>
        /// The number of processor cores.
        /// </summary>
        internal double Cpu { get; set; }

        /// <summary>
        /// The memory.
        /// </summary>
        internal double MemoryInGb { get; set; }
    }

    /// <summary>
    /// The shell properties.
    /// </summary>
    public class ShellProperties
    {
        /// <summary>
        /// Gets or sets the flag to indicate if the shell should be deleted after execution.
        /// </summary>
        public bool SkipDeleteAfterExecution { get; set; }

        /// <summary>
        /// Gets or sets the maximum execution time.
        /// </summary>
        public string MaxExecutionTime { get; set; }
    }

    /// <summary>
    /// The shell package.
    /// </summary>
    public class ShellPackage
    {
        /// <summary>
        /// Gets or sets the reference.
        /// </summary>
        public ParameterReference Reference { get; set; }
    }
}
