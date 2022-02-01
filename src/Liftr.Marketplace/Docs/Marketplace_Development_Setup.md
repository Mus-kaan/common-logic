# What you need to know after marketplace offer configuration

1. Marketplace offer should be allowlisted on the list of offers on Marketplace RP to allow purchase of saas resources. [See here](https://dev.azure.com/msazure/Liftr/_git/Liftr.Common?path=/src/Liftr.Marketplace/Docs/Marketplace_Development_Setup.md&version=GBmaster&line=10&lineEnd=11&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents)

2. Marketplace offer should be allowlisted by the Portal team by setting `HideFromSaasBlade=True` flag to the offer properties [See here](https://dev.azure.com/msazure/Liftr/_git/Liftr.Common?path=/src/Liftr.Marketplace/Docs/Marketplace_Development_Setup.md&version=GBmaster&line=10&lineEnd=11&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents)

3. Redirect your marketplace offer create button to your liftr resource creation flow [See here](https://dev.azure.com/msazure/Liftr/_git/Liftr.Common?path=/src/Liftr.Marketplace/Docs/Marketplace_Development_Setup.md&version=GBmaster&line=21&lineEnd=21&lineStartColumn=1&lineEndColumn=74&lineStyle=plain&_a=contents)


## How to allowlist Marketplace offer on Marketplace RP

This involves adding the `offer_id` of your marketplace offer to a list of offers on Marketplace RP to allow validation of purchase for the saas resource. 
- Create an ICM ticket to Marketplace team requesting allowlisting of your offer. Set
**OWNING TEAM - Marketplace**

*This is an example [ICM TICKET](https://portal.microsofticm.com/imp/v3/incidents/details/271138618/home) showing the error that occurs when this step has not been done.*

## How to allowlist Marketplace offer with publishing team

This involves changing the properties of your offer by adding `HideFromSaasBlade=True` flag that allows the saas resource to be created as a liftr offer.
1. Create an ICM ticket to Marketplace Pubishing Team to add this flag. This should be done on the live offer. Set **OWNING TEAM - CPX-MIX-Ingestion Publishing Service**

2. Create an ICM ticket to Marketplace Pubishing Team to add `disableSendEmailOnPurchase=True` flag on your offer to disable receiving an email every time a new saas resouce is purchased. (Should be done for Liftr offers)

- See example [ICM Ticket](https://portal.microsofticm.com/imp/v3/incidents/details/270693746/home)

## How to redirect Marketplace offer creation flow to liftr creation flow

Setting the create button on the Marketplace offer to redirect to the create flow developed by the Liftr team.
- Create an ICM Ticket to Marketplace Portal team and request that they do this redirect for you. Set **OWNING TEAM - CPX-MIX- Ingestion Publishing Service**