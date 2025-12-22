$Dlls = dir ..\mpv\build -include "*.dll" -recurse
mkdir .cache

foreach ($Dll in $Dlls) {
    Copy-Item $Dll.FullName -Destination ".cache/" -Force
    Write-Host "Copied $($Dll.Name)"
}
