<#
  .SYNOPSIS
  Deploys application code and/or infrastructure for a Demo the SecOps Steward application

  .DESCRIPTION
  Creates Azure infrastructure via a Bicep file for a Demo installation, which does not have access to any resources or integrations

  .PARAMETER Location
  Location for resources created as a part of this operation

  .PARAMETER ResourceGroup
  Resource group to contain resources which run this instance of SoS

  .PARAMETER WebAppName
  URL name of SecOps Steward UI application. This will result in <name>.azurewebsites.net

  .PARAMETER DeploymentId
  Unique identifier to append to resources and the resource group, to correlate objects.
  If not present, it will be randomly generated.

  .PARAMETER SkipInfrastructure
  If provided, the script will not deploy the Azure infrastructure Bicep file.
  Documentation shows this as having a switch parameter, but in practice, just specifying
  the option is sufficient to enable it.

  .INPUTS
  None

  .OUTPUTS
  This script does not generate files but will provide output via the console for use
  in populating values in other files, such as appsettings.Development.Json

  .EXAMPLE
  PS> .\deploy-demo.ps1

  .EXAMPLE
  PS> .\deploy-demo.ps1 -DeploymentId abcdefg -SkipInfrastructure
#>

param (
    [String]$Location = "westus", 
    [String]$ResourceGroup = $null, 
    [String]$WebAppName = "soswebapp", 
    [String]$DeploymentId = $null, # random string to append to all resources
    [switch]$SkipInfrastructure = $false
)

# ---
#region Dependency Functions
function Get-RandomAlphaNumeric { param([int]$length=8) Write-Output (-join (((48..57)+(65..90)+(97..122)) * 80 |Get-Random -Count $length |%{[char]$_})) }
#endregion
# ---

if (!$DeploymentId) { $DeploymentId = (Get-RandomAlphaNumeric).ToLower() }

Write-Host @'

    @@@@@@@@  @@@@@@@@  @@@@@@@@     
    @@@  @@@  @@@  @@@  @@@  @@@     
    @@@  @@@  @@@  @@@  @@@  @@@     
    @@@       @@@  @@@  @@@         SecOps Steward
    @@@@@@@@  @@@  @@@  @@@@@@@@    Demo Site Deployment Tool  
         @@@  @@@  @@@       @@@
    @@@  @@@  @@@  @@@  @@@  @@@    Empowering administrators to
    @@@@ @@@  @@@  @@@  @@@ @@@@    cultivate a more secure cloud!
      @@@@@@  @@@  @@@  @@@@@@@
          @@  @@@  @@@  @@
              @@@  @@@
                 @@

'@ -ForegroundColor Blue

Write-Host "`nThis service is a DEMO and does not actually use any integration resources:" -ForegroundColor Red
Write-Host "- Discovery and Resource selection are disabled." -ForegroundColor Red
Write-Host "- Data is purged periodically when the service restarts." -ForegroundColor Red
Write-Host "- Packages are stored in memory only." -ForegroundColor Red
Write-Host "- Agent and User Enrollment is disabled.`n" -ForegroundColor Red

Write-Host "Beginning deployment of SecOps Steward DEMO..." -ForegroundColor Green

# generate a random RG if none is given
if ([string]::IsNullOrEmpty($ResourceGroup)) { $ResourceGroup = "soswebapp-" + $DeploymentId }

#region Deploy Infrastructure
if (!$SkipInfrastructure)
{
    Write-Host "### Deploying Azure Infrastructure " -ForegroundColor Cyan
    Write-Host "Deployment ID:`t`t" -nonewline -ForegroundColor Red
    Write-Host $DeploymentId
    Write-Host "Location:`t`t" -nonewline -ForegroundColor Green
    Write-Host $Location
    Write-Host "Resource Group:`t`t" -nonewline -ForegroundColor Green
    Write-Host $ResourceGroup
    Write-Host "DEMO WebApp Name:`t" -nonewline -ForegroundColor Green
    Write-Host $WebAppName".azurewebsites.net"

    # Deploy Bicep files
    $deployed = az deployment group create `
    --template-file ${PSScriptRoot}\SecOpsStewardDemo.bicep `
    --resource-group $ResourceGroup `
    --parameters `
    webAppName=$WebAppName `
    randomString=$DeploymentId

    if ($LastExitCode -ne 0)
    {
        Write-Host "Deployment failed; review and fix above error(s)."
        return
    }

    Write-Host "### Azure Infrastructure Deployment Complete" -ForegroundColor Cyan
}

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