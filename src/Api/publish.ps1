$Version = '1.0.0'
$PublishBaseDir = 'publish'
$Platforms = @('win-x64', 'linux-x64', 'osx-x64')

if (-not(Test-Path -Path $PublishBaseDir -PathType Container))
{
    New-Item -ItemType Directory -Path $PublishBaseDir | Out-Null
}

Remove-Item -Path "$PublishBaseDir/*" -Recurse -Force

foreach ($platform in $Platforms)
{
    dotnet publish -r $platform -c Release -p:DebugType=none --self-contained true -o "$PublishBaseDir/$platform"
    if ($LASTEXITCODE -ne 0)
    {
        exit 1
    }

    Set-Location "$PublishBaseDir/$platform"
    Compress-Archive -Path * -DestinationPath "../featbit_agent_${platform}_${Version}.tar.gz"
    Set-Location ../../
}