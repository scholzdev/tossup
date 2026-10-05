# Build and package a Windows player using the installed Unity Editor.
param([string]$UnityEditor = $env:UNITY_EDITOR)
$ErrorActionPreference = "Stop"
$projectRoot = Split-Path $PSScriptRoot -Parent
$editorVersion = (Select-String -Path "$projectRoot/ProjectSettings/ProjectVersion.txt" -Pattern '^m_EditorVersion: (.+)$').Matches.Groups[1].Value
if (-not $UnityEditor) {
    $UnityEditor = "$env:ProgramFiles/Unity/Hub/Editor/$editorVersion/Editor/Unity.exe"
}
if (-not (Test-Path $UnityEditor -PathType Leaf)) {
    throw "Unity Editor not found: $UnityEditor. Install Unity $editorVersion or pass -UnityEditor <path>."
}
if (Test-Path "$projectRoot/Temp/UnityLockfile") {
    throw "Close this project in Unity before building, or use Tossup > Build Windows Player in the Editor."
}
$buildDir = "$projectRoot/Builds/Windows"
$logPath = "$projectRoot/Builds/unity-build-windows.log"
$playerPath = "$buildDir/Tossup.exe"
New-Item -ItemType Directory -Force $buildDir | Out-Null
$buildId = $env:TOSSUP_BUILD_ID
if (-not $buildId) {
    $buildId = git -C $projectRoot rev-parse --short HEAD
    if ($LASTEXITCODE -ne 0) { throw "Could not read Git build ID; set TOSSUP_BUILD_ID." }
}
$arguments = @('-batchmode','-nographics','-quit','-projectPath',"`"$projectRoot`"",
    '-buildTarget','StandaloneWindows64','-executeMethod','Tossup.EditorTools.TossupBuild.BuildWindows',
    '-buildOutput',"`"$playerPath`"",'-buildId',$buildId,'-logFile',"`"$logPath`"")
Set-Content -Path $logPath -Value ""
$process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -Wait -PassThru
$expected = "Tossup: build Succeeded -> $([System.IO.Path]::GetFullPath($playerPath))"
$succeeded = (Test-Path $logPath) -and (Select-String -Path $logPath -SimpleMatch -Quiet -Pattern $expected)
if ($process.ExitCode -ne 0 -or -not $succeeded -or -not (Test-Path $playerPath -PathType Leaf) -or (Get-Item $playerPath).Length -eq 0) {
    if (Test-Path $logPath) { Get-Content $logPath -Tail 60 }
    throw "Unity Windows build failed. Install Windows Build Support for Unity $editorVersion; see $logPath."
}
$packageDir = "$projectRoot/dist/windows"
New-Item -ItemType Directory -Force $packageDir | Out-Null
$shippingFiles = Get-ChildItem $buildDir | Where-Object { $_.Name -ne 'Tossup_BackUpThisFolder_ButDontShipItWithYourGame' }
Compress-Archive -Path $shippingFiles.FullName -DestinationPath "$packageDir/Tossup-windows.zip" -Force
Write-Output "Built $playerPath and packaged $packageDir/Tossup-windows.zip"
