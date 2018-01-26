$version = [System.Reflection.Assembly]::LoadFile("out\Release\Sharp7.dll").GetName().Version
$versionStr = "{0}.{1}.{2}" -f ($version.Major, $version.Minor, $version.Build)

Write-Host "Setting .nuspec version tag to $versionStr"

$content = (Get-Content Sharp7.nuspec) 
$content = $content -replace '\$version\$',$versionStr

$content | Out-File _nuspec\Sharp7.compiled.nuspec

& _nuspec\NuGet.exe pack _nuspec\Sharp7.compiled.nuspec