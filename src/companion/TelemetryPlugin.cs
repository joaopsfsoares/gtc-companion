// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public static class TelemetryPlugin {
    public const string FileName="rFactor2SharedMemoryMapPlugin64.dll";
    public const string ProjectUrl="https://github.com/TheIronWolfModding/rF2SharedMemoryMapPlugin#download";
    public const string BundledSha256="9d98d77b767812dca5afeb6663f486b3ad5be090c3e10783f36bc73a470ab5e6";
    public static bool Ready(string game){
        try{if(!AppDiscovery.IsGameDirectory(game))return false;ValidateBinary(PluginPath(game));var config=Config(game);object raw;var plugin=config.TryGetValue(FileName,out raw)?raw as Dictionary<string,object>:null;return !OtherPlugin(game,config)&&Enabled(plugin)&&(!plugin.ContainsKey("UnsubscribedBuffersMask")||(Convert.ToInt32(plugin["UnsubscribedBuffersMask"])&3)==0)&&(!plugin.ContainsKey("EnableLegacyNames")||Convert.ToInt32(plugin["EnableLegacyNames"])==1);}catch{return false;}
    }
    public static void InstallIncluded(string game,Func<bool> running){
        if(Ready(game))return;
        string source=Path.Combine(BundledEngines.Root,"rF2SharedMemoryMap",FileName);
        if(!File.Exists(source))throw new FileNotFoundException("O plugin não está no pacote. Extrai o ZIP completo do Companion.");
        using(var hash=SHA256.Create())if(BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(source))).Replace("-","").ToLowerInvariant()!=BundledSha256)throw new InvalidDataException("O plugin incluído não passou a verificação de integridade. Extrai novamente o pacote completo.");
        Install(game,source,running);
    }
    static string PluginPath(string game){return Path.Combine(game,"Bin64","Plugins",FileName);}
    static string ConfigPath(string game){return Path.Combine(game,"UserData","player","CustomPluginVariables.JSON");}
    static Dictionary<string,object> Config(string game){
        string path=ConfigPath(game);if(!File.Exists(path))return new Dictionary<string,object>();
        var result=CompanionStore.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(path));
        if(result==null)throw new InvalidDataException("A configuração de plugins não é um objeto JSON válido. O ficheiro foi preservado.");return result;
    }
    static bool Enabled(Dictionary<string,object> item){object value;return item!=null&&(item.TryGetValue(" Enabled",out value)||item.TryGetValue("Enabled",out value))&&Convert.ToInt32(value)==1;}
    public static string Status(string game){
        if(!AppDiscovery.IsGameDirectory(game))return "Seleciona primeiro a pasta do rFactor 2.";
        try{
            var config=Config(game);object raw;
            if(OtherPlugin(game,config))return "Foi detetado outro plugin de memória partilhada. Consulta a instalação assistida.";
            if(!File.Exists(PluginPath(game)))return "Plugin de telemetria por instalar.";
            var item=config.TryGetValue(FileName,out raw)?raw as Dictionary<string,object>:null;
            if(!Enabled(item))return "Plugin encontrado · falta ativar.";
            if(item.ContainsKey("UnsubscribedBuffersMask")&&(Convert.ToInt32(item["UnsubscribedBuffersMask"])&3)!=0)return "Plugin ativo · os dados de telemetria estão desativados.";
            if(item.ContainsKey("EnableLegacyNames")&&Convert.ToInt32(item["EnableLegacyNames"])!=1)return "Plugin ativo · falta ativar os nomes compatíveis de telemetria.";
            return "Plugin instalado e ativado · a ligação é confirmada em pista.";
        }catch{return "Não foi possível ler a configuração de plugins. Os ficheiros foram preservados.";}
    }
    static bool OtherPlugin(string game,Dictionary<string,object> config){
        string directory=Path.Combine(game,"Bin64","Plugins");if(!Directory.Exists(directory))return false;
        foreach(string path in Directory.GetFiles(directory,"*.dll")){
            string name=Path.GetFileName(path);if(name.Equals(FileName,StringComparison.OrdinalIgnoreCase)||name.IndexOf("SharedMemory",StringComparison.OrdinalIgnoreCase)<0)continue;
            object raw;var item=config.TryGetValue(name,out raw)?raw as Dictionary<string,object>:null;
            if(item==null||Enabled(item))return true;
        }return false;
    }
    public static void ValidateBinary(string path){
        if(!Path.GetFileName(path).Equals(FileName,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Seleciona o ficheiro "+FileName+" extraído do pacote oficial.");
        using(var reader=new BinaryReader(File.OpenRead(path))){
            if(reader.BaseStream.Length<256||reader.ReadUInt16()!=0x5a4d)throw new InvalidDataException("O ficheiro não é uma DLL Windows válida.");
            reader.BaseStream.Position=0x3c;int offset=reader.ReadInt32();
            if(offset<64||offset>reader.BaseStream.Length-24)throw new InvalidDataException("O cabeçalho da DLL é inválido.");
            reader.BaseStream.Position=offset;if(reader.ReadUInt32()!=0x4550||reader.ReadUInt16()!=0x8664)throw new InvalidDataException("É necessária a DLL para Windows de 64 bits.");
            reader.BaseStream.Position=offset+22;if((reader.ReadUInt16()&0x2000)==0)throw new InvalidDataException("O ficheiro selecionado não é uma DLL.");
        }
    }
    static bool EqualFiles(string a,string b){using(var hash=SHA256.Create())return hash.ComputeHash(File.ReadAllBytes(a)).SequenceEqual(hash.ComputeHash(File.ReadAllBytes(b)));}
    public static string Install(string game,string source,Func<bool> gameRunning){
        if(gameRunning())throw new InvalidOperationException("Fecha o rFactor 2 antes de instalar ou ativar o plugin. O Companion não fecha o jogo.");
        if(!AppDiscovery.IsGameDirectory(game))throw new InvalidOperationException("Seleciona a pasta do rFactor 2 que contém Bin64 e UserData.");
        ValidateBinary(source);var config=Config(game);
        if(OtherPlugin(game,config))throw new InvalidOperationException("Existe outro plugin de memória partilhada ativo ou por configurar. Não foi instalado um segundo plugin. Verifica os plugins em Settings → Gameplay no rFactor e consulta o guia oficial antes de mudar de versão.");
        object raw;Dictionary<string,object> plugin;
        if(config.TryGetValue(FileName,out raw)){
            plugin=raw as Dictionary<string,object>;if(plugin==null)throw new InvalidDataException("A entrada do plugin não é válida. A configuração foi preservada.");
        }else{plugin=new Dictionary<string,object>();config[FileName]=plugin;}
        plugin[" Enabled"]=1;if(plugin.ContainsKey("Enabled"))plugin["Enabled"]=1;
        plugin["UnsubscribedBuffersMask"]=plugin.ContainsKey("UnsubscribedBuffersMask")?Convert.ToInt32(plugin["UnsubscribedBuffersMask"])&~3:160;
        if(plugin.ContainsKey("EnableLegacyNames"))plugin["EnableLegacyNames"]=1;
        string json=CompanionStore.Json.Serialize(config),target=PluginPath(game),variables=ConfigPath(game);
        bool hadDll=File.Exists(target),hadConfig=File.Exists(variables),copyDll=!hadDll||!EqualFiles(source,target);
        if(gameRunning())throw new InvalidOperationException("O rFactor foi aberto entretanto. Fecha o jogo e tenta novamente.");
        string backup=Path.Combine(game,"UserData","player","CompanionPluginBackups",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8));
        Directory.CreateDirectory(backup);
        if(hadDll)File.Copy(target,Path.Combine(backup,FileName));if(hadConfig)File.Copy(variables,Path.Combine(backup,"CustomPluginVariables.JSON"));
        Directory.CreateDirectory(Path.GetDirectoryName(target));Directory.CreateDirectory(Path.GetDirectoryName(variables));
        string dllTemp=target+".gtc-"+Guid.NewGuid().ToString("N"),jsonTemp=variables+".gtc-"+Guid.NewGuid().ToString("N");bool changedDll=false,changedConfig=false;
        try{
            if(copyDll)File.Copy(source,dllTemp);File.WriteAllText(jsonTemp,json,new UTF8Encoding(false));
            if(gameRunning())throw new InvalidOperationException("O rFactor foi aberto entretanto. Fecha o jogo e tenta novamente.");
            if(copyDll){if(hadDll)File.Replace(dllTemp,target,null);else File.Move(dllTemp,target);changedDll=true;}
            if(hadConfig)File.Replace(jsonTemp,variables,null);else File.Move(jsonTemp,variables);changedConfig=true;
        }catch{
            if(changedConfig){if(hadConfig)File.Copy(Path.Combine(backup,"CustomPluginVariables.JSON"),variables,true);else File.Delete(variables);}
            if(changedDll){if(hadDll)File.Copy(Path.Combine(backup,FileName),target,true);else File.Delete(target);}throw;
        }finally{if(File.Exists(dllTemp))File.Delete(dllTemp);if(File.Exists(jsonTemp))File.Delete(jsonTemp);}
        return backup;
    }
}
