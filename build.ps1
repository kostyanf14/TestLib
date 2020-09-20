# Like set -e
$ErrorActionPreference = "Stop"

Set-Location -Path "$PSScriptRoot"

class ProjectInfo
{
    [string]$ProjectPath
    [string]$Configurations
    [string]$Platforms

    ProjectInfo($ProjectPath, $Configurations, $Platforms) {
       $this.ProjectPath = $ProjectPath
       $this.Configurations = $Configurations
       $this.Platforms = $Platforms
    }

    [bool] Build() {
        Write-Host("Building: " + $this.ProjectPath + $this.Configurations + $this.Platforms)

        $args = $this.ProjectPath + " /p:Configuration=" + $this.Configurations + " /p:Platform=" + $this.Platforms + ""

        Start-Process -FilePath "MSBuild.exe" -NoNewWindow -Wait -ArgumentList $args
           
        return $True
    }
}

$ProjectList = @(
    [ProjectInfo]::new("$PSScriptRoot\TestLib.Worker.Updater\TestLib.Worker.Updater.csproj", 'Release', 'AnyCPU'),
    [ProjectInfo]::new("$PSScriptRoot\TestLib.UpdateServer\TestLib.UpdateServer.csproj", 'Release', 'AnyCPU'),
    [ProjectInfo]::new("$PSScriptRoot\TestLib.WorkerService\TestLib.WorkerService.csproj", 'Release', 'x64'),
    [ProjectInfo]::new("$PSScriptRoot\TestLib.WorkerService\TestLib.WorkerService.csproj", 'Release', 'x86')
)

$SolutionPath = "$PSScriptRoot\TestLib.sln"
$BuildDir = "$PSScriptRoot\Build"
$ArtifactsDir = "$BuildDir\artifacts"

if (Test-Path -Path "$BuildDir") {
    Remove-Item -Path "$BuildDir" -Recurse
}

& "NuGet.exe" "restore" $SolutionPath

Foreach ($Project in $ProjectList)
{
    $Project.Build()
}
