# Monitoring Svc related Datasources

### How to use Monitoring Svc Datasources ?
Add below line of code in your start up
```
services.UseMonitoringSvcDataSources(Configuration, _logger);
```
### How to specify Configuration
You need to add below settings in your appsetting.json file
```
"Mongo": {
    "DatabaseName": "monitoringsvc-metadata",
    "VmExtensionDetailsEntityCollectionName": "metadata-vmext-entities",
    "EventHubSourceEntityCollectionName": "metadata-eh-entities",
    "MonitoredEntityCollectionName": "metadata-monitored-entities",
    "LogDBOperation": true
  }
```
### What all datasources it adds ?
It adds below 3 Datasources related to MonitoringSvc
   1. IMonitoringSvcEventEntityDataSource
   2. IMonitoringSvcVmExtensionDetailsEntityDataSource
   3. IMonitoringSvcMonitoredEntityDataSource
