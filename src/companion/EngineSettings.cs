// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

public sealed class EngineSettingChange {
    public string File {get;set;}
    public string[] Keys {get;set;}
    public bool Xml {get;set;}
    public object Before {get;set;}
    public object Value {get;set;}
}
public sealed class EnginePendingSettings {
    public string Engine {get;set;}
    public List<EngineSettingChange> Changes {get;set;}
    public string Error {get;set;}
    public EnginePendingSettings(){Changes=new List<EngineSettingChange>();}
}
public sealed class EngineSetting {
    public string Group,Key,Hint;
    public object Initial;
    public EngineSettingChange Change;
    public object Value{get{return Change.Value;}set{Change.Value=value;}}
    public bool Dirty{get{return !EngineSettingsQueue.Same(Initial,Value);}}
}
public static class EngineSettingsQueue {
    static string QueueName(string engine){if(engine!="tiny"&&engine!="crew")throw new ArgumentException("Motor desconhecido.");return "engine-pending-"+engine+".json";}
    public static EnginePendingSettings Pending(string engine){return CompanionStore.Read<EnginePendingSettings>(QueueName(engine))??new EnginePendingSettings{Engine=engine};}
    public static bool Running(string engine){return LaunchEngine.Running(engine=="tiny"?"tinypedal":"CrewChiefV4");}
    public static bool Same(object a,object b){
        if(a==null||b==null)return a==b;if(IsNumber(a)&&IsNumber(b))return Convert.ToDecimal(a,CultureInfo.InvariantCulture)==Convert.ToDecimal(b,CultureInfo.InvariantCulture);
        return CompanionStore.Json.Serialize(a)==CompanionStore.Json.Serialize(b);
    }
    static bool IsNumber(object value){return value is byte||value is short||value is int||value is long||value is float||value is double||value is decimal;}
    public static void Save(string engine,IEnumerable<EngineSetting> options){
        var pending=Pending(engine);bool resolving=pending.Error!=null;pending.Error=null;
        foreach(var setting in options.Where(x=>x.Dirty)){
            var change=setting.Change;var previous=pending.Changes.FirstOrDefault(x=>x.File==change.File&&x.Xml==change.Xml&&x.Keys.SequenceEqual(change.Keys));
            if(previous!=null){previous.Value=change.Value;if(resolving)previous.Before=change.Before;}else pending.Changes.Add(new EngineSettingChange{File=change.File,Keys=change.Keys,Xml=change.Xml,Before=change.Before,Value=change.Value});
        }
        CompanionStore.Write(QueueName(engine),pending);Apply(engine);
    }
    public static void ApplyReady(){foreach(string engine in new[]{"tiny","crew"})try{if(Pending(engine).Error==null)Apply(engine);}catch(Exception ex){CompanionStore.Log("Definições "+engine+": "+ex.GetType().Name);}}
    public static void Apply(string engine){Apply(engine,()=>Running(engine));}
    internal static void Apply(string engine,Func<bool> running){
        var pending=Pending(engine);if(pending.Changes.Count==0||running())return;
        var migration=CompanionStore.Read<EngineMigration>("engines-migration.json");
        if(migration!=null){
            foreach(var change in pending.Changes){
                if(engine=="crew"&&change.Xml&&migration.CrewFinal&&migration.CrewGlobalsBefore!=null&&(migration.CrewGlobalsBefore.Contains(change.File,StringComparer.OrdinalIgnoreCase)||string.Equals(change.File,BundledEngines.Crew+".config",StringComparison.OrdinalIgnoreCase)))change.File=EngineSettingsSession.CrewGlobalPath(BundledEngines.Crew)??(BundledEngines.Crew+".config");
                if(engine=="tiny"&&migration.TinyFinal&&!string.IsNullOrEmpty(migration.OriginalTiny)){string old=Path.GetDirectoryName(migration.OriginalTiny).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;if(change.File.StartsWith(old,StringComparison.OrdinalIgnoreCase))change.File=Path.Combine(Path.GetDirectoryName(BundledEngines.Tiny),change.File.Substring(old.Length));}
            }
        }
        foreach(var batch in pending.Changes.GroupBy(x=>x.File).ToArray()){
            try{
                string file=Path.GetFullPath(batch.Key);if(!System.IO.File.Exists(file))throw new IOException("O ficheiro de definições não existe. Abre primeiro o motor para criar o perfil.");
                string result;
                if(batch.First().Xml){
                    var xml=new XmlDocument{XmlResolver=null};xml.Load(file);var container=xml.SelectSingleNode("/configuration/userSettings/CrewChiefV4.Properties.Settings");if(container==null)throw new InvalidDataException("Formato de preferências do Crew Chief desconhecido.");
                    foreach(var change in batch){
                        string key=change.Keys.Single();var node=container.ChildNodes.Cast<XmlNode>().FirstOrDefault(x=>x.Name=="setting"&&x.Attributes["name"]!=null&&x.Attributes["name"].Value==key);
                        string current=node==null?null:node.SelectSingleNode("value").InnerText;string before=change.Before==null?null:Convert.ToString(change.Before,CultureInfo.InvariantCulture);string value=Convert.ToString(change.Value,CultureInfo.InvariantCulture);
                        if(current!=before&&current!=value)throw new IOException("A aplicação alterou uma definição entretanto. Recarrega antes de voltar a guardar.");
                        if(node==null){var element=xml.CreateElement("setting");element.SetAttribute("name",key);element.SetAttribute("serializeAs","String");element.AppendChild(xml.CreateElement("value"));node=container.AppendChild(element);}node.SelectSingleNode("value").InnerText=value;
                    }
                    using(var writer=new StringWriter()){xml.Save(writer);result=writer.ToString();}result=result.Replace("encoding=\"utf-16\"","encoding=\"utf-8\"");
                }else{
                    var root=CompanionStore.Json.Deserialize<Dictionary<string,object>>(System.IO.File.ReadAllText(file));
                    foreach(var change in batch){
                        var parent=root;for(int i=0;i<change.Keys.Length-1;i++){object child;if(!parent.TryGetValue(change.Keys[i],out child))parent[change.Keys[i]]=child=new Dictionary<string,object>();parent=child as Dictionary<string,object>;if(parent==null)throw new InvalidDataException("Grupo de definições inválido.");}
                        string key=change.Keys.Last();object current;parent.TryGetValue(key,out current);if(!Same(current,change.Before)&&!Same(current,change.Value))throw new IOException("A aplicação alterou uma definição entretanto. Recarrega antes de voltar a guardar.");parent[key]=change.Value;
                    }
                    result=CompanionStore.Json.Serialize(root);
                }
                string backups=Path.Combine(CompanionStore.Root,"backups","engines");Directory.CreateDirectory(backups);System.IO.File.Copy(file,Path.Combine(backups,DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+engine+"-"+Path.GetFileName(file)+".bak"));
                string temp=file+".gtc-"+Guid.NewGuid().ToString("N")+".tmp";try{System.IO.File.WriteAllText(temp,result,new UTF8Encoding(false));System.IO.File.Replace(temp,file,null);}finally{if(System.IO.File.Exists(temp))System.IO.File.Delete(temp);}
                foreach(var change in batch)pending.Changes.Remove(change);pending.Error=null;CompanionStore.Write(QueueName(engine),pending);
            }catch(Exception ex){pending.Error=ex.Message;CompanionStore.Write(QueueName(engine),pending);throw;}
        }
    }
}

public sealed class EngineSettingsSession {
    sealed class JsonCacheEntry{public long Modified,Length;public Dictionary<string,object> Data;}
    static readonly Dictionary<string,JsonCacheEntry> jsonCache=new Dictionary<string,JsonCacheEntry>(StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<string,Dictionary<string,object>> resources=new Dictionary<string,Dictionary<string,object>>();
    public readonly List<EngineSetting> Options=new List<EngineSetting>();
    public readonly List<string> Profiles=new List<string>();
    public string Engine,Profile,DirectoryPath,Warning;
    public bool Dirty{get{return Options.Any(x=>x.Dirty);}}
    public static Dictionary<string,object> Resource(string name){lock(resources){Dictionary<string,object> data;if(resources.TryGetValue(name,out data))return data;using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(name))using(var reader=new StreamReader(stream))data=CompanionStore.Json.Deserialize<Dictionary<string,object>>(reader.ReadToEnd());resources[name]=data;return data;}}
    static Dictionary<string,object> Read(string path){try{var info=new FileInfo(path);if(!info.Exists)return null;lock(jsonCache){JsonCacheEntry cached;if(jsonCache.TryGetValue(info.FullName,out cached)&&cached.Modified==info.LastWriteTimeUtc.Ticks&&cached.Length==info.Length)return cached.Data;var data=CompanionStore.Json.Deserialize<Dictionary<string,object>>(System.IO.File.ReadAllText(path));if(jsonCache.Count>=48)jsonCache.Clear();jsonCache[info.FullName]=new JsonCacheEntry{Modified=info.LastWriteTimeUtc.Ticks,Length=info.Length,Data=data};return data;}}catch{return null;}}
    void Add(string group,string key,object value,string file,string[] keys,bool xml=false,string hint=null,object before=null,bool missing=false){
        if(key.StartsWith("#")||value==null)return;Options.Add(new EngineSetting{Group=group,Key=key,Hint=hint,Initial=value,Change=new EngineSettingChange{File=file,Keys=keys,Xml=xml,Before=missing?null:before??value,Value=value}});
    }
    void AddGroups(Dictionary<string,object> root,string file,string prefix){
        if(root==null)return;foreach(var pair in root){var group=pair.Value as Dictionary<string,object>;if(group==null)continue;foreach(var item in group)Add(prefix+pair.Key,item.Key,item.Value,file,new[]{pair.Key,item.Key});}
    }
    public static EngineSettingsSession Tiny(string executable,string profile=null){
        var session=new EngineSettingsSession{Engine="tiny"};if(string.IsNullOrWhiteSpace(executable))throw new IOException("Seleciona o motor TinyPedal na página Aplicações.");
        string folder=Path.GetDirectoryName(executable);string config=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"TinyPedal","config.json");var global=Read(config);
        string settings=Path.Combine(folder,"settings");object paths;if(global!=null&&global.TryGetValue("user_path",out paths)){object path;if(((Dictionary<string,object>)paths).TryGetValue("settings_path",out path)&&!string.IsNullOrWhiteSpace(Convert.ToString(path)))settings=Path.IsPathRooted(Convert.ToString(path))?Convert.ToString(path):Path.Combine(folder,Convert.ToString(path));}
        session.DirectoryPath=settings;
        if(System.IO.Directory.Exists(settings))foreach(string file in System.IO.Directory.GetFiles(settings,"*.json").OrderByDescending(System.IO.File.GetLastWriteTimeUtc)){var data=Read(file);if(data!=null&&data.ContainsKey("overlay"))session.Profiles.Add(Path.GetFileName(file));}
        session.Profile=profile??session.Profiles.FirstOrDefault()??"default.json";if(Path.GetFileName(session.Profile)!=session.Profile)throw new ArgumentException("Perfil inválido.");
        string preset=Path.Combine(settings,session.Profile);var root=Read(preset);if(root==null)session.Warning="Abre o TinyPedal uma vez para criar o perfil de widgets.";
        session.AddGroups(root,preset,"");session.AddGroups(global,config,"global/");
        session.MergePending();return session;
    }
    public static IEnumerable<string> CrewGlobalFiles(string executable){
        var roots=new List<string>{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Britton_IT_Ltd")};var directory=new DirectoryInfo(Path.GetDirectoryName(executable));for(int i=0;i<6&&directory!=null;i++,directory=directory.Parent)roots.Add(Path.Combine(directory.FullName,"Britton_IT_Ltd"));
        return roots.Where(System.IO.Directory.Exists).Distinct().SelectMany(root=>System.IO.Directory.GetFiles(root,"user.config",SearchOption.AllDirectories)).Where(path=>path.IndexOf("CrewChiefV4.exe_Url_",StringComparison.OrdinalIgnoreCase)>=0).Distinct(StringComparer.OrdinalIgnoreCase).OrderByDescending(System.IO.File.GetLastWriteTimeUtc);
    }
    public static string CrewGlobalPath(string executable){
        var files=CrewGlobalFiles(executable);var migration=CompanionStore.Read<EngineMigration>("engines-migration.json");
        if(string.Equals(executable,BundledEngines.Crew,StringComparison.OrdinalIgnoreCase)&&migration!=null&&migration.CrewFinal&&migration.CrewGlobalsBefore!=null)files=files.Where(path=>!migration.CrewGlobalsBefore.Contains(path,StringComparer.OrdinalIgnoreCase));
        return files.FirstOrDefault();
    }
    public static EngineSettingsSession Crew(string executable,string profile=null,string globalOverride=null,string profilesOverride=null){
        var session=new EngineSettingsSession{Engine="crew"};if(string.IsNullOrWhiteSpace(executable))throw new IOException("Seleciona o motor Crew Chief na página Aplicações.");
        string global=globalOverride??CrewGlobalPath(executable)??(System.IO.File.Exists(executable+".config")?executable+".config":null);var actual=new Dictionary<string,string>();
        if(global!=null){var xml=new XmlDocument{XmlResolver=null};xml.Load(global);foreach(XmlNode setting in xml.SelectNodes("/configuration/userSettings/CrewChiefV4.Properties.Settings/setting"))actual[setting.Attributes["name"].Value]=setting.SelectSingleNode("value").InnerText;}
        string directory=profilesOverride??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"CrewChiefV4","Profiles");session.DirectoryPath=directory;
        if(System.IO.Directory.Exists(directory))session.Profiles.AddRange(System.IO.Directory.GetFiles(directory,"*.json").Select(Path.GetFileName).OrderBy(x=>x));
        string active;actual.TryGetValue("current_settings_profile",out active);session.Profile=profile??(string.IsNullOrWhiteSpace(active)?"defaultSettings.json":active);if(Path.GetFileName(session.Profile)!=session.Profile)throw new ArgumentException("Perfil inválido.");
        string file=Path.Combine(directory,session.Profile);var root=Read(file);object values;
        if(root!=null&&root.TryGetValue("userSettings",out values)){
            var settings=values as Dictionary<string,object>;foreach(var pair in settings.Where(x=>!x.Key.StartsWith("#"))){object hint;settings.TryGetValue("#"+pair.Key,out hint);session.Add(CrewGroup(pair.Key),pair.Key,pair.Value,file,new[]{"userSettings",pair.Key},false,Convert.ToString(hint));}
        }else session.Warning="Abre o Crew Chief uma vez para criar o perfil do engenheiro.";
        // Global preferences are separate from profiles in the original engine.
        var defaults=Resource("CrewDefaults");string[] globals={"PERSONALISATION_NAME","chief_name","spotter_name","messages_volume","background_volume","VOICE_OPTION","NAUDIO_DEVICE_GUID_MESSAGES","NAUDIO_DEVICE_GUID_BACKGROUND","NAUDIO_RECORDING_DEVICE_GUID"};
        if(global!=null){foreach(string key in globals){string current;actual.TryGetValue(key,out current);object fallback;defaults.TryGetValue(key,out fallback);if(current==null&&fallback==null)continue;object value=current??Convert.ToString(fallback,CultureInfo.InvariantCulture);session.Add("audio",key,value,global,new[]{key},true,null,current,current==null);}string activeProfileValue;actual.TryGetValue("current_settings_profile",out activeProfileValue);session.Add("_internal","current_settings_profile",activeProfileValue??"defaultSettings.json",global,new[]{"current_settings_profile"},true,null,activeProfileValue,activeProfileValue==null);session.Options.Last().Value=session.Profile;}
        session.MergePending();return session;
    }
    static string CrewGroup(string key){
        string value=key.ToLowerInvariant();if(value.Contains("spotter"))return "spotter";if(value.Contains("fuel"))return "fuel";if(value.Contains("voice")||value.Contains("speech")||value.Contains("recognition")||value.Contains("ptt"))return "voice";if(value.Contains("volume")||value.Contains("sound")||value.Contains("audio")||value.Contains("swear")||value.Contains("complain")||value.Contains("messages"))return "messages";if(value.StartsWith("rf2")||value.StartsWith("rfactor"))return "rfactor";return "advanced";
    }
    void MergePending(){
        var pending=EngineSettingsQueue.Pending(Engine);if(pending.Error!=null)Warning=pending.Error;
        foreach(var option in Options){var change=pending.Changes.FirstOrDefault(x=>x.File==option.Change.File&&x.Keys.SequenceEqual(option.Change.Keys));if(change!=null){option.Value=change.Value;if(pending.Error==null){option.Change.Before=change.Value;option.Initial=change.Value;}}}
    }
    public void Save(){EngineSettingsQueue.Save(Engine,Options);foreach(var option in Options){option.Change.Before=option.Value;option.Initial=option.Value;}}
}
