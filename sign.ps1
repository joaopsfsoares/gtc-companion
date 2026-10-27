# Copyright (C) 2026 GTC Companion contributors.
# This file is part of GTC Companion.
# GTC Companion is free software: you can redistribute it and/or modify it
# under the terms of the GNU General Public License, version 3, as published
# by the Free Software Foundation.
# GTC Companion is distributed without any warranty. See LICENSE for details.
# SPDX-License-Identifier: GPL-3.0-only

param(
    [string]$FilePath,
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-fA-F0-9]{40}$')][string]$Thumbprint,
    [Parameter(Mandatory=$true)][string]$TimestampUrl,
    [string]$SignToolPath,
    [switch]$CheckOnly
)
$ErrorActionPreference='Stop'
$timestamp=$null
if(![Uri]::TryCreate($TimestampUrl,[UriKind]::Absolute,[ref]$timestamp) -or $timestamp.Scheme -ne 'https' -or $timestamp.UserInfo){throw 'Indica o endereço HTTPS RFC3161 do fornecedor de assinatura.'}
$certificate=Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object {$_.Thumbprint -eq $Thumbprint} | Select-Object -First 1
if(!$certificate -or !$certificate.HasPrivateKey){throw 'Não existe um certificado de assinatura com chave privada disponível na conta Windows atual.'}
if($certificate.NotAfter -le (Get-Date) -or $certificate.NotBefore -gt (Get-Date)){throw 'O certificado de assinatura está fora da validade.'}
if(!$SignToolPath){
    $command=Get-Command signtool.exe -ErrorAction SilentlyContinue
    if($command){$SignToolPath=$command.Source}else{
        $sdk=Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
        if(Test-Path -LiteralPath $sdk){
            $candidate=Get-ChildItem -LiteralPath $sdk -Directory | Sort-Object Name -Descending | ForEach-Object {Join-Path $_.FullName 'x64\signtool.exe'} | Where-Object {Test-Path -LiteralPath $_} | Select-Object -First 1
            if($candidate){$SignToolPath=$candidate}
        }
    }
}
if(!$SignToolPath -or !(Test-Path -LiteralPath $SignToolPath -PathType Leaf)){throw 'Seleciona o SignTool do Windows SDK com -SignToolPath.'}
if($CheckOnly){Write-Output 'Certificado e ferramenta disponíveis. Nenhum ficheiro foi assinado.';return}
if(!$FilePath -or !(Test-Path -LiteralPath $FilePath -PathType Leaf)){throw 'O ficheiro a assinar não existe.'}
$target=(Resolve-Path -LiteralPath $FilePath).Path
& $SignToolPath sign /sha1 $Thumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 $target
if($LASTEXITCODE -ne 0){throw 'Falhou a assinatura ou o carimbo temporal. O pacote não será publicado.'}
& $SignToolPath verify /pa /all /tw $target
if($LASTEXITCODE -ne 0){throw 'Falhou a verificação da assinatura e do carimbo temporal.'}
$signature=Get-AuthenticodeSignature -LiteralPath $target
if($signature.Status -ne 'Valid' -or !$signature.TimeStamperCertificate){throw 'A assinatura final não é válida ou não tem carimbo temporal.'}
Write-Output ('Assinatura verificada: '+[IO.Path]::GetFileName($target))
