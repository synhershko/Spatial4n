# Making a Release

This project uses Nerdbank.GitVersioning to assist with creating version numbers based on the current branch and commit. This tool handles making pre-release builds on the master branch and production releases on the main branch.

## Prerequisites

- [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- [nbgv tool](https://www.nuget.org/packages/nbgv/) (the version must match the one used in the [dependencies.props](.build/dependencies.props) file)

### Installing NBGV Tool

Perform a one-time install of the nbgv tool using the following dotnet CLI command:

```
dotnet tool install -g nbgv --version <theActualVersion>
```

## Versioning Primer

Being that this is a port of a specific version of spatial4j, it is important to keep the versions aligned. 

- We will only update the major, minor, and build components of the version number if we port over the changes from spatial4j. In other words, if the spatial4j version is 0.4.1, our version should either be or begin with 0.4.1.
- If this is a patch on the .NET side to fix bugs, we will use the revision field to indicate it is a patch. For example, the first patch to 0.4.1 should be 0.4.1.1.

Generally speaking, patches should not have breaking API or behavioral changes. If we need a breaking change to fix a problem, we should consider porting the next version of spatial4j to fix it.

Assembly version should also remain the same when there are no breaking changes so it can be used as a drop-in replacement on .NET Framework.

## Creating a Release Branch

### Ready to Release

When the changes in the main branch are ready to release, create a release branch using the following nbgv tool command as specified in the [documentation](https://github.com/dotnet/Nerdbank.GitVersioning/blob/master/doc/nbgv-cli.md).

For example, the version.json file on the master branch is currently setup as 0.4.1.1-alpha.{height}. We want to go from this version to a release of 0.4.1.1 and set the next version on the main branch as 0.4.1.2-alpha.{height}.

```
nbgv prepare-release --nextVersion 0.4.1.2
```

The command should respond with:

```
release/v0.4.1.1 branch now tracks v0.4.1.1 stabilization and release.
main branch now tracks v0.4.1.2-alpha.{height} development.
```

The tool created a release branch named `release/v0.4.1.1`. Every build from this branch (regardless of how many commits are added) will be versioned 0.4.1.1. 

### Requires Stabilization

When creating a release that may require a few iterations to become stable, it is better to create a beta branch (more about that decision can be found [here](https://github.com/dotnet/Nerdbank.GitVersioning/blob/master/doc/nbgv-cli.md#preparing-a-release)). Starting from the same point as the [Ready to Release](#ready-to-release) scenario, we use the following command.

```
nbgv prepare-release beta --nextVersion 0.4.1.2
```

The command should respond with:

```
release/v0.4.1.1 branch now tracks v0.4.1.1-beta.{height} stabilization and release.
main branch now tracks v0.4.1.2-alpha.{height} development.
```

The tool created a release branch named `release/v0.4.1.1`. Every build from this branch will be given a unique pre-release verrsion starting with 0.4.1.1-beta and ending with the first 10 characters of the git commit hash. 

### Bumping the Version Manually

When skipping to another version that is not a direct increment from the current version, it is necessary to update the version manaually. Before creating a release branch, manually edit the `version` and `assemblyVersion` fields as needed for the release version. See the [version.json schema](https://raw.githubusercontent.com/AArnott/Nerdbank.GitVersioning/master/src/NerdBank.GitVersioning/version.schema.json) to determine valid options.

## Creating a Release Build

The build will automatically launch in Azure DevOps when the release branch is pushed to GitHub. Login to the [Azure DevOps Build Pipeline](https://dev.azure.com/Spatial4n/Spatial4n/_build) where you can view the progress. After the build completes and the tests all pass, download the NuGet files and run some basic checks:

1. Put the `.nupkg` files into a local directory, and add a reference to the directory from Visual Studio. See [this answer](https://stackoverflow.com/a/10240180) for the steps. Check to ensure the NuGet packages can be referenced by a new project and the project will compile.
2. Check the version information in [JetBrains dotPeek](https://www.jetbrains.com/decompiler/) to ensure the assembly version, file version, and informational version are consistent with what was specified in `version.json`.
3. Open the `.nupkg` files in [NuGet Package Explorer](https://www.microsoft.com/en-us/p/nuget-package-explorer/9wzdncrdmdm3#activetab=pivot:overviewtab) and check that files in in the packages are present and that the XML config is up to date.

Optionally, do additional integration testing on project that depend on the component to be sure there are no ill effects.

## Uploading the Release

After the release has passed all checks, it can be uploaded to NuGet.org. Keep in mind that NuGet.org is very unforgiving when it comes to versioning - once a version is uploaded, it is considered to be "taken" permanently (short of contacting support to have them delete it). For that reason, it is important to be thorough with package checks.

To upload to NuGet.org, login to the [Azure DevOps Release Pipeline](https://dev.azure.com/Spatial4n/Spatial4n/_release). 

1. Select the Pipelines > Releases tab.
2. Choose the "Release" for Spatial4n.
3. Click the "Create release" button. A popup form will open.
4. If necessary, select the correct release version from the Artifacts section on the form, as it defaults to the last build.
5. Click the "Create" button. This will trigger the release pipeline, which will upload the NuGet packages.

> **NOTE:** NuGet API keys expire every year. If there is a failure, this is most likely the cause. To fix, login to NuGet.org to generate a new API key, and then update the "NuGet push" task in the release with the API key. You will need to click on "Manage" just below where the External NuGet server is selected, and then click "Edit" on the form that opens to see and edit the ApiKey field.

