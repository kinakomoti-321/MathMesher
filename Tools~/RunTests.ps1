param(
    [Parameter(Mandatory=$true)][string]$ProjectPath,
    [string]$UnityPath = 'C:/Program Files/Unity/Hub/Editor/2022.3.22f1/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath($ProjectPath)
$output = Split-Path $project -Parent
$unityArguments = @('-batchmode', '-nographics', '-projectPath', "`"$project`"", '-runTests', '-testPlatform', 'EditMode', '-testResults', "`"$output/test-results.xml`"", '-logFile', "`"$output/unity-tests.log`"")
$process = Start-Process -FilePath $UnityPath -ArgumentList $unityArguments -WindowStyle Hidden -PassThru
Write-Output "Unity test process ID: $($process.Id)"
