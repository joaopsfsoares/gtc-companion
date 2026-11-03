// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

public sealed class EngineMigration {
    public string OriginalTiny,OriginalCrew;
    public bool TinyFinal,CrewFinal;
    public string[] CrewGlobalsBefore;
}
public static class BundledEngines {
    public static string Root{get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"components");}}
    public static string Tiny{get{return Path.Combine(Root,"TinyPedal","tinypedal.exe");}}
    public static string Crew{get{return Path.Combine(Root,"CrewChiefV4","CrewChiefV4.exe");}}
    static bool SamePath(string a,string b){try{return string.Equals(Path.GetFullPath(a),Path.GetFullPath(b),StringComparison.OrdinalIgnoreCase);}catch{return false;}}
    public static void Configure(LaunchSettings settings){
        var migration=CompanionStore.Read<EngineMigration>("engines-migration.json")??new EngineMigration();
        if(File.Exists(Tiny)&&!SamePath(settings.TinyPedalPath,Tiny)){if(string.IsNullOrEmpty(migration.OriginalTiny)&&File.Exists(settings.TinyPedalPath))migration.OriginalTiny=settings.TinyPedalPath;settings.TinyPedalPath=Tiny;}
        if(File.Exists(Crew)&&!SamePath(settings.CrewChiefPath,Crew)){if(string.IsNullOrEmpty(migration.OriginalCrew)&&File.Exists(settings.CrewChiefPath))migration.OriginalCrew=settings.CrewChiefPath;settings.CrewChiefPath=Crew;}
        CompanionStore.Write("engines-migration.json",migration);Migrate(migration,false);CreateDefaults();
    }
    public static void Prepare(){var migration=CompanionStore.Read<EngineMigration>("engines-migration.json");if(migration!=null)Migrate(migration,true);CreateDefaults();}
    static void Migrate(EngineMigration migration,bool final){
        bool changed=false;
        if(File.Exists(Tiny)&&!migration.TinyFinal&&File.Exists(migration.OriginalTiny)){
            // Copy user data, never executables, from the previous installation.
            // On the final pass the original engine has closed and flushed settings.
            bool idle=!LaunchEngine.Running("tinypedal");string old=Path.GetDirectoryName(migration.OriginalTiny),destination=Path.GetDirectoryName(Tiny);
            foreach(string directory in new[]{"settings","trackmap","deltabest","pacenotes","tracknotes","brandlogo"}){
                string source=Path.Combine(old,directory),target=Path.Combine(destination,directory);if(!Directory.Exists(source))continue;
                foreach(string file in Directory.GetFiles(source,"*",SearchOption.AllDirectories)){
                    string name=file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar);string output=Path.Combine(target,name);if(!idle&&File.Exists(output)&&Path.GetFileName(file)!="default.json")continue;
                    if(!idle&&File.Exists(Path.Combine(destination,".gtc-initial-copy")))continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(output));File.Copy(file,output,true);
                }
            }
            File.WriteAllText(Path.Combine(destination,".gtc-initial-copy"),"");if(idle&&final){migration.TinyFinal=true;changed=true;}
        }
        if(File.Exists(Crew)&&!migration.CrewFinal&&File.Exists(migration.OriginalCrew)&&!LaunchEngine.Running("CrewChiefV4")&&final){
            EngineSettingsQueue.Apply("crew");
            // Preserve global preferences (audio devices, Soares, voices) when the
            // executable URL changes. Profiles and sound packs remain in user data.
            var session=EngineSettingsSession.Crew(migration.OriginalCrew);var original=session.Options.FirstOrDefault(x=>x.Change.Xml);
            string target=Crew+".config";if(original!=null&&File.Exists(target)){
                var previous=new XmlDocument{XmlResolver=null};previous.Load(original.Change.File);var current=new XmlDocument{XmlResolver=null};current.Load(target);
                var source=previous.SelectSingleNode("/configuration/userSettings/CrewChiefV4.Properties.Settings");var destination=current.SelectSingleNode("/configuration/userSettings/CrewChiefV4.Properties.Settings");
                if(source!=null&&destination!=null){foreach(XmlNode node in source.ChildNodes){if(node.Name!="setting")continue;string key=node.Attributes["name"].Value;var existing=destination.ChildNodes.Cast<XmlNode>().FirstOrDefault(x=>x.Name=="setting"&&x.Attributes["name"].Value==key);if(existing!=null)destination.RemoveChild(existing);destination.AppendChild(current.ImportNode(node,true));}current.Save(target);}
            }
            migration.CrewGlobalsBefore=EngineSettingsSession.CrewGlobalFiles(Crew).ToArray();migration.CrewFinal=true;changed=true;
        }
        if(changed)CompanionStore.Write("engines-migration.json",migration);
    }
    internal static bool EnsureTinyDefault(string directory){
        string preset=Path.Combine(directory,"default.json");
        if(File.Exists(preset))return false;
        if(Directory.Exists(directory))foreach(string file in Directory.GetFiles(directory,"*.json")){
            try{var data=CompanionStore.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(file));if(data!=null&&data.ContainsKey("overlay"))return false;}catch(Exception){}
        }
        Directory.CreateDirectory(directory);
        // CreateNew protects a profile created by TinyPedal during the check.
        try{using(var stream=new FileStream(preset,FileMode.CreateNew,FileAccess.Write,FileShare.Read))using(var writer=new StreamWriter(stream,new System.Text.UTF8Encoding(false)))writer.Write(CompanionStore.Json.Serialize(EngineSettingsSession.Resource("TinyDefaults")["preset"]));return true;}catch(IOException){if(File.Exists(preset))return false;throw;}
    }
    static void CreateDefaults(){if(!Complete)return;if(!LaunchEngine.Running("tinypedal"))EnsureTinyDefault(Path.Combine(Path.GetDirectoryName(Tiny),"settings"));string profiles=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"CrewChiefV4","Profiles");string profile=Path.Combine(profiles,"defaultSettings.json");if(!File.Exists(profile)&&!LaunchEngine.Running("CrewChiefV4")){Directory.CreateDirectory(profiles);string[] reserved={"CHANNEL_","TOGGLE_","VOICE_OPTION","background_volume","messages_volume","last_game_definition","UpdateSettings","PERSONALISATION_NAME","app_version","spotter_name","codriver_name","codriver_style","update_notify_attempted","last_trace_file_name","NAUDIO_DEVICE_GUID","NAUDIO_RECORDING_DEVICE_GUID","chief_name","current_settings_profile","main_window_position","main_window_size"};var values=EngineSettingsSession.Resource("CrewDefaults").Where(pair=>!reserved.Any(prefix=>pair.Key.StartsWith(prefix,StringComparison.Ordinal))).ToDictionary(pair=>pair.Key,pair=>pair.Value);File.WriteAllText(profile,CompanionStore.Json.Serialize(new{userSettings=values}));}}
    public static bool Complete{get{return File.Exists(Tiny)&&File.Exists(Crew);}}
}
