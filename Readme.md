# NuGet.Publish Action
This action builds a .NET Project or Solution then packages and publishes it to the `nuget-server` feed.

## Inputs

## `configuration`
The build configuration to use when building the project or solution.  (values: `Debug|Release`, default: `Release`)

## `github-organization`
The GitHub Organization to publish the generated package to which forces the use of GitHub's NuGet Servers.

## `nuget-api-key`
**Required** The API key which is used to authenticate with the NuGet server.

## `nuget-auth-for-build`
Flag that denotes whether to authenticate with the NuGet Server for the build process.

## `nuget-password`
The password with which to authenticate with the NuGet Server during the build process.

## `nuget-username`
The username with which to authenticate with the NuGet Server during the build process.

## `nuspec-file`
The path, relative to the root of the repository, to the NuSpec file.

## `output`
The output format of the application.  (values: `Json|Plain|Silent|Xml`, default: `Silent`)

## `package-name`
The name of the package to generate, that will be published to the NuGet Server.

## `platform`
The platform to use when building the project or solution.  (values: `AnyCpu|Arm64|x64|x86`, default: `AnyCpu`)

## `project`
**Required** The path, relative to the root of the repository, to the project or solution file to be packaged.

## `runtime`
The runtime to use when building the project or solution.  (values: `net8.x|net7.x|net6.x`, default: `net8.x`)

## `scan-for-package-name`
A flag that denotes whether to scan the project or solution file in an attempt to find the package name preferring `PackageId` over `AssemblyName` over `project`.  (default: `false`)'

## `verbosity`
The output level for the build process.  (values: `Detailed|Diagnostic|Minimal|Normal|Quiet`, default: `Minimal`)

## `version`
The static version for the NuGet package.

## `workspace`
The working directory for the processes, this defaults to the GITHUB_WORKSPACE environment variable or the containing directory of the <project> file.

## Outputs

## `package-name`
The name of the generated package that was published to the NuGet Server.

## `package-path`
The path to the generated package that was published to the NuGet Server.

## `process-build-error`
The standard error from the process responsible for building the project.

## `process-build-output`
The standard output from the process responsible for building the project.

## `process-clean-error`
The standard error from the process responsible for cleaning the project.

## `process-clean-output`
The standard output from the process responsible for cleaning the project.

## `process-push-error`
The standard error from the process responsible for pushing the package to the NuGet Server.

## `process-push-output`
The standard output from the process responsible for pushing the package to the NuGet Server.

## `process-pack-error`
The standard error from the process responsible for packing the project.

## `process-pack-output`
The standard output from the process responsible for packing the project.

## `process-restore-error`
The standard error from the process responsible for restoring the project.

## `process-restore-output`
The standard output from the process responsible for restoring the project.

## `version`
The version of the generated package that was published to the NuGet Server.

## Example Usage

### Runtimes
This action supports multiple different .NET runtimes.  The table below shows the supported runtimes and their corresponding `runtime` values.

| Runtime | Value    |
|---------|----------|
| 8.0     | `net8.x` |
| 7.0     | `net7.x` |
| 6.0     | `net6.x` |

### Basic Usage

```yaml
- uses: 'bolvarak/nuget-publish@main'
  with:
    nuget-api-key: ${{ secrets.NUGET_API_KEY | secrets.GITHUB_TOKEN }}
    project: 'src/MyProject/MyProject.csproj'
    runtime: 'net8.x'
```

<!--
# NuGet.Publish Console Application
This project can also be cloned and built locally to provide a console application that can be used to publish a NuGet package to the `nuget-server`.

## Usage
-->
