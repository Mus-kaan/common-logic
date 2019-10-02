The `Microsoft.Liftr.RPaaS` assembly provides RPaaS clients for accessing resource pool. It queries resources from MetaRP

# Registering and Configuring
To enable MetaRPStorageClient, you need to call beloe method from within your ASP.NET Core startup class:
Call `UseMetaRPStorageClient` in `Startup.ConfigureServices` and use the callback to configure trusted thumbprints and other behavior.

```cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.UseMetaRPStorageClient(Configuration);
    }
}
```

Below are the sample RPaaSConfiguration for dogfood
```
"RPaaSConfiguration": {
    "MetaRPEndpoint": "https://api-dogfood.resources.windows-int.net",
    "MetaRPAccessorClientId": "0f84289f-2c8a-4ad8-8d43-da5fc98073f0",
    "MetaRPAccessorClientSecret": "https://ms.portal.azure.com/#@MSAzureCloud.onmicrosoft.com/asset/Microsoft_Azure_KeyVault/Secret/https://app-managementplane-test.vault.azure.net/secrets/monitoringmanagement-RPaaSConfiguration--MetaRPAccessorClientSecret/39ca8f0247ca455d8829bc815e0adf79"
  }
```
