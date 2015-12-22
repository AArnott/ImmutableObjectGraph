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

 [VS]: https://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs.aspx
 [NuProj]: https://onedrive.live.com/redir?resid=63D0C265F96E43D!2477835&authkey=!AHh2k9FoNR-nFHo&ithint=file%2cmsi
 [NuGetClient]: https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
