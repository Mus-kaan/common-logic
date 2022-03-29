[CmdletBinding()]
param (
    $ACRNAME
)

function Update-Images ([System.String]$ACRName, [System.String]$ACRRepoName, [System.String]$ImageVersion, [System.String]$SourceACR) {
    Write-Host "Importing $ACRRepoName Version: $ImageVersion"
    az acr import --name $ACRName --source "${SourceACR}.azurecr.io/${ACRRepoName}:${ImageVersion}" --force
    az acr import --name $ACRName --source "${ACRName}.azurecr.io/${ACRRepoName}:latest" -t "${ACRRepoName}:stable" --force
    az acr import --name $ACRName --source "${ACRName}.azurecr.io/${ACRRepoName}:${ImageVersion}" -t "${ACRRepoName}:latest" --force
}

function Check-And-Import-Images ([System.String]$ACRName, [System.String]$ACRRepoName, [System.String]$ImageVersion, [System.String]$SourceACR) {
    [hashtable]$errorAndWarning = @{}
    $ErrorString = ""
    $IsWarning = $false
    $imageTags = $(az acr repository show-tags -n $ACRName --repository $ACRRepoName -o tsv)
    $presentInLiftrACR = $false
    Write-Host "Checking if the $ACRRepoName image versioned $ImageVersion already exists in the Liftr ACR..."
    foreach ($tag in $imageTags){
        if ($tag -eq $ImageVersion){
            $IsWarning = $true
            Write-Host "##vso[task.logissue type=warning;]$ACRRepoName image already exists in the Liftr ACR. "
            $presentInLiftrACR = $true
            break
        }
    }
    if($presentInLiftrACR -eq $false){
        Write-Host "The entered $ACRRepoName version $ImageVersion does not exist in the Liftr ACR."
        Write-Host "Checking if the $ACRRepoName image versioned $ImageVersion exists in the Geneva ACR..."
        docker manifest inspect "${SourceACR}.azurecr.io/${ACRRepoName}:${ImageVersion}"
        if($? -eq $false){
            $ErrorString = "$ACRRepoName image does not exist in the Geneva ACR. "
        }
        else{
            Update-Images -ACRName $ACRName -ACRRepoName $ACRRepoName -ImageVersion $ImageVersion -SourceACR $SourceACR
        }
    }
    $errorAndWarning.IsWarning = $IsWarning
    $errorAndWarning.ErrorString = $ErrorString
    return $errorAndWarning
}

function Import-Geneva-Images ([System.String]$ACRName) {
    Write-Host "ACR Name: $ACRName"
    $mdmImageImportResult = Check-And-Import-Images -ACRName $ACRName -ACRRepoName "genevamdm" -ImageVersion ${env:MDM_VERSION} -SourceACR "linuxgeneva-microsoft"
    $mdsdImageImportResult = Check-And-Import-Images -ACRName $ACRName -ACRRepoName "genevamdsd" -ImageVersion ${env:MDSD_VERSION} -SourceACR "linuxgeneva-microsoft"
    $fluentdImageImportResult = Check-And-Import-Images -ACRName $ACRName -ACRRepoName "genevafluentd_td-agent" -ImageVersion ${env:FLUENTD_VERSION} -SourceACR "linuxgeneva-microsoft"
    $secpackImageImportResult = Check-And-Import-Images -ACRName $ACRName -ACRRepoName "genevasecpackinstall" -ImageVersion ${env:SEC_PACK_VERSION} -SourceACR "linuxgeneva-microsoft"
    $prommdmconverterImageImportResult = Check-And-Import-Images -ACRName $ACRName -ACRRepoName "shared/prom-mdm-converter" -ImageVersion ${env:PROM_MDM_CONVERTER_VERSION} -SourceACR "liftrmsacr"
    $errorString = $mdmImageImportResult.ErrorString + $mdsdImageImportResult.ErrorString + $fluentdImageImportResult.ErrorString + $secpackImageImportResult.ErrorString + $prommdmconverterImageImportResult.ErrorString
    if($errorString -ne ""){
        Write-Error "$errorString Please re-run with correct versions."
    }
    $isWarning = $mdmImageImportResult.IsWarning -or $mdsdImageImportResult.IsWarning -or $fluentdImageImportResult.IsWarning -or $secpackImageImportResult.IsWarning -or $prommdmconverterImageImportResult.IsWarning
    if($isWarning -eq $true){
        Write-Host "##vso[task.complete result=SucceededWithIssues;]Please re-run with correct versions."
    }
}

Import-Geneva-Images -ACRName $ACRNAME