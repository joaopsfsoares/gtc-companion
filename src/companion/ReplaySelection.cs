// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;

// Selects observed completed laps; recording and replay validation are handled separately.
public sealed class ReplayCandidate {
    public string Driver {get;set;}
    public string VehicleClass {get;set;}
    public string Track {get;set;}
    public int LapNumber {get;set;}
    public double Time {get;set;}
}
public sealed class ReplaySelection {
    readonly Dictionary<string,int> observed=new Dictionary<string,int>();
    readonly Dictionary<string,double> saved=new Dictionary<string,double>();
    readonly Dictionary<string,int> completeAfter=new Dictionary<string,int>();
    readonly Dictionary<string,HashSet<int>> processed=new Dictionary<string,HashSet<int>>();
    string session;
    readonly HashSet<string> completeLapBaseline=new HashSet<string>();
    public static bool HasOtherCarOnTrack(DriverTimingSnapshot snapshot,string player){
        if(snapshot==null||string.IsNullOrWhiteSpace(player))return false;
        var entries=snapshot.entries??snapshot.standings;
        return entries!=null&&entries.Any(e=>e!=null&&!string.IsNullOrWhiteSpace(e.driverName)&&TimingData.Normalize(e.driverName)!=TimingData.Normalize(player)&&e.inPits==false);
    }
    public void SetSaved(string track,string driver,string vehicleClass,double time){
        if(LapReference.Positive(time))saved[Key(track,driver,vehicleClass)]=time;
    }
    public List<ReplayCandidate> Observe(DriverTimingSnapshot snapshot,double? playerBest,string player){
        var found=new List<ReplayCandidate>();
        if(snapshot==null||snapshot.sessionInfo==null||string.IsNullOrWhiteSpace(snapshot.sessionInfo.trackName)||string.IsNullOrWhiteSpace(snapshot.sourceSessionId))return found;
        var entries=snapshot.entries??snapshot.standings;
        if(entries==null)return found;
        string sessionKey=snapshot.sourceSessionId+"|"+snapshot.sessionInfo.sessionCode+"|"+snapshot.sessionInfo.trackName;
        if(session!=sessionKey){observed.Clear();completeLapBaseline.Clear();completeAfter.Clear();processed.Clear();session=sessionKey;}
        foreach(var group in entries.Where(e=>e!=null&&!string.IsNullOrWhiteSpace(e.driverName)).GroupBy(e=>Key(snapshot.sessionInfo.trackName,e.driverName,e.vehicleClass))){
            if(group.Count()!=1)continue;
            var entry=group.Single();double count=entry.lapsCompleted??entry.totalLaps??0;if(double.IsNaN(count)||double.IsInfinity(count)||count<0||count>100000||count!=Math.Truncate(count))continue;int total=(int)count;int before;
            if(!observed.TryGetValue(group.Key,out before)){observed[group.Key]=total;continue;}
            if(total<before){observed[group.Key]=total;completeLapBaseline.Remove(group.Key);completeAfter.Remove(group.Key);processed.Remove(group.Key);continue;}
            observed[group.Key]=total;
            // The first completed lap could have started before recording began.
            if(total>before&&!completeLapBaseline.Contains(group.Key)){completeLapBaseline.Add(group.Key);completeAfter[group.Key]=total;processed[group.Key]=new HashSet<int>();continue;}
            if(!completeLapBaseline.Contains(group.Key))continue;
            if(!playerBest.HasValue||!LapReference.Positive(playerBest.Value)||TimingData.Normalize(entry.driverName)==TimingData.Normalize(player))continue;
            double previous;saved.TryGetValue(group.Key,out previous);
            var completed=(entry.lapHistory??new List<DriverTimingLap>()).Where(l=>l!=null&&l.lapNumber.HasValue&&l.lapNumber.Value>completeAfter[group.Key]&&l.lapNumber.Value<=total&&l.lapNumber.Value==Math.Truncate(l.lapNumber.Value)&&l.lapTime.HasValue&&LapReference.Positive(l.lapTime.Value)&&l.invalid.HasValue&&l.pit.HasValue&&!processed[group.Key].Contains((int)l.lapNumber.Value)).ToList();
            foreach(var lap in completed)processed[group.Key].Add((int)lap.lapNumber.Value);
            var best=completed.Where(l=>l.invalid==false&&l.pit==false&&l.lapTime.Value<playerBest.Value&&(previous<=0||l.lapTime.Value<previous)).OrderBy(l=>l.lapTime.Value).FirstOrDefault();
            if(best!=null)found.Add(new ReplayCandidate{Driver=entry.driverName,VehicleClass=entry.vehicleClass,Track=snapshot.sessionInfo.trackName,LapNumber=(int)best.lapNumber.Value,Time=best.lapTime.Value});
        }
        return found;
    }
    static string Key(string track,string driver,string vehicleClass){return TimingData.Normalize(track)+"|"+TimingData.Normalize(driver)+"|"+TimingData.Normalize(vehicleClass);}
}
