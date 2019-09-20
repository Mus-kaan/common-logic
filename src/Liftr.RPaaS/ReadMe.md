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
    "MetaRPAccessorClientId": "a31ac660-3fdf-4bae-a9de-53800bad6a83",
    "MetaRPAccessorClientSecret": "https://ms.portal.azure.com/#@MSAzureCloud.onmicrosoft.com/resource/subscriptions/d8f298fb-60f5-4676-a7d3-25442ec5ce1e/resourceGroups/do-not-delete-unit-test-rg/providers/Microsoft.KeyVault/vaults/no-delete-liftr-dev/secrets"
  }
```
