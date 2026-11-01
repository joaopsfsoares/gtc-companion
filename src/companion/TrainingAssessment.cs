// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Linq;

public sealed class TrainingAssessment {
    public int? Score;
    public double PacePoints,CleanPoints;
    public bool Partial,Estimated,Provisional;
    public string Label {get{return "Nota "+(Score.HasValue?Score.Value.ToString():"—")+"/100"+(Score.HasValue?(Provisional?" · provisória":Partial?" · parcial":Estimated?" · estimada":""):"");}}
    public string Description {get{return !Score.HasValue?"Completa uma volta para avaliar o stint.":"Ritmo "+PacePoints.ToString("0.0")+"/70 · Limpeza "+CleanPoints.ToString("0.0")+"/30"+(Estimated?" · Dados incompletos: nota estimada.":"");}}
    public static TrainingAssessment Evaluate(TrainingRun run){
        var result=new TrainingAssessment();
        if(run==null||run.Options==null||run.Laps==null||run.Laps.Count==0)return result;
        result.Partial=run.State!="completed";
        result.Provisional=run.Active;
        result.Estimated=run.HadTelemetryGap||run.Laps.Any(l=>l==null||!l.CompleteData);
        // A missing or non-finite lap cannot silently improve the average or score.
        if(!TrainingMath.Positive(run.Options.Target)||double.IsNaN(run.Options.Tolerance)||double.IsInfinity(run.Options.Tolerance)||run.Options.Tolerance<0||run.Laps.Any(l=>l==null||!TrainingMath.Positive(l.Time)))return result;
        double average=run.Laps.Average(l=>l.Time),late=Math.Max(0,average-run.Options.Target-run.Options.Tolerance);
        result.PacePoints=70*Math.Max(0,1-late/(run.Options.Target*.05));
        // Three exits per ten laps exhaust the 30 points for cleanliness.
        // Keep exits on the current, unfinished lap in the assessment as well.
        int exits=Math.Max(Math.Max(0,run.OffTracks),run.Laps.Sum(l=>Math.Max(0,l.OffTracks)));
        result.CleanPoints=Math.Max(0,30-100d*exits/run.Laps.Count);
        result.Score=(int)Math.Round(Math.Max(0,Math.Min(100,result.PacePoints+result.CleanPoints)),MidpointRounding.AwayFromZero);
        return result;
    }
}
