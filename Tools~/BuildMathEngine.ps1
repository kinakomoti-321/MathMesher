param([string]$CacheDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'MathMesherBuild'))
# Rebuilds Editor/Plugins/MathMesher.Fonts.dll (Typography's OpenType reader + embedded math fonts) and the vendored
# LibTessDotNet sources from pinned upstream sources. Package users never need to run this.
$ErrorActionPreference = 'Stop'
$packageRoot = Split-Path $PSScriptRoot -Parent
$fontsCommit = '69fb790c40693ae85d2d8679655ee04e88f35aea'      # CSharpMath repository: source of the bundled font files only
$typographyCommit = 'd331968f3ee714332a8ea4fa95a21c15812a8d93'
$tessCommit = 'bdc0b0f94904dd94c3edfb2c25aa358695276e09'
$utf8 = New-Object System.Text.UTF8Encoding($false)
New-Item -ItemType Directory -Force -Path $CacheDirectory | Out-Null

function Get-PinnedSource([string]$Repository, [string]$Commit, [string]$Name) {
    $target = Join-Path $CacheDirectory $Name
    if (!(Test-Path -LiteralPath $target)) {
        $archive = Join-Path $CacheDirectory "$Name.zip"
        Invoke-WebRequest "https://api.github.com/repos/$Repository/zipball/$Commit" -OutFile $archive
        Expand-Archive -LiteralPath $archive -DestinationPath $target
    }
    return (Get-ChildItem -LiteralPath $target -Directory | Select-Object -First 1).FullName
}

$fontRepository = Get-PinnedSource 'verybadcat/CSharpMath' $fontsCommit 'fonts'
$typography = Get-PinnedSource 'LayoutFarm/Typography' $typographyCommit 'typography'
$tess = Get-PinnedSource 'speps/LibTessDotNet' $tessCommit 'tess'
$buildRoot = Join-Path $CacheDirectory 'build'
if (Test-Path -LiteralPath $buildRoot) { Remove-Item -LiteralPath $buildRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $buildRoot | Out-Null

$headers = New-Object System.Text.StringBuilder
function Copy-Typography([string]$Source, [string]$Name) {
    $destination = Join-Path $buildRoot $Name
    foreach ($file in Get-ChildItem -LiteralPath $Source -Filter '*.cs' -Recurse) {
        if ($file.Name -eq 'AssemblyInfo.cs' -or $file.FullName -match '[\\/](obj|bin)[\\/]') { continue }
        $relative = $file.FullName.Substring($Source.Length).TrimStart('\', '/')
        $target = Join-Path $destination $relative
        New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
        $content = [System.IO.File]::ReadAllText($file.FullName)
        # record the original license header comments of every compiled file
        $headerLines = ($content -split "`n") | Where-Object { $_ -match '^\s*//' } | Select-Object -First 4
        [void]$headers.AppendLine("$Name/$relative").AppendLine(($headerLines -join "`n").TrimEnd()).AppendLine()
        $content = [regex]::Replace($content, '\bTypography\b', 'MathMesher.Internal.Typography')
        [System.IO.File]::WriteAllText($target, $content, $utf8)
    }
}

Copy-Typography (Join-Path $typography 'Typography.OpenFont') 'OpenFont'
Copy-Typography (Join-Path $typography 'PixelFarm.Typography/3_Typography_Contours') 'Contours'
Get-ChildItem -LiteralPath (Join-Path $buildRoot 'Contours') -Filter '*.cs' -Recurse | Where-Object { $_.Name -ne 'GlyphOutlineBuilderBase.cs' } | Remove-Item

$fonts = Join-Path $buildRoot 'Fonts'
New-Item -ItemType Directory -Force -Path $fonts | Out-Null
Copy-Item -Path (Join-Path $fontRepository 'CSharpMath.Rendering/Reference Fonts/*.otf') -Destination $fonts -Force
$project = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <AssemblyName>MathMesher.Fonts</AssemblyName>
    <RootNamespace>MathMesher.Internal</RootNamespace>
    <LangVersion>9.0</LangVersion>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <NoWarn>$(NoWarn);CS8625;CS8618;CS8603;CS8602;CS8604;CS8600;CS8601;CS0169;CS0649;CS0162;CS0168;CS0219;CS0414</NoWarn>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
  <ItemGroup>
    <EmbeddedResource Include="Fonts/*.otf">
      <LogicalName>MathMesher.Internal.Fonts.%(Filename)%(Extension)</LogicalName>
    </EmbeddedResource>
  </ItemGroup>
</Project>
'@
[System.IO.File]::WriteAllText((Join-Path $buildRoot 'MathMesher.Fonts.csproj'), $project, $utf8)
& dotnet build (Join-Path $buildRoot 'MathMesher.Fonts.csproj') -c Release --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Font engine compilation failed.' }
$plugins = Join-Path $packageRoot 'Editor/Plugins'
New-Item -ItemType Directory -Force -Path $plugins | Out-Null
Copy-Item -LiteralPath (Join-Path $buildRoot 'bin/Release/netstandard2.1/MathMesher.Fonts.dll') -Destination $plugins -Force

$tessTarget = Join-Path $packageRoot 'Editor/Internal/Tessellation'
New-Item -ItemType Directory -Force -Path $tessTarget | Out-Null
foreach ($file in Get-ChildItem (Join-Path $tess 'LibTessDotNet/Sources') -Filter '*.cs') {
    $content = [System.IO.File]::ReadAllText($file.FullName).Replace('LibTessDotNet', 'MathMesher.Internal.LibTessDotNet')
    [System.IO.File]::WriteAllText((Join-Path $tessTarget $file.Name), $content, $utf8)
}
$licenses = Join-Path $packageRoot 'Third Party Notices'
New-Item -ItemType Directory -Force -Path $licenses | Out-Null
Copy-Item -LiteralPath (Join-Path $tess 'LICENSE.txt') -Destination (Join-Path $licenses 'LibTessDotNet-SGI.txt') -Force
[System.IO.File]::WriteAllText((Join-Path $licenses 'Original-Source-Headers.txt'), $headers.ToString(), $utf8)
Write-Output "Built MathMesher.Fonts.dll and vendored tessellation sources in $packageRoot"
