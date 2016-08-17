<#
.SYNOPSIS
    Prepares a machine to build and test this project.
#>
Param(
)

Push-Location $PSScriptRoot
try {
    $toolsPath = "$PSScriptRoot\tools"

    # First restore NuProj packages since the solution restore depends on NuProj evaluation succeeding.
    gci "$PSScriptRoot\src\project.json" -rec |? { $_.FullName -imatch 'nuget' } |% {
        & "$toolsPath\Restore-NuGetPackages.ps1" -Path $_ -Verbosity quiet
    }

    & "$toolsPath\Restore-NuGetPackages.ps1" -Path "$PSScriptRoot\src" -Verbosity quiet

    Write-Host "Successfully restored all dependencies" -ForegroundColor Green
}
catch {
    Write-Error "Aborting script due to error"
    exit $lastexitcode
}
finally {
    Pop-Location
}
