#!/bin/bash
Version="1.0.0"
PublishBaseDir="publish"
Platforms=("win-x64" "linux-x64" "osx-x64")

if ! [ -d "${PublishBaseDir}" ]; then
  mkdir "$PublishBaseDir"
fi

rm -rf "$PublishBaseDir"/*

for platform in "${Platforms[@]}"; do
  dotnet publish -r "$platform" -c Release -p:DebugType=none --self-contained true -o "$PublishBaseDir/$platform" || exit 1
  cd "$PublishBaseDir/$platform" || exit 1
  tar -czvf "./featbit_agent_${platform}_${Version}.tar.gz" ./* -C ../ || exit 1
  cd ../../
done
