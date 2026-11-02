# Copyright (C) 2026 GTC Companion contributors.
# This file is part of GTC Companion.
# GTC Companion is free software: you can redistribute it and/or modify it
# under the terms of the GNU General Public License, version 3, as published
# by the Free Software Foundation.
# GTC Companion is distributed without any warranty. See LICENSE for details.
# SPDX-License-Identifier: GPL-3.0-only

param([switch]$Test,[switch]$CodeOnly,[string]$SigningThumbprint,[string]$TimestampUrl,[string]$SignToolPath)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
if(!$CodeOnly -and (!(Test-Path -LiteralPath (Join-Path $root 'components\TinyPedal\tinypedal.exe')) -or !(Test-Path -LiteralPath (Join-Path $root 'components\CrewChiefV4\CrewChiefV4.exe')))){& (Join-Path $root 'prepare-components.ps1')}
if(!$CodeOnly -and !(Test-Path -LiteralPath (Join-Path $root 'components\rF2SharedMemoryMap\rFactor2SharedMemoryMapPlugin64.dll'))){& (Join-Path $root 'prepare-plugin.ps1')}
$signing=@{}
if($SigningThumbprint){
    if(!$TimestampUrl){throw 'Para assinar, indica também -TimestampUrl com o serviço RFC3161 do fornecedor.'}
    $signing=@{Thumbprint=$SigningThumbprint;TimestampUrl=$TimestampUrl;SignToolPath=$SignToolPath}
    & (Join-Path $root 'sign.ps1') @signing -CheckOnly
}elseif($TimestampUrl -or $SignToolPath){throw 'Indica -SigningThumbprint para ativar a assinatura.'}
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(!(Test-Path -LiteralPath $compiler)){throw 'Este projeto compila no Windows, com o .NET Framework instalado.'}
$out=Join-Path $root 'out'
$dist=Join-Path $root 'dist'
New-Item -ItemType Directory -Path $out,$dist -Force | Out-Null
$icon=Join-Path $root 'assets\gtc.ico'
$fontArgs=@('Regular','Medium','Bold')|ForEach-Object{'/resource:'+(Join-Path $root ('assets\fonts\Ubuntu-'+$_+'.ttf'))+',Ubuntu'+$_}
$refs=@('/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:System.Net.Http.dll','/reference:System.Web.Extensions.dll','/reference:System.Core.dll','/reference:System.Xml.dll','/reference:System.IO.Compression.dll','/reference:System.IO.Compression.FileSystem.dll')
$overlay=Join-Path $out 'GTC-Timing.exe'
$overlaySources=Get-ChildItem -LiteralPath (Join-Path $root 'src\overlay') -Filter '*.cs' | ForEach-Object {$_.FullName}
& $compiler /nologo /target:winexe /optimize+ ('/out:'+$overlay) ('/win32icon:'+$icon) ('/resource:'+$icon+',GtcIcon') $fontArgs $refs $overlaySources
if($LASTEXITCODE -ne 0){throw 'Falha na compilação do overlay.'}
if($SigningThumbprint){& (Join-Path $root 'sign.ps1') @signing -FilePath $overlay}
$app=Join-Path $out 'GTC-Companion.exe'
$engineArgs=@(('/resource:'+(Join-Path $root 'assets\engines\crew-defaults.json')+',CrewDefaults'),('/resource:'+(Join-Path $root 'assets\engines\tiny-defaults.json')+',TinyDefaults'))
$companionSources=Get-ChildItem -LiteralPath (Join-Path $root 'src\companion') -Filter '*.cs' | ForEach-Object {$_.FullName}
foreach($file in @('GtcTheme.cs','GtcTypography.cs','TimingData.cs','RankingHistory.cs','GtcSettingsControls.cs','GtcWindowTheme.cs','OverlayCommands.cs','OverlayShortcuts.cs','LiveLap.cs','DriverActivity.cs','PersonalRecordRepository.cs')){$companionSources+=Join-Path $root ('src\overlay\'+$file)}
& $compiler /nologo /target:winexe /optimize+ ('/out:'+$app) ('/win32icon:'+$icon) ('/resource:'+$icon+',GtcIcon') ('/resource:'+(Join-Path $root 'assets\gtc-mark.png')+',GtcMark') ('/resource:'+$overlay+',GtcOverlay') $fontArgs $engineArgs $refs $companionSources
if($LASTEXITCODE -ne 0){throw 'Falha na compilação do launcher.'}
if($SigningThumbprint){& (Join-Path $root 'sign.ps1') @signing -FilePath $app}
if(!$CodeOnly){Copy-Item -LiteralPath (Join-Path $root 'components') -Destination $out -Recurse -Force;Copy-Item -LiteralPath (Join-Path $root 'assets\engines\COMPONENT-SOURCES.md') -Destination (Join-Path $root 'third-party-source\README.md') -Force}
if($Test){
    $testData=Join-Path $out 'test-data'
    New-Item -ItemType Directory -Path $testData -Force | Out-Null
    $report=Join-Path $testData 'launcher-tests.txt'
    $process=Start-Process -FilePath $app -ArgumentList @('--self-test',('"'+$report+'"'),'--data-dir',('"'+$testData+'"')) -WindowStyle Hidden -Wait -PassThru
    if($process.ExitCode -ne 0){throw 'Falharam os testes do launcher. Consulta out/test-data/launcher.log.'}
    Get-Content -LiteralPath $report
    $report=Join-Path $testData 'overlay-tests.txt'
    $process=Start-Process -FilePath $overlay -ArgumentList @('--self-test',('"'+$report+'"')) -WindowStyle Hidden -Wait -PassThru
    if($process.ExitCode -ne 0){throw 'Falharam os testes do overlay. Consulta out/error.log.'}
    Get-Content -LiteralPath $report
}
Copy-Item -LiteralPath $app -Destination $dist -Force
foreach($notice in @('README.md','LICENSE','THIRD-PARTY-NOTICES.md')){Copy-Item -LiteralPath (Join-Path $root $notice) -Destination $dist -Force}
Copy-Item -LiteralPath (Join-Path $root 'assets\fonts\FONT-LICENCE.txt') -Destination $dist -Force
Copy-Item -LiteralPath (Join-Path $root 'assets\engines\TINYPEDAL-LICENSE.txt') -Destination $dist -Force
Copy-Item -LiteralPath (Join-Path $root 'assets\engines\CREWCHIEF-LICENSE.txt') -Destination $dist -Force
if(!$CodeOnly){
    Copy-Item -LiteralPath (Join-Path $root 'components') -Destination $dist -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $root 'third-party-source') -Destination $dist -Recurse -Force
}
$sourceZip=Join-Path $root 'GTC-Companion-source-0.11.5.zip'
$sourceFiles=@('src','assets','README.md','SECURITY.md','SIGNING.md','THIRD-PARTY-NOTICES.md','LICENSE','build.ps1','sign.ps1','prepare-components.ps1','prepare-plugin.ps1','.gitignore') | ForEach-Object {Join-Path $root $_}
Compress-Archive -LiteralPath $sourceFiles -DestinationPath $sourceZip -Force
Copy-Item -LiteralPath $sourceZip -Destination (Join-Path $dist 'SOURCE.zip') -Force
$zip=Join-Path $root 'GTC-Companion-0.11.5.zip'
$package=@('GTC-Companion.exe','README.md','FONT-LICENCE.txt','LICENSE','THIRD-PARTY-NOTICES.md','TINYPEDAL-LICENSE.txt','CREWCHIEF-LICENSE.txt','SOURCE.zip') | ForEach-Object {Join-Path $dist $_}
if(!$CodeOnly){$package+=Join-Path $root 'components';$package+=Join-Path $root 'third-party-source'}
Compress-Archive -LiteralPath $package -DestinationPath $zip -Force
$hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath ($zip+'.sha256') -Value ($hash+'  '+[IO.Path]::GetFileName($zip)) -Encoding ASCII
if(!$SigningThumbprint){Write-Warning 'Pacote criado sem assinatura digital. Não é uma release assinada.'}
Write-Output ('Pacote: '+$zip)
