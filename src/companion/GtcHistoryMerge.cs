// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public static class GtcHistoryMerge {
    static bool Same(string a,string b){return !string.IsNullOrWhiteSpace(a)&&!string.IsNullOrWhiteSpace(b)&&TimingData.Normalize(a)==TimingData.Normalize(b);}
    static bool Anchor(DriverLapPoint a,DriverLapPoint b){return a.Number==b.Number&&a.AtUtc.HasValue&&b.AtUtc.HasValue&&Math.Abs((a.AtUtc.Value-b.AtUtc.Value).TotalSeconds)<=10&&Math.Abs(a.Time-b.Time)<.05;}
    public static List<DriverHistoryRecord> Sessions(IEnumerable<DriverHistoryRecord> source){
        var all=(source??new DriverHistoryRecord[0]).Where(r=>r!=null).ToList();var output=new List<DriverHistoryRecord>();
        foreach(var gtc in all.Where(r=>r.Source=="gtc")){
            var copy=new DriverHistoryRecord{Id=gtc.Id,StreamKey=gtc.StreamKey,Source="gtc",TimingId=gtc.TimingId,Driver=gtc.Driver,Class=gtc.Class,Track=gtc.Track,FirstSeenUtc=gtc.FirstSeenUtc,UpdatedUtc=gtc.UpdatedUtc,TotalLaps=gtc.TotalLaps,LastEventTime=gtc.LastEventTime,LastPitCount=gtc.LastPitCount,LastInPits=gtc.LastInPits,DataRevision=gtc.DataRevision,Laps=gtc.Laps.Select(l=>{var v=l.Copy();v.DataSource="gtc";return v;}).ToList(),Visits=gtc.Visits.ToList()};
            var anchors=gtc.Laps.ToLookup(l=>l.Number);var matches=all.Where(r=>r.Source=="local"&&Same(r.Driver,gtc.Driver)&&Same(r.Track,gtc.Track)&&Same(r.Class,gtc.Class)&&r.Laps.Any(l=>anchors[l.Number].Any(g=>Anchor(g,l)))).ToList();
            // Restarts can split one observed session into several local records.
            // Each record must independently share a time-and-lap anchor with
            // the GTC session; conflicting candidates for a missing lap are not guessed.
            var candidates=matches.SelectMany(r=>r.Laps).Where(l=>l.AtUtc.HasValue&&l.AtUtc.Value<=gtc.UpdatedUtc.AddSeconds(10)&&!copy.Laps.Any(g=>g.Number==l.Number)).GroupBy(l=>l.Number);
            foreach(var group in candidates){var values=group.ToList();var first=values[0];if(values.Any(v=>Math.Abs(v.Time-first.Time)>.25||Math.Abs((v.AtUtc.Value-first.AtUtc.Value).TotalSeconds)>10||v.Valid!=first.Valid))continue;
                var addition=values.OrderByDescending(l=>l.Complete).First().Copy();addition.DataSource="local";addition.PossiblyInvalid=addition.Valid!=false;copy.Laps.Add(addition);
            }
            copy.DataRevision=gtc.DataRevision+matches.Sum(r=>r.DataRevision);copy.Laps=copy.Laps.OrderBy(l=>l.Number).ToList();
            output.Add(copy);
        }
        return output.OrderByDescending(r=>r.FirstSeenUtc).ToList();
    }
    // Only associate CSV samples when driver, track, lap number, time and
    // timestamp identify a single local lap. Never alter the collected record.
    public static void AttachCsvTires(IEnumerable<DriverHistoryRecord> records,IEnumerable<TrainingRun> runs){
        var list=(runs??new TrainingRun[0]).Where(r=>r!=null&&r.Laps!=null).ToList();
        foreach(var record in records??new DriverHistoryRecord[0])foreach(var lap in record.Laps.Where(l=>l.AtUtc.HasValue)){
            var matches=list.Where(r=>Same(r.Driver,record.Driver)&&Same(r.Track,record.Track)).SelectMany(r=>r.Laps).Where(l=>l.TireSource=="csv"&&l.SessionLap==lap.Number&&l.CompletedUtc.HasValue&&Math.Abs((l.CompletedUtc.Value-lap.AtUtc.Value).TotalSeconds)<=10&&Math.Abs(l.Time-lap.Time)<.25).ToList();
            if(matches.Count!=1)continue;var m=matches[0];lap.TireFL=m.TireFL;lap.TireFR=m.TireFR;lap.TireRL=m.TireRL;lap.TireRR=m.TireRR;lap.TireSource="csv";
        }
    }
    public static double? CsvAverageWear(DriverLapPoint lap){
        if(lap==null||lap.TireSource!="csv")return null;var values=new[]{lap.TireFL,lap.TireFR,lap.TireRL,lap.TireRR};
        if(values.Any(v=>!v.HasValue||double.IsNaN(v.Value)||double.IsInfinity(v.Value)||v.Value<0||v.Value>1)||values.All(v=>v.Value==0))return null;
        return 100*(1-values.Average(v=>v.Value));
    }
    public static void WearRange(IEnumerable<double> values,out double low,out double high){
        var v=values.Where(x=>!double.IsNaN(x)&&!double.IsInfinity(x)&&x>=0&&x<=100).ToArray();if(v.Length==0){low=0;high=100;return;}
        double pad=Math.Max(1,(v.Max()-v.Min())*.15);low=Math.Max(0,v.Min()-pad);high=Math.Min(100,v.Max()+pad);if(high-low<.01){low=0;high=100;}
    }
    public static string TireSummary(DriverLapPoint lap){
        if(lap==null)return "Pneus: sem dados";var wear=CsvAverageWear(lap);if(wear.HasValue)return "Pneus · desgaste médio: "+wear.Value.ToString("0.0",CultureInfo.GetCultureInfo("pt-PT"))+"% · telemetria CSV";string compound=string.IsNullOrWhiteSpace(lap.TireCompound)?"":lap.TireCompound.Trim()+" · ";
        var values=new[]{lap.TireFL,lap.TireFR,lap.TireRL,lap.TireRR};
        if(values.Any(v=>!v.HasValue||double.IsNaN(v.Value)||double.IsInfinity(v.Value))||values.All(v=>v.Value==0))return "Pneus: "+compound+"desgaste indisponível";
        return "Pneus "+compound+"DE / DD / TE / TD: "+string.Join(" / ",values.Select(v=>v.Value.ToString("0.###",CultureInfo.GetCultureInfo("pt-PT"))).ToArray())+" · valores GTC, escala não confirmada";
    }
}
