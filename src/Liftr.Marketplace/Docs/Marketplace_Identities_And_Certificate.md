# Identities

## Marketplace Internal APIs (MarketplaceARMClient)

For calling the Marketplace Internal APIs(/api/saasresources), we will be using a FirstPartyApp that has been whitelisted with Marketplace for all Liftr Partners. A common token service has been created to allow Liftr partners to authenticate their applications to Marketplace.

`Below are the steps to onboard to common token service:`

1. In hosting-options file under OneCertCertificates add new certificate for TokenService. (Create infra release for specific partner for this certificates to be added to respective KeyVault)

2. Create AAD-Application for authenticating requests or use a First Party Application.

3. If autheticating with  FPA skip to step 4.
   If using  AAD Application:
	- Run SNI tool for the generated certificate and AAD-Application's appId [see here](https://dev.azure.com/msazure/Liftr/_git/Liftr.Common?path=src/Liftr.Marketplace/Docs/Marketplace_Identities_And_Certificate.md&version=GCce89eeb7df6ea2e3d0f7c20aa2c486aaa134a6a6&line=21&lineStartColumn=1&lineEndColumn=65&_a=contents)

4. Whitelist the Application on Common token service [Sample PR](https://msazure.visualstudio.com/Liftr/_git/Liftr.Gateway/pullrequest/5095641)


## Marketplace Fulfillment and Billing APIs (MarketplaceFulfillmentClient, MarketplaceBillingClient)

1. For calling the Marketplace Fulfillment APIs, we will be using a Service Principal which will be created by us in ***Dogfood tenant for Dev and Dogfood and in AME tenant for Prod***. 
2. Once this is created we need to add Certificate Based Authentication to it. Steps below:

## Adding Certificate Based authentication for Service Principal

Steps can be found here: https://aadwiki.windows-int.net/index.php?title=Subject_Name_and_Issuer_Authentication#MSAL_API_for_Subject_Name_And_Issuer_Authentication

Example: Let us take an app which has following:

```
Tenant Id: f686d426-8d16-42db-81b7-ab578e110ccd
Object Id: 810e2960-21f1-4a8c-96b7-16d7d126e916
Subject Name to be added : liftr-monitoring.first-party.liftr-dev.net
```

Steps:

1. Download the SNIssuerConfigTool.
2. .`\SNIssuerConfig.exe addAppKey f686d426-8d16-42db-81b7-ab578e110ccd 810e2960-21f1-4a8c-96b7-16d7d126e916 AME liftr-monitoring.first-party.liftr-dev.net`
3. `.\SNIssuerConfig.exe getProperty f686d426-8d16-42db-81b7-ab578e110ccd 810e2960-21f1-4a8c-96b7-16d7d126e916`
4. For Dogfood tenant you need to change the config file in the folder you downloaded:
<?xml version="1.0" encoding="utf-8" ?>
	<configuration>
	    <startup> 
	        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5.2" />
	    </startup>
	    <appSettings>
	        <add key="STSUri" value="https://login.windows-ppe.net" />
	        <add key="GraphAPI" value="https://graph.ppe.windows.net" />
	        <add key="IsSilentLogin" value="false" />
	        <add key="SilentLoginUserName" value="" />
	        <add key="SilentLoginPassword" value="" />
	    </appSettings>
	</configuration> 

More details can be found [here](https://microsoft.sharepoint.com/teams/LiftrDev/_layouts/OneNote.aspx?id=%2Fteams%2FLiftrDev%2FSiteAssets%2FLiftrDev%20Notebook&wd=target%28Engineering.one%7CADB27C7A-8527-4301-9D5F-8368BE6488E4%2FCreating%20a%20Service%20Principal%20and%20associating%20it%20with%20Subject%20Name%7C86798C32-14D9-44F8-BC2A-AED8F4EE8062%2F%29)


