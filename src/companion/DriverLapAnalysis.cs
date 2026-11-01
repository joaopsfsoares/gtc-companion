// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public enum LapMetric {Lap,S1,S2,S3}
public static class DriverLapAnalysis {
    public static readonly string[] Names={"Volta completa","Setor 1","Setor 2","Setor 3"};
    public static string Name(LapMetric metric){return Names[(int)metric];}
    public static double? Value(DriverLapPoint lap,LapMetric metric){return lap==null?null:Value(lap.Time,lap.S1,lap.S2,lap.S3,metric);}
    public static double? Value(TrainingLap lap,LapMetric metric){return lap==null?null:Value(lap.Time,lap.S1,lap.S2,lap.S3,metric);}
    static double? Known(double? value){return value.HasValue&&TrainingMath.Positive(value.Value)?value:null;}
    static double? Value(double time,double? s1,double? s2,double? s3,LapMetric metric){
        if(!TrainingMath.Positive(time))return null;if(metric==LapMetric.Lap)return time;
        s1=Known(s1);s2=Known(s2);s3=Known(s3);
        if(s1.HasValue&&s2.HasValue&&s3.HasValue&&Math.Abs(s1.Value+s2.Value+s3.Value-time)>.05)return null;
        double? value=metric==LapMetric.S1?s1:metric==LapMetric.S2?s2:s3;return value.HasValue&&value.Value<=time+.002?value:null;
    }
    public static string Format(double? value,LapMetric metric){return !value.HasValue?"—":metric==LapMetric.Lap?TimingData.Lap(value.Value):value.Value.ToString("0.000",CultureInfo.GetCultureInfo("pt-PT"))+" s";}
    public static DriverLapPoint Reference(IEnumerable<DriverLapPoint> laps){return laps.Where(l=>l.Clean).OrderBy(l=>l.Time).ThenBy(l=>l.Number).FirstOrDefault();}
    public static TrainingLap Reference(IEnumerable<TrainingLap> laps){return laps.Where(l=>l.Valid&&l.CompleteData&&!l.PossiblyInvalid).OrderBy(l=>l.Time).ThenBy(l=>l.Number).FirstOrDefault();}
    public static double? Mean(IEnumerable<DriverLapPoint> laps,LapMetric metric){var values=laps.Where(l=>l.Clean).Select(l=>Value(l,metric)).Where(v=>v.HasValue).Select(v=>v.Value).ToList();return values.Count==0?(double?)null:values.Average();}
    public static string SectorDelta(DriverLapPoint lap,DriverLapPoint reference,LapMetric metric){var value=Value(lap,metric);var baseline=Value(reference,metric);return value.HasValue&&baseline.HasValue?TrainingMath.Delta(value.Value-baseline.Value):"—";}
    public static void CaptureSectors(TrainingLap lap,LiveSample sample,bool timed){
        // Last-sector counters can still belong to an older lap after an invalid
        // crossing. Only keep them when this lap was timed and fully observed.
        if(!timed||!lap.CompleteData||sample.LastS1<=0||sample.LastS12<=sample.LastS1||sample.LastLap<=sample.LastS12)return;
        double a=sample.LastS1,b=sample.LastS12-a,c=sample.LastLap-sample.LastS12;
        if(!TrainingMath.Positive(a)||!TrainingMath.Positive(b)||!TrainingMath.Positive(c)||Math.Abs(a+b+c-lap.Time)>.05)return;
        lap.S1=a;lap.S2=b;lap.S3=c;
    }
}
