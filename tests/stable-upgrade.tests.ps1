[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PreviousSetup,
    [Parameter(Mandatory)][string]$NextSetup,
    [Parameter(Mandatory)][string]$NextFeed,
    [Parameter(Mandatory)][version]$ExpectedVersion
)

$ErrorActionPreference = 'Stop'
if ($env:RUNNER_ENVIRONMENT -ne 'github-hosted' -or $env:GITHUB_ACTIONS -ne 'true' -or
    (Test-Path 'HKLM:\Software\Kvieta') -or (Get-Service KvietaGuardian -ErrorAction SilentlyContinue)) {
    throw 'This installation test requires a fresh GitHub-hosted Windows runner.'
}

function Invoke-Setup([string]$path, [string[]]$arguments) {
    $process = Start-Process -FilePath (Resolve-Path -LiteralPath $path) -ArgumentList $arguments -PassThru
    try {
        if (-not $process.WaitForExit(120000)) { throw 'Setup timed out.' }
        return $process.ExitCode
    } finally { $process.Dispose() }
}

$fixture = Join-Path $env:RUNNER_TEMP 'kvieta-upgrade-fixture'
New-Item -ItemType Directory -Path $fixture | Out-Null
$userName = 'kvieta-test'
$password = ConvertTo-SecureString ("Test!" + [guid]::NewGuid().ToString('N')) -AsPlainText -Force
$enrolledUser = New-LocalUser -Name $userName -Password $password
$settingsPath = Join-Path $fixture 'settings.json'
@{
    SchemaVersion = 10
    SetupCompleted = $true
    # The Family enum is stored as "Protected" for settings-file compatibility.
    Mode = 'Protected'
    AdminPin = @{
        Version = 1
        Iterations = 210000
        SaltBase64 = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(16))
        HashBase64 = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    }
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $settingsPath -Encoding utf8NoBOM
$request = @{ UserSid = $enrolledUser.SID.Value; SettingsPath = $settingsPath } | ConvertTo-Json -Compress
$payload = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($request))

$installed = Invoke-Setup $PreviousSetup @('--elevated-install', '--guardian-payload', $payload)
if ($installed -ne 0 -or (Get-Service KvietaGuardian).Status -ne 'Running') {
    $programData = Join-Path $env:ProgramData 'Kvieta'
    $guardianLog = Join-Path $programData 'guardian-install.log'
    if (Test-Path -LiteralPath $guardianLog) { Get-Content -LiteralPath $guardianLog -Tail 20 }
    Get-ChildItem (Join-Path $programData 'SetupLogs') -Filter '*.log' -ErrorAction SilentlyContinue |
        Select-String -Pattern 'Guardian provisioning exited with code' |
        ForEach-Object { $_.Line }
    throw "The baseline Family installation failed (exit $installed)."
}

$protectedDirectory = Join-Path $env:ProgramData 'Kvieta'
$data = @($settingsPath, (Join-Path $protectedDirectory 'protected-settings.json'),
    (Join-Path $protectedDirectory 'guardian-enrollment.json'))
$before = @(Get-FileHash -LiteralPath $data)
if ((Invoke-Setup $PreviousSetup @('--elevated-install', '--preserve-installation')) -ne 1638) {
    throw 'The update entry point accepted an equal-version package without an explicit repair.'
}
$repair = Invoke-Setup $PreviousSetup @('--elevated-install', '--preserve-installation', '--force-reinstall')
if ($repair -ne 0 -or (Get-Service KvietaGuardian).Status -ne 'Running') {
    throw "The same-version Family repair failed or did not restart Guardian (exit $repair)."
}
$afterRepair = @(Get-FileHash -LiteralPath $data)
if (Compare-Object $before $afterRepair -Property Path, Hash) { throw 'The repair changed protected installation data.' }
$repairLog = Get-ChildItem (Join-Path $protectedDirectory 'SetupLogs') -Filter 'Kvieta-setup-*.log' |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $repairLog -or -not (Select-String -LiteralPath $repairLog.FullName -Pattern 'REINSTALLMODE=amus' -Quiet)) {
    throw 'The same-product repair did not request replacement of the installed binaries.'
}

$cache = Join-Path $env:ProgramFiles 'Kvieta.UpdateCache'
if (-not (Test-Path -LiteralPath (Join-Path $cache 'enabled'))) {
    throw 'Setup did not register startup updates.'
}
$setupName = "Kvieta-Setup-$ExpectedVersion-win-x64.exe"
Copy-Item -LiteralPath $NextSetup -Destination (Join-Path $cache $setupName)
Copy-Item -LiteralPath $NextFeed -Destination (Join-Path $cache "$setupName.json")
Set-Content -LiteralPath (Join-Path $cache 'pending.version') -Value $ExpectedVersion.ToString() -NoNewline

$installedExecutable = Join-Path $env:ProgramFiles 'Kvieta/Kvieta.exe'
$interactiveUser = (Get-CimInstance Win32_ComputerSystem).UserName
if ([string]::IsNullOrWhiteSpace($interactiveUser)) { throw 'An interactive Windows desktop is required for the startup test.' }
$launchTask = 'Kvieta CI Open Control Center'
$action = New-ScheduledTaskAction -Execute $installedExecutable -WorkingDirectory (Split-Path -Parent $installedExecutable)
$principal = New-ScheduledTaskPrincipal -UserId $interactiveUser -LogonType Interactive -RunLevel Limited
Register-ScheduledTask -TaskName $launchTask -Action $action -Principal $principal | Out-Null
Start-ScheduledTask -TaskName $launchTask

$deadline = [DateTimeOffset]::UtcNow.AddMinutes(4)
$attemptPath = Join-Path $cache 'attempt.json'
$attempt = $null
$reopened = $false
while ([DateTimeOffset]::UtcNow -lt $deadline) {
    if (Test-Path -LiteralPath $attemptPath) {
        $attempt = Get-Content -LiteralPath $attemptPath -Raw | ConvertFrom-Json
        if ($null -ne $attempt.ExitCode -and $attempt.ExitCode -ne 0) { break }
        if ($attempt.ExitCode -eq 0) {
            $reopened = $null -ne (Get-CimInstance Win32_Process -Filter "Name='Kvieta.exe'" |
                Where-Object { $_.CommandLine -like '*--resume-after-startup-update*' } | Select-Object -First 1)
            if ($reopened) { break }
        }
    }
    Start-Sleep -Milliseconds 500
}
if (-not $reopened -or $attempt.ExitCode -ne 0 -or (Get-Service KvietaGuardian).Status -ne 'Running') {
    foreach ($taskName in $launchTask, 'Kvieta Apply Update', 'Kvieta Install Update') {
        $info = Get-ScheduledTaskInfo -TaskName $taskName -ErrorAction SilentlyContinue
        if ($null -ne $info) { Write-Output "$taskName last result $($info.LastTaskResult), last run $($info.LastRunTime)" }
    }
    throw "The automated protected upgrade did not reopen Kvieta or restart Guardian (installer exit $($attempt.ExitCode))."
}
$registeredVersion = (Get-ItemProperty -LiteralPath 'HKLM:\Software\Kvieta').InstalledVersion
if ([version]$registeredVersion -ne $ExpectedVersion -or
    (Get-Item -LiteralPath $installedExecutable).VersionInfo.FileVersion -ne "$ExpectedVersion.0") {
    throw 'The upgrade did not install the expected executable and registry version.'
}
$after = @(Get-FileHash -LiteralPath $data)
if (Compare-Object $before $after -Property Path, Hash) { throw 'The update changed protected installation data.' }
if ((Invoke-Setup $PreviousSetup @('--elevated-install', '--preserve-installation')) -ne 1638) {
    throw 'The update entry point did not reject an older Setup.'
}
Write-Output 'A standard user opened Kvieta, got the signed startup update, reopened the new version and retained Guardian protection.'
