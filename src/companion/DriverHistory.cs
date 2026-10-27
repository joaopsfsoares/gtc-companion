// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public sealed class DriverLapPoint {
    public int Number {get;set;}
    public double Time {get;set;}
    public bool? Valid {get;set;}
    public bool? Pit {get;set;}
    public bool Complete {get;set;}
    public DateTime? AtUtc {get;set;}
    public double? S1 {get;set;}
    public double? S2 {get;set;}
    public double? S3 {get;set;}
    public bool Clean {get{return Valid==true&&Pit==false&&Complete;}}
}
public sealed class DriverPitVisit {
    public int EntryAfterLap {get;set;}
    public int? ExitAfterLap {get;set;}
    public DateTime ObservedUtc {get;set;}
    public DateTime? ExitObservedUtc {get;set;}
    public bool Approximate {get;set;}
}
public sealed class DriverStintSlice {
    public int Number,AfterLap,ThroughLap,FirstAvailable,LastAvailable;
    public override string ToString(){return Number==0?"Todos os stints":"Stint "+Number+" · dados V"+FirstAvailable+"–"+LastAvailable;}
}
public sealed class DriverPitMarker {public double AtLap;public int? SplitAfter;public string Label;public bool Approximate;}
public sealed class DriverHistoryRecord {
    [System.Web.Script.Serialization.ScriptIgnore]public int DataRevision;
    public string Id {get;set;}
    public string StreamKey {get;set;}
    public string Source {get;set;}
    public int TimingId {get;set;}
    public string Driver {get;set;}
    public string Class {get;set;}
    public string Track {get;set;}
    public DateTime FirstSeenUtc {get;set;}
    public DateTime UpdatedUtc {get;set;}
    public int TotalLaps {get;set;}
    public double LastEventTime {get;set;}
    public int? LastPitCount {get;set;}
    public bool? LastInPits {get;set;}
    public List<DriverLapPoint> Laps {get;set;}
    public List<DriverPitVisit> Visits {get;set;}
    public DriverHistoryRecord(){Laps=new List<DriverLapPoint>();Visits=new List<DriverPitVisit>();}
    public bool Partial {get{return Laps.Count<TotalLaps||Laps.Any(l=>!l.Complete);}}
    public void Put(DriverLapPoint point){
        if(point==null||point.Number<1||point.Number>100000||!TrainingMath.Positive(point.Time)||point.Time>7200)return;
        var old=Laps.FirstOrDefault(l=>l.Number==point.Number);
        if(old==null){Laps.Add(point);DataRevision++;}
        else if(point.Complete||!old.Complete){if(old.Time!=point.Time||old.Complete!=point.Complete||point.Valid.HasValue&&old.Valid!=point.Valid||point.Pit.HasValue&&old.Pit!=point.Pit||point.AtUtc.HasValue&&old.AtUtc!=point.AtUtc||point.S1.HasValue&&old.S1!=point.S1||point.S2.HasValue&&old.S2!=point.S2||point.S3.HasValue&&old.S3!=point.S3)DataRevision++;old.Time=point.Time;old.Complete=point.Complete;old.Valid=point.Valid??old.Valid;old.Pit=point.Pit??old.Pit;old.AtUtc=point.AtUtc??old.AtUtc;old.S1=point.S1??old.S1;old.S2=point.S2??old.S2;old.S3=point.S3??old.S3;}
        Laps=Laps.OrderBy(l=>l.Number).TakeLastCompat(1500).ToList();
    }
    public List<DriverPitMarker> Markers(){
        var result=Visits.Select(v=>new DriverPitMarker{AtLap=v.EntryAfterLap+.5,SplitAfter=v.ExitAfterLap,Approximate=v.Approximate,Label=v.Approximate&&v.ExitAfterLap.HasValue&&v.ExitAfterLap!=v.EntryAfterLap?"PIT ~V"+v.EntryAfterLap+"–"+v.ExitAfterLap:"PIT após V"+v.EntryAfterLap}).ToList();
        int lastPit=-1;foreach(var lap in Laps.OrderBy(l=>l.Number)){
            bool covered=Visits.Any(v=>lap.Number>=v.EntryAfterLap&&lap.Number<=(v.ExitAfterLap??TotalLaps)+1);
            if(lap.Pit==true&&!covered){if(lastPit>=0&&lap.Number>lastPit+1)result.Add(new DriverPitMarker{AtLap=lastPit,SplitAfter=lastPit,Approximate=true,Label="PIT · V"+lastPit});lastPit=lap.Number;}
            else if(lastPit>=0){result.Add(new DriverPitMarker{AtLap=lastPit,SplitAfter=lastPit,Approximate=true,Label="PIT · V"+lastPit});lastPit=-1;}
        }
        if(lastPit>=0)result.Add(new DriverPitMarker{AtLap=lastPit,SplitAfter=lastPit,Approximate=true,Label="PIT · V"+lastPit});return result;
    }
    public List<DriverStintSlice> Stints(){
        var breaks=new SortedSet<int>(Markers().Where(m=>m.SplitAfter.HasValue).Select(m=>m.SplitAfter.Value));
        var result=new List<DriverStintSlice>{new DriverStintSlice{Number=0,AfterLap=0,ThroughLap=TotalLaps}};int previous=0,number=1;
        foreach(int boundary in breaks.Where(n=>n>0&&n<TotalLaps)){if(Laps.Any(l=>l.Number>previous&&l.Number<=boundary))result.Add(new DriverStintSlice{Number=number++,AfterLap=previous,ThroughLap=boundary});previous=boundary;}
        if(Laps.Any(l=>l.Number>previous))result.Add(new DriverStintSlice{Number=number,AfterLap=previous,ThroughLap=TotalLaps});foreach(var stint in result){var points=Points(stint);stint.FirstAvailable=points.Select(p=>p.Number).DefaultIfEmpty().Min();stint.LastAvailable=points.Select(p=>p.Number).DefaultIfEmpty().Max();}return result;
    }
    public List<DriverLapPoint> Points(DriverStintSlice stint){return Laps.Where(l=>stint==null||stint.Number==0||l.Number>stint.AfterLap&&l.Number<=stint.ThroughLap).OrderBy(l=>l.Number).ToList();}
    public void PitState(bool? inPits,int? count,int completed,DateTime now){
        bool gap=UpdatedUtc!=DateTime.MinValue&&now-UpdatedUtc>TimeSpan.FromSeconds(Source=="gtc"?45:3);
        if(inPits==true&&LastInPits!=true&&!Visits.Any(v=>!v.ExitAfterLap.HasValue))Visits.Add(new DriverPitVisit{EntryAfterLap=completed,ObservedUtc=now,Approximate=!LastInPits.HasValue||gap});
        var open=Visits.LastOrDefault(v=>!v.ExitAfterLap.HasValue);
        if(inPits==false&&open!=null){open.ExitAfterLap=completed;open.ExitObservedUtc=now;open.Approximate|=gap;}
        if(count.HasValue&&LastPitCount.HasValue&&count.Value>LastPitCount.Value&&open==null&&!Visits.Any(v=>v.ExitAfterLap>=Math.Max(0,TotalLaps-1)))Visits.Add(new DriverPitVisit{EntryAfterLap=Math.Max(0,TotalLaps),ExitAfterLap=completed,ObservedUtc=now,ExitObservedUtc=now,Approximate=true});
        if(inPits.HasValue)LastInPits=inPits;if(count.HasValue)LastPitCount=count;
    }
    public override string ToString(){return (Source=="gtc"?"GTC":"Local")+" · "+Track+" · recolha "+FirstSeenUtc.ToLocalTime().ToString("dd/MM HH:mm");}
}
public static class DriverHistoryEnumerable {
    public static IEnumerable<T> TakeLastCompat<T>(this IEnumerable<T> source,int count){var list=source.ToList();return list.Skip(Math.Max(0,list.Count-count));}
}
public sealed class DriverHistoryBook {
    public readonly List<DriverHistoryRecord> Records;
    public int Revision;
    public DriverHistoryBook(){Records=DriverHistoryStore.Load();}
    public DriverHistoryRecord Get(string stream,string source,string driver,string vehicleClass,string track,double clock,int laps,DateTime now){
        var record=Records.Where(r=>r.StreamKey==stream).OrderByDescending(r=>r.UpdatedUtc).FirstOrDefault();
        if(record==null||clock>=0&&clock<record.LastEventTime-.5||laps>=0&&laps<record.TotalLaps){record=new DriverHistoryRecord{Id=Guid.NewGuid().ToString("N"),StreamKey=stream,Source=source,Driver=driver,Class=vehicleClass,Track=track,FirstSeenUtc=now};Records.Add(record);Revision++;}
        return record;
    }
    public static string Hash(string value){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
}
public static class DriverHistoryStore {
    public static List<DriverHistoryRecord> Load(){
        string root=Path.Combine(CompanionStore.Root,"training","drivers");if(!Directory.Exists(root))return new List<DriverHistoryRecord>();
        return Directory.GetFiles(root,"*.json").OrderByDescending(File.GetLastWriteTimeUtc).Take(400).Select(file=>CompanionStore.Read<DriverHistoryRecord>(file)).Where(r=>r!=null&&r.Laps!=null&&r.Visits!=null&&r.Laps.All(l=>l!=null&&TrainingMath.Positive(l.Time)&&l.Number>0)).ToList();
    }
    public static void Save(DriverHistoryRecord record){Guid id;if(!Guid.TryParseExact(record.Id,"N",out id))throw new InvalidDataException("Identificador de histórico inválido.");Directory.CreateDirectory(Path.Combine(CompanionStore.Root,"training","drivers"));CompanionStore.Write(Path.Combine("training","drivers",record.Id+".json"),record);}
}
