The `Microsoft.Liftr.RPaaS` assembly provides RPaaS clients for accessing resource pool. It queries resources from MetaRP

# Registering and Configuring
To enable MetaRPStorageClient, you need to call beloe method from within your ASP.NET Core startup class:
Call `AddMetaRPClient` in `Startup.ConfigureServices` and use the callback to configure trusted thumbprints and other behavior.
[See the 'SampleWebApp' for example](https://msazure.visualstudio.com/Liftr/_git/Liftr.Common?path=%2Fsrc%2FSampleWebApp%2FStartup.cs&version=GBmaster&line=33&lineStyle=plain&lineEnd=46&lineStartColumn=1&lineEndColumn=53).

```cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMetaRPClient(Configuration);
    }
}
```
You have to first configure the token manager.
If you are not using the `UseKeyVaultProvider` web host builder extension, you have to
add a singleton for the key vault client manually in order to authenticate the key vault.
Below are the sample RPaaSConfiguration for dogfood.

[See the 'SampleWebApp' for example](https://msazure.visualstudio.com/Liftr/_git/Liftr.Common?path=%2Fsrc%2FSampleWebApp%2Fappsettings.json&version=GBmaster&line=41&lineStyle=plain&lineEnd=50&lineStartColumn=1&lineEndColumn=4).
```
"MetaRPOptions": {
    "MetaRPEndpoint": "https://api-dogfood.resources.windows-int.net",
    "AccessorClientId": "0f84289f-2c8a-4ad8-8d43-da5fc98073f0",
	"AccessorCertificateName": "MetaRPAccessorCert"
  }
```
