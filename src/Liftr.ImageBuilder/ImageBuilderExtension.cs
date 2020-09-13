//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ImageBuilder
{
    public static class ImageBuilderExtension
    {
        public static Func<CallbackParameters, Task> AfterBakeImageAsync { get; set; }

        public static Func<CallbackParameters, Task> AfterImportImageAsync { get; set; }
    }
}
