//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Liftr.Fluent.Provisioning;
using Microsoft.Liftr.KeyVault;
using System;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent.Tests
{
    internal static class TestEnvSetup
    {
        private const string c_testSshPublicKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQDIoUCnmwyMDFAf0Ia/OnCTR3g9uxp6uxU/"
            + "Sa4VwFEFpOmMH9fUZcSGPMlAZLtXYUrgsNDLDr22wXI8wd8AXQJTxnxmgSISENVVFntC+1WCETQFMZ4BkEeLCGL0s"
            + "CoAEKnWNjlE4qBbZUfkShGCmj50YC9R0zHcqpCbMCz3BjEGrqttlIHaYGKD1v7g2vHEaDj459cqyQw3yBr3l9erS6"
            + "/vJSe5tBtZPimTTUKhLYP+ZXdqldLa/TI7e6hkZHQuMOe2xXCqMfJXp4HtBszIua7bM3rQFlGuBe7+Vv+NzL5wJyy"
            + "y6KnZjoLknnRoeJUSyZE2UtRF6tpkoGu3PhqZBmx7 invalid-user@invalidmachine";

        public static async Task<IVault> SetupGlobalKeyVaultAsync(string resourceGroupName, ILiftrAzure az, KeyVaultClient kvClient)
        {
            var name = SdkContext.RandomResourceName("test-vault-", 15);
            var kv = await az.CreateKeyVaultAsync(TestCommon.Location, resourceGroupName, name, TestCommon.Tags);

            await kv.Update()
            .DefineAccessPolicy()
            .ForServicePrincipal(TestCredentials.ClientId)
            .AllowSecretAllPermissions()
            .AllowCertificateAllPermissions()
            .Attach()
            .ApplyAsync();

            using (var kvValet = new KeyVaultConcierge(kv.VaultUri, kvClient, az.Logger))
            {
                await kvValet.SetSecretAsync(InfrastructureV2.SSHUserNameSecretName, "testvmuser", TestCommon.Tags);
                await kvValet.SetSecretAsync(InfrastructureV2.SSHPasswordSecretName, Guid.NewGuid().ToString(), TestCommon.Tags);
                await kvValet.SetSecretAsync(InfrastructureV2.SSHPublicKeySecretName, c_testSshPublicKey, TestCommon.Tags);
            }

            return kv;
        }
    }
}
