// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
public static class LapTelemetryTests {
    static void Require(bool ok,string why){if(!ok)throw new Exception("Telemetria: "+why);}
    public static void Run(){
        var b=new byte[1888];Action<int,double> put=(p,v)=>Array.Copy(BitConverter.GetBytes(v),0,b,p,8);Array.Copy(BitConverter.GetBytes(17),0,b,0,4);put(12,21);put(24,20);put(160,2);put(168,3);put(176,4);put(388,.9);put(396,.4);put(404,-.3);put(420,.8);put(428,.35);put(436,-.25);put(1000,.9);put(1260,.8);put(1520,.7);put(1780,.6);
        var f=LapTelemetryFrame.Decode(b,0);Require(f.Slot==17&&f.X==2&&f.Brake==.4&&f.FilteredBrake==.35&&f.Steering==-.3&&f.WearFL==.9&&f.WearRR==.6,"campos da memória partilhada");
        var player=new LiveSample{Slot=1};var other=new LiveSample{Slot=17,Driver="Piloto observado",Realtime=false};var activity=new DriverActivitySnapshot{UpdatedUtc=DateTime.UtcNow};activity.Drivers.Add(new DriverActivityEntry{Lap=other,State=DriverPresence.OnTrack});
        Require(ViewedCarReader.Select(17,player,activity,DateTime.UtcNow)==other,"selecionar piloto da câmara");Require(ViewedCarReader.Select(1,player,activity,DateTime.UtcNow)==null,"não duplicar jogador");Require(ViewedCarReader.Select(17,player,activity,DateTime.UtcNow.AddSeconds(2))==null,"rejeitar classificação desatualizada");
        activity.Drivers.Add(new DriverActivityEntry{Lap=other,State=DriverPresence.OnTrack});Require(ViewedCarReader.Select(17,player,activity,DateTime.UtcNow)==null,"rejeitar identificação ambígua");
        string dir=Path.Combine(Path.GetTempPath(),"gtc-lap-test-"+Guid.NewGuid().ToString("N"));
        try{
            var s=new LiveSample{Driver="Joao",Track="Pista",Car="Carro",Slot=17,Realtime=true,Laps=2,LapStart=20,SessionTime=21};
            using(var w=new LapTelemetryWriter(dir)){
                w.Observe(s,f);var captured=new TrainingLap{SessionLap=3,Time=1,CompletedUtc=DateTime.UtcNow};var recovery=new TrainingRun{Driver=s.Driver,Track=s.Track,Car=s.Car,Laps=new System.Collections.Generic.List<TrainingLap>{captured}};w.CaptureWear(recovery,captured);Require(captured.TireSource=="csv"&&captured.TireFL==.9&&captured.TireRR==.6,"guardar e ler os quatro pneus do CSV da volta");var wrong=new TrainingLap{SessionLap=4,Time=1,CompletedUtc=DateTime.UtcNow};recovery.Laps.Add(wrong);w.CaptureWear(recovery,wrong);Require(wrong.TireSource==null,"não associar pneus à volta errada");string first=w.CurrentFile;Require(first.Contains("parcial"),"volta iniciada a meio marcada como parcial");w.Observe(null,null);f.Time+=.1;w.Observe(s,f);w.Observe(s,f);w.Close();Require(File.ReadAllLines(first).Length==3,"falha transitória preserva ficheiro e amostra repetida é ignorada");
                s.SessionTime=30;s.Laps=3;s.LapStart=30;f.Time=30.1;f.LapStart=30;w.Observe(s,f);string second=w.CurrentFile;w.Close();Require(first!=second&&File.Exists(second),"nova volta em ficheiro separado");
                s.Slot=18;w.Observe(s,f);Require(Directory.GetFiles(dir).Length==2,"não gravar o carro errado");
            }
            string observed=Path.Combine(dir,"observados");s.Slot=17;s.Realtime=false;s.InPits=false;using(var w=new LapTelemetryWriter(observed,false)){w.Observe(s,f);Require(File.Exists(w.CurrentFile),"gravar piloto observado mesmo com jogador em spectate");s.InPits=true;w.Observe(s,f);}
        }finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
}
