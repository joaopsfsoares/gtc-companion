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
            if(matches.Count==1){
                var local=matches[0];copy.DataRevision=gtc.DataRevision+local.DataRevision;foreach(var lap in local.Laps.Where(l=>l.AtUtc.HasValue&&l.AtUtc.Value>=gtc.FirstSeenUtc.AddSeconds(-10)&&l.AtUtc.Value<=gtc.UpdatedUtc.AddSeconds(10)&&!copy.Laps.Any(g=>g.Number==l.Number))){
                    var v=lap.Copy();v.DataSource="local";v.PossiblyInvalid=v.Valid!=false;copy.Laps.Add(v);
                }
                copy.Laps=copy.Laps.OrderBy(l=>l.Number).ToList();
            }
            output.Add(copy);
        }
        return output.OrderByDescending(r=>r.FirstSeenUtc).ToList();
    }
    public static string TireSummary(DriverLapPoint lap){
        if(lap==null)return "Pneus: sem dados";string compound=string.IsNullOrWhiteSpace(lap.TireCompound)?"":lap.TireCompound.Trim()+" · ";
        var values=new[]{lap.TireFL,lap.TireFR,lap.TireRL,lap.TireRR};
        if(values.Any(v=>!v.HasValue||double.IsNaN(v.Value)||double.IsInfinity(v.Value))||values.All(v=>v.Value==0))return "Pneus: "+compound+"desgaste indisponível";
        return "Pneus "+compound+"DE / DD / TE / TD: "+string.Join(" / ",values.Select(v=>v.Value.ToString("0.###",CultureInfo.GetCultureInfo("pt-PT"))).ToArray())+" · valores GTC, escala não confirmada";
    }
}
