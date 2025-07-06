# Development

This document provides a guide on how to develop the application on your local computer.

## FeatBit.EvaluationServer.Streaming

The FeatBit Agent relies on the `FeatBit.EvaluationServer.Streaming` NuGet package, which is packaged from
the [Evaluation Server Project](https://github.com/featbit/featbit/tree/main/modules/evaluation-server).

If you need to build and reference this package locally, follow these steps:

1. Change your directory to the evaluation server source folder:
   ```bash
   cd featbit/modules/evaluation-server/src
   ```

2. Open the `Directory.Build.props` file and modify the `PackageVersion` value to a new version, such as `2.3.1-local`.

3. Pack the project into a local NuGet repository:
   ```bash
   cd featbit/modules/evaluation-server
   # use "D:\Local Nuget" for example
   dotnet pack -c Release -o "D:\Local Nuget"
   ```

4. Add the local NuGet source to your project:
   ```bash
   # use "D:\Local Nuget" for example
   dotnet nuget add source "D:\Local Nuget"
   ```

Once you have completed these steps, you can update the `FeatBit.EvaluationServer.Streaming` package version in the
FeatBit Agent project to use the locally built package.

## Multi-platform builds

```bash
# Build for multiple architectures
docker buildx build --platform linux/amd64,linux/arm64 -t featbit-agent:latest .

# Push to registry
docker buildx build --platform linux/amd64,linux/arm64 -t your-registry/featbit-agent:latest --push .
```

### Debugging Commands

```bash
# Check container status
docker ps

# View application logs
docker logs -f featbit-agent

# Execute commands inside container
docker exec -it featbit-agent sh

# Check health status
docker exec featbit-agent curl http://localhost:6100/health/readiness

# Inspect container configuration
docker inspect featbit-agent
```