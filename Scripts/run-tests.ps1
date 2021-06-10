param(
    [string]$dotnet = "dotnet",
    [ValidateSet("all", "net5.0", "net462")]
    [string]$framework = "all",
    [ValidateSet("all", "rewriting", "testing", "tasks-testing", "actors", "actors-testing", "standalone")]
    [string]$test = "all",
    [string]$filter = "",
    [string]$logger = "",
    [ValidateSet("quiet", "minimal", "normal", "detailed", "diagnostic")]
    [string]$v = "normal"
)

Import-Module $PSScriptRoot/powershell/common.psm1 -Force

$targets = [ordered]@{
    "rewriting" = "Tests.Rewriting"
    "testing" = "Tests.BugFinding"
    "tasks-testing" = "Tests.Tasks.BugFinding"
    "actors" = "Tests.Actors"
    "actors-testing" = "Tests.Actors.BugFinding"
    "standalone" = "Tests.Standalone"
}

$ilverify = FindProgram("ilverify.exe")
if ($null -eq $ilverify) {
    &dotnet tool install --global dotnet-ilverify
    $ilverify = FindProgram("ilverify.exe");
}

$dotnet_path = FindDotNet("dotnet");

[System.Environment]::SetEnvironmentVariable('COYOTE_CLI_TELEMETRY_OPTOUT', '1')

Write-Comment -prefix "." -text "Running the Coyote tests" -color "yellow"

# Run all enabled tests.
foreach ($kvp in $targets.GetEnumerator()) {
    if (($test -ne "all") -and ($test -ne $($kvp.Name))) {
        continue
    }

    $frameworks = Get-ChildItem -Path "$PSScriptRoot/../Tests/$($kvp.Value)/bin" | Where-Object Name -CNotIn "net48", "netstandard2.0", "netstandard2.1", "netcoreapp3.1" | Select-Object -expand Name

    foreach ($f in $frameworks) {
        if (($framework -ne "all") -and ($f -ne $framework)) {
            continue
        }

        if (($($kvp.Name) -eq "standalone") -and ($f -eq "net462")) {
            continue
        }

        $target = "$PSScriptRoot/../Tests/$($kvp.Value)/$($kvp.Value).csproj"

        if ($f -eq "net5.0") {
            $AssemblyName = GetAssemblyName($target)
            $command = "$PSScriptRoot/../Tests/$($kvp.Value)/bin/net5.0/$AssemblyName.dll"
            $command = $command + ' -r "' + "$PSScriptRoot/../Tests/$($kvp.Value)/bin/net5.0/*.dll" + '"'
            $command = $command + ' -r "' + "$dotnet_path/packs/Microsoft.NETCore.App.Ref/5.0.0/ref/net5.0/*.dll" + '"'
            $command = $command + ' -r "' + "$PSScriptRoot/../bin/net5.0/*.dll" + '"'
            Invoke-ToolCommand -tool $ilverify -cmd $command -error_msg "Verifying assembly failed"
        }

        Invoke-DotnetTest -dotnet $dotnet -project $($kvp.Name) -target $target -filter $filter -logger $logger -framework $f -verbosity $v
    }
}

Write-Comment -prefix "." -text "Done" -color "green"
