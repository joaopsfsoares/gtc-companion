// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;

public sealed class SessionPositions {
    public string Track { get; set; }
    public string GameRun { get; set; }
    public int? Session { get; set; }
    public double LastElapsed { get; set; }
    public string RoundId { get; set; }
    public DateTime BaselineUtc { get; set; }
    public Dictionary<string,int> InitialRanks { get; set; }
    static string Key(TimingRow row){return TimingData.Normalize(row.driverName)+"|"+TimingData.Normalize(row.vehicleClass);}
    public bool ObserveSession(LiveSample sample,string gameRun){
        if(sample==null)return false;
        string track=TimingData.Normalize(sample.Track);
        bool changed=Track!=track || Session!=sample.Session || sample.SessionTime<LastElapsed-2
            || (!string.IsNullOrEmpty(GameRun)&&!string.IsNullOrEmpty(gameRun)&&GameRun!=gameRun);
        if(changed){InitialRanks=null;BaselineUtc=DateTime.MinValue;}
        Track=track;Session=sample.Session;LastElapsed=sample.SessionTime;
        if(!string.IsNullOrEmpty(gameRun))GameRun=gameRun;
        return changed;
    }
    public bool UpdateRanks(List<TimingRow> rows,string roundId){
        if(RoundId!=roundId){RoundId=roundId;InitialRanks=null;BaselineUtc=DateTime.MinValue;}
        if(InitialRanks!=null||rows.Count==0)return false;
        InitialRanks=new Dictionary<string,int>();
        for(int i=0;i<rows.Count;i++)if(!InitialRanks.ContainsKey(Key(rows[i])))InitialRanks.Add(Key(rows[i]),i+1);
        BaselineUtc=DateTime.UtcNow;return true;
    }
    public int? Movement(TimingRow row,int currentPosition){
        int initial;
        return InitialRanks!=null&&InitialRanks.TryGetValue(Key(row),out initial)?(int?)(initial-currentPosition):null;
    }
}
