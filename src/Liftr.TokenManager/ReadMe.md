The `Microsoft.Liftr.TokenManager` assembly provides TokenManager clients for AAD. It optimise token retrieval through caching of tokens.

# Registering and Configuring
To enable TokenManager, you need to call beloe method from within your ASP.NET Core startup class:
Call `UseTokenManager` in `Startup.ConfigureServices` and use the callback to configure trusted thumbprints and other behavior.

```cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.UseTokenManager(Configuration);
    }
}
```

Below are the sample TokenManagerConfiguration for dogfood
```
"TokenManagerConfiguration": {
    "ArmEndpoint": "https://api-dogfood.resources.windows-int.net",
    "AadEndpoint": "https://login.windows-ppe.net",
    "TenantId": "72f988bf-86f1-41af-91ab-2d7cd011db47"
  }
```
