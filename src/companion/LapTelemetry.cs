// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Linq;

// Offsets and packing match pyRfactor2SharedMemory rF2VehicleTelemetry (pack 4).
public sealed class LapTelemetryFrame {
    public int Slot,Gear; public double Time,LapStart,X,Y,Z,Throttle,Brake,Steering,FilteredThrottle,FilteredBrake,FilteredSteering,PressureFL,PressureFR,PressureRL,PressureRR,WearFL,WearFR,WearRL,WearRR;
    public static LapTelemetryFrame Decode(byte[] b,int p){
        Func<int,double> d=o=>BitConverter.ToDouble(b,p+o);
        return new LapTelemetryFrame{Slot=BitConverter.ToInt32(b,p),Time=d(12),LapStart=d(24),X=d(160),Y=d(168),Z=d(176),Gear=BitConverter.ToInt32(b,p+352),Throttle=d(388),Brake=d(396),Steering=d(404),FilteredThrottle=d(420),FilteredBrake=d(428),FilteredSteering=d(436),PressureFL=d(880),PressureFR=d(1140),PressureRL=d(1400),PressureRR=d(1660),WearFL=d(1000),WearFR=d(1260),WearRL=d(1520),WearRR=d(1780)};
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
    readonly string root; readonly bool requireRealtime; StreamWriter writer; string identity; public string PreviousFile{get;private set;} double start,last=-1; DateTime flush=DateTime.MinValue,missingSince=DateTime.MinValue; public string CurrentFile{get;private set;}
    public LapTelemetryWriter(string path,bool requirePlayerRealtime=true){root=path;requireRealtime=requirePlayerRealtime;}
    static string Number(double n){return n.ToString("R",CultureInfo.InvariantCulture);}
    static bool Good(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
    static string Text(string v){return "\""+(v??"").Replace("\"","\"\"")+"\"";}
    public void Observe(LiveSample s,LapTelemetryFrame f){
        if(s==null||f==null||(requireRealtime&&!s.Realtime)||s.InPits||s.Slot!=f.Slot||!Good(f.Time)||!Good(f.LapStart)||!Good(f.X)||!Good(f.Y)||!Good(f.Z)||!Good(f.Throttle)||!Good(f.Brake)||!Good(f.Steering)||!Good(f.FilteredThrottle)||!Good(f.FilteredBrake)||!Good(f.FilteredSteering)||f.Time<f.LapStart||Math.Abs(s.SessionTime-f.Time)>3){if(missingSince==DateTime.MinValue)missingSince=DateTime.UtcNow;if((DateTime.UtcNow-missingSince).TotalSeconds>2)Close();return;}
        missingSince=DateTime.MinValue;
        string key=s.Track+"|"+s.Car+"|"+s.Driver+"|"+s.Session+"|"+s.Slot;
        if(writer==null||identity!=key||Math.Abs(start-f.LapStart)>.01||f.Time<last){
            if(writer!=null)PreviousFile=CurrentFile;Close();Directory.CreateDirectory(root);identity=key;start=f.LapStart;last=-1;
            int lap=s.Laps+1+(f.LapStart>s.LapStart+1?1:0);string partial=f.Time-f.LapStart>.5?"_parcial":"";
            string driver=new string((s.Driver??"Piloto").Where(c=>!char.IsControl(c)&&Array.IndexOf(Path.GetInvalidFileNameChars(),c)<0).Take(50).ToArray()).Trim().TrimEnd('.');if(driver.Length==0)driver="Piloto";
            CurrentFile=Path.Combine(root,DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff")+"_"+driver+"_volta_"+lap+partial+"_"+Guid.NewGuid().ToString("N").Substring(0,8)+".csv");
            writer=new StreamWriter(new FileStream(CurrentFile,FileMode.CreateNew,FileAccess.Write,FileShare.Read),new UTF8Encoding(true));
            writer.WriteLine("pista;carro;piloto;volta_sessao;tempo_sessao_s;tempo_volta_s;posicao_x_m;posicao_y_m;posicao_z_m;acelerador_bruto;travao_bruto;direcao_bruta;acelerador_filtrado;travao_filtrado;direcao_filtrada;pressao_travao_fe;pressao_travao_fd;pressao_travao_te;pressao_travao_td;marcha;pneu_fe_restante;pneu_fd_restante;pneu_te_restante;pneu_td_restante");
        }
        if(f.Time<=last)return;last=f.Time;
        int number=s.Laps+1+(f.LapStart>s.LapStart+1?1:0);
        writer.WriteLine(string.Join(";",new[]{Text(s.Track),Text(s.Car),Text(s.Driver),number.ToString(CultureInfo.InvariantCulture),Number(f.Time),Number(f.Time-f.LapStart),Number(f.X),Number(f.Y),Number(f.Z),Number(f.Throttle),Number(f.Brake),Number(f.Steering),Number(f.FilteredThrottle),Number(f.FilteredBrake),Number(f.FilteredSteering),Number(f.PressureFL),Number(f.PressureFR),Number(f.PressureRL),Number(f.PressureRR),f.Gear.ToString(CultureInfo.InvariantCulture),Number(f.WearFL),Number(f.WearFR),Number(f.WearRL),Number(f.WearRR)}));
        if((DateTime.UtcNow-flush).TotalSeconds>=1){writer.Flush();flush=DateTime.UtcNow;}
    }
    static bool Same(string a,string b){return !string.IsNullOrWhiteSpace(a)&&!string.IsNullOrWhiteSpace(b)&&TimingData.Normalize(a)==TimingData.Normalize(b);}
    static bool ReadWear(string path,TrainingRun run,TrainingLap lap){
        try{
            using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var reader=new StreamReader(stream)){
                var header=TelemetryLap.Parse(reader.ReadLine()??"");var ix=header.Select((name,i)=>new{ name,i }).ToDictionary(x=>x.name,x=>x.i,StringComparer.OrdinalIgnoreCase);
                string[] required={"pista","carro","piloto","volta_sessao","tempo_volta_s","pneu_fe_restante","pneu_fd_restante","pneu_te_restante","pneu_td_restante"};if(required.Any(k=>!ix.ContainsKey(k)))return false;
                string first=reader.ReadLine(),last=first,line;while((line=reader.ReadLine())!=null)if(line.Length>0)last=line;if(first==null||last==null)return false;
                var row=TelemetryLap.Parse(last);if(row.Length<header.Length)return false;
                int number;double elapsed;if(!int.TryParse(row[ix["volta_sessao"]],out number)||!lap.SessionLap.HasValue||number!=lap.SessionLap.Value||!double.TryParse(row[ix["tempo_volta_s"]],NumberStyles.Float,CultureInfo.InvariantCulture,out elapsed)||Math.Abs(elapsed-lap.Time)>3)return false;
                if(!Same(run.Track,row[ix["pista"]])||!Same(run.Driver,row[ix["piloto"]])||!Same(run.Car,row[ix["carro"]]))return false;
                string[] names={"pneu_fe_restante","pneu_fd_restante","pneu_te_restante","pneu_td_restante"};double[] v=new double[4];for(int i=0;i<4;i++)if(!double.TryParse(row[ix[names[i]]],NumberStyles.Float,CultureInfo.InvariantCulture,out v[i])||v[i]<0||v[i]>1||double.IsNaN(v[i]))return false;
                if(v.All(x=>x==0))return false;lap.TireFL=v[0];lap.TireFR=v[1];lap.TireRL=v[2];lap.TireRR=v[3];lap.TireSource="csv";return true;
            }
        }catch(IOException){return false;}catch(UnauthorizedAccessException){return false;}catch(InvalidDataException){return false;}catch(ArgumentException){return false;}
    }
    public static int RecoverWear(TrainingRun run,string directory){
        if(run==null||run.Laps==null||!Directory.Exists(directory))return 0;
        var missing=run.Laps.Where(l=>l.TireSource!="csv"&&l.CompletedUtc.HasValue&&l.SessionLap.HasValue).ToList();if(missing.Count==0)return 0;
        var files=new DirectoryInfo(directory).GetFiles("*.csv").Where(f=>missing.Any(l=>Math.Abs((f.LastWriteTimeUtc-l.CompletedUtc.Value.ToUniversalTime()).TotalSeconds)<20)).ToList();int recovered=0;
        foreach(var lap in missing){var candidates=files.Where(f=>Math.Abs((f.LastWriteTimeUtc-lap.CompletedUtc.Value.ToUniversalTime()).TotalSeconds)<20).ToList();var matches=new System.Collections.Generic.List<TrainingLap>();foreach(var f in candidates){var copy=lap.Copy();if(ReadWear(f.FullName,run,copy))matches.Add(copy);}if(matches.Count==1){var m=matches[0];lap.TireFL=m.TireFL;lap.TireFR=m.TireFR;lap.TireRL=m.TireRL;lap.TireRR=m.TireRR;lap.TireSource="csv";recovered++;}}
        return recovered;
    }
    public void CaptureWear(TrainingRun run,TrainingLap lap){
        if(run==null||lap==null)return;try{if(writer!=null)writer.Flush();}catch(IOException){}
        RecoverWear(run,root);
    }
    public void Close(){if(writer!=null){writer.Dispose();writer=null;}identity=null;last=-1;}
    public void Dispose(){Close();}
}

// Graphics is unsubscribed by default; request only its read-only output channel.
public sealed class ViewedCarReader : IDisposable {
    MemoryMappedFile graphics,extended;MemoryMappedViewAccessor gv,ev;DateTime nextRequest=DateTime.MinValue;
    public int? Read(){
        try{
            if(extended==null){extended=MemoryMappedFile.OpenExisting("$rFactor2SMMP_Extended$",MemoryMappedFileRights.Read);ev=extended.CreateViewAccessor(0,10152,MemoryMappedFileAccess.Read);}
            uint v=ev.ReadUInt32(0);if(v!=ev.ReadUInt32(4))return null;int mask=ev.ReadInt32(10144);if(v!=ev.ReadUInt32(0)||v!=ev.ReadUInt32(4))return null;
            if((mask&32)!=0){if(DateTime.UtcNow>=nextRequest){nextRequest=DateTime.UtcNow.AddSeconds(5);RequestGraphics();}return null;}
            if(graphics==null){graphics=MemoryMappedFile.OpenExisting("$rFactor2SMMP_Graphics$",MemoryMappedFileRights.Read);gv=graphics.CreateViewAccessor(0,272,MemoryMappedFileAccess.Read);}
            // The plugin writes Graphics without updating its version counters.
            int first=gv.ReadInt32(136),second=gv.ReadInt32(136);return first>=0&&first==second?(int?)first:null;
        }catch{Dispose();return null;}
    }
    static void RequestGraphics(){
        try{using(var map=MemoryMappedFile.OpenExisting("$rFactor2SMMP_PluginControl$",MemoryMappedFileRights.ReadWrite))using(var view=map.CreateViewAccessor(0,20,MemoryMappedFileAccess.ReadWrite)){
            uint begin=view.ReadUInt32(0),end=view.ReadUInt32(4);if(begin!=end)return;int layout=view.ReadInt32(8),mask=view.ReadInt32(12);byte hw=view.ReadByte(16),weather=view.ReadByte(17),rules=view.ReadByte(18);
            if(layout!=0&&layout!=1)return;if(view.ReadUInt32(0)!=begin||view.ReadUInt32(4)!=end)return;
            uint next=unchecked(begin+1);view.Write(0,next);view.Write(8,1);view.Write(12,mask|32);view.Write(16,hw);view.Write(17,weather);view.Write(18,rules);view.Write(4,next);
        }}catch{}
    }
    public static LiveSample Select(int? slot,LiveSample player,DriverActivitySnapshot activity,DateTime now){
        if(!slot.HasValue||player==null||activity==null||now-activity.UpdatedUtc>TimeSpan.FromSeconds(1)||slot.Value==player.Slot)return null;
        var matches=activity.Drivers.Where(x=>x.Lap!=null&&x.Lap.Slot==slot.Value&&!x.IsPlayer&&x.State==DriverPresence.OnTrack).ToList();return matches.Count==1?matches[0].Lap:null;
    }
    public void Dispose(){if(gv!=null){gv.Dispose();gv=null;}if(graphics!=null){graphics.Dispose();graphics=null;}if(ev!=null){ev.Dispose();ev=null;}if(extended!=null){extended.Dispose();extended=null;}}
}
