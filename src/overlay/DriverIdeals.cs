// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

// GTC values have priority. Local records fill only fields absent from the API.
// The permitted read-only live snapshot supplies valid lap history and presence.
// Missing sectors never become zero; observed ideals are not all-time guarantees.
public sealed class DriverIdeals {
    readonly PersonalRecordBook book;
    public readonly GtcRecordCache Online;
    DriverActivitySnapshot remoteActivity;
    readonly Dictionary<string,LiveLapModel> observers=new Dictionary<string,LiveLapModel>();
    public DriverIdeals(PersonalRecordBook records,GtcRecordCache online=null){book=records;Online=online??new GtcRecordCache();if(Online.Records==null)Online.Records=new PersonalRecordBook();}
    public void ObserveLeaderboard(TimingSnapshot snapshot){
        if(snapshot==null||snapshot.activeRound==null||snapshot.rows==null)return;
        if(Online.RoundId!=snapshot.activeRound.id){Online.RoundId=snapshot.activeRound.id;Online.Records=new PersonalRecordBook();remoteActivity=null;}
        foreach(var row in snapshot.rows){if(string.IsNullOrWhiteSpace(row.driverName)||!LapReference.Positive(row.bestLapTime))continue;
            var key=new LiveSample{Track=snapshot.activeRound.trackName,Driver=row.driverName,CarClass=row.vehicleClass};
            MergeOnline(key,new LapReference{Time=row.bestLapTime,S1=row.s1,S2=row.s2,S3=row.s3});
        }
    }
    public void ObserveRemote(RemoteLapSnapshot snapshot,string track){ObserveRemote(snapshot,track,DateTime.UtcNow);}
    public void ObserveRemote(RemoteLapSnapshot snapshot,string track,DateTime now){
        if(snapshot==null||snapshot.sessionInfo==null||TimingData.Normalize(snapshot.sessionInfo.trackName)!=TimingData.Normalize(track))return;
        DateTime observed=now,parsed;if(!string.IsNullOrWhiteSpace(snapshot.generatedAt)){observed=DateTime.TryParse(snapshot.generatedAt,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal,out parsed)&&parsed<=now.AddSeconds(5)?parsed:DateTime.MinValue;}
        remoteActivity=new DriverActivitySnapshot{Track=track,UpdatedUtc=observed};
        foreach(var entry in snapshot.entries??snapshot.standings??new List<RemoteLapDriver>()){
            if(entry==null||string.IsNullOrWhiteSpace(entry.driverName))continue;
            remoteActivity.Drivers.Add(new DriverActivityEntry{Name=entry.driverName,Class=entry.vehicleClass,State=entry.inGarageStall==true?DriverPresence.Paddock:entry.inPits==true?DriverPresence.InPits:entry.inPits==false&&entry.inGarageStall==false?DriverPresence.OnTrack:DriverPresence.Unknown});
            foreach(var lap in entry.lapHistory??new List<RemoteLapEntry>()){
                if(lap==null||lap.invalid!=false||lap.pit!=false||!lap.s1.HasValue||!lap.s2.HasValue||!lap.s3.HasValue||!lap.lapTime.HasValue||!LapReference.Positive(lap.lapTime.Value)||!LapReference.Positive(lap.s1.Value)||!LapReference.Positive(lap.s2.Value)||!LapReference.Positive(lap.s3.Value)||Math.Abs(lap.s1.Value+lap.s2.Value+lap.s3.Value-lap.lapTime.Value)>.02)continue;
                var sample=new LiveSample{Track=track,Driver=entry.driverName,CarClass=entry.vehicleClass};
                MergeOnline(sample,new LapReference{Time=lap.lapTime.Value,S1=lap.s1,S2=lap.s2,S3=lap.s3});
            }
        }
    }
    void MergeOnline(LiveSample sample,LapReference lap){Online.Records.Merge(Online.Records.Get(sample),lap);book.Merge(book.Get(sample),lap);}
    public DriverPresence Presence(TimingRow row,string track,DriverActivitySnapshot local,DateTime now){
        if(remoteActivity!=null&&now-remoteActivity.UpdatedUtc<=TimeSpan.FromSeconds(30)){
            // DriverActivitySnapshot also enforces its three-second window. The
            // remote snapshot is polled at 15 s, so evaluate its identity at its timestamp.
            var value=remoteActivity.Find(row,track,remoteActivity.UpdatedUtc);if(value!=DriverPresence.Unknown)return value;
        }
        return local==null?DriverPresence.Unknown:local.Find(row,track,now);
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
        var record=PreferredRecord(row,track);return record==null?null:LiveLapModel.BuildIdeal(record);
    }
    public PersonalRecord PreferredRecord(TimingRow row,string track){
        var api=FindRecord(Online.Records,row,track);var local=FindRecord(book,row,track);if(api==null)return local==null?null:local.Copy();
        var result=api.Copy();if(local!=null){result.S1=result.S1??local.S1;result.S2=result.S2??local.S2;result.S3=result.S3??local.S3;}if(row!=null&&LapReference.Positive(row.bestLapTime)){result.Time=row.bestLapTime;result.BestLap=new LapReference{Time=row.bestLapTime,S1=row.s1,S2=row.s2,S3=row.s3};}return result;
    }
    static PersonalRecord FindRecord(PersonalRecordBook records,TimingRow row,string track){
        if(row==null||records==null||records.Entries==null)return null;
        string prefix=TimingData.Normalize(track)+"|",suffix="|"+TimingData.Normalize(row.driverName);
        var matches=records.Entries.Where(record=>record.Key.StartsWith(prefix,StringComparison.Ordinal)&&record.Key.EndsWith(suffix,StringComparison.Ordinal)).ToList();
        string exact=prefix+TimingData.Normalize(row.vehicleClass)+suffix;
        var recordExact=matches.FirstOrDefault(record=>record.Key==exact);
        return recordExact??(matches.Count==1&&(string.IsNullOrWhiteSpace(row.vehicleClass)||matches[0].Key==prefix+suffix)?matches[0]:null);
    }
}
public sealed class GtcRecordCache {public string RoundId {get;set;}public PersonalRecordBook Records {get;set;}public GtcRecordCache(){Records=new PersonalRecordBook();}}

public sealed class RemoteLapSnapshot {
    public string generatedAt {get;set;}
    public RemoteLapSession sessionInfo {get;set;}
    public List<RemoteLapDriver> entries {get;set;}
    public List<RemoteLapDriver> standings {get;set;}
}
public sealed class RemoteLapSession {public string trackName {get;set;}}
public sealed class RemoteLapDriver {
    public string driverName {get;set;}
    public string vehicleClass {get;set;}
    public bool? inPits {get;set;}
    public bool? inGarageStall {get;set;}
    public List<RemoteLapEntry> lapHistory {get;set;}
}
public sealed class RemoteLapEntry {
    public double? lapTime {get;set;}
    public double? s1 {get;set;}
    public double? s2 {get;set;}
    public double? s3 {get;set;}
    public bool? invalid {get;set;}
    public bool? pit {get;set;}
}
