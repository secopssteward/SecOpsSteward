# Set-ExecutionPolicy Unrestricted -Scope Process
# Install-Module AzureRM -Scope CurrentUser

Param ([string] $appName, [string] $rgName = 'sos-app', [string] $location = 'East US')

function Get-RandomCharacters($length) {
    $characters = 'abcdefghiklmnoprstuvwxyzABCDEFGHKLMNOPRSTUVWXYZ1234567890#*@+-_'
    $random = 1..$length | ForEach-Object { Get-Random -Maximum $characters.length } 
    $private:ofs='' 
    return [String]$characters[$random]
}

New-AzResourceGroup -Name $rgName
                    -Location $location

New-AzureADGroup -Description 'SecOps Steward Admins' `
                 -DisplayName 'SoS Admins' `
                 -MailEnabled $false `
                 -SecurityEnabled $true `
                 -MailNickName 'sos-admins'

New-AzureADGroup -Description 'SecOps Steward Agents' `
                 -DisplayName 'SoS Agents' `
                 -MailEnabled $false `
                 -SecurityEnabled $true `
                 -MailNickName 'sos-agents'

New-AzureADGroup -Description 'SecOps Steward Users' `
                 -DisplayName 'SoS Users' `
                 -MailEnabled $false `
                 -SecurityEnabled $true `
                 -MailNickName 'sos-users'

$aadApp = New-AzureADApplication -DisplayName 'SecOps Steward WebUI' `
                                 -IdentifierUris 'https://$(appName).azurewebsites.net' `
                                 -ReplyUrls 'https://$($appName).azurewebsites.net/signin-oidc'
$tenant = Get-AzTenant

New-AzResourceGroupDeployment -ResourceGroupName $rgName `
                              -TemplateFile SecOpsStewardAzure.bicep `
                              -aadAppId $aadApp.Id
                              -tenantId $tenant.Id
                              -aadAppName $appName
                              -sqlAdministratorLogin "admin$($appName)"
                              -sqlAdministratorPassword Get-RandomCharacters -length 24