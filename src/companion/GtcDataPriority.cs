// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Linq;

public static class GtcDataPriority {
    public static List<DriverHistoryRecord> Sessions(IEnumerable<DriverHistoryRecord> records){return records.OrderByDescending(r=>r.Source=="gtc").ThenByDescending(r=>r.FirstSeenUtc).ToList();}
    public static double? TrainingBest(string driver,LiveSample current,IEnumerable<CachedSnapshot> caches,IEnumerable<DriverHistoryRecord> histories,int timingId,PersonalRecordBook local,out string context){
        context="Introduz uma melhor volta válida nas condições de referência.";
        if(current!=null&&TimingData.Normalize(current.Driver)!=TimingData.Normalize(driver))current=null;
        string track=current==null?null:current.Track,vehicleClass=current==null?null:current.CarClass;
        var snapshots=caches.Where(c=>c!=null&&c.Data!=null&&c.Data.activeRound!=null&&c.Data.rows!=null&&!string.IsNullOrWhiteSpace(c.Data.activeRound.trackName)).OrderByDescending(c=>c.UpdatedUtc).ToList();
        // A recent cache from another track must not replace a matching snapshot.
        if(!string.IsNullOrWhiteSpace(track))snapshots=snapshots.Where(c=>TimingData.Normalize(c.Data.activeRound.trackName)==TimingData.Normalize(track)).ToList();
        foreach(var snapshot in snapshots){
            if(string.IsNullOrWhiteSpace(track))track=snapshot.Data.activeRound.trackName;
            if(TimingData.Normalize(track)!=TimingData.Normalize(snapshot.Data.activeRound.trackName))continue;
            var rows=snapshot.Data.rows.Where(r=>r!=null&&TimingData.Normalize(r.driverName)==TimingData.Normalize(driver)&&LapReference.Positive(r.bestLapTime)).ToList();
            if(!string.IsNullOrWhiteSpace(vehicleClass))rows=rows.Where(r=>string.IsNullOrWhiteSpace(r.vehicleClass)||TimingData.Normalize(r.vehicleClass)==TimingData.Normalize(vehicleClass)).ToList();
            if(rows.Count==1){context="GTC · "+snapshot.Data.activeRound.trackName+" · "+rows[0].driverName;return rows[0].bestLapTime;}
        }
        var remote=histories.Where(r=>r.Source=="gtc"&&r.TimingId==timingId&&TimingData.Normalize(r.Driver)==TimingData.Normalize(driver)&&r.Laps.Any(l=>l.Clean)).OrderByDescending(r=>r.UpdatedUtc).ToList();
        if(!string.IsNullOrWhiteSpace(track))remote=remote.Where(r=>TimingData.Normalize(r.Track)==TimingData.Normalize(track)).ToList();
        if(!string.IsNullOrWhiteSpace(vehicleClass))remote=remote.Where(r=>string.IsNullOrWhiteSpace(r.Class)||TimingData.Normalize(r.Class)==TimingData.Normalize(vehicleClass)).ToList();
        if(remote.Count>0){var latest=remote[0];context="GTC · voltas disponíveis · "+latest.Track+" · "+latest.Driver;return latest.Laps.Where(l=>l.Clean).Min(l=>l.Time);}
        double? best=current!=null&&LapReference.Positive(current.BestLap)?(double?)current.BestLap:null;
        if(!string.IsNullOrWhiteSpace(track)&&local!=null&&local.Entries!=null){
            string prefix=TimingData.Normalize(track)+"|",suffix="|"+TimingData.Normalize(driver);var records=local.Entries.Where(r=>r.Key.StartsWith(prefix,StringComparison.Ordinal)&&r.Key.EndsWith(suffix,StringComparison.Ordinal)&&r.Time.HasValue&&LapReference.Positive(r.Time.Value)).ToList();
            if(!string.IsNullOrWhiteSpace(vehicleClass))records=records.Where(r=>r.Key==prefix+TimingData.Normalize(vehicleClass)+suffix).ToList();
            if(records.Count==1&&(!best.HasValue||records[0].Time<best))best=records[0].Time;
        }
        if(best.HasValue)context="Local · apoio sem referência GTC · "+track+" · "+driver;
        return best;
    }
}
