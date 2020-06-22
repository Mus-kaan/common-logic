# Marketplace Environment to use

## Dev and Dogfood
For our Dev and Dogfood, we will be using the Microsoft Dogfood tenant.

```
Marketplace Uri: https://marketplaceapi.spza-internal.net
API-Version: 2018-08-31
Target resource: 4c328f8a-1356-4991-883b-ff83cb17aba3
AAD endpoint: https://login.windows-ppe.net
```
## Production

For production, we will be using the Microsoft AME tenant.

```
Marketplace Uri : https://marketplaceapi.microsoft.com
API-Version: 2018-08-31
Target resource: 62d94f6c-d599-489b-a797-3e10e42fbe22
AAD endpoint: https://login.microsoftonline.com
```
# Marketplace Offer in Partner Center

Details on how to publish the offer can be found In the [Marketplace Saas Documentation](https://docs.microsoft.com/en-us/azure/marketplace/partner-center-portal/create-new-saas-offer)

## Dev and Dogfood

For Dev and Dogfood, we will be publishing a offer in which:

1. Webhook Url will be pointing to our Dev or Dogfood AKS cluster
2. In the **Technical Configuration** section, we will be adding a [Service Principal](./Marketplace_Identities_And_Certificate.md) which was created in the Microsoft Dogfood tenant. This is because we will be calling the AAD endpoint to get the token needed for calling the fulfillment apis. Once created we need to add Certificate Based Authentication for it.
3. Once you have created this offer, you might need to ask Marketplace team to add this to the offers available in Dogfood environment, or you can try to create the offer in the [PPE Partner Center](https://partner.microsoft-ppe.com/en-us/dashboard/commercial-marketplace/overview)

```
Note: If you are creating offers which have non-zero costs, then you will need a Pay-As-You-Go Subscription (PAYG)
```

## Production

For production, the partner will be publishing the offer using:

1. Webhook url that will point to the webhook in our Production AKS cluster
2. Service Principal that we will provide and manage. This Service Principal will be created in the AME tenant. Once created we will also be adding Certificate Based authentication to it so that we can use Certs present in our Keyvault to get the token to call the Fulfillment apis.

```
Note: If you are creating offers which have non-zero costs, then you will need a Pay-As-You-Go Subscription (PAYG)
```



