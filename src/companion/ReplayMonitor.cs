// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public sealed class ReplayPending {
    public string ProfileId {get;set;}
    public string GameDirectory {get;set;}
    public string Track {get;set;}
    public string SourceSessionId {get;set;}
    public DateTime StartedUtc {get;set;}
    public List<ReplayCandidate> Candidates {get;set;}
    public ReplayPending(){Candidates=new List<ReplayCandidate>();}
}
public sealed class ReplaySpectatorBackup {public string GameDirectory {get;set;}public bool Previous {get;set;}}

public sealed class ReplayMonitor:IDisposable {
    readonly HttpClient http=new HttpClient{Timeout=TimeSpan.FromSeconds(10),MaxResponseContentBufferSize=4000000};
    readonly CancellationTokenSource cancel=new CancellationTokenSource();
    readonly ReplaySelection selector=new ReplaySelection();
    ReplayPending pending;
    bool busy,disposed,recording;
    DateTime nextStart=DateTime.MinValue;
    public string Status {get;private set;}
    public ReplayMonitor(){Status="Monitorização desativada.";pending=CompanionStore.Read<ReplayPending>("replay-pending.json");}
    public async Task Tick(LaunchSettings settings){
        if(disposed||busy)return;busy=true;
        try{
            bool gameOpen=LaunchEngine.Running("rFactor2");
            if(!gameOpen){RestoreSpectator();recording=false;if(pending!=null){Status="A verificar e guardar os replays…";await Task.Run(()=>FinishPending());}}
            if(!settings.AutoReplay){Status=pending!=null?"Gravação pendente: o replay será verificado depois da sessão.":"Monitorização desativada.";return;}
            var profile=CompetitionProfiles.Selected(settings);
            if(profile==null||profile.TimingId<=0||!CompetitionProfiles.HasAccess(profile)||string.IsNullOrWhiteSpace(settings.Driver)){Status="Configura o piloto, a classificação e o servidor.";return;}
            if(pending!=null&&!recording){Status=gameOpen?"À espera que termine o rFactor para guardar o replay.":"Replays pendentes: verifica a pasta da sessão.";return;}
            var snapshot=await Get<DriverTimingSnapshot>("https://api.gtcracecenter.com/livetiming/snapshot/"+profile.TimingId);
            DateTimeOffset generated;
            if(snapshot==null||snapshot.sessionInfo==null||string.IsNullOrWhiteSpace(snapshot.sourceSessionId)||!DateTimeOffset.TryParse(snapshot.generatedAt,out generated)||DateTimeOffset.UtcNow-generated>TimeSpan.FromSeconds(45)||generated-DateTimeOffset.UtcNow>TimeSpan.FromMinutes(1)){Status="A aguardar dados atuais da GTC.";return;}
            if(recording){
                if(pending==null||pending.ProfileId!=profile.Id||pending.SourceSessionId!=snapshot.sourceSessionId||TimingData.Normalize(pending.Track)!=TimingData.Normalize(snapshot.sessionInfo.trackName)){Status="A sessão mudou; termina o jogo para verificar a gravação anterior.";return;}
                string joinState=await Get<string>("http://localhost:5397/rest/multiplayer/join/state");
                if(joinState!="JOIN_JOINED_SERVER"&&joinState!="JOIN_REJOINED_SERVER"){recording=false;Status="A ligação terminou; fecha o rFactor para verificar a gravação.";return;}
                var board=await Get<TimingSnapshot>("https://api.gtcracecenter.com/livetiming/configs/"+profile.TimingId+"/leaderboard/snapshot");
                double? best=null;string ownClass=null;if(board!=null&&board.activeRound!=null&&TimingData.Normalize(board.activeRound.trackName)==TimingData.Normalize(pending.Track)&&board.rows!=null){var own=board.rows.Where(r=>r!=null&&TimingData.Normalize(r.driverName)==TimingData.Normalize(settings.Driver)&&LapReference.Positive(r.bestLapTime)).ToList();if(own.Count==1&&!string.IsNullOrWhiteSpace(own[0].vehicleClass)){best=own[0].bestLapTime;ownClass=own[0].vehicleClass;}}
                foreach(var candidate in selector.Observe(snapshot,best,settings.Driver)){
                    if(string.IsNullOrWhiteSpace(candidate.VehicleClass)||TimingData.Normalize(candidate.VehicleClass)!=TimingData.Normalize(ownClass))continue;
                    var prior=pending.Candidates.FirstOrDefault(c=>TimingData.Normalize(c.Driver)==TimingData.Normalize(candidate.Driver)&&TimingData.Normalize(c.VehicleClass)==TimingData.Normalize(candidate.VehicleClass));
                    if(prior==null)pending.Candidates.Add(candidate);else if(candidate.Time<prior.Time){pending.Candidates.Remove(prior);pending.Candidates.Add(candidate);}
                }
                CompanionStore.Write("replay-pending.json",pending);
                Status=best.HasValue?"A gravar como espetador · "+pending.Candidates.Count+" piloto(s) com volta candidata.":"A gravar · sem melhor volta de referência na GTC.";return;
            }
            if(gameOpen){Status="rFactor já aberto: a sessão atual foi mantida.";return;}
            if(!ReplaySelection.HasOtherCarOnTrack(snapshot,settings.Driver)){Status="À espera de outro piloto em pista.";return;}
            if(DateTime.UtcNow<nextStart){Status="A aguardar nova tentativa de ligação.";return;}
            if(!AppDiscovery.IsGameDirectory(settings.GameDirectory)||!File.Exists(AppDiscovery.SteamExe())){Status="Não foi possível localizar o rFactor ou o Steam.";return;}
            if(!ReplayEnabled(settings.GameDirectory)){Status="Ativa a gravação de replays nas definições do rFactor.";return;}
            SetSpectator(settings.GameDirectory);
            try{
                Status="A abrir o rFactor em modo espetador…";nextStart=DateTime.UtcNow.AddMinutes(2);
                Process.Start(new ProcessStartInfo(AppDiscovery.SteamExe(),"-applaunch 365960"){UseShellExecute=true});
                using(var engine=new LaunchEngine()){
                    string outcome=await engine.Connect("http://localhost:5397",settings.JoinUrl,false,s=>Status=s,()=>{},cancel.Token,null,()=>LaunchEngine.Running("rFactor2"),false);
                    if(!outcome.StartsWith("Ligado",StringComparison.Ordinal))throw new IOException("A entrada no servidor não foi confirmada.");
                }
                pending=new ReplayPending{ProfileId=profile.Id,GameDirectory=settings.GameDirectory,Track=snapshot.sessionInfo.trackName,SourceSessionId=snapshot.sourceSessionId,StartedUtc=DateTime.UtcNow};
                CompanionStore.Write("replay-pending.json",pending);recording=true;Status="Ligado como espetador. A aguardar voltas completas.";
            }catch{if(!LaunchEngine.Running("rFactor2"))RestoreSpectator();throw;}
        }catch(OperationCanceledException){if(!disposed)Status="Monitorização interrompida.";}
        catch(Exception ex){Status="Monitorização: "+ex.Message;CompanionStore.Log("Replay monitor: "+ex.GetType().Name+" "+ex.Message);}
        finally{busy=false;}
    }
    async Task<T> Get<T>(string url) where T:class{using(var response=await http.GetAsync(url,cancel.Token)){response.EnsureSuccessStatusCode();return CompanionStore.Json.Deserialize<T>(await response.Content.ReadAsStringAsync());}}
    static bool ReplayEnabled(string game){string text=File.ReadAllText(Path.Combine(game,"UserData","player","player.JSON"));return Regex.IsMatch(text,"\\\"Record Replays\\\"\\s*:\\s*1(?:\\s*[,}])");}
    static readonly Regex Spectator=new Regex("(\\\"Spectator Mode\\\"\\s*:\\s*)(true|false)",RegexOptions.IgnoreCase);
    static string Multiplayer(string game){return Path.Combine(game,"UserData","player","Multiplayer.JSON");}
    static void ReplaceConfig(string path,string value){string temp=path+".gtc-replay.tmp";File.WriteAllText(temp,value,new UTF8Encoding(false));try{File.Replace(temp,path,null);}finally{if(File.Exists(temp))File.Delete(temp);}}
    static void SetSpectator(string game){
        if(LaunchEngine.Running("rFactor2"))throw new IOException("O rFactor já está aberto.");
        string file=Multiplayer(game),original=File.ReadAllText(file);var matches=Spectator.Matches(original);if(matches.Count!=1)throw new IOException("Não foi possível confirmar a opção de espetador.");
        bool old=string.Equals(matches[0].Groups[2].Value,"true",StringComparison.OrdinalIgnoreCase);
        CompanionStore.Write("replay-spectator-backup.json",new ReplaySpectatorBackup{GameDirectory=game,Previous=old});
        if(LaunchEngine.Running("rFactor2"))throw new IOException("O rFactor abriu durante a preparação.");
        if(!old)ReplaceConfig(file,Spectator.Replace(original,"$1true",1));
    }
    static void RestoreSpectator(){
        var backup=CompanionStore.Read<ReplaySpectatorBackup>("replay-spectator-backup.json");if(backup==null)return;
        if(LaunchEngine.Running("rFactor2"))return;string file=Multiplayer(backup.GameDirectory),text=File.ReadAllText(file);var matches=Spectator.Matches(text);if(matches.Count!=1)throw new IOException("Não foi possível repor o modo de espetador.");
        if(!backup.Previous&&string.Equals(matches[0].Groups[2].Value,"true",StringComparison.OrdinalIgnoreCase))ReplaceConfig(file,Spectator.Replace(text,"$1false",1));
        File.Delete(Path.Combine(CompanionStore.Root,"replay-spectator-backup.json"));
    }
    void FinishPending(){
        if(pending.Candidates.Count==0){File.Delete(Path.Combine(CompanionStore.Root,"replay-pending.json"));pending=null;return;}
        string dir=Path.Combine(pending.GameDirectory,"UserData","Replays");if(!Directory.Exists(dir))throw new IOException("Pasta de replays do rFactor não encontrada.");
        var files=Directory.GetFiles(dir,"*.Vcr").Where(f=>File.GetLastWriteTimeUtc(f)>=pending.StartedUtc.AddSeconds(-5)&&!Path.GetFileName(f).Contains("HOT LAP")).ToArray();
        if(files.Length!=1)throw new IOException("Não foi possível identificar um único replay da sessão; os ficheiros foram mantidos.");
        foreach(var candidate in pending.Candidates.ToArray()){
            ReplayArchive.Save(CompanionStore.Root,pending.ProfileId,candidate,files[0]);pending.Candidates.Remove(candidate);CompanionStore.Write("replay-pending.json",pending);
        }
        File.Delete(Path.Combine(CompanionStore.Root,"replay-pending.json"));pending=null;
    }
    public void Dispose(){disposed=true;cancel.Cancel();http.Dispose();cancel.Dispose();}
}
