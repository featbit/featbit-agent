#!/bin/bash
publishBaseDir="publish"
platforms=("win-x64" "linux-x64" "osx-x64")

if ! [ -d "${publishBaseDir}" ]; then
  mkdir "$publishBaseDir"
fi

rm -rf "$publishBaseDir"/*

for platform in "${platforms[@]}"; do
  dotnet publish -r "$platform" -c Release -p:DebugType=none --self-contained true -o "$publishBaseDir/$platform" || exit 1
  cd "$publishBaseDir/$platform" || exit 1
  tar -czvf "./featbit_agent_${platform}_${version}.tar.gz" ./* -C ../ || exit 1
  cd ../../
done
