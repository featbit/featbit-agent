$publishBaseDir = '../../publish'
$platforms = @('win-x64', 'linux-x64', 'osx-x64')

if (-not(Test-Path -Path $publishBaseDir -PathType Container))
{
    New-Item -ItemType Directory -Path $publishBaseDir | Out-Null
}

Remove-Item -Path "$publishBaseDir/*" -Recurse -Force

foreach ($platform in $platforms)
{
    dotnet publish -r $platform -c Release -p:DebugType=none --self-contained true -o "$publishBaseDir/$platform"
    if ($LASTEXITCODE -ne 0)
    {
        Write-Error "Failed to publish the application for platform $platform"
        exit 1
    }

    Compress-Archive -Path "$publishBaseDir/$platform/*" -DestinationPath "$publishBaseDir/featbit_agent_${platform}_${version}.zip"
}