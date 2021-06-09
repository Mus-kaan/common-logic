//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using NSG = Microsoft.Azure.Management.Network.Fluent.NetworkSecurityGroup.Definition;

namespace Microsoft.Liftr.Fluent
{
    public static class FluentExtensions
    {
        public static NSG.IWithCreate AllowVNet80TCPInBound(this NSG.IWithCreate nsgDefWithCreate, int priority = 3100)
        {
            if (nsgDefWithCreate == null)
            {
                throw new ArgumentNullException(nameof(nsgDefWithCreate));
            }

            return nsgDefWithCreate.DefineRule("AllowVNet80TCPInBound")
                    .AllowInbound()
                    .FromAddress("VirtualNetwork")
                    .FromAnyPort()
                    .ToAddress("VirtualNetwork")
                    .ToPort(80)
                    .WithProtocol(Azure.Management.Network.Fluent.Models.SecurityRuleProtocol.Tcp)
                    .WithPriority(priority)
                    .Attach();
        }

        public static NSG.IWithCreate AllowVNet443TCPInBound(this NSG.IWithCreate nsgDefWithCreate, int priority = 3200)
        {
            if (nsgDefWithCreate == null)
            {
                throw new ArgumentNullException(nameof(nsgDefWithCreate));
            }

            return nsgDefWithCreate.DefineRule("AllowVNet443TCPInBound")
                    .AllowInbound()
                    .FromAddress("VirtualNetwork")
                    .FromAnyPort()
                    .ToAddress("VirtualNetwork")
                    .ToPort(443)
                    .WithProtocol(Azure.Management.Network.Fluent.Models.SecurityRuleProtocol.Tcp)
                    .WithPriority(priority)
                    .Attach();
        }

        public static NSG.IWithCreate AllowAny80TCPInBound(this NSG.IWithCreate nsgDefWithCreate, int priority = 3300)
        {
            if (nsgDefWithCreate == null)
            {
                throw new ArgumentNullException(nameof(nsgDefWithCreate));
            }

            return nsgDefWithCreate.DefineRule("AllowAny80TCPInBound")
                    .AllowInbound()
                    .FromAnyAddress()
                    .FromAnyPort()
                    .ToAnyAddress()
                    .ToPort(80)
                    .WithProtocol(Azure.Management.Network.Fluent.Models.SecurityRuleProtocol.Tcp)
                    .WithPriority(priority)
                    .Attach();
        }

        public static NSG.IWithCreate AllowAny443TCPInBound(this NSG.IWithCreate nsgDefWithCreate, int priority = 3400)
        {
            if (nsgDefWithCreate == null)
            {
                throw new ArgumentNullException(nameof(nsgDefWithCreate));
            }

            return nsgDefWithCreate.DefineRule("AllowAny443TCPInBound")
                    .AllowInbound()
                    .FromAnyAddress()
                    .FromAnyPort()
                    .ToAnyAddress()
                    .ToPort(443)
                    .WithProtocol(Azure.Management.Network.Fluent.Models.SecurityRuleProtocol.Tcp)
                    .WithPriority(priority)
                    .Attach();
        }

        public static NSG.IWithCreate AllowAny5000TCPInBound(this NSG.IWithCreate nsgDefWithCreate, int priority = 3500)
        {
            if (nsgDefWithCreate == null)
            {
                throw new ArgumentNullException(nameof(nsgDefWithCreate));
            }

            return nsgDefWithCreate.DefineRule("AllowAny5000TCPInBound")
                    .AllowInbound()
                    .FromAnyAddress()
                    .FromAnyPort()
                    .ToAnyAddress()
                    .ToPort(5000)
                    .WithProtocol(Azure.Management.Network.Fluent.Models.SecurityRuleProtocol.Tcp)
                    .WithPriority(priority)
                    .Attach();
        }
    }
}
