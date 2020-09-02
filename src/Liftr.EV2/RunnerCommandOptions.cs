//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using CommandLine;

namespace Microsoft.Liftr.EV2
{
    public class RunnerCommandOptions
    {
        [Option("hostingOptionsFile", Required = false, HelpText = "Path to the hosting options file.")]
        public string HostingOptionsFile { get; set; }

        [Option("ev2OptionsFile", Required = false, HelpText = "Path to the ev2 artifact options file.")]
        public string HostingEV2OptionsFile { get; set; }

        [Option("ev2ImgOptionsFile", Required = true, HelpText = "Path to the image builder ev2 artifact options file.")]
        public string ImageBuilderEV2OptionsFile { get; set; }

        [Option('o', "output", Required = true, HelpText = "Out directory path.")]
        public string OuputDir { get; set; }
    }
}
