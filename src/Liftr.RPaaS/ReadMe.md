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
You have to first configure the token manager.
If you are not using the `UseKeyVaultProvider` web host builder extension, you have to
add a singleton for the key vault client manually in order to authenticate the key vault.
Below are the sample RPaaSConfiguration for dogfood.
```
"RPaaSConfiguration": {
    "MetaRPEndpoint": "https://api-dogfood.resources.windows-int.net",
    "MetaRPAccessorClientId": "0f84289f-2c8a-4ad8-8d43-da5fc98073f0",
    "MetaRPAccessorVaultEndpoint": "https://ibuild-rpdata-cus.vault.azure.net/",
	"MetaRPAccessorCertificateName": "MetaRPAccessorCert"
  }
```
