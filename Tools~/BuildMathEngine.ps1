param([string]$CacheDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'MathMesherBuild'))
$ErrorActionPreference = 'Stop'
$packageRoot = Split-Path $PSScriptRoot -Parent
$mathCommit = '69fb790c40693ae85d2d8679655ee04e88f35aea'
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

$mathSource = Get-PinnedSource 'verybadcat/CSharpMath' $mathCommit 'source'
$fontSource = Get-PinnedSource 'LayoutFarm/Typography' $typographyCommit 'typography'
$tessSource = Get-PinnedSource 'speps/LibTessDotNet' $tessCommit 'tess'
$buildRoot = Join-Path $CacheDirectory 'engine'
New-Item -ItemType Directory -Force -Path $buildRoot | Out-Null

function Copy-PrivateSources([string]$Source, [string]$Name) {
    $destination = Join-Path $buildRoot $Name
    foreach ($file in Get-ChildItem -LiteralPath $Source -Filter '*.cs' -Recurse) {
        if ($file.Name -eq 'AssemblyInfo.cs' -or $file.FullName -match '[\\/](obj|bin)[\\/]') { continue }
        $relative = $file.FullName.Substring($Source.Length).TrimStart('\', '/')
        $target = Join-Path $destination $relative
        New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
        $content = [System.IO.File]::ReadAllText($file.FullName)
        $content = [regex]::Replace($content, '\bCSharpMath\b', 'TeXmexsh.Internal.CSharpMath')
        $content = [regex]::Replace($content, '\bTypography\b', 'TeXmexsh.Internal.Typography')
        if ($file.Name -eq 'LaTeXSettings.cs') {
            $content = $content.Replace('Err(@"\prime won''t be supported as Unicode has no matching character. Use '' instead.")', 'Ok(new Ordinary("′"))')
        }
        if ($Name -in @('Core', 'MathEditor', 'Rendering') -and $content -notmatch 'namespace\s+MathMesher\.Internal\.CSharpMath\.Atom\s*\{' -and $content -match '\bRange\b' -and $content -notmatch 'using\s+Range\s*=') {
            $namespacePattern = New-Object System.Text.RegularExpressions.Regex('(namespace\s+[\w.]+\s*\{)')
            $content = $namespacePattern.Replace($content, ('$1' + "`nusing Range = TeXmexsh.Internal.CSharpMath.Atom.Range;`n"), 1)
        }
        [System.IO.File]::WriteAllText($target, $content, $utf8)
    }
}

Copy-PrivateSources (Join-Path $mathSource 'CSharpMath') 'Core'
Copy-PrivateSources (Join-Path $mathSource 'CSharpMath.Editor') 'MathEditor'
Copy-PrivateSources (Join-Path $mathSource 'CSharpMath.Rendering') 'Rendering'
Copy-PrivateSources (Join-Path $fontSource 'Typography.OpenFont') 'OpenFont'
Copy-PrivateSources (Join-Path $fontSource 'Typography.GlyphLayout') 'GlyphLayout'
Copy-PrivateSources (Join-Path $fontSource 'Typography.TextBreak/Typography.TextBreak') 'TextBreak'
$outline = Join-Path $fontSource 'PixelFarm.Typography/3_Typography_Contours/GlyphOutlineBuilderBase.cs'
$outlineText = [System.IO.File]::ReadAllText($outline).Replace('Typography', 'TeXmexsh.Internal.Typography')
[System.IO.File]::WriteAllText((Join-Path $buildRoot 'GlyphOutlineBuilderBase.cs'), $outlineText, $utf8)
$fonts = Join-Path $buildRoot 'Fonts'
New-Item -ItemType Directory -Force -Path $fonts | Out-Null
Copy-Item -Path (Join-Path $mathSource 'CSharpMath.Rendering/Reference Fonts/*.otf') -Destination $fonts -Force
$project = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <AssemblyName>TeXmexsh.MathEngine</AssemblyName>
    <RootNamespace>MathMesher.Internal</RootNamespace>
    <LangVersion>9.0</LangVersion>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <NoWarn>$(NoWarn);CS8625;CS8618;CS8603;CS8602;CS8604;CS8600;CS8601;CS0169;CS0649</NoWarn>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
  <ItemGroup>
    <EmbeddedResource Include="Fonts/*.otf">
      <LogicalName>TeXmexsh.Internal.CSharpMath.Rendering.Reference_Fonts.%(Filename)%(Extension)</LogicalName>
    </EmbeddedResource>
  </ItemGroup>
</Project>
'@
[System.IO.File]::WriteAllText((Join-Path $buildRoot 'TeXmexsh.MathEngine.csproj'), $project, $utf8)
& dotnet build (Join-Path $buildRoot 'TeXmexsh.MathEngine.csproj') -c Release --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Math engine compilation failed.' }
$plugins = Join-Path $packageRoot 'Editor/Plugins'
New-Item -ItemType Directory -Force -Path $plugins | Out-Null
Copy-Item -LiteralPath (Join-Path $buildRoot 'bin/Release/netstandard2.1/TeXmexsh.MathEngine.dll') -Destination $plugins -Force

$tessTarget = Join-Path $packageRoot 'Editor/Internal/Tessellation'
New-Item -ItemType Directory -Force -Path $tessTarget | Out-Null
foreach ($file in Get-ChildItem (Join-Path $tessSource 'LibTessDotNet/Sources') -Filter '*.cs') {
    $content = [System.IO.File]::ReadAllText($file.FullName).Replace('LibTessDotNet', 'MathMesher.Internal.LibTessDotNet')
    [System.IO.File]::WriteAllText((Join-Path $tessTarget $file.Name), $content, $utf8)
}
$licenses = Join-Path $packageRoot 'Third Party Notices'
New-Item -ItemType Directory -Force -Path $licenses | Out-Null
Copy-Item -LiteralPath (Join-Path $mathSource 'License') -Destination (Join-Path $licenses 'CSharpMath-MIT.txt') -Force
Copy-Item -LiteralPath (Join-Path $tessSource 'LICENSE.txt') -Destination (Join-Path $licenses 'LibTessDotNet-SGI.txt') -Force
Write-Output "Built private Editor-only math engine and vendored tessellation sources in $packageRoot"
