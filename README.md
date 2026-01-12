[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=muons-io_dock-gen&metric=bugs)](https://sonarcloud.io/summary/new_code?id=muons-io_dock-gen)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=muons-io_dock-gen&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=muons-io_dock-gen)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=muons-io_dock-gen&metric=sqale_index)](https://sonarcloud.io/summary/new_code?id=muons-io_dock-gen)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=muons-io_dock-gen&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=muons-io_dock-gen)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=muons-io_dock-gen&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=muons-io_dock-gen)

# dock-gen

dock-gen is a simple .NET tool designed to generate Dockerfiles for one or multiple projects in a solution based on
project dependencies.

> Please note, dock-gen is still in its early stages, some features may not be stable.

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

### Command reference

| Command | Aliases | Description |
|---|---|---|
| `generate` | `g`, `gen` | Generate Dockerfiles for eligible projects discovered from `--solution` or `--directory`, or for a single project specified via `--project`. |
| `update` | `u`, `upd` | Update Dockerfiles for eligible projects discovered from `--solution` or `--directory`, or for a single project specified via `--project`. By default rewrites the whole file; with `--only-references` updates only the project-reference `COPY` section inside the `build` stage and leaves the rest untouched. |

### Global options

| Option | Aliases | Type | Default | Description |
|---|---|---|---|---|
| `--verbose` | `--debug`, `--trace` | `bool` | `false` | Enable detailed logging (includes trace output). Use before the command name. |

### generate (`g`, `gen`)

| Option | Aliases | Type | Default | Description |
|---|---|---|---|---|
| `--directory` | `-d` | `string` |  | Discover projects under a directory (recursively) and generate Dockerfiles for eligible projects. |
| `--solution` | `-s` | `string` |  | Discover projects in a solution and generate Dockerfiles for eligible projects. |
| `--project` | `-p` | `string` |  | Generate a Dockerfile for a single project. |
| `--analyzer` | `-a` | `string` | `FastAnalyzer` | Analyzer to use: `FastAnalyzer`, `DesignBuildTimeAnalyzer`, `SimpleAnalyzer`. |
| `--multi-arch` |  | `bool` | `true` | Generate a multi-arch Dockerfile (`FROM --platform=$BUILDPLATFORM`). |

If none of `--solution`, `--project`, or `--directory` are provided, dock-gen will try to locate all projects inside the current directory recursively.

### update (`u`, `upd`)

| Option | Aliases | Type | Default | Description |
|---|---|---|---|---|
| `--directory` | `-d` | `string` |  | Discover projects under a directory (recursively) and update Dockerfiles for eligible projects. |
| `--solution` | `-s` | `string` |  | Discover projects in a solution and update Dockerfiles for eligible projects. |
| `--project` | `-p` | `string` |  | Update the Dockerfile for a single project. |
| `--analyzer` | `-a` | `string` | `FastAnalyzer` | Analyzer to use: `FastAnalyzer`, `DesignBuildTimeAnalyzer`, `SimpleAnalyzer`. |
| `--multi-arch` |  | `bool` | `true` | Generate a multi-arch Dockerfile (`FROM --platform=$BUILDPLATFORM`). |
| `--only-references` |  | `bool` | `false` | Update only the project-reference `COPY` section in the existing Dockerfile. |

If none of `--solution`, `--project`, or `--directory` are provided, dock-gen will try to locate all projects inside the current directory recursively.

### Examples

Generate Dockerfiles for all projects in a solution:
```bash
dotnet dockgen generate --solution .\\Solution.sln
```

Generate Dockerfiles (using alias):
```bash
dotnet dockgen g --solution .\\Solution.sln
```

Update Dockerfiles for all projects in a solution:
```bash
dotnet dockgen update --solution .\\Solution.sln
```

Update only the project reference COPY section in existing Dockerfiles:
```bash
dotnet dockgen update --solution .\\Solution.sln --only-references
```

Enable detailed logging (any of these is equivalent):
```bash
dotnet dockgen --verbose g --solution .\\Solution.sln
```

Use a non-default analyzer:
```bash
dotnet dockgen generate --analyzer DesignBuildTimeAnalyzer --solution .\\Solution.sln
```

## Notes and limitations

- `FastAnalyzer` does not run MSBuild. It can’t evaluate all conditional logic, custom tasks, or target execution.
  If your repo relies heavily on dynamic MSBuild evaluation, prefer `DesignBuildTimeAnalyzer`.

## Features

- **Generate Dockerfile**: Generate a Dockerfile for a single or multiple projects at once
- **Multi-Stage Build**: Generate a multi-stage Dockerfile for a project
- **Central Package/Build Management**: dock-gen supports CPM/CBM in your project

## Supported properties

Description for MSBuild properties can be found
here: [Official .NET docs](https://learn.microsoft.com/en-us/dotnet/core/docker/publish-as-container?pivots=dotnet-8-0)

| Property                 | Description                     | MSBuild Property | Custom Property | Default Value                           |
|--------------------------|---------------------------------|------------------|-----------------|-----------------------------------------|
| ContainerBaseImage*      |                                 | &#x2714; yes     | &#x2718; no     | mcr.microsoft.com:443/dotnet/aspnet:10.0 |
| ContainerRegistry        | Registry of the base image      | &#x2714; yes     | &#x2718; no     | mcr.microsoft.com                       |
| ContainerRepository      | Repository of the base image    | &#x2714; yes     | &#x2718; no     | dotnet/aspnet                           |
| ContainerFamily          | Family of the base image        | &#x2714; yes     | &#x2718; no     |
| ContainerImageTag        | Tag of the base image           | &#x2714; yes     | &#x2718; no     | 10.0                                     |
| ContainerBasePort        | Port of the base image          | &#x2718; no      | &#x2714; yes    | 443                                     |
| ContainerPort            | Port(s) to expose in Dockerfile | &#x2714; yes     | &#x2718; no     |
| ContainerBuildImage      | Build image for the project     | &#x2718; no      | &#x2714; yes    | mcr.microsoft.com:443/dotnet/sdk:10.0    |
| ContainerBuildRegistry   | Registry of the build image     | &#x2718; no      | &#x2714; yes    | mcr.microsoft.com                       |
| ContainerBuildRepository | Repository of the build image   | &#x2718; no      | &#x2714; yes    | dotnet/sdk                              |
| ContainerBuildFamily     | Family of the build image       | &#x2718; no      | &#x2714; yes    |
| ContainerBuildImageTag   | Tag of the build image          | &#x2718; no      | &#x2714; yes    | 10.0                                     |
| ContainerBuildPort       | Port of the build image         | &#x2718; no      | &#x2714; yes    | 443                                     |

## Roadmap

- [ ] Support more advanced Dockerfile customizations

## Contributing

We welcome contributions to dock-gen!

## License

dock-gen is open source software licensed under the MIT. See the [LICENSE](LICENSE) file for more details.

## Contact

If you have any questions or feedback, please feel free to create an issue.

## Troubleshooting

- Dockerfile not generated
  - Only executable projects (`OutputType=Exe`) get Dockerfiles. Library projects are skipped.
  - Test projects are skipped.

- Dependency graph looks incomplete
  - Try `--analyzer DesignBuildTimeAnalyzer` to use MSBuild design-time evaluation.

## Acknowledgements

This project makes use of the following open source projects:

- [Buildalyzer](https://github.com/phmonte/Buildalyzer): A utility for performing design-time builds of .NET projects to
  obtain information such as package references and compiler flags.
- [Serilog](https://github.com/serilog/serilog): Simple .NET logging with fully-structured events.
- [.NET](https://github.com/dotnet): .NET platform
