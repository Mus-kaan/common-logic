//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using CommandLine;

namespace Microsoft.Liftr.EV2
{
    public class RunnerCommandOptions
    {
        [Option('i', "input", Required = true, HelpText = "Path to the ev2 artifact options file.")]
        public string InputFile { get; set; }

        [Option('o', "output", Required = true, HelpText = "Out directory path.")]
        public string OuputDir { get; set; }
    }
}
