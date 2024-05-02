[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=muons-io_dock-gen&metric=bugs)](https://sonarcloud.io/summary/new_code?id=muons-io_dock-gen)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=muons-io_dock-gen&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=muons-io_dock-gen)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=muons-io_dock-gen&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=muons-io_dock-gen)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=muons-io_dock-gen&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=muons-io_dock-gen)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=muons-io_dock-gen&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=muons-io_dock-gen)

# dock-gen

dock-gen is a very simple .NET tool designed to generate Dockerfiles for one or multiple projects in a solution based on
dependencies in a predictable way each time.

> Please note, dock-gen is still in its early stages, some features may not be stable.

> Since .NET 7, dotnet comes with a built-in feature to publish app as container image.
> If you don't need to generate Dockerfiles, you can use the `dotnet publish` command to publish your application as
> container.
> For more information, see
> the [official documentation](https://learn.microsoft.com/en-us/dotnet/core/docker/publish-as-container?pivots=dotnet-8-0).
> dock-gen is designed to provide more control over the Dockerfile generation process but it may not be suitable for all
> use cases.

## Installation

To install dock-gen, you can use the `dotnet tool install` command:

a) global installation:

```bash
dotnet tool install --global dockgen
```

b) local installation:
(optional) if you don't have a manifest file for your project, create one:

```bash
dotnet new tool-manifest
```

Add the tool to the manifest file:

```bash
dotnet tool install dockgen
```

## Usage

You can use dock-gen with the `generate` command. This command takes two optional parameters:

- `-s` or `--solution`: To specify the solution file.
- `-p` or `--project`: To specify the project file.

Here is an example of how to use the `generate` command:

```bash
dotnet dockgen generate -s MySolution.sln -p MyProject.csproj
```

If no solution or project file is specified, dock-gen will try to find a solution file in the current directory.

## Features

- **Generate Dockerfile**: Generate a Dockerfile for a single or multiple projects at once
- **Multi-Stage Build**: Generate a multi-stage Dockerfile for a project
- **Central Package/Build Management**: dock-gen supports CPM/CBM in your project

## Supported properties

Description for MSBuild properties can be found
here: [Official .NET docs](https://learn.microsoft.com/en-us/dotnet/core/docker/publish-as-container?pivots=dotnet-8-0)

| Property                 | Description                     | MSBuild Property | Custom Property | Default Value                           |
|--------------------------|---------------------------------|------------------|-----------------|-----------------------------------------|
| ContainerBaseImage*      |                                 | &#x2714; yes     | &#x2718; no     | mcr.microsoft.com:443/dotnet/aspnet:8.0 |
| ContainerRegistry        | Registry of the base image      | &#x2714; yes     | &#x2718; no     | mcr.microsoft.com                       |
| ContainerRepository      | Repository of the base image    | &#x2714; yes     | &#x2718; no     | dotnet/aspnet                           |
| ContainerFamily          | Family of the base image        | &#x2714; yes     | &#x2718; no     |
| ContainerImageTag        | Tag of the base image           | &#x2714; yes     | &#x2718; no     | 8.0                                     |
| ContainerBasePort        | Port of the base image          | &#x2718; no      | &#x2714; yes    | 443                                     |
| ContainerPort            | Port(s) to expose in Dockerfile | &#x2714; yes     | &#x2718; no     |
| ContainerBuildImage      | Build image for the project     | &#x2718; no      | &#x2714; yes    | mcr.microsoft.com:443/dotnet/sdk:8.0    |
| ContainerBuildRegistry   | Registry of the build image     | &#x2718; no      | &#x2714; yes    | mcr.microsoft.com                       |
| ContainerBuildRepository | Repository of the build image   | &#x2718; no      | &#x2714; yes    | dotnet/sdk                              |
| ContainerBuildFamily     | Family of the build image       | &#x2718; no      | &#x2714; yes    |
| ContainerBuildImageTag   | Tag of the build image          | &#x2718; no      | &#x2714; yes    | 8.0                                     |
| ContainerBuildPort       | Port of the build image         | &#x2718; no      | &#x2714; yes    | 443                                     |

## Roadmap

- [ ] Support more advanced Dockerfile customizations

## Contributing

We welcome contributions to dock-gen!

## License

dock-gen is open source software licensed under the MIT. See the [LICENSE](LICENSE) file for more details.

## Contact

If you have any questions or feedback, please feel free to create an issue.

## Acknowledgements

This project makes use of the following open source projects:

- [Buildalyzer](https://github.com/phmonte/Buildalyzer): A utility for performing design-time builds of .NET projects to
  obtain information such as package references and compiler flags.
