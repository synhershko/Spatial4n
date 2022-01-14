# Making a Release

This project uses Nerdbank.GitVersioning to assist with creating version numbers based on the current branch and commit. This tool handles making pre-release builds on the master branch and production releases on the main branch.

## Prerequisites

- [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- [nbgv tool](https://www.nuget.org/packages/nbgv/) (the version must match the one used in the [dependencies.props](.build/dependencies.props) file)

### Installing NBGV Tool

Perform a one-time install of the nbgv tool using the following dotnet CLI command:

```console
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

```console
nbgv prepare-release --nextVersion 0.4.1.2
```

The command should respond with:

```console
release/v0.4.1.1 branch now tracks v0.4.1.1 stabilization and release.
main branch now tracks v0.4.1.2-alpha.{height} development.
```

The tool created a release branch named `release/v0.4.1.1`. Every build from this branch (regardless of how many commits are added) will be versioned 0.4.1.1. 

### Requires Stabilization

When creating a release that may require a few iterations to become stable, it is better to create a beta branch (more about that decision can be found [here](https://github.com/dotnet/Nerdbank.GitVersioning/blob/master/doc/nbgv-cli.md#preparing-a-release)). Starting from the same point as the [Ready to Release](#ready-to-release) scenario, we use the following command.

```console
nbgv prepare-release beta --nextVersion 0.4.1.2
```

The command should respond with:

```console
release/v0.4.1.1 branch now tracks v0.4.1.1-beta.{height} stabilization and release.
main branch now tracks v0.4.1.2-alpha.{height} development.
```

The tool created a release branch named `release/v0.4.1.1`. Every build from this branch will be given a unique pre-release version starting with 0.4.1.1-beta and ending with the first 10 characters of the git commit hash. 

### Bumping the Version Manually

When skipping to another version that is not a direct increment from the current version, it is necessary to update the version manually. Before creating a release branch, manually edit the `version` and `assemblyVersion` fields as needed for the release version. See the [version.json schema](https://raw.githubusercontent.com/AArnott/Nerdbank.GitVersioning/master/src/NerdBank.GitVersioning/version.schema.json) to determine valid options.

## Correcting the Release Version Height

Nerdbank.GitVersioning is designed in a way that it doesn't produce the same version number twice. This is done by using a "git height", which counts the number of commits since the last version update. This works great for CI, but is less than ideal when we don't want to skip over versions for the release.

To compensate for the fact that this counter considers each commit a new "version" the `versionHeightOffset` can be adjusted just prior to the release to set it back to the next version number. This can be done by using the following command to see what version we are currently on, and then adjusting the value accordingly.

```console
nbgv get-version
```

> **NOTE:** At the time of this writing Nerdbank.GitVersioning doesn't support 4-component version numbers for NuGet package version or informational version. This may be added in the future. See https://github.com/dotnet/Nerdbank.GitVersioning/issues/709. However, our build is setup to create these numbers so if a 4-component number needs to be created, it is a good idea to do an actual build to determine if the version numbers are correct.

Then open the `version.json` file at the repository root, and set the `versionHeightOffset` using the formula `versionHeightOffset - (versionHeight - desiredHeight) - 1`. For example, if the current version is 2.0.1-beta-0014 and we want to release 2.0.1-beta-0005 (because the last version released was 2.0.1-beta-0004), and the `versionHeightOffset` is set to -21:

###### Calculating versionHeightOffset
```
-21 - ((14 - 5) + 1) = -31
```

So, we must set `versionHeightOffset` to -31 and commit the change.

Note that the + 1 is because we are creating a new commit that will increment the number by 1. The change must be committed to see the change to the version number. Run the command again to check that the version will be correct.

```console
nbgv get-version
```

## Creating a Release Build

The build will automatically launch in Azure DevOps when the release branch is pushed to GitHub. Login to the [Azure DevOps Build Pipeline](https://dev.azure.com/Spatial4n/Spatial4n/_build) where you can view the progress. After the build completes and the tests all pass, download the NuGet files and run some basic checks:

1. Put the `.nupkg` files into a local directory, and add a reference to the directory from Visual Studio. See [this answer](https://stackoverflow.com/a/10240180) for the steps. Check to ensure the NuGet packages can be referenced by a new project and the project will compile.
2. Check the version information in [JetBrains dotPeek](https://www.jetbrains.com/decompiler/) to ensure the assembly version, file version, and informational version are consistent with what was specified in `version.json`.
3. Open the `.nupkg` files in [NuGet Package Explorer](https://www.microsoft.com/en-us/p/nuget-package-explorer/9wzdncrdmdm3#activetab=pivot:overviewtab) and check that files in in the packages are present and that the XML config is up to date.

Optionally, do additional integration testing on project that depend on the component to be sure there are no ill effects.

#### Overriding the Version

If something goes wrong with the version generation during the build, it is possible to create a new build with the correct numbers by explicitly defining **all 4** of the following environment variables in the [Azure DevOps Build Pipeline](https://dev.azure.com/Spatial4n/Spatial4n/_build) and triggering it manually. This should only be done as a last resort to unblock a blocked release.

- `PackageVersion` - The NuGet package version.
- `AssemblyVersion` - The binary version of the assembly.
- `FileVersion` - The version that is stamped on the assembly that can be viewed in the file properties.
- `InformationalVersion` - This field defines the first part of the informational version. The commit hash (or partial commit hash) will be suffixed to this value automatically. (i.e. `+ad0250a082`)

> **NOTE:** Azure Artifacts does not allow NuGet packages with duplicate versions to be uploaded. If Azure Artifacts is enabled on the release build, it can be disabled by removing the `ArtifactFeedID` variable from the build pipeline.

## Preparing for Release

### Tagging the Commit

Before creating the release, tag the commit that the package was built on (the partial commit hash is available in the Azure DevOps pipeline and also in the informational version of the assembly).

```console
git tag -a <package-version> <commit-hash> -m "<package-version>"
git push <remote-name> <release-branch> --tags
```

### Creating a GitHub Release

Go to the [GitHub Releases Page](https://github.com/synhershko/Spatial4n/releases) and click on the "Draft a new release" button.

Select the tag that was created in the previous step, and also enter it as the title of the release.

Use the following template to summarize the commits for the current release:

```console
## Change Log

1. **BREAKING:** `Some.NameSpace.SomeType`: Renamed xyz (fixes #35)
2. **BUG:** `Some.NameSpace.SomeType`: Corrected invalid version number string (fixes #36)
3. Added tests for .NET 6.0
```

- Start with the namespace and/or type that is affected (if it is a broad change, begin with `**SWEEP:**`)
- Highlight any bugs or breaking changes so they are easy to identify.
- Include a link to the issue or PR if there is one.

When complete, check the box (or not) indicating it is a pre-release and click "Save draft". Publishing the release takes place after the release to NuGet.org.

## Uploading the Release

After the release has passed all checks, it can be uploaded to NuGet.org. Keep in mind that NuGet.org is very unforgiving when it comes to versioning - once a version is uploaded, it is considered to be "taken" permanently (short of contacting support to have them delete it). For that reason, it is important to be thorough with package checks.

To upload to NuGet.org, login to the [Azure DevOps Release Pipeline](https://dev.azure.com/Spatial4n/Spatial4n/_release). 

1. Select the Pipelines > Releases tab.
2. Choose the "Release" for Spatial4n.
3. Click the "Create release" button. A popup form will open.
4. If necessary, select the correct release version from the Artifacts section on the form, as it defaults to the last build.
5. Click the "Create" button. This will trigger the release pipeline, which will upload the NuGet packages.

> **NOTE:** NuGet API keys expire every year. If there is a failure, this is most likely the cause. To fix, login to NuGet.org to generate a new API key, and then update the "NuGet push" task in the release with the API key. You will need to click on "Manage" just below where the External NuGet server is selected, and then click "Edit" on the form that opens to see and edit the ApiKey field.

## Post Release Steps

After the release has been uploaded to NuGet.org:

1. Open the [GitHub Releases Page](https://github.com/synhershko/Spatial4n/releases)
2. Edit the current draft release that was created in [Creating a GitHub Release](#creating-a-github-release)
3. Review to ensure proper spelling, links, etc.
4. Scroll to the bottom of the page and click on "Publish release"

### Merge the Release Branch

Finally, merge the release branch to the main branch and push the changes to GitHub.

```console
git checkout <main-branch>
git merge <release-branch>
git push <remote> <main-branch>
```

