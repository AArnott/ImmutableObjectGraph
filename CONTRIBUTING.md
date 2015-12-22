Contributing to this project
============================

## Prerequisites

This project is actively developed using the following software.
It is highly recommended that anyone contributing to this library use the same
software.

1. [Visual Studio 2015][VS].
2. [NuProj for VS2015][NuProj]

All other dependencies are acquired via NuGet.

## Building

To build this repository from the command line, you must first execute a complete NuGet package restore.
Assuming your working directory is the root directory of this git repo, the command is:

    nuget restore src

You may need to [download NuGet.exe][NuGetClient] first.

Everything in the repo may be built via building the solution file
either from Visual Studio 2015 or the command line:

    msbuild src\ImmutableObjectGraph.sln

## Testing

The Visual Studio 2015 Test Explorer will list and execute all tests.

## Pull requests

Pull requests are welcome! They may contain additional test cases (e.g. to demonstrate a failure),
and/or product changes (with bug fixes or features). All product changes should be accompanied by
additional tests to cover and justify the product change unless the product change is strictly an
efficiency improvement and no outwardly observable change is expected.

In the master branch, all tests should always pass. Added tests that fail should be marked as Skip
via `[Fact(Skip = "Test does not pass yet")]` or similar message to keep our test pass rate at 100%.

## Self-service releases for contributors

As soon as you send a pull request, a build is executed and updated NuGet packages
are published to this Package Feed:

    https://ci.appveyor.com/nuget/ImmutableObjectGraph

By adding this URL to your package sources you can immediately install your version
of the NuGet packages to your project. This can be done by adding a nuget.config file
with the following content to the root of your project's repo:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
        <add key="ImmutableObjectGraph CI" value="https://ci.appveyor.com/nuget/ImmutableObjectGraph" />
    </packageSources>
</configuration>
```

You can then install the package(s) while you have your new "ImmutableObjectGraph CI" package source selected:

```powershell
Install-Package ImmutableObjectGraph.Generation -Pre -Version 0.1.41-beta-g02f355c05d
```

Take care to set the package version such that it exactly matches the AppVeyor build
for your pull request. You can get the version number by reviewing the result of the
validation build for your pull request, clicking ARTIFACTS, and noting the version
of the produced packages.

There are two styles of tests:

1. Generated and compiled at build time.
2. Generated and compiled at test execution time.

The build-time generated tests are in source files with `Build Action` set to `Compile`
and `Custom Tool` set to `MSBuild:GenerateCodeFromAttributes`. This style is best
suited for when you will be testing the functionality of the generated code and must write
`[Fact]`'s that call into that generated code.

The test execution time tests are in source files with `Build Action` set to
`Embedded Resource` and `Custom Tool` is blank. There are associated test method(s)
in another compiled test class (typically `CodeGenTests.cs`) that will extract
the embedded resource at test execution time, execute code generation, and compile the
result. This style is best suited for tests that want to assert API aspects of the generated
code (such as asserting that no public constructor exists).

 [VS]: https://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs.aspx
 [NuProj]: https://onedrive.live.com/redir?resid=63D0C265F96E43D!2477835&authkey=!AHh2k9FoNR-nFHo&ithint=file%2cmsi
 [NuGetClient]: https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
