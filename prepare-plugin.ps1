# Copyright (C) 2026 GTC Companion contributors.
# SPDX-License-Identifier: GPL-3.0-only
param([string]$PluginArchive,[string]$SourceArchive)
$ErrorActionPreference='Stop'
$cache=Join-Path $PSScriptRoot 'component-cache'
$plugin=Join-Path $PSScriptRoot 'components\rF2SharedMemoryMap'
$sources=Join-Path $PSScriptRoot 'third-party-source'
New-Item -ItemType Directory -Path $cache,$plugin,$sources -Force | Out-Null
if(!$PluginArchive){
    $PluginArchive=Join-Path $cache 'rf2_sm_tools_3.7.15.1.zip'
    if(!(Test-Path -LiteralPath $PluginArchive)){Invoke-WebRequest -Uri 'https://github.com/joaopsfsoares/gtc-companion/releases/download/v0.7.6/rf2_sm_tools_3.7.15.1.zip' -OutFile $PluginArchive}
}
if((Get-FileHash -LiteralPath $PluginArchive -Algorithm SHA256).Hash -ne '0D176F2C9B7F5BAB8684AEBC611AA7C43B244CF2D11B49B26D2BDF164B9D8918'){throw 'O arquivo do plugin não corresponde à versão fixada.'}
Expand-Archive -LiteralPath $PluginArchive -DestinationPath $cache -Force
$upstream=Join-Path $cache 'rf2_sm_tools_3.7.15.1'
Copy-Item -LiteralPath (Join-Path $upstream 'rf2smmp\rFactor2SharedMemoryMapPlugin64.dll') -Destination $plugin -Force
Copy-Item -LiteralPath (Join-Path $upstream 'readme.txt') -Destination $plugin -Force
foreach($item in @(@('RF2-LICENSE.txt','LICENSE.txt'),@('RF2-README.md','README.md'),@('RF2-PROVENANCE.json','PROVENANCE.json'))){Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('assets\plugins\'+$item[0])) -Destination (Join-Path $plugin $item[1]) -Force}
if(!$SourceArchive){
    $SourceArchive=Join-Path $cache 'rF2SharedMemoryMap-3.7.15.1-source.zip'
    if(!(Test-Path -LiteralPath $SourceArchive)){Invoke-WebRequest -Uri 'https://codeload.github.com/TheIronWolfModding/rF2SharedMemoryMapPlugin/zip/48aa12dbb68849923870acd8e68044c46c3d83eb' -OutFile $SourceArchive}
}
if((Get-FileHash -LiteralPath $SourceArchive -Algorithm SHA256).Hash -ne 'CDD1C0062F1A08FB2D0E5C3EDF8DF4ED37A0CD2B119A977B08563F9F02B1E69E'){throw 'As fontes do plugin não correspondem à revisão fixada.'}
Copy-Item -LiteralPath $SourceArchive -Destination (Join-Path $sources 'rF2SharedMemoryMap-3.7.15.1-source.zip') -Force
Write-Output 'Plugin 3.7.15.1, licença e fontes preparados.'
