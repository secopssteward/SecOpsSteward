# ---
#region Dependency Functions
function Get-RandomAlphaNumeric { param([int]$length=8) Write-Output (-join (((48..57)+(65..90)+(97..122)) * 80 |Get-Random -Count $length |%{[char]$_})) }
# From https://gist.github.com/Badabum/a61e49019fb96bef4d5d9712e07b2af7
function Join-Objects($source, $extend){
    if($source.GetType().Name -eq "PSCustomObject" -and $extend.GetType().Name -eq "PSCustomObject"){
        foreach($Property in $source | Get-Member -type NoteProperty, Property){
            if($extend.$($Property.Name) -eq $null){
              continue;
            }
            $source.$($Property.Name) = Join-Objects $source.$($Property.Name) $extend.$($Property.Name)
        }
    }else{
       $source = $extend;
    }
    # check for an array type. powershell will convert this to a primitive if it is an array of fewer than 2 values
    if($source.GetType().Name -eq "Object[]" -and $source.Count -lt 2){
        return ,$source
    }else{
        return $source
    }
}
function AddPropertyRecurse($source, $toExtend){
    if($source.GetType().Name -eq "PSCustomObject"){
        foreach($Property in $source | Get-Member -type NoteProperty, Property){
            if($toExtend.$($Property.Name) -eq $null){
              $toExtend | Add-Member -MemberType NoteProperty -Value $source.$($Property.Name) -Name $Property.Name `
            }
            else{
               $toExtend.$($Property.Name) = AddPropertyRecurse $source.$($Property.Name) $toExtend.$($Property.Name)
            }
        }
    }
    return $toExtend
}
function Json-Merge($source, $extend){
    $merged = Join-Objects $source $extend
    return $merged
}
#endregion
# ---

Write-Host "### Writing appSettings.Development.json" -ForegroundColor Cyan
    Write-Host "Reading in existing (or creating) appsettings.Development.json"

    $appSettingsPath="..\src\UI\SecOpsSteward.UI\appsettings.Development.json"
    if (!(Test-Path $appSettingsPath)) { "{}" | Out-File $appSettingsPath -Force }
    $existing = Get-Content -Path $appSettingsPath -Raw | ConvertFrom-Json

    $DeploymentId = "deploymentid"
    $subId = "subid"
    $tenantId = "tenantId"
    $ApplicationId = "appId"
    $ApplicationSecret = "appsecret"
    $ResourceGroup = "rg"
    $sqlAdmininstratorLogin = "sqllogin"
    $sqlAdministratorPassword = "sqlpass"
    $storageKey = "storageKey"

    $DeploymentIdLower = $DeploymentId.ToLower()
    $newValues = @'
{
"AzureAd": {
"Instance": "https://login.microsoftonline.com/",
"TenantId": "$tenantId",
"ClientId": "$ApplicationId",
"ClientSecret": "$ApplicationSecret",
"CallbackPath": "/signin-oidc"
},
"Chimera": {
"SubscriptionId": "$subId",
"VaultName": "sosvault$DeploymentId",
"PackageRepoAccount": "sosblobs$DeploymentIdLower",
"PackageRepoContainer": "packages",
"ServiceBusNamespace": "sosbus$DeploymentId",
"ResourceGroup": "$ResourceGroup"
},
"ConnectionStrings": {
"Database": "Data Source=tcp:sossql$DeploymentId.database.windows.net,1433;Initial Catalog=sosdb;User Id=$sqlAdmininstratorLogin@sossql$DeploymentId.database.windows.net;Password=$sqlAdministratorPassword;",
"AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=sosblob$DeploymentId;AccountKey=$storageKey;EndpointSuffix=core.windows.net"
}
}
'@ | ConvertFrom-Json;

    Write-Host "Merging JSON objects" -ForegroundColor Cyan
    $newFile = Json-Merge $existing $newValues | ConvertTo-Json

Write-Host $newFile