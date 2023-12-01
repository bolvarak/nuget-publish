## We'll need to the .NET 8.0 SDK to build our action processor
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

## Copy our action processor srouce code into the image
COPY "." "/build"

## Publish our action processor build
RUN dotnet publish "/build/NuGet.Publish.csproj" \
    --configuration "Release" \
    --force --nologo \
    --output "/publish" \
    --runtime "linux-musl-x64" \
    --self-contained true \
    --verbosity "minimal";

## We'll need the .NET 6.0 SDK to run our action processor
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS final

# Add the .NET 7.x SDK
COPY --from=mcr.microsoft.com/dotnet/sdk:7.0-alpine "/usr/share/dotnet" "/usr/share/dotnet"

# Add the .NET 8.0 SDK
COPY --from=mcr.microsoft.com/dotnet/sdk:8.0-alpine "/usr/share/dotnet" "/usr/share/dotnet"

## Copy our published action processor build into place
COPY --from=build "/publish/NuGet.Publish" "/usr/bin/NuGet.Publish"

## Install our dependencies
RUN apk add --no-cache \
    icu-libs \
    libgcc \
    libstdc++;

## Define the entrypoint into the Docker image as our action processor
ENTRYPOINT ["NuGet.Publish"]
