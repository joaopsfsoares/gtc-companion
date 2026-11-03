// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
public static class LapTelemetryTests {
    static void Require(bool ok,string why){if(!ok)throw new Exception("Telemetria: "+why);}
    public static void Run(){
        var b=new byte[1888];Action<int,double> put=(p,v)=>Array.Copy(BitConverter.GetBytes(v),0,b,p,8);Array.Copy(BitConverter.GetBytes(17),0,b,0,4);put(12,21);put(24,20);put(160,2);put(168,3);put(176,4);put(388,.9);put(396,.4);put(404,-.3);put(420,.8);put(428,.35);put(436,-.25);
        var f=LapTelemetryFrame.Decode(b,0);Require(f.Slot==17&&f.X==2&&f.Brake==.4&&f.FilteredBrake==.35&&f.Steering==-.3,"campos da memória partilhada");
        string dir=Path.Combine(Path.GetTempPath(),"gtc-lap-test-"+Guid.NewGuid().ToString("N"));
        try{
            var s=new LiveSample{Driver="Joao",Track="Pista",Car="Carro",Slot=17,Realtime=true,Laps=2,LapStart=20,SessionTime=21};
            using(var w=new LapTelemetryWriter(dir)){
                w.Observe(s,f);string first=w.CurrentFile;Require(first.Contains("parcial"),"volta iniciada a meio marcada como parcial");w.Observe(s,f);w.Close();Require(File.ReadAllLines(first).Length==2,"amostra repetida ignorada");
                s.SessionTime=30;s.Laps=3;s.LapStart=30;f.Time=30.1;f.LapStart=30;w.Observe(s,f);string second=w.CurrentFile;w.Close();Require(first!=second&&File.Exists(second),"nova volta em ficheiro separado");
                s.Slot=18;w.Observe(s,f);Require(Directory.GetFiles(dir).Length==2,"não gravar o carro errado");
            }
        }finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
}
