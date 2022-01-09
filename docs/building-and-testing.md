# Building and Testing

## Command Line

### Prerequisites

- [PowerShell](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell) 3.0 or higher (see [this question](http://stackoverflow.com/questions/1825585/determine-installed-powershell-version) to check your PowerShell version)
- [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)

### Execution

> **NOTE:** If the project is open in Visual Studio, its background restore may interfere with these commands. It is recommended to close all instances of Visual Studio that have this project open before executing.

To build the source, clone or download and unzip the repository. From the repository or distribution root, execute the **build** command from a command prompt and include the desired options from the build options table below:

##### Windows

```
> build [options]
```

##### Linux or macOS

```
./build [options]
```

> **NOTE:** The `build` file will need to be given permission to run using the command `chmod u+x build` before the first execution.

#### Build Options

The following options are case-insensitive. Each option has both a short form indicated by a single `-` and a long form indicated by `--`. The options that require a value must be followed by a space and then the value, similar to running the [dotnet CLI](https://docs.microsoft.com/en-us/dotnet/core/tools/).

<table>
    <tr>
        <th>Short</th>
        <th>Long</th>
        <th>Description</th>
        <th>Example</th>
    </tr>
    <tr>
        <td>&#8209;config</td>
        <td>&#8209;&#8209;configuration</td>
        <td>The build configuration ("Release" or "Debug").</td>
        <td>build&nbsp;&#8209;&#8209;configuration Debug</td>
    </tr>
    <tr>
        <td>&#8209;t</td>
        <td>&#8209;&#8209;test</td>
        <td>Runs the tests after building. This option does not require a value.</td>
        <td>build&nbsp;&#8209;t</td>
    </tr>
</table>

For example the following command creates a Release build with NuGet package with a version generated using the nbgv tool and will also run the tests for every target framework.

##### Windows

```
> build --configuration Release --test
```

##### Linux or macOS

```
./build --configuration Release --test
```

NuGet packages are output by the build to the `/_artifacts/NuGetPackages/` directory. Test results (if applicable) are output to the `/_artifacts/TestResults/` directory.

You can setup Visual Studio to read the NuGet packages like any NuGet feed by following these steps:

1. In Visual Studio, right-click the solution in Solution Explorer, and choose "Manage NuGet Packages for Solution"
2. Click the gear icon next to the Package sources dropdown.
3. Click the `+` icon (for add)
4. Give the source a name such as `spatial4n Local Packages`
5. Click the `...` button next to the Source field, and choose the `/src/_artifacts/NuGetPackages` folder on your local system.
6. Click Ok

Then all you need to do is choose the `spatial4n Local Packages` feed from the dropdown (in the NuGet Package Manager) and you can search for, install, and update the NuGet packages just as you can with any Internet-based feed.

## Visual Studio

### Prerequisites

1. Visual Studio 2019 or higher
2. [.NET 6.0 SDK or higher](https://dotnet.microsoft.com/download/visual-studio-sdks)

> **NOTE:** Preview versions of .NET SDK require the "Use previews of the .NET SDK (requires restart)" option to be enabled in Visual Studio under Tools > Options > Environment > Preview Features. .NET 6.0 is not supported on Visual Studio 2019, so the only option available for building on VS 2019 is to use a pre-release .NET 6.0 SDK.

### Execution

1. Open `Spatial4n.sln` in Visual Studio.
2. Build a project or the entire solution, and wait for Visual Studio to discover the tests.
3. Run or debug the tests in Test Explorer, optionally using the desired filters.

> **TIP:** When running tests in Visual Studio, [set the default processor architecture to either 32 or 64 bit](https://stackoverflow.com/a/45946727) depending on your preference.
