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

public sealed class CompetitionProfile {
    public string Id {get;set;}
    public string Name {get;set;}
    public string PageUrl {get;set;}
    public string ServerHost {get;set;}
    public int ServerPort {get;set;}
    public string ServerPassword {get;set;}
    public int TimingId {get;set;}
    public CompetitionProfile(){Id=Guid.NewGuid().ToString("N");Name="O meu campeonato";PageUrl="";ServerHost="";ServerPassword="";}
    public CompetitionProfile Copy(){return new CompetitionProfile{Id=Id,Name=Name,PageUrl=PageUrl,ServerHost=ServerHost,ServerPort=ServerPort,ServerPassword=ServerPassword,TimingId=TimingId};}
    public override string ToString(){return Name;}
}
public sealed class LegacyCompetitionSettings {
    public string ServerHost {get;set;}
    public int ServerPort {get;set;}
    public string ServerPassword {get;set;}
    public int TimingId {get;set;}
}
public sealed class OverlayInstance {
    public string Version {get;set;}
    public int Pid {get;set;}
    public string ProfileId {get;set;}
    public string Name {get;set;}
    public string Directory {get;set;}
    public string Driver {get;set;}
    public int TimingId {get;set;}
    public bool Standalone {get;set;}
    public string GameStamp {get;set;}
}
public sealed class CompetitionConnection {
    public string ProfileId {get;set;}
    public string Host {get;set;}
    public int Port {get;set;}
    public string GameStamp {get;set;}
}
public static class CompetitionProfiles {
    public static string PreferencesDirectory{get{return Path.Combine(CompanionStore.Root,"overlay");}}
    public static bool HasAccess(CompetitionProfile p){return p!=null&&!string.IsNullOrWhiteSpace(p.ServerHost)&&p.ServerPort>0&&p.ServerPort<=65535;}
    public static CompetitionProfile Selected(LaunchSettings settings){return settings.Competitions==null?null:settings.Competitions.FirstOrDefault(p=>p!=null&&p.Id==settings.SelectedCompetitionId);}
    public static CompetitionProfile EnsureSelected(LaunchSettings settings,bool create=false){
        if(settings.Competitions==null)settings.Competitions=new List<CompetitionProfile>();
        var profile=Selected(settings);if(profile!=null)return profile;
        if(settings.Competitions.Count==0){settings.SelectedCompetitionId="";if(!create)return null;settings.Competitions.Add(new CompetitionProfile());}
        profile=settings.Competitions[0];settings.SelectedCompetitionId=profile.Id;return profile;
    }
    public static bool Remove(LaunchSettings settings,string id){
        var profile=settings.Competitions.FirstOrDefault(p=>p.Id==id);if(profile==null)return false;
        int index=settings.Competitions.IndexOf(profile);string selection=settings.SelectedCompetitionId;
        settings.Competitions.RemoveAt(index);
        if(selection==id)settings.SelectedCompetitionId=settings.Competitions.Count==0?"":settings.Competitions[Math.Min(index,settings.Competitions.Count-1)].Id;
        try{CompanionStore.Write("settings.json",settings);}catch{settings.Competitions.Insert(index,profile);settings.SelectedCompetitionId=selection;throw;}
        return true;
    }
    public static string DataDirectory(CompetitionProfile profile){
        Guid id;if(profile==null||!Guid.TryParse(profile.Id,out id))throw new InvalidDataException("Identificador de campeonato inválido.");
        return Path.Combine(CompanionStore.Root,"competitions",id.ToString("N"),"timing-"+Math.Max(0,profile.TimingId));
    }
    public static string GameStamp(){
        string result="";DateTime oldest=DateTime.MaxValue;
        foreach(var process in System.Diagnostics.Process.GetProcessesByName("rFactor2"))using(process)try{if(process.StartTime<oldest){oldest=process.StartTime;result=process.Id+"|"+process.StartTime.ToUniversalTime().Ticks;}}catch{}
        return result;
    }
    static bool OwnDirectory(string path){try{return Path.GetFullPath(path).StartsWith(Path.GetFullPath(CompanionStore.Root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);}catch{return false;}}
    static bool IsOverlay(int pid){try{using(var process=System.Diagnostics.Process.GetProcessById(pid))return process.ProcessName.Equals("GTC-Timing",StringComparison.OrdinalIgnoreCase);}catch{return false;}}
    public static OverlayInstance RunningOverlay(LaunchSettings settings){
        var active=CompanionStore.Read<OverlayInstance>("active-overlay.json");if(active!=null&&OwnDirectory(active.Directory)&&IsOverlay(active.Pid))return active;
        var old=CompanionStore.Read<Dictionary<string,object>>(Path.Combine("overlay","status.json"));object pid;
        if(old!=null&&old.TryGetValue("pid",out pid)&&IsOverlay(Convert.ToInt32(pid))){var profile=EnsureSelected(settings);if(profile!=null)return new OverlayInstance{Pid=Convert.ToInt32(pid),Directory=PreferencesDirectory,ProfileId=profile.Id,Name=profile.Name,Driver=settings.Driver,TimingId=profile.TimingId,GameStamp=GameStamp()};}
        return null;
    }
    public static string ControlDirectory(LaunchSettings settings){var active=RunningOverlay(settings);var selected=EnsureSelected(settings);return active!=null?active.Directory:selected==null?PreferencesDirectory:DataDirectory(selected);}
    public static bool SameConnection(CompetitionConnection connection,CompetitionProfile profile,string gameStamp){return connection!=null&&profile!=null&&!string.IsNullOrEmpty(gameStamp)&&connection.GameStamp==gameStamp&&connection.ProfileId==profile.Id&&connection.Host==profile.ServerHost&&connection.Port==profile.ServerPort;}
    public static bool ConnectionMatches(LaunchSettings settings){
        var profile=EnsureSelected(settings);string stamp=GameStamp();if(SameConnection(CompanionStore.Read<CompetitionConnection>("connection.json"),profile,stamp))return true;
        return false;
    }
    public static void RecordConnection(LaunchSettings settings){var profile=EnsureSelected(settings);CompanionStore.Write("connection.json",new CompetitionConnection{ProfileId=profile.Id,Host=profile.ServerHost,Port=profile.ServerPort,GameStamp=GameStamp()});}
    public static LaunchSettings Load(bool persist){
        var settings=CompanionStore.Read<LaunchSettings>("settings.json")??new LaunchSettings();var raw=CompanionStore.Read<Dictionary<string,object>>("settings.json");bool migrated=raw==null||!raw.ContainsKey("Competitions")||settings.Competitions==null;
        if(migrated){
            var legacy=CompanionStore.Read<LegacyCompetitionSettings>("settings.json");var profile=new CompetitionProfile();
            if(legacy!=null){profile.ServerHost=legacy.ServerHost??"";profile.ServerPort=legacy.ServerPort;profile.ServerPassword=legacy.ServerPassword??"";profile.TimingId=Math.Max(0,legacy.TimingId);if(HasAccess(profile)){profile.Name=profile.TimingId==110?"Porsche 911 Cup":"Campeonato atual";if(profile.TimingId==110)profile.PageUrl="https://esports.gtcracecenter.com/comp/porsche-911-cup-9922";}}
            settings.Competitions=new List<CompetitionProfile>{profile};settings.SelectedCompetitionId=profile.Id;
            if(persist){CopyLegacyData(profile);CompanionStore.Write("settings.json",settings);}
        }
        EnsureSelected(settings);return settings;
    }
    static void CopyLegacyData(CompetitionProfile profile){
        string source=PreferencesDirectory;if(!Directory.Exists(source))return;string target=DataDirectory(profile);Directory.CreateDirectory(target);
        foreach(string file in Directory.GetFiles(source,"*.json")){
            string name=Path.GetFileName(file);if(name!="cache.json"&&name!="personal-records.json"&&name!="positions-session.json"&&!name.StartsWith("ranking-history-",StringComparison.Ordinal))continue;
            string destination=Path.Combine(target,name);if(!File.Exists(destination))File.Copy(file,destination);
        }
    }
    public static int ParseTimingLink(string link){
        if(string.IsNullOrWhiteSpace(link))return 0;int id;if(int.TryParse(link.Trim(),out id)&&id>0)return id;
        Uri uri;if(!Uri.TryCreate(link.Trim(),UriKind.Absolute,out uri)||uri.Scheme!="https"||uri.Host!="esports.gtcracecenter.com")throw new ArgumentException("Usa a ligação da classificação GTC, ou deixa o campo em branco.");
        var match=System.Text.RegularExpressions.Regex.Match(uri.AbsolutePath,@"^/livetiming/(\d+)/?$");
        if(!match.Success||!int.TryParse(match.Groups[1].Value,out id)||id<1)throw new ArgumentException("A ligação da classificação deve ter o formato /livetiming/123.");return id;
    }
    public static string ValidateProfile(LaunchSettings settings,CompetitionProfile candidate){
        if(string.IsNullOrWhiteSpace(candidate.Name)||candidate.Name.Trim().Length>80)return "Indica um nome de campeonato com até 80 caracteres.";
        if(settings.Competitions.Any(p=>p.Id!=candidate.Id&&TimingData.Normalize(p.Name)==TimingData.Normalize(candidate.Name)))return "Já existe um campeonato com esse nome.";
        if(!string.IsNullOrWhiteSpace(candidate.PageUrl)){Uri uri;if(!Uri.TryCreate(candidate.PageUrl,UriKind.Absolute,out uri)||uri.Scheme!="https"||uri.Host!="esports.gtcracecenter.com")return "Usa uma página https da GTC para o campeonato.";}
        return candidate.TimingId<0?"A classificação não é válida.":null;
    }
}
