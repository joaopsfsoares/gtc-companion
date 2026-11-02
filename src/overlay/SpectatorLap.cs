// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;

// The rFactor local watch API explicitly identifies the camera's current car.
public sealed class WatchedCar {
    public string driverName {get;set;}
    public bool? hasFocus {get;set;}
    public bool? player {get;set;}
    public bool? pitting {get;set;}
    public bool? inGarageStall {get;set;}
    public int? lapsCompleted {get;set;}
    public double? timeIntoLap {get;set;}
    public string sector {get;set;}
    public double? currentSectorTime1 {get;set;}
    public double? currentSectorTime2 {get;set;}
}
public sealed class SpectatorLap {
    public string Driver;
    public int LapNumber;
    public double? Elapsed;
    public bool InPits;
    public string Sector;
    public double? S1,S12;
    public LiveLapView ToView(){
        var view=new LiveLapView{LapNumber=LapNumber,Elapsed=Elapsed};
        if(InPits||!Elapsed.HasValue||Elapsed.Value<0)return view;
        double time=Elapsed.Value;
        if(string.Equals(Sector,"SECTOR1",StringComparison.OrdinalIgnoreCase)){view.Sectors[0]=time;return view;}
        if(!string.Equals(Sector,"SECTOR2",StringComparison.OrdinalIgnoreCase)&&!string.Equals(Sector,"SECTOR3",StringComparison.OrdinalIgnoreCase))return view;
        if(!S1.HasValue||!LapReference.Positive(S1.Value)||S1.Value>time)return view;
        view.Sectors[0]=S1;view.Done[0]=true;
        if(string.Equals(Sector,"SECTOR2",StringComparison.OrdinalIgnoreCase)){view.Sectors[1]=time-S1.Value;return view;}
        if(!string.Equals(Sector,"SECTOR3",StringComparison.OrdinalIgnoreCase)||!S12.HasValue||!LapReference.Positive(S12.Value)||S12.Value<=S1.Value||S12.Value>time)return view;
        view.Sectors[1]=S12.Value-S1.Value;view.Done[1]=true;view.Sectors[2]=time-S12.Value;return view;
    }
}
public sealed class SpectatorLapTracker {
    SpectatorLap latest;
    DateTime received;
    public void Update(IEnumerable<WatchedCar> cars,DateTime now){
        var focus=(cars??Enumerable.Empty<WatchedCar>()).Where(x=>x!=null&&x.hasFocus==true).ToArray();
        if(focus.Length!=1||focus[0].player!=false||string.IsNullOrWhiteSpace(focus[0].driverName)){latest=null;return;}
        var car=focus[0];bool pits=car.pitting==true||car.inGarageStall==true;
        var elapsed=car.timeIntoLap;
        if(!elapsed.HasValue||double.IsNaN(elapsed.Value)||double.IsInfinity(elapsed.Value)||elapsed.Value<0||elapsed.Value>14400||pits)elapsed=null;
        latest=new SpectatorLap{Driver=car.driverName.Trim(),LapNumber=car.lapsCompleted.HasValue&&car.lapsCompleted.Value>=0&&car.lapsCompleted.Value<100000?car.lapsCompleted.Value+1:0,Elapsed=elapsed,InPits=pits,Sector=car.sector,S1=car.currentSectorTime1,S12=car.currentSectorTime2};
        received=now;
    }
    public SpectatorLap Current(DateTime now){
        var age=(now-received).TotalSeconds;if(latest==null||age<0||age>2)return null;
        return new SpectatorLap{Driver=latest.Driver,LapNumber=latest.LapNumber,InPits=latest.InPits,Sector=latest.Sector,S1=latest.S1,S12=latest.S12,Elapsed=latest.Elapsed.HasValue?(double?)(latest.Elapsed.Value+Math.Min(age,0.75)):null};
    }
}
