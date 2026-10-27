// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Threading;
using System.Threading.Tasks;

public static class ComponentSetup {
    static readonly SemaphoreSlim Gate=new SemaphoreSlim(1,1);
    public static string SoundRoot(string executable){return CrewVoices.SoundRoot(executable,EngineSettingsSession.Crew(executable).Options);}
    public static bool NeedsSetup(LaunchSettings settings){
        try{return !TelemetryPlugin.Ready(settings.GameDirectory)||(settings.StartCrewChief&&!CrewVoices.Ready(SoundRoot(settings.CrewChiefPath)));}catch{return true;}
    }
    public static async Task Run(string game,string executable,bool plugin,bool voices,Action<string,string> progress,CancellationToken cancel){
        if(!await Gate.WaitAsync(0,cancel))throw new InvalidOperationException("Já existe uma instalação em curso.");
        try{
            if(plugin){cancel.ThrowIfCancellationRequested();progress("plugin","A verificar o plugin…");if(TelemetryPlugin.Ready(game))progress("plugin","Já instalado e ativado.");else{await Task.Run(()=>TelemetryPlugin.InstallIncluded(game,()=>LaunchEngine.Running("rFactor2")),cancel);progress("plugin","Instalado e ativado. Disponível no próximo arranque do jogo.");}}
            if(voices){cancel.ThrowIfCancellationRequested();string root=SoundRoot(executable);await CrewVoices.Install(root,message=>progress("voices",message),cancel);progress("voices","Vozes, nomes e personalizações instalados.");}
        }finally{Gate.Release();}
    }
}
