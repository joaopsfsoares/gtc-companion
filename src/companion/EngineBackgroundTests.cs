// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Threading.Tasks;

public static class EngineBackgroundTests {
    public static void Run(){
        if(EngineBackground.MainTitle("Crew Chief Loading", "Crew Chief")||EngineBackground.MainTitle("Crew Chief - Update", "Crew Chief")||!EngineBackground.MainTitle("Crew Chief - Active Profile: defaultSettings - Running: rFactor 2", "Crew Chief"))throw new Exception("A janela de carregamento não deve ser confundida com a janela principal do Crew Chief.");
        var states=new[]{EngineBackground.WindowObservation.NotFound,EngineBackground.WindowObservation.HiddenNow,EngineBackground.WindowObservation.AlreadyHidden,EngineBackground.WindowObservation.HiddenNow,EngineBackground.WindowObservation.AlreadyHidden,EngineBackground.WindowObservation.AlreadyHidden};int index=0;
        bool hidden=Task.Run(()=>EngineBackground.Watch(()=>states[index++],()=>Task.FromResult(0),states.Length,2)).GetAwaiter().GetResult();
        if(!hidden||index!=states.Length)throw new Exception("Uma janela que reaparece no arranque deve voltar a ser ocultada até estabilizar.");
        if(Task.Run(()=>EngineBackground.Watch(()=>EngineBackground.WindowObservation.NotFound,()=>Task.FromResult(0),3,2)).GetAwaiter().GetResult())throw new Exception("Não indicar sucesso se a janela principal não apareceu.");
        if(Task.Run(()=>EngineBackground.Watch(()=>EngineBackground.WindowObservation.HiddenNow,()=>Task.FromResult(0),3,2)).GetAwaiter().GetResult())throw new Exception("Não indicar sucesso se a janela continuou a reaparecer.");
    }
}
