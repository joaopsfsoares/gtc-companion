// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

public sealed class TrainingOptions {
    public bool Minutes {get;set;}
    public int Amount {get;set;}
    public double Target {get;set;}
    public double Tolerance {get;set;}
    public bool ShowHud {get;set;}
    public int HudX {get;set;}
    public int HudY {get;set;}
    public int Opacity {get;set;}
    public TrainingOptions(){Amount=15;Target=90;Tolerance=.5;ShowHud=true;HudX=-1;HudY=-1;Opacity=90;}
    public TrainingOptions Copy(){return (TrainingOptions)MemberwiseClone();}
    public void Validate(){if(Amount<1||Amount>(Minutes?240:999))throw new ArgumentException("Escolhe entre 1 e "+(Minutes?240:999)+" "+(Minutes?"minutos":"voltas")+".");if(!TrainingMath.Positive(Target)||Target<10||Target>1800)throw new ArgumentException("O ritmo alvo deve estar entre 0:10 e 30:00.");if(double.IsNaN(Tolerance)||double.IsInfinity(Tolerance)||Tolerance<.05||Tolerance>30)throw new ArgumentException("A janela deve estar entre ±0,05 e ±30 segundos.");}
}
public sealed class TrainingLap {
    public int Number {get;set;}
    public double Time {get;set;}
    public bool Valid {get;set;}
    public bool CompleteData {get;set;}
    public int OffTracks {get;set;}
}
public sealed class TrainingRun {
    public string Id {get;set;}
    public TrainingOptions Options {get;set;}
    public string Track {get;set;}
    public string Car {get;set;}
    public string Driver {get;set;}
    public string State {get;set;}
    public string Reason {get;set;}
    public DateTime CreatedUtc {get;set;}
    public DateTime? FinishedUtc {get;set;}
    public List<TrainingLap> Laps {get;set;}
    public double Elapsed {get;set;}
    public int OffTracks {get;set;}
    public bool HadTelemetryGap {get;set;}
    public TrainingRun(){Laps=new List<TrainingLap>();State="armed";}
    public double? Average {get{return Laps.Count==0?(double?)null:Laps.Average(x=>x.Time);}}
    public int InWindow {get{return Laps.Count(x=>Math.Abs(x.Time-Options.Target)<=Options.Tolerance+.000001);}}
    public bool Active {get{return State=="armed"||State=="running";}}
}
public static class TrainingMath {
    public static bool Positive(double x){return x>0&&!double.IsNaN(x)&&!double.IsInfinity(x);}
    public static double RacePace(double best,double marginPercent){if(!Positive(best)||best<10||best>1800||double.IsNaN(marginPercent)||marginPercent<0||marginPercent>10)throw new ArgumentException("Indica uma volta válida e uma margem entre 0 e 10%.");double pace=best*(1+marginPercent/100);if(pace>1800)throw new ArgumentException("O alvo calculado excede 30 minutos.");return pace;}
    public static bool ParsePace(string value,out double seconds){
        seconds=0;string[] fields=(value??"").Trim().Replace(',','.').Split(':');double tail;int minutes;
        if(fields.Length!=2||!int.TryParse(fields[0],NumberStyles.None,CultureInfo.InvariantCulture,out minutes)||!double.TryParse(fields[1],NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out tail)||tail<0||tail>=60)return false;
        seconds=minutes*60+tail;return seconds>=10&&seconds<=1800;
    }
    public static string Lap(double? time){return time.HasValue?TimingData.Lap(time.Value):"—";}
    public static string Delta(double value){return (value>0?"+":value<0?"−":"±")+Math.Abs(value).ToString("0.000",CultureInfo.GetCultureInfo("pt-PT"))+" s";}
    public static string Clock(double time){int seconds=(int)Math.Ceiling(Math.Max(0,time));return (seconds/60)+":"+(seconds%60).ToString("00");}
    public static string Remaining(TrainingRun run){if(run==null)return "—";return run.Options.Minutes?Clock(run.Options.Amount*60-run.Elapsed)+" restantes":Math.Max(0,run.Options.Amount-run.Laps.Count)+" voltas restantes";}
}
public sealed class TrainingModel {
    public TrainingRun Run {get;private set;}
    public string Notice="Pronto para preparar um stint.";
    public int Revision {get;private set;}
    LiveSample previous;
    string identity;
    double started,lastTime,lapStart,outSince=-1,inSince=-1;
    bool valid,completeData,offTrack;
    int lapCounter,lapOffTracks;
    static string Identity(LiveSample sample){return sample.Track+"|"+sample.Car+"|"+sample.Driver+"|"+sample.Session+"|"+sample.Slot;}
    public void Arm(TrainingOptions options){
        options.Validate();if(Run!=null&&Run.Active)throw new InvalidOperationException("Termina o stint atual antes de começar outro.");
        Run=new TrainingRun{Id=Guid.NewGuid().ToString("N"),Options=options.Copy(),CreatedUtc=DateTime.UtcNow};previous=null;identity=null;outSince=inSince=-1;offTrack=false;Revision++;Notice="A aguardar a próxima passagem pela meta. A volta de saída não conta.";
    }
    public void Update(LiveSample sample){
        if(Run==null||!Run.Active)return;
        if(sample==null){Notice="A aguardar telemetria. O tempo fica no último valor recebido.";return;}
        if(!TrainingMath.Positive(sample.SessionTime)||double.IsNaN(sample.LapStart)||double.IsInfinity(sample.LapStart))return;
        string key=Identity(sample);
        if(Run.State=="armed"){
            if(previous!=null&&Identity(previous)==key&&sample.Realtime&&!sample.InPits&&sample.Distance>=0&&sample.Laps==previous.Laps+1&&sample.LapStart>previous.LapStart+.05){
                Run.State="running";Run.Track=sample.Track;Run.Car=sample.Car;Run.Driver=sample.Driver;identity=key;started=sample.LapStart;lapStart=sample.LapStart;lapCounter=sample.Laps;lastTime=sample.SessionTime;valid=sample.CountLapFlag==2;completeData=true;lapOffTracks=0;Revision++;Notice="Stint em curso.";
            }else{previous=sample;return;}
        }
        if(identity!=key||sample.SessionTime<lastTime-.5){Stop("Sessão, pista ou carro alterados.",true);return;}
        if(!sample.Realtime||sample.InPits||sample.Distance<0){Stop(sample.InPits?"Regresso às boxes.":"Condução interrompida.",true);return;}
        if(sample.SessionTime-lastTime>3){completeData=false;Run.HadTelemetryGap=true;}
        if(Math.Abs(sample.LapStart-lapStart)>.05){
            double duration=sample.LapStart-lapStart;int countDelta=sample.Laps-lapCounter;
            bool uncountedInvalid=countDelta==0&&!valid&&completeData&&previous!=null&&previous.Sector==0&&sample.Sector==1;
            if((countDelta!=1&&!uncountedInvalid)||duration<10||!TrainingMath.Positive(duration)){Stop("Faltam dados para reconstruir uma volta completa.",true);return;}
            // Invalidated laps can have no official LastLap, or retain an older
            // lap time. The two observed line crossings still give their duration.
            bool gameTimed=valid&&TrainingMath.Positive(sample.LastLap)&&Math.Abs(sample.LastLap-duration)<.25;
            Run.Laps.Add(new TrainingLap{Number=Run.Laps.Count+1,Time=gameTimed?sample.LastLap:duration,Valid=valid&&gameTimed,CompleteData=completeData,OffTracks=lapOffTracks});Run.Elapsed=Math.Max(0,sample.LapStart-started);Revision++;
            if((!Run.Options.Minutes&&Run.Laps.Count>=Run.Options.Amount)||(Run.Options.Minutes&&Run.Elapsed>=Run.Options.Amount*60-.001)){Finish("completed","Objetivo de duração concluído.");return;}
            lapCounter=sample.Laps;lapStart=sample.LapStart;valid=sample.CountLapFlag==2;completeData=sample.SessionTime-sample.LapStart<=3;lapOffTracks=0;
        }else if(sample.Laps!=lapCounter){Stop("Contador de voltas alterado.",true);return;}
        if(sample.CountLapFlag!=2)valid=false;
        Run.Elapsed=Math.Max(0,sample.SessionTime-started);DetectOffTrack(sample);lastTime=sample.SessionTime;previous=sample;
        Notice=Run.Options.Minutes&&Run.Elapsed>=Run.Options.Amount*60?"Tempo atingido · termina esta volta.":"Stint em curso.";
    }
    void DetectOffTrack(LiveSample sample){
        // Approximate track corridor, not official track-limit penalties. Margin
        // places the centre about half a car width beyond the reported edge.
        bool known=TrainingMath.Positive(Math.Abs(sample.TrackEdge))&&Math.Abs(sample.TrackEdge)<200&&!double.IsNaN(sample.PathLateral)&&!double.IsInfinity(sample.PathLateral);
        if(!known){outSince=inSince=-1;return;}
        bool outside=Math.Abs(sample.PathLateral)>Math.Abs(sample.TrackEdge)+1.0;
        if(outside){inSince=-1;if(outSince<0)outSince=sample.SessionTime;if(!offTrack&&sample.SessionTime-outSince>=.6){offTrack=true;Run.OffTracks++;lapOffTracks++;Revision++;}}
        else{outSince=-1;if(inSince<0)inSince=sample.SessionTime;if(sample.SessionTime-inSince>=.6)offTrack=false;}
    }
    public void Stop(string reason="Terminado manualmente.",bool interrupted=false){if(Run!=null&&Run.Active)Finish(interrupted?"interrupted":"stopped",reason);}
    void Finish(string state,string reason){Run.State=state;Run.Reason=reason;Run.FinishedUtc=DateTime.UtcNow;Notice=reason;Revision++;}
    public void Restore(TrainingRun run){Run=run;previous=null;if(run!=null&&run.Active)Stop("A aplicação foi fechada durante o stint.",true);else Notice=run==null?"Pronto para preparar um stint.":run.Reason;}
}
public static class TrainingStore {
    public static void Save(TrainingRun run){if(run!=null){Guid id;if(!Guid.TryParseExact(run.Id,"N",out id))throw new InvalidDataException("Identificador de stint inválido.");Directory.CreateDirectory(Path.Combine(CompanionStore.Root,"training","stints"));CompanionStore.Write(Path.Combine("training","stints",run.Id+".json"),run);}}
    public static List<TrainingRun> History(){
        string root=Path.Combine(CompanionStore.Root,"training","stints");if(!Directory.Exists(root))return new List<TrainingRun>();
        return Directory.GetFiles(root,"*.json").OrderByDescending(File.GetLastWriteTimeUtc).Take(30).Select(file=>CompanionStore.Read<TrainingRun>(file)).Where(Usable).OrderByDescending(run=>run.CreatedUtc).ToList();
    }
    static bool Usable(TrainingRun run){try{Guid id;if(run==null||run.Options==null||run.Laps==null||!Guid.TryParseExact(run.Id,"N",out id))return false;run.Options.Validate();return run.Elapsed>=0&&!double.IsInfinity(run.Elapsed)&&run.Laps.All(lap=>lap!=null&&lap.Number>0&&TrainingMath.Positive(lap.Time));}catch{return false;}}
}
