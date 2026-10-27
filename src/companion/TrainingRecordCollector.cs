// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;

// Collects complete valid laps even when the stint HUD is hidden or disarmed.
// Missing, invalid, pit and partial laps cannot improve local records.
public sealed class TrainingRecordCollector {
    public readonly PersonalRecordBook Book=new PersonalRecordBook();
    public LiveSample Latest {get;private set;}
    public DateTime LatestUtc {get;private set;}
    string identity;
    double start,lastTime;
    int laps;
    bool observed,valid;
    public void Observe(LiveSample sample){
        if(sample==null)return;Latest=sample;LatestUtc=DateTime.UtcNow;
        string key=sample.Track+"|"+sample.Car+"|"+sample.Driver+"|"+sample.Session+"|"+sample.Slot;
        if(key!=identity||sample.SessionTime<lastTime-.5){identity=key;start=sample.LapStart;laps=sample.Laps;observed=false;valid=false;}
        if(sample.SessionTime-lastTime>3)valid=false;
        if(Math.Abs(sample.LapStart-start)>.05){
            double duration=sample.LapStart-start;
            if(observed&&valid&&sample.Realtime&&!sample.InPits&&sample.Laps==laps+1&&LapReference.Positive(sample.LastLap)&&Math.Abs(sample.LastLap-duration)<.25){
                var lap=new LapReference{Time=sample.LastLap};
                if(sample.LastS1>0&&sample.LastS12>sample.LastS1&&sample.LastLap>sample.LastS12){lap.S1=sample.LastS1;lap.S2=sample.LastS12-sample.LastS1;lap.S3=sample.LastLap-sample.LastS12;}
                Book.Merge(Book.Get(sample),lap,true);
            }
            start=sample.LapStart;laps=sample.Laps;observed=sample.SessionTime-sample.LapStart<=1;valid=observed&&sample.CountLapFlag==2&&sample.Realtime&&!sample.InPits&&sample.Distance>=0;
        }
        if(!sample.Realtime||sample.InPits||sample.CountLapFlag!=2||sample.Distance<0||sample.Laps!=laps)valid=false;
        lastTime=sample.SessionTime;
    }
}
