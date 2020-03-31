The `Microsoft.Liftr.TokenManager` assembly provides TokenManager clients for AAD. It optimise token retrieval through caching of tokens.

# Registering and Configuring
To enable TokenManager, you need to provide the TokenManagerConfiguration.
For example for the MetaRPStorageClient we have the following TokenManagerConfiguration for Dogfood. 

```
"TokenManagerConfiguration": {
    "TargetResource": "https://management.core.windows.net/",
    "AadEndpoint": "https://login.windows-ppe.net",
    "TenantId": "f686d426-8d16-42db-81b7-ab578e110ccd"
 }
```

Every service that needs to authenticate with AAD should declare its own TokenManagerConfiguration (preferrably in the Options, for eg, `MetaRPOptions`)
and then get the token manager configuration during its initialization, to be passed on to the TokenManager.

For eg, for MetaRp we have the following MetaRPOptions
```
"MetaRPOptions": {
    "MetaRPEndpoint": "https://api-dogfood.resources.windows-int.net",
    "AccessorClientId": "0f84289f-2c8a-4ad8-8d43-da5fc98073f0",
    "AccessorCertificateName": "MetaRPAccessorCert",
    "TokenManagerConfiguration": {
      "TargetResource": "https://management.core.windows.net/",
      "AadEndpoint": "https://login.windows-ppe.net",
      "TenantId": "f686d426-8d16-42db-81b7-ab578e110ccd"
    }
  }
```

And to access the TokenManagerConfiguration we are using,
```cs
var tokenManagerConfiguration = sp.GetService<IOptions<MetaRPOptions>>().Value.TokenManagerConfiguration;
```

The TokenManager also needs a `CertificateStore` hence you need to add the following line in the Startup.cs in order to add a CertificateStore to the dependency injection container
```cs
services.AddSingleton<CertificateStore>();
```
