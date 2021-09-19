<#
  .SYNOPSIS
  Deploys application code and/or infrastructure for the SecOps Steward application

  .DESCRIPTION
  Creates Azure infrastructure via a Bicep file, creates roles and RBAC rules, then builds and deploys the current code.

  .PARAMETER Location
  Location for resources created as a part of this operation

  .PARAMETER ResourceGroup
  Resource group to contain resources which run this instance of SoS

  .PARAMETER ApplicationId
  Azure AD application (client) ID

  .PARAMETER ApplicationSecret
  Azure AD application secret

  .PARAMETER ApplicationName
  Will search for an application with this name, or create an application with this name
  if none currently exists.

  .PARAMETER WebAppName
  URL name of SecOps Steward UI application. This will result in <name>.azurewebsites.net

  .PARAMETER SqlAdministratorLogin
  SQL Server administrator user name (the password will be generated)

  .PARAMETER DeploymentId
  Unique identifier to append to resources and the resource group, to correlate objects.
  If not present, it will be randomly generated.

  .PARAMETER HideSecrets
  If provided, the script will not show passwords or other sensitive info.

  .PARAMETER SkipInfrastructure
  If provided, the script will not deploy the Azure infrastructure Bicep file.
  Documentation shows this as having a switch parameter, but in practice, just specifying
  the option is sufficient to enable it.

  .PARAMETER IAmADeveloper
  If provided, RBAC roles will be created to allow the running user to run the web UI
  locally but still be able to connect to created cloud resources. This will also write
  an appsettings.Development.json to the Web UI, to allow a local instance to communicate
  with cloud-provisioned resources.
  This should ONLY be used by developers trying to enhance or debug the application.
  Documentation shows this as having a switch parameter, but in practice, just specifying
  the option is sufficient to enable it.

  .INPUTS
  None

  .OUTPUTS
  This script does not generate files but will provide output via the console for use
  in populating values in other files, such as appsettings.Development.Json

  .EXAMPLE
  PS> .\deploy.ps1

  .EXAMPLE
  PS> .\deploy.ps1 -IAmADeveloper

  .EXAMPLE
  PS> .\deploy.ps1 -DeploymentId abcdefg -SkipInfrastructure
#>

param (
    [String]$Location = "westus", 
    [String]$ResourceGroup = $null, 
    [String]$ApplicationId = $null, 
    [String]$ApplicationSecret = $null, 
    [String]$ApplicationName = "SecOpsSteward",
    [String]$WebAppName = "soswebapp", 
    [String]$SqlAdministratorLogin = "sossqladmin",
    [String]$AdminUserPrincipal = $null,
    [String]$DeploymentId = $null, # random string to append to all resources
    [switch]$HideSecrets = $false,
    [switch]$SkipInfrastructure = $false,
    [switch]$IAmADeveloper = $false
)

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
    return $source
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
    $extended = AddPropertyRecurse $merged $extend
    return $extended
}
#endregion
# ---

if (!$DeploymentId) { $DeploymentId = (Get-RandomAlphaNumeric).ToLower() }

Write-Host @'

    @@@@@@@@  @@@@@@@@  @@@@@@@@     
    @@@  @@@  @@@  @@@  @@@  @@@     
    @@@  @@@  @@@  @@@  @@@  @@@     
    @@@       @@@  @@@  @@@         SecOps Steward
    @@@@@@@@  @@@  @@@  @@@@@@@@    System Deployment Tool  
         @@@  @@@  @@@       @@@
    @@@  @@@  @@@  @@@  @@@  @@@    Empowering administrators to
    @@@@ @@@  @@@  @@@  @@@ @@@@    cultivate a more secure cloud!
      @@@@@@  @@@  @@@  @@@@@@@
          @@  @@@  @@@  @@
              @@@  @@@
                 @@

'@ -ForegroundColor Blue

Write-Host "Beginning deployment of SecOps Steward..." -ForegroundColor Green

# generate a random RG if none is given
if ([string]::IsNullOrEmpty($ResourceGroup)) { $ResourceGroup = "soswebapp-" + $DeploymentId }

# generate a random password if none is given
$SqlAdministratorLoginPassword = Get-RandomAlphaNumeric -length 24

# get info for the current "az" user.
# this user will receive the first set of permissions for the Key Vault (admin, contributor, user access admin)

if ([string]::IsNullOrEmpty($AdminUserPrincipal)) {
    $signedInUser = $(az ad signed-in-user show | ConvertFrom-Json)
    $signedInUserName = $signedInUser.userPrincipalName
    $signedInUserObjectId = $signedInUser.objectId
}
else
{
    $signedInUserName = "Manually input"
    $signedInUserObjectId = $AdminUserPrincipal
}

#region AAD App Setup
# Create AAD app if ID is not specified
if ([string]::IsNullOrEmpty($ApplicationId))
{
    # Check if the app exists under the given name instead of the ID
    $existingApps = az ad app list --filter "displayname eq '${ApplicationName}'" | ConvertFrom-Json
    if ($existingApps)
    {
        $ApplicationId = $existingApps[0].appId
        Write-Host "### AAD Application Exists, using " -NoNewline -ForegroundColor Cyan
        Write-Host ${ApplicationId} -ForegroundColor Green
    }
    else
    {
        Write-Host "### Creating New AAD Application " -ForegroundColor Cyan

        # (...below resources in order...)
        # Azure Key Vault => user_impersonation
        # Azure Service Management => user_impersonation
        # Azure Storage => user_impersonation
        # Microsoft Graph => user.read
        $requiredResources = @'
        [
            {
                "resourceAppId": "e406a681-f3d4-42a8-90b6-c2b029497af1",
                "resourceAccess": [
                    {
                        "id": "03e0da56-190b-40ad-a80c-ea378c433f7f",
                        "type": "Scope"
                    }
                ]
            },
            {
                "resourceAppId": "cfa8b339-82a2-471a-a3c9-0fc0be7a4093",
                "resourceAccess": [
                    {
                        "id": "f53da476-18e3-4152-8e01-aec403e6edc0",
                        "type": "Scope"
                    }
                ]
            },
            {
                "resourceAppId": "797f4846-ba00-4fd7-ba43-dac1f8f63013",
                "resourceAccess": [
                    {
                        "id": "41094075-9dad-400e-a0bd-54e686782033",
                        "type": "Scope"
                    }
                ]
            },
            {
                "resourceAppId": "00000003-0000-0000-c000-000000000000",
                "resourceAccess": [
                    {
                        "id": "e1fe6dd8-ba31-4d61-89e7-88639da4683d",
                        "type": "Scope"
                    }
                ]
            }
        ]
'@ | ConvertTo-Json

        # Create application with resource definitions above
        $adApp = az ad app create `
        --display-name "${sosAadApplicationName}" `
        --reply-urls "https://${WebAppName}.azurewebsites.net/signin-oidc" `
        --required-resource-accesses $requiredResources

        $ApplicationId = $adApp[0].appId
    }
}

# Create a new app secret if one was not specified
if ([string]::IsNullOrEmpty($ApplicationSecret))
{
    Write-Host "### Generating AAD Application Secret " -ForegroundColor Cyan
    $ApplicationSecret = Get-RandomAlphaNumeric -length 20
    az ad app credential reset `
        --id $ApplicationId --append `
        --credential-description "Deployed WebUI" `
        --password $ApplicationSecret
    if ($LastExitCode -ne 0)
    {
        Write-Host "Application client secret creation failed!"
        return
    }
}
else { Write-Host "### AAD Application Secret was provided, skipping generation" -ForegroundColor Cyan }
#endregion

#region Deploy Infrastructure
if (!$SkipInfrastructure)
{
    Write-Host "### Deploying Azure Infrastructure " -ForegroundColor Cyan
    Write-Host "Deployment ID:`t`t" -nonewline -ForegroundColor Red
    Write-Host $DeploymentId
    Write-Host "Location:`t`t" -nonewline -ForegroundColor Green
    Write-Host $Location
    Write-Host "Primary Admin:`t`t" -nonewline -ForegroundColor Green 
    Write-Host $signedInUserName
    Write-Host "Primary Admin OID:`t" -nonewline -ForegroundColor Green 
    Write-Host $signedInUserObjectId
    Write-Host "Resource Group:`t`t" -nonewline -ForegroundColor Green
    Write-Host $ResourceGroup
    Write-Host "App ID:`t`t`t" -nonewline -ForegroundColor Green
    Write-Host $ApplicationId
    Write-Host "WebApp Name:`t`t" -nonewline -ForegroundColor Green
    Write-Host $WebAppName".azurewebsites.net"

    if (!$HideSecrets)
    {
        Write-Host "SQL Admin User:`t`t" -nonewline -ForegroundColor Green
        Write-Host $SqlAdministratorLogin
        Write-Host "SQL Admin Password:`t" -nonewline -ForegroundColor Green
        Write-Host $SqlAdministratorLoginPassword
    }

    # Debug roles are RBAC roles assigned to the principal of the user running this tool
    # This should only be used for developers if they are running the app locally
    $debugRolesText = ""
    if ($IAmADeveloper) { $debugRolesText = "includeDebugRoles=true"; }

    # Deploy Bicep files
    $deployed = az deployment sub create `
    --location $Location `
    --template-file ${PSScriptRoot}\SecOpsSteward.bicep `
    --parameters `
    resourceGroupName=$ResourceGroup `
    azureAdApplicationId=$ApplicationId `
    azureAdApplicationSecret=$ApplicationSecret `
    webAppName=$WebAppName `
    sqlAdministratorLogin=$SqlAdministratorLogin `
    sqlAdministratorLoginPassword=$SqlAdministratorLoginPassword `
    mainUserPrincipal=$signedInUserObjectId `
    deploymentId=$DeploymentId `
    $debugRolesText

    if ($LastExitCode -ne 0)
    {
        Write-Host "Deployment failed; review and fix above error(s)."
        return
    }

    Write-Host "### Azure Infrastructure Deployment Complete" -ForegroundColor Cyan

    #region Developer SQL Firewall Rule / File Creation
    # If debug roles are included, assume the user is doing development and
    # will need values for their appsettings.Deployment.json -- echo them here
    if ($IAmADeveloper)
    {
        $localIp = (Invoke-WebRequest -uri "http://ifconfig.me/ip").Content
        Write-Host "### Adding SQL Server firewall rule for " -nonewline -ForegroundColor Cyan
        Write-Host $localIp -ForegroundColor Green
        $written = az sql server firewall-rule create `
            --resource-group $ResourceGroup `
            --server sossql$DeploymentId `
            --name "Local Development" `
            --start-ip-address $localIp `
            --end-ip-address $localIp

        if ($LastExitCode -ne 0)
        {
            Write-Host $written
            Write-Host "Firewall rule creation failed"  -ForegroundColor Red
        }

        $storageKeys = az storage account keys list -n sosblobs$DeploymentId | ConvertFrom-Json
        $storageKey = $storageKeys[0].value
        $sub = az account show | ConvertFrom-Json
        $subId = $sub.id
        $tenantId = $sub.tenantId

        Write-Host "### Writing appSettings.Development.json" -ForegroundColor Cyan
        $appSettingsPath="..\src\SOSWebUI\SecOpsSteward.UI\appsettings.Development.json"
        if (!(Test-Path $appSettingsPath)) { "{}" | Out-File $appSettingsPath -Force }
        $existing = Get-Content -Path $appSettingsPath -Raw | ConvertFrom-Json

        $DeploymentIdLower = $DeploymentId.ToLower()
        $newValues = @"
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
    "AgentVaultName": "sosavault$DeploymentId",
    "UserVaultName": "sosuvault$DeploymentId",
    "PackageRepoAccount": "sosblobs$DeploymentIdLower",
    "PackageRepoContainer": "packages",
    "ServiceBusNamespace": "sosbus$DeploymentId",
    "ResourceGroup": "$ResourceGroup"
},
"ConnectionStrings": {
    "Database": "Data Source=tcp:sossql$DeploymentId.database.windows.net,1433;Initial Catalog=sosdb;User Id=SqlAdmininistratorLogin@sossql$DeploymentId.database.windows.net;Password=$SqlAdministratorLoginPassword;",
    "AzureWebJobsStorage": "DefaultEndpointsProtocol=https;AccountName=sosblob$DeploymentId;AccountKey=$storageKey;EndpointSuffix=core.windows.net"
}
}
"@ | ConvertFrom-Json;

        $newFile = Json-Merge $existing $newValues | ConvertTo-Json

        $newFile | Out-File $appSettingsPath -Force
    }
    #endregion
}
else { Write-Host "Skipping infrastructure deployment..." }
#endregion

#region App Build/Publish
# move to project folder
Remove-Item $PSScriptRoot\publish -Recurse -ErrorAction Ignore
Push-Location $PSScriptRoot\..\src\SOSWebUI\SecOpsSteward.UI

Write-Host "### Restoring packages" -ForegroundColor Cyan
$restored = dotnet restore
if ($LastExitCode -ne 0)
{
    Write-Host $restored
    Write-Host "Application restore failed" -ForegroundColor Red
}

Write-Host "### Building WebUI code" -ForegroundColor Cyan
$built = dotnet build
if ($LastExitCode -ne 0)
{
    Write-Host $built
    Write-Host "Application build failed" -ForegroundColor Red
}

Write-Host "### Creating a publish bundle" -ForegroundColor Cyan
$published = dotnet publish -o ..\..\..\deploy\publish
if ($LastExitCode -ne 0)
{
    Write-Host $published
    Write-Host "Application publish failed" -ForegroundColor Red
}

Write-Host "### Copying in project description" -ForegroundColor Cyan
Copy-Item *.csproj ..\..\..\deploy\publish
Pop-Location
# move to publish folder
Push-Location $PSScriptRoot\publish

Write-Host "### Applying publish artifacts to Azure application" -ForegroundColor Cyan
$appUp = az webapp up --plan "soshosting${DeploymentId}" --resource-group $ResourceGroup --name $WebAppName
if ($LastExitCode -ne 0)
{
    Write-Host $appUp
    Write-Host "Artifact was not applied to Azure application" -ForegroundColor Red
}

Pop-Location
#endregion