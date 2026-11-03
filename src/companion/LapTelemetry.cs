// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

// Offsets and packing match pyRfactor2SharedMemory rF2VehicleTelemetry (pack 4).
public sealed class LapTelemetryFrame {
    public int Slot,Gear; public double Time,LapStart,X,Y,Z,Throttle,Brake,Steering,FilteredThrottle,FilteredBrake,FilteredSteering,PressureFL,PressureFR,PressureRL,PressureRR;
    public static LapTelemetryFrame Decode(byte[] b,int p){
        Func<int,double> d=o=>BitConverter.ToDouble(b,p+o);
        return new LapTelemetryFrame{Slot=BitConverter.ToInt32(b,p),Time=d(12),LapStart=d(24),X=d(160),Y=d(168),Z=d(176),Gear=BitConverter.ToInt32(b,p+352),Throttle=d(388),Brake=d(396),Steering=d(404),FilteredThrottle=d(420),FilteredBrake=d(428),FilteredSteering=d(436),PressureFL=d(880),PressureFR=d(1140),PressureRL=d(1400),PressureRR=d(1660)};
    }
}
public sealed class LapTelemetryReader : IDisposable {
    MemoryMappedFile map;MemoryMappedViewAccessor view;readonly byte[] bytes=new byte[241680];
    public LapTelemetryFrame Read(int slot){
        try{
            if(map==null){map=MemoryMappedFile.OpenExisting("$rFactor2SMMP_Telemetry$",MemoryMappedFileRights.Read);view=map.CreateViewAccessor(0,bytes.Length,MemoryMappedFileAccess.Read);}
            for(int n=0;n<3;n++){
                uint v=view.ReadUInt32(0);if(v!=view.ReadUInt32(4))continue;view.ReadArray(0,bytes,0,bytes.Length);
                if(BitConverter.ToUInt32(bytes,0)!=v||BitConverter.ToUInt32(bytes,4)!=v||view.ReadUInt32(0)!=v||view.ReadUInt32(4)!=v)continue;
                int count=BitConverter.ToInt32(bytes,12);if(count<0||count>128)throw new InvalidDataException("Mapa de telemetria incompatível");
                for(int i=0;i<count;i++){int p=16+i*1888;if(BitConverter.ToInt32(bytes,p)==slot)return LapTelemetryFrame.Decode(bytes,p);}return null;
            }return null;
        }catch{Dispose();return null;}
    }
    public void Dispose(){if(view!=null){view.Dispose();view=null;}if(map!=null){map.Dispose();map=null;}}
}
public sealed class LapTelemetryWriter : IDisposable {
    readonly string root; StreamWriter writer; string identity; double start,last=-1; DateTime flush=DateTime.MinValue; public string CurrentFile{get;private set;}
    public LapTelemetryWriter(string path){root=path;}
    static string Number(double n){return n.ToString("R",CultureInfo.InvariantCulture);}
    static bool Good(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
    static string Text(string v){return "\""+(v??"").Replace("\"","\"\"")+"\"";}
    public void Observe(LiveSample s,LapTelemetryFrame f){
        if(s==null||f==null||!s.Realtime||s.InPits||s.Slot!=f.Slot||!Good(f.Time)||!Good(f.LapStart)||!Good(f.X)||!Good(f.Y)||!Good(f.Z)||!Good(f.Throttle)||!Good(f.Brake)||!Good(f.Steering)||!Good(f.FilteredThrottle)||!Good(f.FilteredBrake)||!Good(f.FilteredSteering)||f.Time<f.LapStart||Math.Abs(s.SessionTime-f.Time)>3){Close();return;}
        string key=s.Track+"|"+s.Car+"|"+s.Driver+"|"+s.Session+"|"+s.Slot;
        if(writer==null||identity!=key||Math.Abs(start-f.LapStart)>.01||f.Time<last){
            Close();Directory.CreateDirectory(root);identity=key;start=f.LapStart;last=-1;
            int lap=s.Laps+1+(f.LapStart>s.LapStart+1?1:0);string partial=f.Time-f.LapStart>.5?"_parcial":"";
            CurrentFile=Path.Combine(root,DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff")+"_volta_"+lap+partial+"_"+Guid.NewGuid().ToString("N").Substring(0,8)+".csv");
            writer=new StreamWriter(new FileStream(CurrentFile,FileMode.CreateNew,FileAccess.Write,FileShare.Read),new UTF8Encoding(true));
            writer.WriteLine("pista;carro;piloto;volta_sessao;tempo_sessao_s;tempo_volta_s;posicao_x_m;posicao_y_m;posicao_z_m;acelerador_bruto;travao_bruto;direcao_bruta;acelerador_filtrado;travao_filtrado;direcao_filtrada;pressao_travao_fe;pressao_travao_fd;pressao_travao_te;pressao_travao_td;marcha");
        }
        if(f.Time<=last)return;last=f.Time;
        int number=s.Laps+1+(f.LapStart>s.LapStart+1?1:0);
        writer.WriteLine(string.Join(";",new[]{Text(s.Track),Text(s.Car),Text(s.Driver),number.ToString(CultureInfo.InvariantCulture),Number(f.Time),Number(f.Time-f.LapStart),Number(f.X),Number(f.Y),Number(f.Z),Number(f.Throttle),Number(f.Brake),Number(f.Steering),Number(f.FilteredThrottle),Number(f.FilteredBrake),Number(f.FilteredSteering),Number(f.PressureFL),Number(f.PressureFR),Number(f.PressureRL),Number(f.PressureRR),f.Gear.ToString(CultureInfo.InvariantCulture)}));
        if((DateTime.UtcNow-flush).TotalSeconds>=1){writer.Flush();flush=DateTime.UtcNow;}
    }
    public void Close(){if(writer!=null){writer.Dispose();writer=null;}identity=null;last=-1;}
    public void Dispose(){Close();}
}
