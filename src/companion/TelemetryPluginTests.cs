// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class TelemetryPluginTests {
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void Reject(Action action,string message){bool rejected=false;try{action();}catch{rejected=true;}Require(rejected,message);}
    public static void Run(){
        string root=Path.Combine(CompanionStore.Root,"plugin-tests-"+Guid.NewGuid().ToString("N")),game=Path.Combine(root,"game"),source=Path.Combine(root,TelemetryPlugin.FileName);
        Directory.CreateDirectory(Path.Combine(game,"Bin64","Plugins"));Directory.CreateDirectory(Path.Combine(game,"UserData","player"));File.WriteAllText(Path.Combine(game,"Bin64","rFactor2.exe"),"");
        // Minimal PE fixture exercises validation without loading any native code.
        byte[] pe=new byte[512];pe[0]=0x4d;pe[1]=0x5a;pe[0x3c]=0x80;pe[0x80]=0x50;pe[0x81]=0x45;pe[0x84]=0x64;pe[0x85]=0x86;pe[0x97]=0x20;File.WriteAllBytes(source,pe);
        string variables=Path.Combine(game,"UserData","player","CustomPluginVariables.JSON"),target=Path.Combine(game,"Bin64","Plugins",TelemetryPlugin.FileName);
        string initial="{\"Other.dll\":{\" Enabled\":1,\"custom\":\"preserve\"},\""+TelemetryPlugin.FileName+"\":{\" Enabled\":0,\"UnsubscribedBuffersMask\":163,\"EnableDirectMemoryAccess\":0,\"EnableLegacyNames\":0,\"Unknown\":42}}";File.WriteAllText(variables,initial);
        Reject(()=>TelemetryPlugin.Install(game,source,()=>true),"Não instalar com o jogo aberto");Require(File.ReadAllText(variables)==initial&&!File.Exists(target),"Jogo aberto não pode modificar ficheiros");
        string backup=TelemetryPlugin.Install(game,source,()=>false);var config=CompanionStore.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(variables));var plugin=(Dictionary<string,object>)config[TelemetryPlugin.FileName];
        Require(Convert.ToInt32(plugin[" Enabled"])==1&&Convert.ToInt32(plugin["UnsubscribedBuffersMask"])==160&&Convert.ToInt32(plugin["EnableLegacyNames"])==1,"Ativar plugin e os buffers necessários");
        Require(Convert.ToInt32(plugin["Unknown"])==42&&Convert.ToInt32(plugin["EnableDirectMemoryAccess"])==0&&((Dictionary<string,object>)config["Other.dll"])["custom"].ToString()=="preserve","Preservar preferências e plugins alheios");
        Require(File.ReadAllText(Path.Combine(backup,"CustomPluginVariables.JSON"))==initial&&File.ReadAllBytes(target).SequenceEqual(pe),"Backup e instalação da DLL");
        byte[] replacement=(byte[])pe.Clone();replacement[400]=3;File.WriteAllBytes(source,replacement);backup=TelemetryPlugin.Install(game,source,()=>false);Require(File.ReadAllBytes(Path.Combine(backup,TelemetryPlugin.FileName)).SequenceEqual(pe)&&File.ReadAllBytes(target).SequenceEqual(replacement),"Backup da DLL substituída");
        File.WriteAllText(variables,"{invalid");Reject(()=>TelemetryPlugin.Install(game,source,()=>false),"Rejeitar JSON inválido");Require(File.ReadAllText(variables)=="{invalid","Não substituir JSON inválido");
        File.WriteAllText(variables,initial);File.WriteAllText(Path.Combine(game,"Bin64","Plugins","RF2.3.8.SharedMemoryMapPlugin.dll"),"fixture");Reject(()=>TelemetryPlugin.Install(game,source,()=>false),"Não criar plugins concorrentes");Require(File.ReadAllText(variables)==initial,"Preservar configuração quando existe conflito");
        string alternate=Path.Combine(root,"fresh");Directory.CreateDirectory(Path.Combine(alternate,"Bin64"));Directory.CreateDirectory(Path.Combine(alternate,"UserData"));File.WriteAllText(Path.Combine(alternate,"Bin64","rFactor2.exe"),"");TelemetryPlugin.Install(alternate,source,()=>false);Require(TelemetryPlugin.Status(alternate).Contains("instalado e ativado"),"Instalação numa pasta sem configuração anterior");
        int checks=0;Reject(()=>TelemetryPlugin.Install(alternate,source,()=>++checks>=2),"Jogo aberto durante a preparação deve cancelar a instalação");
        checks=0;Reject(()=>TelemetryPlugin.Install(alternate,source,()=>++checks>=3),"Jogo aberto antes da escrita deve cancelar a instalação");Require(!Directory.GetFiles(Path.Combine(alternate,"Bin64","Plugins"),"*.gtc-*").Any(),"Eliminar os ficheiros temporários de uma operação cancelada");
        pe[0x84]=0x4c;pe[0x85]=1;File.WriteAllBytes(source,pe);Reject(()=>TelemetryPlugin.ValidateBinary(source),"Rejeitar DLL de 32 bits");
        File.WriteAllText(source,"not a DLL");Reject(()=>TelemetryPlugin.Install(alternate,source,()=>false),"Rejeitar ficheiros sem cabeçalho PE");
    }
}
