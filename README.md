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

You can use dock-gen with the `generate` command.

### Inputs

You can provide these options to tell dock-gen where to find projects or solutions:
```bash
--solution  (-s) : path to a solution file
--project   (-p) : path to a project file
--directory (-d) : path to a directory
```

If none of these options are provided, dock-gen will try to locate all projects inside the current directory recursively.

### Analyzer selection

`dock-gen` supports multiple analyzers that trade accuracy for performance.

```bash
--analyzer (-a) : SimpleAnalyzer | DesignBuildTimeAnalyzer (default) | FastAnalyzer
```

- `DesignBuildTimeAnalyzer` (default)
  - Uses Buildalyzer and a design-time build.
  - Slowest, but generally the most compatible with real-world MSBuild evaluation.

- `SimpleAnalyzer`
  - Uses MSBuild evaluation (`ProjectCollection.LoadProject`) without running a design-time build.
  - Faster than `DesignBuildTimeAnalyzer`, but still depends on MSBuild evaluation and may be slower than `FastAnalyzer` on large solutions.

- `FastAnalyzer`
  - Optimized for speed and designed to avoid MSBuild project loading.
  - Reads project files and common repo-wide MSBuild files directly (for example `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`).
  - Best used when you want a fast dependency graph + enough properties for Dockerfile generation.

### Examples

Generate Dockerfiles for all projects in a solution:
```bash
dotnet dockgen generate --solution .\Solution.sln
```

Use the fast analyzer (recommended for large repos when you don’t need full MSBuild evaluation):
```bash
dotnet dockgen generate --analyzer FastAnalyzer --solution .\Solution.sln
```

If no solution or project file is specified, dock-gen will try to find projects under the current directory.

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
