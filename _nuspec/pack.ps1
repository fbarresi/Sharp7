$version = [System.Reflection.Assembly]::LoadFile("..\out\Release\Sharp7.dll").GetName().Version
$versionStr = "{0}.{1}.{2}" -f ($version.Major, $version.Minor, $version.Build)

Write-Host "Setting .nuspec version tag to $versionStr"

$content = (Get-Content Sharp7.nuspec) 
$content = $content -replace '\$version\$',$versionStr

$content | Out-File Sharp7.compiled.nuspec

& NuGet.exe pack Sharp7.compiled.nuspec