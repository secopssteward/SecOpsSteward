Write-Host "Building, signing, and uploading plugin packages to public repository..."

$folders = Get-ChildItem -Path '..\plugins'

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
        ..\tools\packaging\SOSPackaging  new --config ..\config.txt $buildFolder
    }
}