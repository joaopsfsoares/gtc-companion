// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;

// All values are observed locally or come from the permitted leaderboard.
// The read-only live snapshot supplies valid lap history when an ideal view is enabled.
// Missing sectors never become zero; observed ideals are not all-time guarantees.
public sealed class DriverIdeals {
    readonly PersonalRecordBook book;
    readonly Dictionary<string,LiveLapModel> observers=new Dictionary<string,LiveLapModel>();
    public DriverIdeals(PersonalRecordBook records){book=records;}
    public void ObserveLeaderboard(TimingSnapshot snapshot){
        if(snapshot==null||snapshot.activeRound==null||snapshot.rows==null)return;
        foreach(var row in snapshot.rows){if(string.IsNullOrWhiteSpace(row.driverName)||!LapReference.Positive(row.bestLapTime))continue;
            var key=new LiveSample{Track=snapshot.activeRound.trackName,Driver=row.driverName,CarClass=row.vehicleClass};
            book.Merge(book.Get(key),new LapReference{Time=row.bestLapTime,S1=row.s1,S2=row.s2,S3=row.s3});
        }
    }
    public void ObserveRemote(RemoteLapSnapshot snapshot,string track){
        if(snapshot==null||snapshot.sessionInfo==null||TimingData.Normalize(snapshot.sessionInfo.trackName)!=TimingData.Normalize(track))return;
        foreach(var entry in snapshot.entries??snapshot.standings??new List<RemoteLapDriver>()){
            if(string.IsNullOrWhiteSpace(entry.driverName)||entry.lapHistory==null)continue;
            foreach(var lap in entry.lapHistory){
                if(lap.invalid!=false||lap.pit!=false||!lap.s1.HasValue||!lap.s2.HasValue||!lap.s3.HasValue||!LapReference.Positive(lap.lapTime)||!LapReference.Positive(lap.s1.Value)||!LapReference.Positive(lap.s2.Value)||!LapReference.Positive(lap.s3.Value)||Math.Abs(lap.s1.Value+lap.s2.Value+lap.s3.Value-lap.lapTime)>.02)continue;
                var sample=new LiveSample{Track=track,Driver=entry.driverName,CarClass=entry.vehicleClass};
                book.Merge(book.Get(sample),new LapReference{Time=lap.lapTime,S1=lap.s1,S2=lap.s2,S3=lap.s3});
            }
        }
    }
    public void ObserveLocal(DriverActivitySnapshot activity,string track,string ownDriver,DateTime now){
        if(activity==null||now-activity.UpdatedUtc>TimeSpan.FromSeconds(3)||TimingData.Normalize(activity.Track)!=TimingData.Normalize(track))return;
        foreach(var entry in activity.Drivers){var sample=entry.Lap;if(sample==null||string.IsNullOrWhiteSpace(sample.Driver)||TimingData.Normalize(sample.Driver)==TimingData.Normalize(ownDriver))continue;
            string key=TimingData.Normalize(sample.Track)+"|"+sample.Session+"|"+sample.Slot+"|"+sample.Driver+"|"+sample.Car;
            LiveLapModel model;if(!observers.TryGetValue(key,out model)){if(observers.Count>=256)observers.Clear();model=new LiveLapModel(book);observers[key]=model;}
            model.Update(sample,null,track,sample.Driver,false);
        }
    }
    public LiveLapView Find(TimingRow row,string track){
        if(row==null||book.Entries==null)return null;
        string prefix=TimingData.Normalize(track)+"|",suffix="|"+TimingData.Normalize(row.driverName);
        var matches=book.Entries.Where(record=>record.Key.StartsWith(prefix,StringComparison.Ordinal)&&record.Key.EndsWith(suffix,StringComparison.Ordinal)).ToList();
        string exact=prefix+TimingData.Normalize(row.vehicleClass)+suffix;
        var recordExact=matches.FirstOrDefault(record=>record.Key==exact);
        var selected=recordExact??(matches.Count==1?matches[0]:null);
        return selected==null?null:LiveLapModel.BuildIdeal(selected);
    }
}

public sealed class RemoteLapSnapshot {
    public RemoteLapSession sessionInfo {get;set;}
    public List<RemoteLapDriver> entries {get;set;}
    public List<RemoteLapDriver> standings {get;set;}
}
public sealed class RemoteLapSession {public string trackName {get;set;}}
public sealed class RemoteLapDriver {
    public string driverName {get;set;}
    public string vehicleClass {get;set;}
    public List<RemoteLapEntry> lapHistory {get;set;}
}
public sealed class RemoteLapEntry {
    public double lapTime {get;set;}
    public double? s1 {get;set;}
    public double? s2 {get;set;}
    public double? s3 {get;set;}
    public bool? invalid {get;set;}
    public bool? pit {get;set;}
}
