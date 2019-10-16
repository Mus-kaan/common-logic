﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using NSG = Microsoft.Azure.Management.Network.Fluent.NetworkSecurityGroup.Definition;

namespace Microsoft.Liftr.Fluent
{
    public static class FluentExtensions
    {
        public static NSG.IWithCreate AllowVNet80InBound(this NSG.IWithCreate nsgDefWithCreate, int priority = 3100)
        {
            if (nsgDefWithCreate == null)
            {
                throw new ArgumentNullException(nameof(nsgDefWithCreate));
            }

            return nsgDefWithCreate.DefineRule("AllowVNet80InBound")
                    .AllowInbound()
                    .FromAddress("VirtualNetwork")
                    .FromAnyPort()
                    .ToAddress("VirtualNetwork")
                    .ToPort(80)
                    .WithAnyProtocol()
                    .WithPriority(priority)
                    .Attach();
        }

        public static NSG.IWithCreate AllowVNet443InBound(this NSG.IWithCreate nsgDefWithCreate, int priority = 3200)
        {
            if (nsgDefWithCreate == null)
            {
                throw new ArgumentNullException(nameof(nsgDefWithCreate));
            }

            return nsgDefWithCreate.DefineRule("AllowVNet443InBound")
                    .AllowInbound()
                    .FromAddress("VirtualNetwork")
                    .FromAnyPort()
                    .ToAddress("VirtualNetwork")
                    .ToPort(443)
                    .WithAnyProtocol()
                    .WithPriority(priority)
                    .Attach();
        }

        public static NSG.IWithCreate AllowAny80InBound(this NSG.IWithCreate nsgDefWithCreate, int priority = 3300)
        {
            if (nsgDefWithCreate == null)
            {
                throw new ArgumentNullException(nameof(nsgDefWithCreate));
            }

            return nsgDefWithCreate.DefineRule("AllowAny80InBound")
                    .AllowInbound()
                    .FromAnyAddress()
                    .FromAnyPort()
                    .ToAddress("VirtualNetwork")
                    .ToPort(80)
                    .WithAnyProtocol()
                    .WithPriority(priority)
                    .Attach();
        }

        public static NSG.IWithCreate AllowAny443InBound(this NSG.IWithCreate nsgDefWithCreate, int priority = 3400)
        {
            if (nsgDefWithCreate == null)
            {
                throw new ArgumentNullException(nameof(nsgDefWithCreate));
            }

            return nsgDefWithCreate.DefineRule("AllowAny443InBound")
                    .AllowInbound()
                    .FromAnyAddress()
                    .FromAnyPort()
                    .ToAddress("VirtualNetwork")
                    .ToPort(443)
                    .WithAnyProtocol()
                    .WithPriority(priority)
                    .Attach();
        }
    }
}