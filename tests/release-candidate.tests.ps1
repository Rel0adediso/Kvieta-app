[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Candidate,
    [Parameter(Mandatory)][version]$ExpectedVersion,
    [string]$PreviousSetup,
    [version]$PreviousVersion = '6.5.0'
)

$ErrorActionPreference = 'Stop'
$installerKey = 'HKLM:\Software\Kvieta'
if ($env:RUNNER_ENVIRONMENT -ne 'github-hosted' -or $env:GITHUB_ACTIONS -ne 'true' -or
    (Test-Path $installerKey) -or (Get-Service KvietaGuardian -ErrorAction SilentlyContinue)) {
    throw 'This acceptance test requires a fresh GitHub-hosted Windows runner.'
}

function Invoke-InstallerProcess([string]$path, [string[]]$arguments) {
    $process = Start-Process -FilePath (Resolve-Path -LiteralPath $path).Path -ArgumentList $arguments -PassThru
    try {
        if (-not $process.WaitForExit(120000)) { throw "Timed out waiting for $path." }
        return $process.ExitCode
    } finally {
        $process.Dispose()
    }
}

$fixture = Join-Path $env:RUNNER_TEMP 'kvieta-candidate'
New-Item -ItemType Directory -Path $fixture | Out-Null
if (-not $PreviousSetup) {
    $PreviousSetup = Join-Path $fixture 'Kvieta-Setup-Alpha-6.5.exe'
    Invoke-WebRequest -Uri 'https://github.com/Rel0adediso/Kvieta-app/releases/download/kvieta-alpha-6.5/Kvieta-Setup-Alpha-6.5.exe' -OutFile $PreviousSetup
    if ((Get-FileHash -LiteralPath $PreviousSetup).Hash -ne 'b3bbb4f7d58422b3e34e13ece5a6c2615d1a6dd1d1b94680ed9599f767dd7bc7') {
        throw 'The published Alpha 6.5 Setup did not match the pinned release hash.'
    }
}

$password = ConvertTo-SecureString ("Test!" + [guid]::NewGuid().ToString('N')) -AsPlainText -Force
$user = New-LocalUser -Name 'kvieta-candidate' -Password $password
$settings = Join-Path $fixture 'settings.json'
@{
    SchemaVersion = 10
    SetupCompleted = $true
    Mode = 'Protected'
    DefaultDailyLimitMinutes = 95
    BlockedWebDomains = @('example.org')
    AdminPin = @{
        Version = 1
        Iterations = 210000
        SaltBase64 = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(16))
        HashBase64 = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    }
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $settings -Encoding utf8NoBOM
$request = @{ UserSid = $user.SID.Value; SettingsPath = $settings } | ConvertTo-Json -Compress
$payload = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($request))
$previousExit = Invoke-InstallerProcess $PreviousSetup @('--elevated-install', '--guardian-payload', $payload)
if ($previousExit -ne 0 -or (Get-Service KvietaGuardian).Status -ne 'Running') {
    throw "The previous Family installation failed (exit $previousExit)."
}
$previousProduct = Get-ItemProperty -LiteralPath $installerKey
if ([version]$previousProduct.InstalledVersion -ne $PreviousVersion) {
    throw "The fixture did not install version $PreviousVersion."
}

$programData = Join-Path $env:ProgramData 'Kvieta'
$policyPath = Join-Path $programData 'protected-settings.json'
$enrollmentPath = Join-Path $programData 'guardian-enrollment.json'
$protectedFiles = @($settings, $policyPath, $enrollmentPath)
$previousPolicy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json -AsHashtable
$previousEnrollment = (Get-FileHash -LiteralPath $enrollmentPath).Hash
$upgrade = Invoke-InstallerProcess $Candidate @('--elevated-install', '--preserve-installation', '--legacy-user-sid', $user.SID.Value)
$product = Get-ItemProperty -LiteralPath $installerKey
$application = Join-Path $env:ProgramFiles 'Kvieta/Kvieta.exe'
if ($upgrade -ne 0 -or [version]$product.InstalledVersion -ne $ExpectedVersion -or
    (Get-Item -LiteralPath $application).VersionInfo.FileVersion -ne "$ExpectedVersion.0" -or
    $product.ProductCode -eq $previousProduct.ProductCode -or
    (Test-Path "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\$($previousProduct.ProductCode)") -or
    (Get-Service KvietaGuardian).Status -ne 'Running') {
    throw "Setup did not replace build $PreviousVersion with the candidate and restart Guardian (exit $upgrade)."
}
$policy = Get-Content -LiteralPath $policyPath -Raw | ConvertFrom-Json -AsHashtable
if (-not $previousPolicy.Contains('BlockShortFormVideo')) {
    if ($policy.BlockShortFormVideo -ne $false) { throw 'The new short video policy was enabled during migration.' }
    $policy.Remove('BlockShortFormVideo')
}
if (($policy | ConvertTo-Json -Depth 40 -Compress) -cne ($previousPolicy | ConvertTo-Json -Depth 40 -Compress) -or
    (Get-FileHash -LiteralPath $enrollmentPath).Hash -ne $previousEnrollment -or
    (Get-FileHash -LiteralPath $settings).Hash -ne (Get-FileHash -LiteralPath $policyPath).Hash) {
    throw "Upgrading from build $PreviousVersion changed the existing policy, profile, or PIN enrollment."
}

$beforeRepair = @(Get-FileHash -LiteralPath $protectedFiles)
$repairSource = Join-Path $env:ProgramFiles "Kvieta/Kvieta-Repair-$(([guid]$product.ProductCode).ToString('D')).msi"
if (-not (Test-Path -LiteralPath $repairSource)) { throw 'Setup did not retain an MSI for offline repair.' }
$standardUser = [pscredential]::new("$env:COMPUTERNAME\$($user.Name)", $password)
$stop = Start-Process "$env:SystemRoot/System32/sc.exe" -ArgumentList @('stop', 'KvietaGuardian') `
    -Credential $standardUser -WorkingDirectory $env:SystemRoot -PassThru
try {
    if (-not $stop.WaitForExit(15000) -or $stop.ExitCode -ne 5 -or (Get-Service KvietaGuardian).Status -ne 'Running') {
        throw 'A standard user must be denied permission to stop Guardian.'
    }
} finally {
    $stop.Dispose()
}
$repair = Invoke-InstallerProcess $application @('--windows-admin-verification', '--audit-event', 'recovery.installer.repair')
$repairLog = Get-ChildItem -LiteralPath (Join-Path $programData 'SetupLogs') -Filter 'Kvieta-repair-*.log' |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($repair -ne 0 -or (Get-Service KvietaGuardian).Status -ne 'Running' -or -not $repairLog -or
    -not (Select-String -LiteralPath $repairLog.FullName -Pattern 'MainEngineThread is returning (0|3010)$' -Quiet)) {
    throw "The Windows administrator recovery repair failed (exit $repair)."
}
if (Compare-Object $beforeRepair @(Get-FileHash -LiteralPath $protectedFiles) -Property Path, Hash) {
    throw 'Administrator recovery repair changed protected installation data.'
}

$logPath = Join-Path $env:RUNNER_TEMP 'kvieta-candidate-uninstall.log'
$uninstallDeadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
do {
    $uninstall = Invoke-InstallerProcess "$env:SystemRoot/System32/msiexec.exe" @('/x', $product.ProductCode, '/qn', '/norestart', '/L*v', ('"' + $logPath + '"'))
    if ($uninstall -ne 1618 -or [DateTimeOffset]::UtcNow -ge $uninstallDeadline) { break }
    Start-Sleep -Seconds 2
} while ($true)
if ($uninstall -ne 0 -or (Test-Path -LiteralPath $installerKey) -or
    (Get-Service KvietaGuardian -ErrorAction SilentlyContinue) -or (Test-Path -LiteralPath $application) -or
    (Test-Path -LiteralPath $repairSource)) {
    Get-CimInstance Win32_Process -Filter "Name='msiexec.exe'" | Select-Object ProcessId, ParentProcessId, CommandLine | Format-List
    Get-WinEvent -FilterHashtable @{ LogName = 'Application'; ProviderName = 'MsiInstaller' } -MaxEvents 12 -ErrorAction SilentlyContinue |
        Select-Object TimeCreated, Id, Message | Format-List
    throw "The MSI uninstall did not remove the candidate and Guardian (exit $uninstall)."
}
foreach ($task in 'Kvieta Prepare Update', 'Kvieta Apply Update', 'Kvieta Install Update') {
    if (Get-ScheduledTask -TaskName $task -ErrorAction SilentlyContinue) { throw "Uninstall left the $task scheduled task." }
}
Write-Output "The packaged candidate upgraded build $PreviousVersion, passed administrator repair, and uninstalled."
