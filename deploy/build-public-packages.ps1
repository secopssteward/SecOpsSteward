Write-Host "Building, signing, and uploading plugin packages to public repository..."

$folders = Get-ChildItem -Path '..\plugins' -Filter SecOpsSteward.Plugins.* |
     ? {$_.PsIsContainer -and $_.FullName -notmatch 'Shared' }

foreach ($file in $folders)
{
    Write-Host "* $file";
    $buildFolder = $file.FullName;
    $files = Get-ChildItem $buildFolder -recurse -filter  "*.dll"
    if ($files.Length -eq 0)
    {
        Write-Host "`- This folder does not have any libraries in it; did you build the extension?"
    }
    else
    {
        ..\src\Tools\SOSPackaging\bin\Debug\net5.0\SOSPackaging  new --config ..\config.txt $buildFolder
    }
}