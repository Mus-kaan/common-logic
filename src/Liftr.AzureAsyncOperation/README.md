# Azure ARM async operation documentation
Full detailed documentation is described here. https://github.com/Azure/azure-resource-manager-rpc/blob/783f3f4a108215dd57ed449d0b4406f913480757/v1.0/Addendum.md#operation-resource-format

# How to use this lib in your code
## 1. Add AsyncOperationProvider instance, this depends on MongoCollection factory
```
    services.AddSingleton<IAsyncOperationStatusDataSource, AsyncOperationStatusDataSource>((sp) =>
    {
        var timeSource = sp.GetService<ITimeSource>();
        var options = sp.GetService<IOptions<AzureAsyncOperationOptions>>().Value;
        var factory = sp.GetService<MongoCollectionsFactory>();
        IMongoCollection<AsyncOperationStatusEntity> collection = null;
        try
        {
            collection = factory.GetCollection<AsyncOperationStatusEntity>(options.AsyncOperationCollectionName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.Error(ex, "Collection doesn't exist.");

            // Fall back to create the collection.
            // TODO: make sure this will only happen for dev environment.
#pragma warning disable CS0618 // Type or member is obsolete
            collection = factory.CreateCollection<AsyncOperationStatusEntity>(options.AsyncOperationCollectionName);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        return new AsyncOperationStatusDataSource(collection);
    });
    services.AddSingleton<IAsyncOperationProvider, AsyncOperationProvider>()
```
## 2. Create async operation in controlled whichever controlled API you want to run as async, return the status "Accepted"
```
    [HttpPut]
    [Route(IncrediBuildRPConstants.EntityRoute)]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IncrediBuildResource))]
    [ProducesResponseType((int)HttpStatusCode.Created, Type = typeof(IncrediBuildResource))]
    [SwaggerRequestExample(typeof(IncrediBuildResource), typeof(IncrediBuildResourceExample))]
    public async Task<IActionResult> CreateAsync(string subscriptionId, string resourceGroupName, string clusterName, IncrediBuildResource cluster)
    {
        var asyncOperation = await _asyncOperationProvider.CreateAsync(subscriptionId, IncrediBuildRPConstants.ProviderName, HttpContext, retryAfter: 5);
        var _ = _businessLogic.CreateClusterAsync(subscriptionId, resourceGroupName, clusterName, cluster, asyncOperation);
        return Accepted();
    }
```

## 3. Use asyncOperationProvider instance to update status of operation as below.
```
public async Task CreateClusterAsync(
    string subscriptionId,
    string resourceGroup,
    string resourceName,
    IncrediBuildResource resource,
    AsyncOperationStatusEntity asyncOperation)
{
    await _asyncOperationStatusDataSource.UpdateAsync(asyncOperation.OperationId, OperationStatus.Running);
    await Task.Delay(60);
    await _asyncOperationStatusDataSource.UpdateAsync(asyncOperation.OperationId, OperationStatus.Succeeded);
    await Task.FromResult("PLACEHOLDER");
}
```
## 4. Just defined OperationsController.cs class inherited from AsyncOperationBaseController.cs, with API version
```
public class OperationStatusController : AsyncOperationsBaseController
{
    public OperationStatusController(IAsyncOperationProvider asyncOperationProvider)
        : base(asyncOperationProvider)
    {
    }
}
```


