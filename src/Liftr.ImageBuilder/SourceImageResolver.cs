//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Liftr.Contracts;
using System;

namespace Microsoft.Liftr.ImageBuilder
{
    public static class SourceImageResolver
    {
        public static PlatformImageIdentifier ResolvePlatformSourceImage(SourceImageType imageType)
        {
            PlatformImageIdentifier image = null;

            if (!imageType.IsPlatformImage())
            {
                throw new InvalidOperationException($"Image type '{imageType}' is not a platform image.");
            }

            switch (imageType)
            {
                case SourceImageType.WindowsServer2016Datacenter:
                    {
                        image = new PlatformImageIdentifier()
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
                        image = new PlatformImageIdentifier()
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
                        image = new PlatformImageIdentifier()
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
                        image = new PlatformImageIdentifier()
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
                        image = new PlatformImageIdentifier()
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
                        image = new PlatformImageIdentifier()
                        {
                            Publisher = "MicrosoftWindowsServer",
                            Offer = "WindowsServer",
                            Sku = "2019-Datacenter-with-Containers",
                            Version = "latest",
                        };
                        break;
                    }

                case SourceImageType.UbuntuServer1804:
                    {
                        image = new PlatformImageIdentifier()
                        {
                            Publisher = "Canonical",
                            Offer = "UbuntuServer",
                            Sku = "18.04-LTS",
                            Version = "latest",
                        };
                        break;
                    }

                // case SourceImageType.RedHat7LVM:
                //    {
                //        image = new PlatformImageIdentifier()
                //        {
                //            Publisher = "RedHat",
                //            Offer = "RHEL",
                //            Sku = "7-LVM",
                //            Version = "latest",
                //        };
                //        break;
                //    }

                // case SourceImageType.CentOS:
                //    {
                //        image = new PlatformImageIdentifier()
                //        {
                //            Publisher = "CoreOS",
                //            Offer = "CoreOS",
                //            Sku = "Stable",
                //            Version = "latest",
                //        };
                //        break;
                //    }
                default:
                    throw new InvalidOperationException("The source image is not supported: " + imageType.ToString());
            }

            return image;
        }
    }
}
