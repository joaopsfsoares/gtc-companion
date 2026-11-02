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
}
public sealed class SpectatorLap {
    public string Driver;
    public int LapNumber;
    public double? Elapsed;
    public bool InPits;
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
        latest=new SpectatorLap{Driver=car.driverName.Trim(),LapNumber=car.lapsCompleted.HasValue&&car.lapsCompleted.Value>=0&&car.lapsCompleted.Value<100000?car.lapsCompleted.Value+1:0,Elapsed=elapsed,InPits=pits};
        received=now;
    }
    public SpectatorLap Current(DateTime now){
        var age=(now-received).TotalSeconds;if(latest==null||age<0||age>2)return null;
        return new SpectatorLap{Driver=latest.Driver,LapNumber=latest.LapNumber,InPits=latest.InPits,Elapsed=latest.Elapsed.HasValue?(double?)(latest.Elapsed.Value+Math.Min(age,0.75)):null};
    }
}
