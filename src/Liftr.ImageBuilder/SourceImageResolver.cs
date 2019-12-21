//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System;

namespace Microsoft.Liftr.ImageBuilder
{
    public static class SourceImageResolver
    {
        public static PlatformImageIdentifier ResolveWindowsSourceImage(SourceImageType imageType)
        {
            PlatformImageIdentifier windowsSourceImage = null;

            if (imageType <= SourceImageType.WindowsServer2019DatacenterContainers)
            {
                switch (imageType)
                {
                    case SourceImageType.WindowsServer2016Datacenter:
                        {
                            windowsSourceImage = new PlatformImageIdentifier()
                            {
                                Publisher = "MicrosoftWindowsServer",
                                Offer = "WindowsServer",
                                Sku = "2016-Datacenter",
                                Version = "latest",
                            };
                            break;
                        }

                    case SourceImageType.WindowsServer2016DatacenterCore:
                        {
                            windowsSourceImage = new PlatformImageIdentifier()
                            {
                                Publisher = "MicrosoftWindowsServer",
                                Offer = "WindowsServer",
                                Sku = "2016-Datacenter-Core",
                                Version = "latest",
                            };
                            break;
                        }

                    case SourceImageType.WindowsServer2016DatacenterContainers:
                        {
                            windowsSourceImage = new PlatformImageIdentifier()
                            {
                                Publisher = "MicrosoftWindowsServer",
                                Offer = "WindowsServer",
                                Sku = "2016-Datacenter-with-Containers",
                                Version = "latest",
                            };
                            break;
                        }

                    case SourceImageType.WindowsServer2019Datacenter:
                        {
                            windowsSourceImage = new PlatformImageIdentifier()
                            {
                                Publisher = "MicrosoftWindowsServer",
                                Offer = "WindowsServer",
                                Sku = "2019-Datacenter",
                                Version = "latest",
                            };
                            break;
                        }

                    case SourceImageType.WindowsServer2019DatacenterCore:
                        {
                            windowsSourceImage = new PlatformImageIdentifier()
                            {
                                Publisher = "MicrosoftWindowsServer",
                                Offer = "WindowsServer",
                                Sku = "2019-Datacenter-Core",
                                Version = "latest",
                            };
                            break;
                        }

                    case SourceImageType.WindowsServer2019DatacenterContainers:
                        {
                            windowsSourceImage = new PlatformImageIdentifier()
                            {
                                Publisher = "MicrosoftWindowsServer",
                                Offer = "WindowsServer",
                                Sku = "2019-Datacenter-with-Containers",
                                Version = "latest",
                            };
                            break;
                        }

                    default:
                        throw new InvalidOperationException("The windows source image is not supported: " + imageType.ToString());
                }
            }

            return windowsSourceImage;
        }
    }
}
