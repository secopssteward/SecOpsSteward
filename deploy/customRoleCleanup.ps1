# This script cleans up the custom roles SecOps Steward creates in Azure AD.

Write-Host @'
    @@@@@@@@  @@@@@@@@  @@@@@@@@     
    @@@  @@@  @@@  @@@  @@@  @@@     
    @@@  @@@  @@@  @@@  @@@  @@@     
    @@@       @@@  @@@  @@@         SecOps Steward
    @@@@@@@@  @@@  @@@  @@@@@@@@    Custom Role Cleanup Tool
         @@@  @@@  @@@       @@@
    @@@  @@@  @@@  @@@  @@@  @@@
    @@@@ @@@  @@@  @@@  @@@ @@@@
      @@@@@@  @@@  @@@  @@@@@@@
          @@  @@@  @@@  @@
              @@@  @@@
                 @@

'@ -ForegroundColor Blue

Write-Host "### Warning ###" -ForegroundColor Red
Write-Host "This tool will remove all roles in Azure AD which start with 'SoSRole - '."
Write-Host "These roles are created by SecOps Steward to control access to workflow resources."
Write-Host "Press any key to continue";
$null = $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown');

Write-Host "Getting SecOps Steward Roles from AAD..."
$roles = az role definition list --query "[?starts_with(roleName, 'SoSRole - ')].[id,roleName]" | ConvertFrom-Json

foreach ($r in $roles)
{
    Write-Host * $r[1] ( $r[0] )
    Write-Host Retrieving current assignments of this role:
    $qStr = "[?roleDefinitionId == '" + $r[0] + "']"
    $assignments = az role assignment list --all --query ""$qStr"" | ConvertFrom-Json
    foreach ($a in $assignments)
    {
        Write-Host $a.id
        az role assignment delete --ids $a.id
    }

    Write-Host Removing role definition
    az role definition delete --name $r[1]

    Write-Host \n---------------------------------------\n
}

Write-Host Operation complete.
