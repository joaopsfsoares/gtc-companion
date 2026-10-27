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

public sealed class RankingPoint {
    public DateTime AtUtc {get;set;}
    public string Round {get;set;}
    public Dictionary<string,int> Ranks {get;set;}
}
public sealed class RankingHistory {
    public List<RankingPoint> Points {get;set;}
    public RankingHistory(){Points=new List<RankingPoint>();}
    public static string RoundKey(TimingSnapshot snapshot){return snapshot==null||snapshot.activeRound==null?"":snapshot.activeRound.id+"|"+TimingData.Normalize(snapshot.activeRound.trackName);}
    public static string DriverKey(TimingRow row){return TimingData.Normalize(row.driverName)+"|"+TimingData.Normalize(row.vehicleClass);}
    public bool Record(TimingSnapshot snapshot,List<TimingRow> rows,DateTime now){
        if(rows.Count==0||string.IsNullOrEmpty(RoundKey(snapshot)))return false;
        if(Points==null)Points=new List<RankingPoint>();string round=RoundKey(snapshot);
        var last=Points.LastOrDefault(p=>p.Round==round);
        if(last!=null&&(now-last.AtUtc).TotalSeconds<60)return false;
        var ranks=new Dictionary<string,int>();for(int i=0;i<rows.Count;i++)if(!ranks.ContainsKey(DriverKey(rows[i])))ranks.Add(DriverKey(rows[i]),i+1);
        Points.Add(new RankingPoint{AtUtc=now,Round=round,Ranks=ranks});
        Points=Points.Where(p=>p.AtUtc>=now.AddDays(-8)).OrderBy(p=>p.AtUtc).ToList();return true;
    }
    public RankingPoint Reference(TimingSnapshot snapshot,int mode,int hours,DateTime now){
        if(mode==0||Points==null)return null;
        DateTime cutoff=mode==2?now.ToLocalTime().Date.AddDays(-1).ToUniversalTime():now.AddHours(-Math.Max(1,Math.Min(168,hours)));
        string round=RoundKey(snapshot);
        var point=Points.Where(p=>p.Round==round&&p.AtUtc<=cutoff).OrderByDescending(p=>p.AtUtc).FirstOrDefault();
        // A long gap while the app was closed is not a reliable hourly reference.
        return point!=null&&(cutoff-point.AtUtc).TotalMinutes<=15?point:null;
    }
    public static int? Movement(RankingPoint point,TimingRow row,int current){int initial;return point!=null&&point.Ranks!=null&&point.Ranks.TryGetValue(DriverKey(row),out initial)?(int?)(initial-current):null;}
    public static void SelfTest(){
        DateTime now=new DateTime(2026,10,25,14,0,0,DateTimeKind.Utc);var data=new TimingSnapshot{activeRound=new RoundInfo{id="a",trackName="Track"}};
        var a=new TimingRow{driverName="A",vehicleClass="Cup"};var b=new TimingRow{driverName="B",vehicleClass="Cup"};var history=new RankingHistory();
        history.Record(data,new List<TimingRow>{a,b},now.AddHours(-2));history.Record(data,new List<TimingRow>{b,a},now);
        var reference=history.Reference(data,1,2,now);
        if(Movement(reference,b,1)!=1||Movement(reference,a,2)!=-1)throw new Exception("Histórico: referência horária e movimentos");
        if(history.Reference(data,1,1,now)!=null)throw new Exception("Histórico: intervalo sem recolha não pode inventar referência");
        data.activeRound.id="other";if(history.Reference(data,1,2,now)!=null)throw new Exception("Histórico: não comparar rondas distintas");data.activeRound.id="a";
        DateTime midnight=now.ToLocalTime().Date.AddDays(-1).ToUniversalTime();var daily=new RankingHistory();daily.Record(data,new List<TimingRow>{a,b},midnight);
        if(daily.Reference(data,2,24,now)==null)throw new Exception("Histórico: ontem às 00:00");
    }
}
