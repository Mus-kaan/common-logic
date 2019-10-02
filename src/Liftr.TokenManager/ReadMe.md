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
    "ArmEndpoint": "https://management.core.windows.net/",
    "AadEndpoint": "https://login.windows-ppe.net",
    "TenantId": "f686d426-8d16-42db-81b7-ab578e110ccd"
  }
```
