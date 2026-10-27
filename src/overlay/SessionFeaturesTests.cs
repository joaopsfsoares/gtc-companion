// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Text;
public static class SessionFeaturesTests {
    public static void Run(){
        var data=new byte[75312];Array.Copy(Encoding.UTF8.GetBytes("Pista"),0,data,12,5);data[127]=1;
        for(int i=0;i<2;i++){int p=560+i*584;string name=i==0?"Alpha":"Beta";Array.Copy(Encoding.UTF8.GetBytes(name),0,data,p+4,name.Length);data[p+197]=3;Array.Copy(BitConverter.GetBytes(100d),0,data,p+104,8);}data[560+584+198]=1;
        var now=DateTime.UtcNow;var activity=DriverActivitySnapshot.Read(data,2,now);
        if(activity.Find(new TimingRow{driverName="Alpha"},"Pista",now)!=DriverPresence.OnTrack||activity.Find(new TimingRow{driverName="Beta"},"Pista",now)!=DriverPresence.InPits)throw new Exception("Distinguir pista e boxes pela telemetria local.");
        if(activity.Find(new TimingRow{driverName="Alpha"},"Outra pista",now)!=DriverPresence.Unknown||activity.Find(new TimingRow{driverName="Alpha"},"Pista",now.AddSeconds(4))!=DriverPresence.Unknown||activity.Find(new TimingRow{driverName="Ausente"},"Pista",now)!=DriverPresence.Unknown)throw new Exception("Não inferir presença a partir de dados ausentes, antigos ou de outra pista.");
        activity.Drivers.Add(new DriverActivityEntry{Name="Alpha",State=DriverPresence.InPits});if(activity.Find(new TimingRow{driverName="Alpha"},"Pista",now)!=DriverPresence.Unknown)throw new Exception("Nomes ambíguos não podem ter um estado inventado.");
        var model=new LiveLapModel();var sample=new LiveSample{Track="Pista",Driver="Soares",Car="Cup",CarClass="Cup",Session=1,Slot=1,Laps=2,SessionTime=110,LapStart=100,Sector=1,CountLapFlag=2,Distance=100,Realtime=true,LastLap=90,LastS1=20,LastS12=50,BestLap=90,BestS1=20,BestS12=50};
        model.Update(sample,null,"Pista","Soares",false);sample.Laps=3;sample.SessionTime=190.5;sample.LapStart=190.5;sample.LastLap=90.5;sample.LastS12=50.5;
        var current=model.Update(sample,null,"Pista","Soares",false);if(current.Finished||model.LastCompleted==null||model.LastCompleted.LapNumber!=3||model.LastCompleted.Elapsed!=90.5)throw new Exception("A volta anterior deve ter uma linha independente da volta atual.");
        sample.SessionTime+=10;model.Update(sample,null,"Pista","Soares",false);if(model.LastCompleted.Elapsed!=90.5)throw new Exception("A volta anterior deve permanecer depois da indicação temporária de fim de volta.");
        sample.CountLapFlag=1;model.Update(sample,null,"Pista","Soares",false);sample.Laps=4;sample.LapStart=280;sample.SessionTime=280;sample.LastLap=89;sample.CountLapFlag=2;model.Update(sample,null,"Pista","Soares",false);
        if(model.LastCompleted.Valid||model.LastCompleted.LapRecord||model.LastCompleted.State!="Inválida"||model.Book.Entries[0].Time!=90)throw new Exception("Uma volta inválida não pode tornar-se recorde pessoal.");
        var comparison=new LiveSample{Track="Compare",Driver="Soares",Car="Cup",CarClass="Cup",Session=1,Slot=1,Laps=2,SessionTime=110,LapStart=100,Sector=1,CountLapFlag=2,Distance=100,Realtime=true,LastLap=95,LastS1=22,LastS12=54,BestLap=91,BestS1=21,BestS12=52};
        var best=new TimingRow{driverName="Soares",bestLapTime=90,s1=20,s2=30,s3=40};var compareModel=new LiveLapModel();compareModel.Update(comparison,best,"Compare","Soares",false);comparison.Sector=2;comparison.CurrentS1=20.5;comparison.SessionTime=120.5;
        var recordDelta=compareModel.Update(comparison,best,"Compare","Soares",false);if(Math.Abs(recordDelta.Gap.Value-.5)>.0001)throw new Exception("O diferencial predefinido deve comparar com o recorde, não com a volta anterior.");
        var previousDelta=compareModel.Update(comparison,best,"Compare","Soares",false,true);if(Math.Abs(previousDelta.Gap.Value+1.5)>.0001)throw new Exception("A opção de comparação anterior deve manter a diferença para a última volta.");
        var book=new PersonalRecordBook();var saved=book.Get(comparison);saved.Merge(new LapReference{Time=85,S1=18,S2=33,S3=34});saved.Merge(new LapReference{Time=86,S1=17,S2=34,S3=35});var historic=new LiveLapModel(book);var historicDelta=historic.Update(comparison,best,"Compare","Soares",false);if(historicDelta.Reference.Time!=85||Math.Abs(historicDelta.Gap.Value-2.5)>.0001)throw new Exception("Usar os setores da volta recorde, sem somar recordes individuais de voltas diferentes.");
        var ideal=LiveLapModel.BuildIdeal(saved);if(!ideal.Elapsed.HasValue||Math.Abs(ideal.Elapsed.Value-81)>.0001||ideal.Sectors[0]!=17||ideal.Sectors[1]!=30||ideal.Sectors[2]!=34)throw new Exception("A volta ideal deve somar os melhores setores individuais de voltas diferentes.");
        var partial=LiveLapModel.BuildIdeal(new PersonalRecord{S1=20,S2=30});if(partial.Elapsed.HasValue||partial.Sectors[2].HasValue)throw new Exception("Não inventar o tempo ideal quando faltar um setor.");
        if(historic.Ideal==null||historic.Ideal.Elapsed!=ideal.Elapsed)throw new Exception("A volta ideal deve estar disponível no modelo ativo.");
        historic.Update(null,best,"Compare","Soares",false);if(historic.Ideal!=null)throw new Exception("Não apresentar setores de outro piloto sem telemetria atual.");
        var driverBook=new PersonalRecordBook();var ideals=new DriverIdeals(driverBook);
        var remote=new RemoteLapSnapshot{sessionInfo=new RemoteLapSession{trackName="Remote"},entries=new System.Collections.Generic.List<RemoteLapDriver>{new RemoteLapDriver{driverName="Alpha",vehicleClass="Cup",lapHistory=new System.Collections.Generic.List<RemoteLapEntry>{
            new RemoteLapEntry{lapTime=90,s1=20,s2=30,s3=40,invalid=false,pit=false},new RemoteLapEntry{lapTime=91,s1=19,s2=33,s3=39,invalid=false,pit=false},
            new RemoteLapEntry{lapTime=70,s1=10,s2=20,s3=40,invalid=true,pit=false},new RemoteLapEntry{lapTime=71,s1=11,s2=20,s3=40,invalid=false,pit=true},new RemoteLapEntry{lapTime=72,s1=12,s2=20,s3=40,pit=false},
            new RemoteLapEntry{lapTime=20,s1=1,s2=1,s3=1,invalid=false,pit=false}}}}};
        ideals.ObserveRemote(remote,"Remote");var driverRow=new TimingRow{driverName="Alpha",vehicleClass="Cup"};var driverIdeal=ideals.Find(driverRow,"Remote");if(driverIdeal==null||driverIdeal.Elapsed!=88)throw new Exception("A ideal remota deve juntar setores de voltas válidas, excluindo boxes, invalidade, flags ausentes e somas inconsistentes.");
        if(ideals.Find(new TimingRow{driverName="Beta",vehicleClass="Cup"},"Remote")!=null||ideals.Find(driverRow,"Outra pista")!=null)throw new Exception("Não atribuir uma ideal a outro piloto ou pista.");
        var revision=driverBook.Revision;ideals.ObserveRemote(remote,"Outra pista");ideals.ObserveRemote(remote,"Remote");if(driverBook.Revision!=revision)throw new Exception("Ignorar outra pista e não regravar históricos repetidos.");
        var preferences=new Preferences();if(preferences.ShowIdealLap)throw new Exception("A volta ideal deve ser opcional.");new OverlayPatch{ShowIdealLap=true,ShowDriverIdealLaps=true}.Apply(preferences);if(!preferences.ShowIdealLap||!preferences.ShowDriverIdealLaps)throw new Exception("Ativar a volta ideal nas definições.");new OverlayPatch{ShowPreviousLap=false,ShowDriverActivity=false}.Apply(preferences);if(preferences.ShowPreviousLap||preferences.ShowDriverActivity)throw new Exception("As duas opções de apresentação devem poder ser desligadas.");
    }
}
