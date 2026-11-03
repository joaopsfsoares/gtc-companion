// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

public sealed class TelemetryPoint {
    public double Time,X,Z,Throttle,Brake,Steering,Distance;
}
public sealed class TelemetryLap {
    public string Driver,Track,Car,Path; public bool Complete,HasGaps; public string Issue;
    public readonly List<TelemetryPoint> Points=new List<TelemetryPoint>();
    static string[] Parse(string line){var result=new List<string>();var b=new StringBuilder();bool quoted=false;for(int i=0;i<line.Length;i++){char c=line[i];if(c=='"'){if(quoted&&i+1<line.Length&&line[i+1]=='"'){b.Append('"');i++;}else quoted=!quoted;}else if(c==';'&&!quoted){result.Add(b.ToString());b.Clear();}else b.Append(c);}if(quoted)throw new InvalidDataException("Linha CSV incompleta.");result.Add(b.ToString());return result.ToArray();}
    public static TelemetryLap Load(string path){
        if(new FileInfo(path).Length>25*1024*1024)throw new InvalidDataException("Ficheiro demasiado grande.");
        var result=new TelemetryLap{Path=path};using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var reader=new StreamReader(stream,Encoding.UTF8,true)){
            string first=reader.ReadLine();if(first==null)throw new InvalidDataException("Ficheiro vazio.");string[] h=Parse(first);var ix=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);for(int i=0;i<h.Length;i++)ix[h[i]]=i;
            string[] required={"piloto","pista","carro","tempo_volta_s","posicao_x_m","posicao_z_m","acelerador_bruto","travao_bruto","direcao_bruta"};foreach(string k in required)if(!ix.ContainsKey(k))throw new InvalidDataException("Este CSV não contém os campos gravados pelo Companion: "+k+".");
            string line;while((line=reader.ReadLine())!=null){if(line.Length==0)continue;var row=Parse(line);if(row.Length<h.Length)continue;Func<string,string> s=k=>row[ix[k]];Func<string,double> n=k=>double.Parse(s(k),CultureInfo.InvariantCulture);
                if(result.Points.Count==0){result.Driver=s("piloto");result.Track=s("pista");result.Car=s("carro");}
                if(s("piloto")!=result.Driver||s("pista")!=result.Track||s("carro")!=result.Car)throw new InvalidDataException("O ficheiro contém dados de pilotos, carros ou pistas diferentes.");
                var p=new TelemetryPoint{Time=n("tempo_volta_s"),X=n("posicao_x_m"),Z=n("posicao_z_m"),Throttle=n("acelerador_bruto"),Brake=n("travao_bruto"),Steering=n("direcao_bruta")};
                if(new[]{p.Time,p.X,p.Z,p.Throttle,p.Brake,p.Steering}.Any(x=>double.IsNaN(x)||double.IsInfinity(x)))throw new InvalidDataException("Amostra inválida.");
                if(result.Points.Count>0){var prev=result.Points[result.Points.Count-1];if(p.Time<=prev.Time)continue;p.Distance=prev.Distance+Math.Sqrt((p.X-prev.X)*(p.X-prev.X)+(p.Z-prev.Z)*(p.Z-prev.Z));}result.Points.Add(p);if(result.Points.Count>100000)throw new InvalidDataException("Demasiadas amostras.");
            }
        }
        if(result.Points.Count<2)throw new InvalidDataException("São necessárias pelo menos duas amostras.");
        var a=result.Points[0];var z=result.Points[result.Points.Count-1];double closure=Math.Sqrt((a.X-z.X)*(a.X-z.X)+(a.Z-z.Z)*(a.Z-z.Z));var gaps=result.Points.Zip(result.Points.Skip(1),(x,y)=>y.Time-x.Time).ToArray();bool gap=gaps.Any(x=>x>3);result.HasGaps=gaps.Any(x=>x>.12);
        result.Complete=a.Time<=.5&&z.Time>=15&&closure<=35&&!gap&&z.Distance>300;result.Issue=result.Complete?null:"Volta parcial ou incompleta: a comparação por distância não está disponível.";return result;
    }
    public TelemetryPoint At(double fraction){
        var last=Points[Points.Count-1];double target=Math.Max(0,Math.Min(1,fraction))*last.Distance;int lo=0,hi=Points.Count-1;while(lo+1<hi){int mid=(lo+hi)/2;if(Points[mid].Distance<target)lo=mid;else hi=mid;}var a=Points[lo];var b=Points[hi];double d=b.Distance-a.Distance,t=d<=0?0:(target-a.Distance)/d;Func<double,double,double> mix=(x,y)=>x+(y-x)*t;
        return new TelemetryPoint{Time=mix(a.Time,b.Time),X=mix(a.X,b.X),Z=mix(a.Z,b.Z),Throttle=mix(a.Throttle,b.Throttle),Brake=mix(a.Brake,b.Brake),Steering=mix(a.Steering,b.Steering),Distance=target};
    }
    public static string Problem(TelemetryLap a,TelemetryLap b){if(a==null||b==null)return "Escolhe duas voltas gravadas pelo Companion.";if(!string.Equals(a.Track,b.Track,StringComparison.OrdinalIgnoreCase))return "As voltas são de pistas diferentes.";if(!a.Complete||!b.Complete)return "Uma das voltas é parcial ou incompleta. Escolhe duas voltas completas.";return null;}
}
