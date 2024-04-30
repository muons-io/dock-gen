[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=muons-io_dock-gen&metric=bugs)](https://sonarcloud.io/summary/new_code?id=muons-io_dock-gen)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=muons-io_dock-gen&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=muons-io_dock-gen)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=muons-io_dock-gen&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=muons-io_dock-gen)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=muons-io_dock-gen&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=muons-io_dock-gen)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=muons-io_dock-gen&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=muons-io_dock-gen)

# dock-gen

dock-gen is a very simple .NET tool designed to generate Dockerfiles for one or multiple projects in a solution based on dependencies in a predictable way each time. 
It provides a simple and easy way to generate Dockerfiles for your .NET application.

> Please note that since .NET 7, dotnet comes with a built-in feature to publish app as container image. 
> If you don't need to generate Dockerfiles, you can use the `dotnet publish` command to publish your application as container.
> For more information, see the [official documentation](https://learn.microsoft.com/en-us/dotnet/core/docker/publish-as-container?pivots=dotnet-8-0).

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

## Roadmap

- [ ] List supported MSBuild properties and items used for Dockerfile customization
- [ ] Support more advanced Dockerfile customizations
- [ ] Performance improvements

## Contributing

We welcome contributions to dock-gen! 

## License

dock-gen is open source software licensed under the MIT. See the [LICENSE](LICENSE) file for more details.

## Contact

If you have any questions or feedback, please feel free to create an issue.

## Acknowledgements


This project makes use of the following open source projects:

- [Buildalyzer](https://github.com/phmonte/Buildalyzer): A utility for performing design-time builds of .NET projects to obtain information such as package references and compiler flags.
