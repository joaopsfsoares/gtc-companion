// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;

public static class TrainingHistoryView {
    static bool Same(string a,string b){return !string.IsNullOrWhiteSpace(a)&&!string.IsNullOrWhiteSpace(b)&&TimingData.Normalize(a)==TimingData.Normalize(b);}
    static bool Match(TrainingLap local,DriverLapPoint remote,DateTime start,DateTime end){
        if(remote.AtUtc==null||remote.AtUtc.Value<start.AddSeconds(-10)||remote.AtUtc.Value>end.AddSeconds(10))return false;
        if(local.CompletedUtc.HasValue)return Math.Abs((local.CompletedUtc.Value-remote.AtUtc.Value).TotalSeconds)<=10&&(!local.SessionLap.HasValue||local.SessionLap.Value==remote.Number);
        return Math.Abs(local.Time-remote.Time)<.05;
    }
    public static TrainingRun Merge(TrainingRun run,IEnumerable<DriverHistoryRecord> source,int timingId,DateTime now){
        if(run==null)return null;var result=new TrainingRun{Id=run.Id,Options=run.Options,Track=run.Track,Car=run.Car,Driver=run.Driver,State=run.State,Reason=run.Reason,RecoveryNote=run.RecoveryNote,CreatedUtc=run.CreatedUtc,FinishedUtc=run.FinishedUtc,Elapsed=run.Elapsed,OffTracks=run.OffTracks,HadTelemetryGap=run.HadTelemetryGap,Automatic=run.Automatic,GoalReached=run.GoalReached,Laps=run.Laps.Select(l=>{var c=l.Copy();c.DataSource="local";return c;}).ToList()};
        if(string.IsNullOrWhiteSpace(run.Track)||string.IsNullOrWhiteSpace(run.Driver))return result;
        DateTime end=run.FinishedUtc??now;var candidates=(source??new DriverHistoryRecord[0]).Where(r=>r!=null&&r.Source=="gtc"&&r.TimingId==timingId&&Same(r.Driver,run.Driver)&&Same(r.Track,run.Track)&&r.Laps.Any(g=>run.Laps.Any(l=>Match(l,g,run.CreatedUtc,end)))).ToList();
        if(candidates.Count!=1)return result;result.GtcMatched=true;var record=candidates[0];
        foreach(var lap in result.Laps){
            var matches=record.Laps.Where(g=>Match(lap,g,run.CreatedUtc,end)).ToList();
            if(matches.Count==1){var g=matches[0];lap.Time=g.Time;lap.Valid=g.Valid==true;lap.CompleteData=g.Complete;lap.S1=g.S1;lap.S2=g.S2;lap.S3=g.S3;lap.DataSource="gtc";lap.TireCompound=g.TireCompound;if(lap.TireSource!="csv"){lap.TireFL=g.TireFL;lap.TireFR=g.TireFR;lap.TireRL=g.TireRL;lap.TireRR=g.TireRR;}}
            else if(matches.Count==0&&(lap.CompletedUtc.HasValue?lap.CompletedUtc.Value>=record.FirstSeenUtc.AddSeconds(-10)&&lap.CompletedUtc.Value<=record.UpdatedUtc.AddSeconds(10):run.CreatedUtc>=record.FirstSeenUtc.AddSeconds(-10)&&end<=record.UpdatedUtc.AddSeconds(10))){lap.PossiblyInvalid=lap.Valid;}
        }
        return result;
    }
    public static string TireSummary(TrainingLap lap){if(lap==null)return "Pneus: sem dados";if(lap.TireSource=="csv"&&lap.TireFL.HasValue&&lap.TireFR.HasValue&&lap.TireRL.HasValue&&lap.TireRR.HasValue)return "Pneus · desgaste médio: "+(100*(1-(lap.TireFL.Value+lap.TireFR.Value+lap.TireRL.Value+lap.TireRR.Value)/4)).ToString("0.0")+"% · telemetria CSV";return GtcHistoryMerge.TireSummary(new DriverLapPoint{TireCompound=lap.TireCompound,TireFL=lap.TireFL,TireFR=lap.TireFR,TireRL=lap.TireRL,TireRR=lap.TireRR});}
}
