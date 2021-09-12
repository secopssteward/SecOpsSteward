<#
  .SYNOPSIS
  Deploys application code and/or infrastructure for the SecOps Steward Agent application

  .DESCRIPTION
  Creates Azure infrastructure for the Agent via a Bicep file, then builds and deploys the current Agent code.

  .PARAMETER Location
  Location for Agent resources created as a part of this operation

  .PARAMETER ResourceGroup
  Resource group to contain resources which run this agent

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
  None

  .EXAMPLE
  PS> .\deploy-agent.ps1

  .EXAMPLE
  PS> .\deploy-agent.ps1 -Location westus

  .EXAMPLE
  PS> .\deploy-agent.ps1 -SkipInfrastructure
#>

param (
    [String]$Location = "westus", 
    [String]$ResourceGroup = $null, 
    [String]$AgentDeploymentId = $null, # random string to append to all resources
    [switch]$SkipInfrastructure = $false
)

# ---
function Get-RandomAlphaNumeric { param([int]$length=8) Write-Output (-join (((48..57)+(65..90)+(97..122)) * 80 |Get-Random -Count $length |%{[char]$_})) }
# ---

if (!$AgentDeploymentId) { $AgentDeploymentId = (Get-RandomAlphaNumeric).ToLower() }
if (!$ResourceGroup) { $ResourceGroup = "sosagent-"+$AgentDeploymentId }

Write-Host @'
    @@@@@@@@  @@@@@@@@  @@@@@@@@     
    @@@  @@@  @@@  @@@  @@@  @@@     
    @@@  @@@  @@@  @@@  @@@  @@@     
    @@@       @@@  @@@  @@@         SecOps Steward
    @@@@@@@@  @@@  @@@  @@@@@@@@    Agent Deployment Tool  
         @@@  @@@  @@@       @@@
    @@@  @@@  @@@  @@@  @@@  @@@
    @@@@ @@@  @@@  @@@  @@@ @@@@
      @@@@@@  @@@  @@@  @@@@@@@
          @@  @@@  @@@  @@
              @@@  @@@
                 @@

'@ -ForegroundColor Blue

Write-Host "Beginning deployment of a SecOps Steward Agent..." -ForegroundColor Green

if (!$SkipInfrastructure)
{
    $exists = az group exists --name $ResourceGroup
    $existsBool = [System.Convert]::ToBoolean($exists)

    if (!$existsBool)
    {
        Write-Host "### Creating Resource Group" $ResourceGroup  -ForegroundColor Cyan
        $empty = az group create --name $ResourceGroup --location $Location
    }

    Write-Host "### Deploying Agent Infrastructure " -ForegroundColor Cyan
    Write-Host "Agent Deployment ID:`t" -nonewline -ForegroundColor Red
    Write-Host $AgentDeploymentId
    Write-Host "Location:`t`t" -nonewline -ForegroundColor Green
    Write-Host $Location
    Write-Host "Resource Group:`t`t" -nonewline -ForegroundColor Green
    Write-Host $ResourceGroup

    # Deploy Bicep files
    az deployment group create `
    --resource-group $ResourceGroup `
    --template-file .\SOSAgent.bicep `
    --parameters appNamePrefix=$AgentDeploymentId

    if ($LastExitCode -ne 0)
    {
        Write-Host "Deployment failed; review and fix above error(s)."
        return
    }

    $identity = az functionapp identity show -g $ResourceGroup -n sosagent$AgentDeploymentId

    Write-Host "### Agent Infrastructure Deployment Complete" -ForegroundColor Cyan
    Write-Host "Location:`t`t" -nonewline -ForegroundColor Green
    Write-Host $Location
    Write-Host "Resource Group:`t`t" -nonewline -ForegroundColor Green
    Write-Host $ResourceGroup
    Write-Host "Agent Identity Principal:`t" -nonewline -ForegroundColor Green
    Write-Host $identity.principalId

    Write-Host Note that the Agent installation is not complete until you enroll it with the -ForegroundColor Cyan
    Write-Host SecOps Steward Web UI. This final action will configure the agent to work with -ForegroundColor Cyan
    Write-Host your existing SoS system. -ForegroundColor Cyan
}
else { Write-Host "Skipping Agent infrastructure deployment..." }

# app build/publish here
# move to project folder
Remove-Item .\agent-publish -Recurse -ErrorAction Ignore
Push-Location ..\src\Agents\SecOpsSteward.Coordinator.AzureFunctions
Write-Host "### Restoring packages" -ForegroundColor Cyan
dotnet restore
Write-Host "### Building SoS Agent code" -ForegroundColor Cyan
dotnet build
Write-Host "### Creating a publish bundle" -ForegroundColor Cyan
dotnet publish -o ..\..\..\deploy\agent-publish
Write-Host "### Copying in project description" -ForegroundColor Cyan
Copy-Item *.csproj ..\..\..\deploy\agent-publish
Pop-Location
# move to publish folder
Write-Host "### Zipping application" -ForegroundColor Cyan
Compress-Archive -Path .\agent-publish\* -DestinationPath .\agent-publish\app.zip
Write-Host "### Applying agent publish artifacts to Azure application" -ForegroundColor Cyan
az functionapp deployment source config-zip -g $ResourceGroup -n sosagent$AgentDeploymentId --src .\agent-publish\app.zip