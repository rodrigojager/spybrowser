$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$feed = Join-Path $root 'artifacts/packages'
$demo = Join-Path $root 'tools/Cursory.PackageDemo'
$project = Join-Path $root 'src/SpyBrowser.Cursory/SpyBrowser.Cursory.csproj'
New-Item -ItemType Directory -Force $feed | Out-Null
Remove-Item (Join-Path $feed 'SpyBrowser.Cursory.*.nupkg') -ErrorAction SilentlyContinue

dotnet build $project -c Release
if ($LASTEXITCODE -ne 0) { throw "Native library build failed: $LASTEXITCODE" }
dotnet pack $project -c Release --no-build -o $feed
if ($LASTEXITCODE -ne 0) { throw "Native library pack failed: $LASTEXITCODE" }
python (Join-Path $root 'tools/package-demo/verify_package.py') --feed $feed --project $root
if ($LASTEXITCODE -ne 0) { throw "NuGet artifact verification failed: $LASTEXITCODE" }

# A same-version candidate must never be satisfied by an older global NuGet cache entry.
$env:NUGET_PACKAGES = Join-Path $feed ('consumer-cache-' + [Guid]::NewGuid().ToString('N'))
dotnet restore (Join-Path $demo 'Cursory.PackageDemo.csproj') --configfile (Join-Path $demo 'NuGet.Config')
if ($LASTEXITCODE -ne 0) { throw "Package consumer restore failed: $LASTEXITCODE" }
dotnet build (Join-Path $demo 'Cursory.PackageDemo.csproj') -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Package consumer build failed: $LASTEXITCODE" }
$dotnet = (Get-Command dotnet).Source
$restrictedPath = Split-Path $dotnet
$env:PATH = $restrictedPath
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$exe = Join-Path $demo 'bin/Release/net8.0/Cursory.PackageDemo.dll'
$start = New-Object System.Diagnostics.ProcessStartInfo
$start.FileName = $dotnet
# Arguments is supported by both Windows PowerShell 5.1 and PowerShell 7.
$start.Arguments = '"' + $exe + '"'
$start.UseShellExecute = $false
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
$p = [System.Diagnostics.Process]::Start($start)
$outTask = $p.StandardOutput.ReadToEndAsync()
$errTask = $p.StandardError.ReadToEndAsync()
$children = @()
while (-not $p.HasExited) {
    $processes = Get-CimInstance Win32_Process
    $children += @($processes | Where-Object { $_.ParentProcessId -eq $p.Id })
    Start-Sleep -Milliseconds 40
}
$p.WaitForExit()
$output = $outTask.GetAwaiter().GetResult()
$errorOutput = $errTask.GetAwaiter().GetResult()
$output | Write-Output
if ($errorOutput) { $errorOutput | Write-Error }
if ($p.ExitCode -ne 0) { throw "NuGet consumer failed with exit code $($p.ExitCode)" }
if ($children.Count -gt 0) { throw "Consumer spawned child process(es): $($children.Name -join ', ')" }
"NoSidecarProof=consumer PID $($p.Id) had no child process in Win32_Process snapshot"
"PATH restricted to dotnet host directory: $restrictedPath"
