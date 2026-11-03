// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Linq;
public static class TelemetryComparisonTests {
    static void Check(bool condition,string message){if(!condition)throw new Exception("Comparação de telemetria: "+message);}
    public static void Run(){
        string dir=Path.Combine(Path.GetTempPath(),"gtc-comparison-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        try{string file=Path.Combine(dir,"lap.csv");var data=new StringBuilder("piloto;pista;carro;tempo_volta_s;posicao_x_m;posicao_z_m;acelerador_bruto;travao_bruto;direcao_bruta\n");
            for(int i=0;i<=200;i++){double angle=2*Math.PI*i/200;data.AppendFormat(CultureInfo.InvariantCulture,"\"Piloto; Um\";Estoril;Porsche;{0};{1};{2};0.8;0.2;-0.1\n",i*.5,Math.Cos(angle)*200,Math.Sin(angle)*200);}File.WriteAllText(file,data.ToString());
            var lap=TelemetryLap.Load(file);Check(lap.Complete&&lap.Driver=="Piloto; Um","leitura e deteção de volta completa");Check(Math.Abs(lap.At(.5).Time-50)<.1,"interpolação por distância");Check(TelemetryLap.Problem(lap,lap)==null,"mesma pista");
            lap.Track="Outra";var same=TelemetryLap.Load(file);Check(TelemetryLap.Problem(lap,same)!=null,"não comparar pistas diferentes");File.WriteAllLines(file,File.ReadAllLines(file).Take(100).ToArray());var partial=TelemetryLap.Load(file);Check(!partial.Complete&&TelemetryLap.Problem(partial,same)!=null,"não apresentar parciais como completas");
        }finally{Directory.Delete(dir,true);}
    }
}
