// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public sealed class DriverHistoryCollector {
    public readonly DriverHistoryBook Book=new DriverHistoryBook();
    readonly string observation=Guid.NewGuid().ToString("N");
    sealed class LocalState {public LiveSample Last;public bool Full,Valid,Pit;}
    readonly Dictionary<string,LocalState> states=new Dictionary<string,LocalState>();
    public void Local(DriverActivitySnapshot activity,DateTime now){
        if(activity==null||now-activity.UpdatedUtc>TimeSpan.FromSeconds(3))return;
        foreach(var driver in activity.Drivers){
            var s=driver.Lap;if(s==null||driver.State==DriverPresence.Unknown||string.IsNullOrWhiteSpace(s.Driver))continue;
            string stream=DriverHistoryBook.Hash("local|"+observation+"|"+s.Track+"|"+s.Session+"|"+s.Slot+"|"+s.Driver+"|"+s.Car);
            var record=Book.Get(stream,"local",s.Driver,s.CarClass,s.Track,s.SessionTime,s.Laps,now);
            record.PitState(s.InPits,driver.PitStops,s.Laps,now);
            LocalState state;if(!states.TryGetValue(record.Id,out state)){state=new LocalState();states[record.Id]=state;}
            if(state.Last!=null&&s.SessionTime-state.Last.SessionTime>3)state.Full=false;
            if(state.Last!=null&&Math.Abs(s.LapStart-state.Last.LapStart)>.05){
                int delta=s.Laps-state.Last.Laps;double duration=s.LapStart-state.Last.LapStart;
                if(delta==1&&TrainingMath.Positive(s.LastLap))record.Put(new DriverLapPoint{Number=s.Laps,Time=s.LastLap,Valid=state.Full?(bool?)state.Valid:null,Pit=state.Pit?(bool?)true:state.Full?(bool?)false:null,Complete=state.Full,AtUtc=now,S1=s.LastS1>0?(double?)s.LastS1:null,S2=s.LastS1>0&&s.LastS12>s.LastS1?(double?)(s.LastS12-s.LastS1):null,S3=s.LastS12>0&&s.LastLap>s.LastS12?(double?)(s.LastLap-s.LastS12):null});
                else if(delta==1&&duration>=10)record.Put(new DriverLapPoint{Number=s.Laps,Time=duration,Valid=false,Pit=state.Pit,Complete=state.Full,AtUtc=now});
                state.Full=s.SessionTime-s.LapStart<=1;state.Valid=s.CountLapFlag==2;state.Pit=s.InPits;
            }
            if(driver.State==DriverPresence.Paddock){state.Full=false;state.Valid=false;}
            if(s.CountLapFlag!=2)state.Valid=false;if(s.InPits)state.Pit=true;
            state.Last=s;record.TotalLaps=Math.Max(record.TotalLaps,s.Laps);record.LastEventTime=s.SessionTime;record.UpdatedUtc=now;Book.Revision++;
        }
    }
    public void Remote(DriverTimingSnapshot snapshot,int timingId,DateTime now){
        if(snapshot==null||snapshot.sessionInfo==null||string.IsNullOrWhiteSpace(snapshot.sessionInfo.trackName))throw new System.IO.InvalidDataException("O snapshot não contém uma sessão identificável.");
        string session=string.IsNullOrWhiteSpace(snapshot.sourceSessionId)?observation:snapshot.sourceSessionId;
        var entries=snapshot.entries??snapshot.standings??new List<DriverTimingEntry>();
        foreach(var group in entries.Where(e=>e!=null&&!string.IsNullOrWhiteSpace(e.driverName)).GroupBy(e=>TimingData.Normalize(e.driverName)+"|"+TimingData.Normalize(e.vehicleClass))){
            if(group.Count()!=1)continue;var driver=group.Single();int total=Counter(driver.lapsCompleted??driver.totalLaps)??-1;double clock=snapshot.sessionInfo.currentEventTime??-1;
            string stream=DriverHistoryBook.Hash("gtc|"+timingId+"|"+session+"|"+snapshot.sessionInfo.sessionCode+"|"+snapshot.sessionInfo.trackName+"|"+group.Key);
            var record=Book.Get(stream,"gtc",driver.driverName,driver.vehicleClass,snapshot.sessionInfo.trackName,clock,total,now);
            record.TimingId=timingId;
            record.PitState(driver.inPits,Counter(driver.pitStops??driver.numPitstops),Math.Max(total,record.TotalLaps),now);
            foreach(var lap in driver.lapHistory??new List<DriverTimingLap>()){
                int? number=Counter(lap.lapNumber);if(!number.HasValue||!lap.lapTime.HasValue||number.Value>total&&total>=0)continue;
                DateTime parsed;DateTime? at=DateTime.TryParse(lap.completedAt,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal,out parsed)?(DateTime?)parsed:null;
                record.Put(new DriverLapPoint{Number=number.Value,Time=lap.lapTime.Value,Valid=lap.invalid.HasValue?(bool?)!lap.invalid.Value:null,Pit=lap.pit,Complete=lap.invalid.HasValue&&lap.pit.HasValue,AtUtc=at,S1=lap.s1,S2=lap.s2,S3=lap.s3});
            }
            if(total>record.TotalLaps&&driver.lastLapTime.HasValue&&TrainingMath.Positive(driver.lastLapTime.Value)&&!record.Laps.Any(l=>l.Number==total))record.Put(new DriverLapPoint{Number=total,Time=driver.lastLapTime.Value,Complete=false});
            record.TotalLaps=Math.Max(record.TotalLaps,Math.Max(total,record.Laps.Select(l=>l.Number).DefaultIfEmpty().Max()));if(clock>=0)record.LastEventTime=clock;record.UpdatedUtc=now;Book.Revision++;
        }
    }
    static int? Counter(double? value){return value.HasValue&&!double.IsNaN(value.Value)&&!double.IsInfinity(value.Value)&&value.Value>=0&&value.Value<=100000&&Math.Truncate(value.Value)==value.Value?(int?)value.Value:null;}
}
// Only sporting fields are deserialized. Server access, fuel, tyres and IDs are
// never persisted. The source session identifier is used only in a hashed key.
public sealed class DriverTimingSnapshot {public string generatedAt{get;set;}public string sourceSessionId{get;set;}public DriverTimingSession sessionInfo{get;set;}public List<DriverTimingEntry> entries{get;set;}public List<DriverTimingEntry> standings{get;set;}}
public sealed class DriverTimingSession {public string trackName{get;set;}public double? sessionCode{get;set;}public double? currentEventTime{get;set;}}
public sealed class DriverTimingEntry {public string driverName{get;set;}public string vehicleClass{get;set;}public double? lapsCompleted{get;set;}public double? totalLaps{get;set;}public double? pitStops{get;set;}public double? numPitstops{get;set;}public bool? inPits{get;set;}public double? lastLapTime{get;set;}public List<DriverTimingLap> lapHistory{get;set;}}
public sealed class DriverTimingLap {public double? lapNumber{get;set;}public double? lapTime{get;set;}public double? s1{get;set;}public double? s2{get;set;}public double? s3{get;set;}public bool? invalid{get;set;}public bool? pit{get;set;}public string completedAt{get;set;}}
