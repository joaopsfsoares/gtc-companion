// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Microsoft.Win32;

public sealed class LaunchSettings {
    public bool Configured {get;set;}
    public string Driver {get;set;}
    public string GameDirectory {get;set;}
    public bool StartCrewChief {get;set;}
    public string CrewChiefPath {get;set;}
    public bool StartTinyPedal {get;set;}
    public string TinyPedalPath {get;set;}
    public bool AutoStart {get;set;}
    public bool Borderless {get;set;}
    public bool AutoReplay {get;set;}
    public List<CompetitionProfile> Competitions {get;set;}
    public string SelectedCompetitionId {get;set;}
    [ScriptIgnore]public string ServerHost {get{var p=CompetitionProfiles.Selected(this);return p==null?"":p.ServerHost;}set{CompetitionProfiles.EnsureSelected(this,true).ServerHost=value;}}
    [ScriptIgnore]public int ServerPort {get{var p=CompetitionProfiles.Selected(this);return p==null?0:p.ServerPort;}set{CompetitionProfiles.EnsureSelected(this,true).ServerPort=value;}}
    [ScriptIgnore]public string ServerPassword {get{var p=CompetitionProfiles.Selected(this);return p==null?"":p.ServerPassword;}set{CompetitionProfiles.EnsureSelected(this,true).ServerPassword=value;}}
    [ScriptIgnore]public int TimingId {get{var p=CompetitionProfiles.Selected(this);return p==null?0:p.TimingId;}set{CompetitionProfiles.EnsureSelected(this,true).TimingId=value;}}
    public LaunchSettings(){Driver="";GameDirectory="";CrewChiefPath="";TinyPedalPath="";AutoStart=true;Borderless=true;Competitions=new List<CompetitionProfile>();}
    [ScriptIgnore]public string JoinUrl {get{return "http://localhost:5397/rest/multiplayer/join?host="+Uri.EscapeDataString(ServerHost??"")+"&port="+ServerPort+"&password="+Uri.EscapeDataString(ServerPassword??"");}}
}
public static class ServerAccess {
    public static void ImportLink(LaunchSettings settings,string link){
        Uri uri;
        if(!Uri.TryCreate((link??"").Trim(),UriKind.Absolute,out uri)||uri.Scheme!="http"||
            (uri.Host!="localhost"&&uri.Host!="127.0.0.1")||uri.Port!=5397||uri.AbsolutePath!="/rest/multiplayer/join"||
            !string.IsNullOrEmpty(uri.UserInfo)||!string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException("Usa o link de entrada do rFactor obtido no site da GTC após iniciar sessão.");
        var values=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach(string pair in uri.Query.TrimStart('?').Split('&')){
            if(pair.Length==0)continue;int split=pair.IndexOf('=');
            string key=Uri.UnescapeDataString((split<0?pair:pair.Substring(0,split)).Replace("+"," "));
            string value=Uri.UnescapeDataString((split<0?"":pair.Substring(split+1)).Replace("+"," "));
            if(values.ContainsKey(key))throw new ArgumentException("O link contém parâmetros repetidos. Copia-o novamente do site.");values.Add(key,value);
        }
        string host,port,password;int number;
        if(!values.TryGetValue("host",out host)||!values.TryGetValue("port",out port)||!int.TryParse(port,out number)||number<1||number>65535||string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("O link de entrada não contém um servidor e uma porta válidos.");
        Uri server;
        if(!Uri.TryCreate("http://"+host,UriKind.Absolute,out server)||server.Host!=host||host.Any(char.IsWhiteSpace))throw new ArgumentException("O endereço de servidor no link não é válido.");
        values.TryGetValue("password",out password);settings.ServerHost=host;settings.ServerPort=number;settings.ServerPassword=password??"";
    }
}
public static class CompanionStore {
    public static string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"GTC-Companion");
    public static readonly JavaScriptSerializer Json=new JavaScriptSerializer{MaxJsonLength=4000000};
    public static T Read<T>(string file) where T:class{try{return Json.Deserialize<T>(File.ReadAllText(Path.Combine(Root,file)));}catch{return null;}}
    public static void Write(string file,object data){Directory.CreateDirectory(Root);string target=Path.Combine(Root,file),temp=target+".tmp";File.WriteAllText(temp,Json.Serialize(data),new UTF8Encoding(false));if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);}
    public static void Log(string text){try{Directory.CreateDirectory(Root);File.AppendAllText(Path.Combine(Root,"launcher.log"),DateTime.UtcNow.ToString("o")+" "+text+Environment.NewLine);}catch{}}
}
public static class AppDiscovery {
    public static bool IsGameDirectory(string dir){return !string.IsNullOrWhiteSpace(dir)&&File.Exists(Path.Combine(dir,"Bin64","rFactor2.exe"))&&Directory.Exists(Path.Combine(dir,"UserData"));}
    static string RegistryString(string key,string name){try{return Registry.GetValue(key,name,"") as string??"";}catch{return "";}}
    public static string SteamExe(){
        var candidates=new[]{RegistryString(@"HKEY_CURRENT_USER\Software\Valve\Steam","SteamExe"),Path.Combine(RegistryString(@"HKEY_CURRENT_USER\Software\Valve\Steam","SteamPath"),"steam.exe"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),"Steam","steam.exe")};
        return candidates.FirstOrDefault(File.Exists)??"";
    }
    public static IEnumerable<string> GameCandidates(){
        foreach(var p in Process.GetProcessesByName("rFactor2")){string file="";try{file=p.MainModule.FileName;}catch{}finally{p.Dispose();}if(!string.IsNullOrEmpty(file))yield return Path.GetDirectoryName(Path.GetDirectoryName(file));}
        string steam=SteamExe();if(string.IsNullOrEmpty(steam))yield break;
        string root=Path.GetDirectoryName(steam);var libraries=new List<string>{root};
        try{string vdf=File.ReadAllText(Path.Combine(root,"steamapps","libraryfolders.vdf"));foreach(Match match in Regex.Matches(vdf,"\"path\"\\s*\"([^\"]+)\""))libraries.Add(match.Groups[1].Value.Replace("\\\\","\\"));}catch{}
        foreach(string library in libraries.Distinct(StringComparer.OrdinalIgnoreCase))yield return Path.Combine(library,"steamapps","common","rFactor 2");
    }
    static string FindApp(string folder,string exe){
        var roots=new[]{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"AppData","Local"),Path.GetDirectoryName(CompanionStore.Root),Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\')),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)};
        foreach(string root in roots){if(string.IsNullOrEmpty(root))continue;string file=Path.Combine(root,folder,exe);if(File.Exists(file))return file;}
        foreach(string hive in new[]{@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall",@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Uninstall",@"HKEY_LOCAL_MACHINE\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"}){
            try{using(var key=(hive.StartsWith("HKEY_CURRENT_USER")?Registry.CurrentUser:Registry.LocalMachine).OpenSubKey(hive.Substring(hive.IndexOf('\\')+1))){if(key==null)continue;foreach(string child in key.GetSubKeyNames())using(var app=key.OpenSubKey(child)){string location=app.GetValue("InstallLocation","") as string;string file=string.IsNullOrEmpty(location)?"":Path.Combine(location,exe);if(File.Exists(file))return file;}}}catch{}
        }
        return "";
    }
    public static void Fill(LaunchSettings settings){
        BundledEngines.Configure(settings);
        if(!IsGameDirectory(settings.GameDirectory))settings.GameDirectory=GameCandidates().FirstOrDefault(IsGameDirectory)??"";
        if(!File.Exists(settings.CrewChiefPath))settings.CrewChiefPath=FindApp("CrewChiefV4","CrewChiefV4.exe");
        if(!File.Exists(settings.TinyPedalPath))settings.TinyPedalPath=FindApp("TinyPedal","tinypedal.exe");
        if(!settings.Configured){settings.StartCrewChief=File.Exists(settings.CrewChiefPath);settings.StartTinyPedal=File.Exists(settings.TinyPedalPath);}
        if(string.IsNullOrWhiteSpace(settings.Driver))try{var player=CompanionStore.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(settings.GameDirectory,"UserData","player","player.JSON")));var driver=(Dictionary<string,object>)player["DRIVER"];settings.Driver=Convert.ToString(driver.ContainsKey("Player Name")?driver["Player Name"]:"");}catch{}
    }
}
public sealed class LaunchEngine : IDisposable {
    readonly HttpClient http;
    readonly int stepMs,coldMs,warmMs;
    public LaunchEngine():this(new HttpClientHandler{AllowAutoRedirect=false,UseProxy=false},500,3000,500){}
    public LaunchEngine(HttpMessageHandler handler,int step,int cold,int warm){http=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(20)};stepMs=step;coldMs=cold;warmMs=warm;}
    public static List<string> Validate(LaunchSettings s,bool requireAccess=true,bool requireDriver=true){
        var errors=new List<string>();
        if(!AppDiscovery.IsGameDirectory(s.GameDirectory))errors.Add("Seleciona a pasta do rFactor 2 (a que contém Bin64 e UserData).");
        if(requireDriver&&string.IsNullOrWhiteSpace(s.Driver))errors.Add("Indica o teu nome de piloto, tal como aparece na tabela GTC.");
        Uri host;if(requireAccess&&(string.IsNullOrWhiteSpace(s.ServerHost)||!Uri.TryCreate("http://"+s.ServerHost,UriKind.Absolute,out host)||host.Host!=s.ServerHost||s.ServerHost.Any(char.IsWhiteSpace)||s.ServerPort<1||s.ServerPort>65535))errors.Add("Cola um link de entrada válido na página Servidor.");
        if(requireAccess&&s.TimingId<0)errors.Add("O identificador da classificação não é válido.");
        if(s.StartCrewChief&&!File.Exists(s.CrewChiefPath))errors.Add("O motor Crew Chief está em falta. Extrai novamente o pacote completo ou desativa-o.");
        if(s.StartTinyPedal&&!File.Exists(s.TinyPedalPath))errors.Add("O motor TinyPedal está em falta. Extrai novamente o pacote completo ou desativa-o.");
        return errors;
    }
    public static bool Running(string name){var processes=Process.GetProcessesByName(name);bool found=processes.Length>0;foreach(var p in processes)p.Dispose();return found;}
    static void StartApp(string exe,string args,bool hidden=false){Process.Start(new ProcessStartInfo(exe,args){WorkingDirectory=Path.GetDirectoryName(exe),UseShellExecute=true,WindowStyle=hidden?ProcessWindowStyle.Hidden:ProcessWindowStyle.Normal});}
    static async Task WaitForProcess(string name,CancellationToken cancel){var watch=Stopwatch.StartNew();while(!Running(name)){if(watch.Elapsed>TimeSpan.FromSeconds(20))throw new IOException("A aplicação "+name+" não abriu. Confirma o caminho nas definições.");await Task.Delay(200,cancel);}}
    static async Task Companion(string exe,string args,Action<string> progress,CancellationToken cancel){BundledEngines.Prepare();EngineSettingsQueue.ApplyReady();string name=Path.GetFileNameWithoutExtension(exe);bool crew=name.Equals("CrewChiefV4",StringComparison.OrdinalIgnoreCase),tiny=name.Equals("tinypedal",StringComparison.OrdinalIgnoreCase);if(Running(name)){progress(name+" já está aberto.");if(crew)EngineBackground.RequestHidden(exe,"Crew Chief");return;}progress("A abrir "+name+"…");StartApp(exe,args,crew);await WaitForProcess(name,cancel);if(crew)EngineBackground.RequestHidden(exe,"Crew Chief");else if(tiny)await EngineBackground.HideTinyOnce(exe);}
    static void SetBorderless(LaunchSettings settings){
        string file=Path.Combine(settings.GameDirectory,"UserData","Config_DX11.ini");if(!File.Exists(file))throw new IOException("Não encontrei Config_DX11.ini. Abre o rFactor uma vez para criar a configuração de vídeo.");
        string original=File.ReadAllText(file),next=original;
        foreach(var pair in new[]{new[]{"WindowedMode","0"},new[]{"Borderless","1"}}){string expression="^"+pair[0]+"=\\d+";if(!Regex.IsMatch(next,expression,RegexOptions.Multiline))throw new IOException("Não encontrei a opção de vídeo "+pair[0]+".");next=Regex.Replace(next,expression,pair[0]+"="+pair[1],RegexOptions.Multiline);}
        if(next==original)return;string backups=Path.Combine(CompanionStore.Root,"backups");Directory.CreateDirectory(backups);File.Copy(file,Path.Combine(backups,"Config_DX11-"+DateTime.UtcNow.Ticks+".ini"));File.WriteAllText(file,next,new UTF8Encoding(false));
    }
    public static void StartOverlay(LaunchSettings settings,bool settingsOnly){
        var selected=CompetitionProfiles.EnsureSelected(settings);if(selected==null)throw new InvalidOperationException("Adiciona primeiro um campeonato.");var active=CompetitionProfiles.RunningOverlay(settings);
        string root=CompetitionProfiles.DataDirectory(selected);Directory.CreateDirectory(root);
        if(active!=null&&(active.Version!=AppInfo.Version||active.ProfileId!=selected.Id||active.TimingId!=selected.TimingId||active.Driver!=settings.Driver||active.Directory!=root||active.Standalone!=settingsOnly||settings.TimingId==0)){
            File.WriteAllText(Path.Combine(active.Directory,"request-close.txt"),"");var wait=Stopwatch.StartNew();while(Running("GTC-Timing")&&wait.ElapsedMilliseconds<3500)Thread.Sleep(100);
            if(Running("GTC-Timing"))throw new InvalidOperationException("Fecha o overlay GTC para aplicar o campeonato selecionado.");active=null;
        }
        if(settings.TimingId==0){if(settingsOnly)throw new InvalidOperationException("Adiciona a ligação da classificação nas definições deste campeonato para mostrar o overlay.");return;}
        if(active!=null)return;
        if(Running("GTC-Timing"))throw new InvalidOperationException("Fecha o overlay anterior e volta a iniciar a sessão.");
        string close=Path.Combine(root,"request-close.txt");if(File.Exists(close))File.Delete(close);
        string prefRoot=CompetitionProfiles.PreferencesDirectory;Directory.CreateDirectory(prefRoot);string preferences=Path.Combine(prefRoot,"settings.json");
        var prefs=CompanionStore.Read<Preferences>(preferences)??new Preferences();prefs.Player=settings.Driver;CompanionStore.Write(preferences,prefs);
        string bin=Path.Combine(CompanionStore.Root,"bin");Directory.CreateDirectory(bin);string exe=Path.Combine(bin,"GTC-Timing.exe");
        using(var source=typeof(LaunchEngine).Assembly.GetManifestResourceStream("GtcOverlay"))using(var target=File.Create(exe))source.CopyTo(target);
        var info=new ProcessStartInfo(exe,settingsOnly?"--standalone":""){WorkingDirectory=bin,UseShellExecute=false};
        info.EnvironmentVariables["GTC_TIMING_DATA"]=root;info.EnvironmentVariables["GTC_TIMING_PREFERENCES"]=prefRoot;info.EnvironmentVariables["GTC_TIMING_LAUNCHER"]=typeof(LaunchEngine).Assembly.Location;info.EnvironmentVariables["GTC_COMPANION_DATA"]=CompanionStore.Root;info.EnvironmentVariables["GTC_TIMING_MANAGED"]="1";info.EnvironmentVariables["GTC_TIMING_DRIVER"]=settings.Driver;info.EnvironmentVariables["GTC_TIMING_ID"]=settings.TimingId.ToString();
        using(var process=Process.Start(info))CompanionStore.Write("active-overlay.json",new OverlayInstance{Version=AppInfo.Version,Pid=process.Id,ProfileId=selected.Id,Name=selected.Name,Directory=root,Driver=settings.Driver,TimingId=settings.TimingId,Standalone=settingsOnly,GameStamp=CompetitionProfiles.GameStamp()});
    }
    async Task<object> GetJson(string uri,CancellationToken cancel){using(var request=CancellationTokenSource.CreateLinkedTokenSource(cancel)){request.CancelAfter(2000);using(var response=await http.GetAsync(uri,request.Token)){response.EnsureSuccessStatusCode();return CompanionStore.Json.DeserializeObject(await response.Content.ReadAsStringAsync());}}}
    static string Value(Dictionary<string,object> map,string key){return map!=null&&map.ContainsKey(key)?Convert.ToString(map[key]):"";}
    async Task<bool> Joined(string origin,CancellationToken cancel){try{string state=Convert.ToString(await GetJson(origin+"/rest/multiplayer/join/state",cancel));return state=="JOIN_JOINED_SERVER"||state=="JOIN_REJOINED_SERVER";}catch(HttpRequestException){return false;}}
    public async Task<string> Connect(string origin,string joinUrl,bool alreadyOpen,Action<string> progress,Action ready,CancellationToken cancel,Action<string> phase=null,Func<bool> isGameOpen=null,bool allowExisting=true){
        var clock=Stopwatch.StartNew();long menuSince=-1;bool processSeen=alreadyOpen;progress("A aguardar pelo menu do rFactor 2…");
        while(clock.Elapsed<TimeSpan.FromMinutes(6)){
            cancel.ThrowIfCancellationRequested();
            if(!processSeen&&isGameOpen!=null&&isGameOpen()){processSeen=true;if(phase!=null)phase("process");progress("rFactor 2 aberto. A aguardar pelo menu…");}
            try{
                // The REST join state remains authoritative while driving or in
                // the garage. Do not require the main-menu navigation endpoint.
                if(processSeen&&await Joined(origin,cancel)){if(phase!=null)phase("menu");if(allowExisting)ready();if(phase!=null)phase("existing");return "O jogo já está ligado a um servidor. A sessão atual foi mantida, sem novo pedido de entrada.";}
                var navigation=(Dictionary<string,object>)await GetJson(origin+"/navigation/state",cancel);object stateObject;var state=navigation.TryGetValue("state",out stateObject)?stateObject as Dictionary<string,object>:null;
                object loadingObject;var loading=navigation.TryGetValue("loadingStatus",out loadingObject)?loadingObject as Dictionary<string,object>:null;
                bool isLoading=Value(loading,"loading").Equals("True",StringComparison.OrdinalIgnoreCase);
                if(Value(state,"navigationState")=="NAV_MAIN_MENU"&&!isLoading){
                    if(menuSince<0){menuSince=clock.ElapsedMilliseconds;progress("Menu detetado. A preparar a entrada no servidor…");}
                    if(clock.ElapsedMilliseconds-menuSince>=(alreadyOpen?warmMs:coldMs)){
                        if(phase!=null)phase("menu");
                        ready();progress("A enviar a entrada no servidor GTC…");
                        if(phase!=null)phase("join");
                        using(var response=await http.GetAsync(joinUrl,cancel)){
                            if((int)response.StatusCode<200||(int)response.StatusCode>=400)throw new InvalidOperationException("O rFactor recusou o pedido (HTTP "+(int)response.StatusCode+"). Confirma o servidor e a palavra-passe.");
                            string reply=(await response.Content.ReadAsStringAsync()).Trim();if(reply=="false"||reply=="null")throw new InvalidOperationException("O rFactor não aceitou a entrada. Consulta a mensagem no jogo.");
                        }
                        CompanionStore.Log("Pedido de entrada enviado ao rFactor.");progress("Pedido enviado. A confirmar a ligação…");
                        var confirmation=Stopwatch.StartNew();while(confirmation.Elapsed<TimeSpan.FromSeconds(20)){try{if(await Joined(origin,cancel)){if(phase!=null)phase("connected");return "Ligado ao servidor. Boa sessão!";}}catch(HttpRequestException){}await Task.Delay(stepMs,cancel);}
                        if(phase!=null)phase("action-required");
                        return "Entrada enviada ao jogo. Se aparecer o ecrã da sessão, escolhe Drive → Join Server para concluir.";
                    }
                }else{menuSince=-1;progress("Regressa ao menu principal do rFactor para entrar no servidor.");}
            }catch(HttpRequestException){menuSince=-1;}catch(TaskCanceledException){if(cancel.IsCancellationRequested)throw;menuSince=-1;}
            await Task.Delay(stepMs,cancel);
        }
        throw new TimeoutException("O menu do rFactor não ficou disponível a tempo. Regressa ao menu principal e tenta novamente.");
    }
    public async Task<string> WaitForMenu(string origin,bool alreadyOpen,Action<string> progress,CancellationToken cancel,Action<string> phase=null,Func<bool> isGameOpen=null){
        cancel.ThrowIfCancellationRequested();
        if(alreadyOpen){if(phase!=null)phase("existing-game");return "O rFactor já está aberto. A sessão atual foi mantida.";}
        var clock=Stopwatch.StartNew();bool processSeen=false;progress("A aguardar pelo menu do rFactor 2…");
        while(clock.Elapsed<TimeSpan.FromMinutes(6)){
            cancel.ThrowIfCancellationRequested();
            if(!processSeen&&isGameOpen!=null&&isGameOpen()){processSeen=true;if(phase!=null)phase("process");progress("rFactor 2 aberto. A carregar o menu…");}
            try{
                var navigation=(Dictionary<string,object>)await GetJson(origin+"/navigation/state",cancel);object stateObject,loadingObject;
                var state=navigation.TryGetValue("state",out stateObject)?stateObject as Dictionary<string,object>:null;
                var loading=navigation.TryGetValue("loadingStatus",out loadingObject)?loadingObject as Dictionary<string,object>:null;
                if(Value(state,"navigationState")=="NAV_MAIN_MENU"&&!Value(loading,"loading").Equals("True",StringComparison.OrdinalIgnoreCase)){
                    if(phase!=null)phase("menu");return "rFactor aberto no menu. Podes escolher singleplayer ou multiplayer no jogo.";
                }
            }catch(HttpRequestException){}catch(TaskCanceledException){if(cancel.IsCancellationRequested)throw;}
            await Task.Delay(stepMs,cancel);
        }
        throw new TimeoutException("O menu do rFactor não ficou disponível a tempo. Confirma o estado do jogo.");
    }
    public async Task<string> Run(LaunchSettings settings,Action<string> progress,CancellationToken cancel,Action<string,string> step=null,bool joinServer=true){
        bool gameAlreadyOpen=Running("rFactor2");
        if(gameAlreadyOpen&&joinServer){
            try{if(await Joined("http://localhost:5397",cancel)){if(step!=null){step("game","ready");step("server","existing");step("crew",Running("CrewChiefV4")?"existing":"off");step("tiny",Running("tinypedal")?"existing":"off");step("overlay",Running("GTC-Timing")?"existing":"off");}return "O jogo já está ligado a um servidor. A sessão atual foi mantida, sem novo pedido de entrada.";}}catch(HttpRequestException){}catch(TaskCanceledException){if(cancel.IsCancellationRequested)throw;}
        }
        var errors=Validate(settings,joinServer,joinServer);if(errors.Count>0)throw new InvalidOperationException(string.Join(Environment.NewLine,errors));
        if(!gameAlreadyOpen&&!File.Exists(AppDiscovery.SteamExe()))throw new IOException("Não encontrei o Steam. Abre o Steam e volta a tentar.");
        string current="game";Action<string,string> update=(id,state)=>{current=id;if(step!=null)step(id,state);};
        try{
            bool open=gameAlreadyOpen;update("game",open?"ready":"pending");
            if(!open&&settings.Borderless)SetBorderless(settings);
            if(settings.StartCrewChief){update("crew","working");await Companion(settings.CrewChiefPath,"-game RF2",progress,cancel);update("crew","ready");}else update("crew","off");
            if(settings.StartTinyPedal){update("tiny","working");await Companion(settings.TinyPedalPath,"",progress,cancel);update("tiny","ready");}else update("tiny","off");
            update("game",open?"ready":"working");
            if(!open){progress("A abrir o rFactor 2…");StartApp(AppDiscovery.SteamExe(),"-applaunch 365960");}
            if(!joinServer){
                update("server","not-requested");update("overlay","off");current="game";
                return await WaitForMenu("http://localhost:5397",open,progress,cancel,phase=>update("game",phase=="menu"?"ready":"opened"),()=>Running("rFactor2"));
            }
            bool overlay=false;if(open)current="server";string result=await Connect("http://localhost:5397",settings.JoinUrl,open,progress,()=>{
                if(!overlay){update("overlay",settings.TimingId>0?"working":"off");StartOverlay(settings,false);overlay=true;}
            },cancel,phase=>{
                if(phase=="process")update("game","opened");
                if(phase=="menu"){update("game","ready");current="server";}
                if(phase=="join")update("server","working");
                if(phase=="connected"){CompetitionProfiles.RecordConnection(settings);update("server","ready");}
                if(phase=="existing")update("server","existing");
                if(phase=="action-required")update("server","action");
            },()=>Running("rFactor2"),CompetitionProfiles.ConnectionMatches(settings));
            if(overlay&&settings.TimingId>0){current="overlay";await WaitForProcess("GTC-Timing",cancel);update("overlay","ready");}return result;
        }catch(OperationCanceledException){update(current,"cancelled");throw;}catch{update(current,"failed");throw;}
    }
    public void Dispose(){http.Dispose();}
}
