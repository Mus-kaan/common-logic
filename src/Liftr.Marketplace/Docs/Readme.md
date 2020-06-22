This project contains the components needed for the Liftr Marketplace Integration.
There are three main parts of this projec1:

# Marketplace ARM Client

## Function
In case of Liftr, we are calling the Internal Marketplace APIs to create a Saas resource, when the user creates a new Liftr resource(for example, Liftr-Datadog or Liftr-Confluent). This client enables us to call the Marketplace Internal APIs to do the same.
The API spec of Marketplace can be found for [Dogfood](https://marketplaceapi.spza-internal.net/swagger/ui/index#!/SubscriptionResourceV2/SubscriptionResourceV2_Put)


## How to use ?

To use the `MarketplaceARMClient`:

1. Add the following line to your Startup.cs - `services.AddMarketplaceARMClient(Configuration);` (Please refer to latest code in [StartupExtensions](../StartupExtensions.cs))
2. Add the MarketplaceSaasOptions in the  appsettings.json or in Keyvault

### Credentials to Use
To call the Marketplace Internal APIs, Marketplace has whitelisted our **FirstPartyApp** to call their endpoint. The application id can be found [here](https://msazure.visualstudio.com/Liftr/_wiki/wikis/Liftr.wiki/42611/Monitoring-(log-forwarder-and-metrics-crawler)).

```
"MarketplaceARMClientOptions": {
    "API": {
      "Endpoint": "https://marketplaceapi.spza-internal.net", // Dogfood marketplace
      "ApiVersion": "2018-08-31"
    },
    "MarketplaceFPAOptions": {
      "ApplicationId": "055caf97-1b4f-4730-9f5d-acc24b707b06",
      "CertificateName": "FirstPartyAppCert",
      "TargetResource": "4c328f8a-1356-4991-883b-ff83cb17aba3", //Dogfood marketplace target resource
      "AadEndpoint": "https://login.windows-ppe.net",
      "TenantId": "f686d426-8d16-42db-81b7-ab578e110ccd" //Dogfood
    }
  },
```

# Marketplace Fulfillment Client

## Function
Marketplace Fulfillment is a set of APIs that are used by partners to communicate with Marketplace to perform functions on the Marketplace subscription like Delete, Activate, etc.
More details can be found [here](https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2).

## How to use ?

To use the `MarketplaceFulfillmentClient`:

1. Add the following line to your Startup.cs - `services.AddMarketplaceFulfillmentClient(Configuration);` (Please refer to latest code in [StartupExtensions](../StartupExtensions.cs))
2. Add the MarketplaceSaasOptions(according to the environment Dogfood or Production) in the  appsettings.json or in Keyvault. Here is an example: 

## Identity to Use
For Fulfillment we will be using the Service Principal that was provided in the [Technical Configuration](https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/create-new-saas-offer#technical-configuration) section of the offer when the offer was published.

The Service Principal will be created in Dogfood tenant for Dev and Dogfood and in AME for Production. This is the SP that will be added in the Technical Configuration when [publishing the offer](./Marketplace_Environment.md).
After creating this Service Principal, we need to [add the Certificate Based authentication](./Marketplace_Identities_And_Certificate.md) for it.

```
"MarketplaceSaasOptions": {
  "API": {
    "Endpoint": "https://marketplaceapi.spza-internal.net/",
    "ApiVersion": "2018-08-31"
  },
  "SaasOfferTechnicalConfig": {
    "ApplicationId": "bf901379-e946-48c2-a6fa-906ef12cb172", // Service Principal created in Dogfood tenant
    "CertificateName": "DatadogMPCert",
    "TargetResource": "4c328f8a-1356-4991-883b-ff83cb17aba3",
    "AadEndpoint": "https://login.windows-ppe.net",
    "TenantId": "f686d426-8d16-42db-81b7-ab578e110ccd" // Dogfood
  }
},
```

# Marketplace Billing Client

## Function
[Marketplace Billing](https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/saas-metered-billing) is a set of APIs that are used by partners to communicate with Marketplace to send custom usage details for the Marketplace Saas Subscription. For example, the partner can have a custom meter that every email sent over the first 100 emails will be charged at 1$ per email. In that case, the partner uses the billing apis to tell Marketplace that this subscription has used this much quantity for this meter so that marketplace will bill the user accordingly
More details can be found [here](hhttps://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/marketplace-metering-service-apis).

## How to use ?

To use the `MarketplaceBillingClient`:

1. Add the following line to your Startup.cs - `services.AddMarketplaceBillingClient(Configuration);`(Please refer to latest code in [StartupExtensions](../StartupExtensions.cs))
2. The BillingClient uses the same set of credentials as the FulfillmentClient as these are the ones that identify the partner to Marketplace.
2. Add the MarketplaceSaasOptions(according to the environment Dogfood or Production) in the  appsettings.json or in Keyvault. Here is an example: 

### Endpoints to use
The endpoints for Marketplace to be used can be found in [Marketplace Endpoint](./Marketplace_Environment.md)

## Identity to Use
We will be using the same identity as the `MarketplaceFulfillmentClient`

```
"MarketplaceSaasOptions": {
  "API": {
    "Endpoint": "https://marketplaceapi.spza-internal.net/",
    "ApiVersion": "2018-08-31"
  },
  "SaasOfferTechnicalConfig": {
    "ApplicationId": "bf901379-e946-48c2-a6fa-906ef12cb172",
    "CertificateName": "DatadogMPCert",
    "TargetResource": "4c328f8a-1356-4991-883b-ff83cb17aba3",
    "AadEndpoint": "https://login.windows-ppe.net",
    "TenantId": "f686d426-8d16-42db-81b7-ab578e110ccd" // Dogfood
  }
},
```

## Function
Marketplace Fulfillment is a set of APIs that are used by partners to communicate with Marketplace to perform functions on the Marketplace subscription like Delete, Activate, etc.
More details can be found [here](https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/pc-saas-fulfillment-api-v2).

## How to use ?

To use the `MarketplaceFulfillmentClient`:

1. Add the following line to your Startup.cs - `services.AddMarketplaceFulfillmentClient(Configuration);` (Please refer to latest code in [StartupExtensions](./StartupExtensions.cs))
2. Add the MarketplaceSaasOptions in the  appsettings.json or in Keyvault

### Endpoints to use
The endpoints for Marketplace to be used can be found in [Marketplace Endpoint](./Marketplace_Environment.md)

### Credentials to Use
We will be using a Service Principal that we create and provide to the partner. The Service Principal will be created in Dogfood tenant for Dev and Dogfood and in AME for Production.
After creating this Service Principal, we need to add the Certificate Based authentication for it.

```
"MarketplaceSaasOptions": {
  "API": {
    "Endpoint": "https://marketplaceapi.spza-internal.net/",
    "ApiVersion": "2018-08-31"
  },
  "SaasOfferTechnicalConfig": {
    "ApplicationId": "bf901379-e946-48c2-a6fa-906ef12cb172",
    "CertificateName": "DatadogMPCert",
    "TargetResource": "4c328f8a-1356-4991-883b-ff83cb17aba3",
    "AadEndpoint": "https://login.windows-ppe.net",
    "TenantId": "f686d426-8d16-42db-81b7-ab578e110ccd" // Dogfood
  }
},
```
