# Copyright (C) 2026 GTC Companion contributors.
# SPDX-License-Identifier: GPL-3.0-only
param([string]$TinyArchive,[string]$CrewInstaller)
$ErrorActionPreference='Stop'
$cache=Join-Path $PSScriptRoot 'component-cache'
$components=Join-Path $PSScriptRoot 'components'
$sources=Join-Path $PSScriptRoot 'third-party-source'
New-Item -ItemType Directory -Path $cache,$components,$sources -Force | Out-Null
if(!$TinyArchive){$TinyArchive=Join-Path $cache 'TinyPedal-2.52.0-windows.zip';if(!(Test-Path -LiteralPath $TinyArchive)){Invoke-WebRequest -Uri 'https://github.com/TinyPedal/TinyPedal/releases/download/v2.52.0/TinyPedal-2.52.0-windows.zip' -OutFile $TinyArchive}}
if(!$CrewInstaller){$CrewInstaller=Join-Path $cache 'CrewChiefV4.msi';if(!(Test-Path -LiteralPath $CrewInstaller)){Invoke-WebRequest -Uri 'https://thecrewchief.org/downloads/CrewChiefV4.msi' -OutFile $CrewInstaller}}
if((Get-FileHash -LiteralPath $TinyArchive -Algorithm SHA256).Hash -ne '02F7F2492E447C1580DFC20E4FBC487987FB13951AC08340E47846A59BB0874B'){throw 'O pacote TinyPedal não corresponde à versão fixada.'}
if((Get-FileHash -LiteralPath $CrewInstaller -Algorithm SHA256).Hash -ne 'B7C9960478670081EB3A138AA84A5FD4AFEA3D9043511469FD9E18986D09AA94'){throw 'O pacote Crew Chief não corresponde à versão fixada; revê a versão antes de o substituir.'}
$tiny=Join-Path $components 'TinyPedal'
if(!(Test-Path -LiteralPath (Join-Path $tiny 'tinypedal.exe'))){Expand-Archive -LiteralPath $TinyArchive -DestinationPath $cache -Force;Copy-Item -LiteralPath (Join-Path $cache 'TinyPedal') -Destination $components -Recurse -Force}
$crew=Join-Path $components 'CrewChiefV4'
if(!(Test-Path -LiteralPath (Join-Path $crew 'CrewChiefV4.exe'))){
    $image=Join-Path $cache 'crew-image';New-Item -ItemType Directory -Path $image -Force | Out-Null
    $process=Start-Process -FilePath 'msiexec.exe' -ArgumentList @('/a',('"'+[IO.Path]::GetFullPath($CrewInstaller)+'"'),'/qn',('TARGETDIR="'+$image+'"')) -WindowStyle Hidden -Wait -PassThru
    if($process.ExitCode -ne 0){throw 'Não foi possível extrair a imagem do instalador Crew Chief.'}
    Copy-Item -LiteralPath (Join-Path $image 'Britton IT Ltd\CrewChiefV4') -Destination $components -Recurse -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets\engines\CREWCHIEF-LICENSE.txt') -Destination (Join-Path $crew 'LICENSE.txt') -Force
$checkout=Join-Path $cache 'TinyPedal-source'
if(!(Test-Path -LiteralPath (Join-Path $checkout '.git'))){& git clone --depth 1 --branch v2.52.0 --recurse-submodules --shallow-submodules https://github.com/TinyPedal/TinyPedal.git $checkout;if($LASTEXITCODE -ne 0){throw 'Não foi possível obter as fontes correspondentes do TinyPedal.'}}
$revision=(& git -C $checkout rev-parse HEAD).Trim();if($revision -ne 'bf43bd5db0cd15f57672e5c75696ced0df020781'){throw 'A revisão TinyPedal não corresponde aos binários.'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$sourceZip=Join-Path $sources 'TinyPedal-2.52.0-source.zip'
if(!(Test-Path -LiteralPath $sourceZip)){
    $zip=[IO.Compression.ZipFile]::Open($sourceZip,[IO.Compression.ZipArchiveMode]::Create)
    try{foreach($file in Get-ChildItem -LiteralPath $checkout -File -Recurse -Force){$relative=$file.FullName.Substring($checkout.Length+1);if($relative -match '(^|[\\/])\.git([\\/]|$)' -or $relative -match '(^|[\\/])__pycache__([\\/]|$)'){continue};[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$file.FullName,('TinyPedal/'+$relative.Replace('\','/')),[IO.Compression.CompressionLevel]::Optimal) | Out-Null}}finally{$zip.Dispose()}
}
$manifest=@{TinyPedal=@{version='2.52.0';license='GPL-3.0-or-later';origin='https://github.com/TinyPedal/TinyPedal/releases/tag/v2.52.0';archiveSha256=(Get-FileHash -LiteralPath $TinyArchive -Algorithm SHA256).Hash.ToLowerInvariant()};CrewChief=@{version='4.19.5.8';license='MIT';origin='https://thecrewchief.org/downloads/CrewChiefV4.msi';archiveSha256=(Get-FileHash -LiteralPath $CrewInstaller -Algorithm SHA256).Hash.ToLowerInvariant()}}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $components 'manifest.json') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets\engines\COMPONENT-SOURCES.md') -Destination (Join-Path $sources 'README.md') -Force
Write-Output 'Motores e fontes correspondentes preparados.'
