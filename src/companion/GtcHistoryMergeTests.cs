// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Linq;
public static class GtcHistoryMergeTests {
    static void Check(bool value,string message){if(!value)throw new Exception("Histórico GTC: "+message);}
    public static void Run(){
        DateTime now=DateTime.UtcNow;
        var remote=new DriverHistoryRecord{Id="r",Source="gtc",TimingId=110,Driver="Test driver",Class="Cup",Track="Test track",FirstSeenUtc=now.AddMinutes(-5),UpdatedUtc=now,TotalLaps=3};
        remote.Put(new DriverLapPoint{Number=1,Time=90.01,Valid=true,Pit=false,Complete=true,AtUtc=now.AddMinutes(-2),TireCompound="Dry",TireFL=.8,TireFR=.9,TireRL=.7,TireRR=.6});
        var local=new DriverHistoryRecord{Id="l",Source="local",Driver="Test driver",Class="Cup",Track="Test track",FirstSeenUtc=now.AddMinutes(-4),UpdatedUtc=now,TotalLaps=3};
        local.Put(new DriverLapPoint{Number=1,Time=90.0,Valid=true,Pit=false,Complete=true,AtUtc=now.AddMinutes(-2).AddSeconds(1)});
        local.Put(new DriverLapPoint{Number=2,Time=91,Valid=false,Pit=false,Complete=true,AtUtc=now.AddMinutes(-1)});
        local.Put(new DriverLapPoint{Number=3,Time=92,Valid=true,Pit=false,Complete=true,AtUtc=now.AddSeconds(-5)});
        var merged=GtcHistoryMerge.Sessions(new[]{remote,local}).Single();
        Check(merged.Laps.Count==3&&merged.Laps[0].Time==90.01&&merged.Laps[0].DataSource=="gtc","GTC prevalece sobre valor local");
        Check(merged.Laps[1].DataSource=="local"&&merged.Laps[1].Valid==false&&!merged.Laps[1].Clean,"completar e identificar volta inválida local");
        Check(merged.Laps[2].PossiblyInvalid&&!merged.Laps[2].Clean,"ausência GTC não prova invalidade");
        Check(remote.Laps.Count==1&&local.Laps.Count==3,"projeção não altera os registos originais");
        var conflict=new DriverHistoryRecord{Id="duplicate",Source="local",Driver=local.Driver,Track=local.Track,Class=local.Class,FirstSeenUtc=local.FirstSeenUtc,UpdatedUtc=local.UpdatedUtc,Laps=local.Laps.Select(x=>x.Copy()).ToList()};
        Check(GtcHistoryMerge.Sessions(new[]{remote,local,conflict}).Single().Laps.Count==1,"sessões locais ambíguas não são misturadas");
        Check(GtcHistoryMerge.TireSummary(remote.Laps[0]).Contains("DE / DD / TE / TD"),"apresentar valores de pneus disponíveis");
        Check(GtcHistoryMerge.TireSummary(new DriverLapPoint{TireCompound="Dry",TireFL=0,TireFR=0,TireRL=0,TireRR=0}).Contains("indisponível"),"valores de pneus nulos não viram percentagem inventada");
        var run=new TrainingRun{Id="s",Driver=local.Driver,Track=local.Track,CreatedUtc=now.AddMinutes(-3),FinishedUtc=now,State="completed",Options=new TrainingOptions(),Laps=new System.Collections.Generic.List<TrainingLap>{new TrainingLap{Number=1,SessionLap=1,CompletedUtc=now.AddMinutes(-2).AddSeconds(1),Time=90,Valid=true,CompleteData=true},new TrainingLap{Number=2,SessionLap=2,CompletedUtc=now.AddMinutes(-1),Time=91,Valid=false,CompleteData=true}}};
        var view=TrainingHistoryView.Merge(run,new[]{remote},110,now);Check(view.GtcMatched&&view.Laps[0].Time==90.01&&view.Laps[0].DataSource=="gtc"&&view.Laps[1].DataSource=="local"&&run.Laps[0].Time==90,"gráfico próprio usa GTC e preserva original");
        var measured=run.Laps[0];measured.TireSource="csv";measured.TireFL=.9;measured.TireFR=.8;measured.TireRL=.7;measured.TireRR=.6;
        var csvView=GtcHistoryMerge.Sessions(new[]{remote,local}).Single();GtcHistoryMerge.AttachCsvTires(new[]{csvView},new[]{run});Check(Math.Abs(GtcHistoryMerge.CsvAverageWear(csvView.Laps[0]).Value-25)<.0001,"desgaste dos quatro pneus cruza CSV com a volta certa");Check(GtcHistoryMerge.CsvAverageWear(remote.Laps[0])==null,"não alterar fonte GTC nem assumir a sua escala");
        GtcHistoryMerge.AttachCsvTires(new[]{csvView},new[]{run,run});Check(GtcHistoryMerge.CsvAverageWear(csvView.Laps[1])==null,"não inventar desgaste para voltas sem dados");
        var wrongRecord=GtcHistoryMerge.Sessions(new[]{remote}).Single();wrongRecord.Driver="Other";GtcHistoryMerge.AttachCsvTires(new[]{wrongRecord},new[]{run});Check(GtcHistoryMerge.CsvAverageWear(wrongRecord.Laps[0])==null,"não atribuir pneus de outro piloto");
        var unmatched=TrainingHistoryView.Merge(run,new[]{remote},111,now);Check(!unmatched.GtcMatched&&unmatched.Laps.All(l=>l.DataSource=="local"),"não cruzar campeonatos nem afirmar correspondência");
    }
}
