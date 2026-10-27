// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;

// Read-only layout of the installed rF2SharedMemoryMapPlugin 3.7 scoring map.
// Pack=4: version block 12, scoring info 548, vehicle scoring 584 bytes.
// Field definitions: TonyWhitley/pyRfactor2SharedMemory rF2data.py.
public sealed class LiveSample {
    public string Track,Driver,Car,CarClass;
    public int Session,Slot,Laps,Sector,CountLapFlag;
    public bool Realtime,InPits;
    public double SessionTime,LapStart,Distance,PathLateral,TrackEdge,CurrentS1,CurrentS12,LastS1,LastS12,LastLap,BestLap,BestS1,BestS12;
}
public sealed class Rf2ScoringReader : IDisposable {
    MemoryMappedFile mapping;
    MemoryMappedViewAccessor view;
    readonly byte[] bytes=new byte[75312];
    uint previousVersion;
    DateTime changedUtc=DateTime.MinValue;
    public string Error { get; private set; }
    public DriverActivitySnapshot Activity {get;private set;}
    public LiveSample Read() {
        try {
            if(mapping==null){mapping=MemoryMappedFile.OpenExisting("$rFactor2SMMP_Scoring$",MemoryMappedFileRights.Read);view=mapping.CreateViewAccessor(0,bytes.Length,MemoryMappedFileAccess.Read);}
            for(int attempt=0;attempt<3;attempt++) {
                uint version=view.ReadUInt32(0);
                if(version!=view.ReadUInt32(4))continue;
                view.ReadArray(0,bytes,0,bytes.Length);
                if(BitConverter.ToUInt32(bytes,0)!=version || BitConverter.ToUInt32(bytes,4)!=version || view.ReadUInt32(0)!=version || view.ReadUInt32(4)!=version)continue;
                if(version!=previousVersion || changedUtc==DateTime.MinValue){previousVersion=version;changedUtc=DateTime.UtcNow;}
                if((DateTime.UtcNow-changedUtc).TotalSeconds>3){Activity=null;Error="Telemetria sem atualização";return null;}
                int count=BitConverter.ToInt32(bytes,116);
                if(count<0||count>128)throw new InvalidOperationException("Layout de telemetria incompatível");
                Activity=DriverActivitySnapshot.Read(bytes,count,DateTime.UtcNow);
                for(int i=0;i<count;i++){
                    int p=560+i*584;
                    if(bytes[p+196]==0)continue;
                    var sample=new LiveSample {
                        Track=Text(12,64),Driver=Text(p+4,32),Car=Text(p+36,64),CarClass=Text(p+200,32),
                        Session=BitConverter.ToInt32(bytes,76),Slot=BitConverter.ToInt32(bytes,p),
                        SessionTime=BitConverter.ToDouble(bytes,80),Realtime=bytes[127]!=0,
                        Laps=BitConverter.ToInt16(bytes,p+100),Sector=bytes[p+102],CountLapFlag=bytes[p+506],InPits=bytes[p+198]!=0,
                        LapStart=BitConverter.ToDouble(bytes,p+256),Distance=BitConverter.ToDouble(bytes,p+104),
                        PathLateral=BitConverter.ToDouble(bytes,p+112),TrackEdge=BitConverter.ToDouble(bytes,p+120),
                        CurrentS1=BitConverter.ToDouble(bytes,p+176),CurrentS12=BitConverter.ToDouble(bytes,p+184),
                        LastS1=BitConverter.ToDouble(bytes,p+152),LastS12=BitConverter.ToDouble(bytes,p+160),LastLap=BitConverter.ToDouble(bytes,p+168),
                        BestLap=BitConverter.ToDouble(bytes,p+144),BestS1=BitConverter.ToSingle(bytes,p+528),BestS12=BitConverter.ToSingle(bytes,p+532)
                    };
                    Error="";return sample;
                }
                Error="Sem carro do jogador";return null;
            }
            Error="A aguardar telemetria consistente";return null;
        } catch(Exception){Error="Sem ligação à telemetria do rFactor";Dispose();return null;}
    }
    string Text(int offset,int length){int end=offset;while(end<offset+length&&bytes[end]!=0)end++;return Encoding.UTF8.GetString(bytes,offset,end-offset);}
    public void Dispose(){Activity=null;if(view!=null){view.Dispose();view=null;}if(mapping!=null){mapping.Dispose();mapping=null;}}
}
public sealed class LapReference {
    public double Time;
    public double? S1,S2,S3;
    public LapReference Copy(){return new LapReference{Time=Time,S1=S1,S2=S2,S3=S3};}
    public double? Sector(int index){return index==0?S1:index==1?S2:S3;}
    public static bool Positive(double value){return value>0&&!double.IsNaN(value)&&!double.IsInfinity(value);}
    public static LapReference FromGame(LiveSample s){
        if(!Positive(s.BestLap))return null;
        return new LapReference{Time=s.BestLap,S1=Positive(s.BestS1)?(double?)s.BestS1:null,
            S2=Positive(s.BestS1)&&s.BestS12>s.BestS1?(double?)(s.BestS12-s.BestS1):null,
            S3=Positive(s.BestS12)&&s.BestLap>s.BestS12?(double?)(s.BestLap-s.BestS12):null};
    }
    public static LapReference Choose(LiveSample sample,TimingRow online){
        var game=FromGame(sample);
        if(online==null||!Positive(online.bestLapTime))return game;
        // These must be sectors of the same lap, not personal sector records.
        var result=new LapReference{Time=online.bestLapTime,S1=online.s1,S2=online.s2,S3=online.s3};
        if(game!=null && game.Time<result.Time-0.002)return game;
        if(game!=null && Math.Abs(game.Time-result.Time)<=0.002){
            if(!result.S1.HasValue)result.S1=game.S1;
            if(!result.S2.HasValue)result.S2=game.S2;
            if(!result.S3.HasValue)result.S3=game.S3;
        }
        return result;
    }
    public static LapReference Choose(LiveSample sample,TimingRow online,PersonalRecord history){
        var current=Choose(sample,online);if(history==null||!history.Time.HasValue||!Positive(history.Time.Value))return current;
        if(current!=null&&current.Time<=history.Time.Value+.0005)return current;
        return history.BestLap!=null&&Math.Abs(history.BestLap.Time-history.Time.Value)<.002?history.BestLap.Copy():new LapReference{Time=history.Time.Value};
    }
}
public sealed class LiveLapView {
    public string State="Sem telemetria";
    public int LapNumber;
    public bool Valid,Finished,CompareToPrevious;
    public double? Elapsed,Gap;
    public int Split;
    public double?[] Sectors=new double?[3];
    public double?[] Deltas=new double?[3];
    public bool[] Done=new bool[3];
    public bool[] SectorRecords=new bool[3];
    public bool LapRecord;
    public LapReference Reference;
    public LapReference Previous;
    public PersonalRecord Records;
}
public sealed class PersonalRecord {
    public string Key { get; set; }
    public double? Time { get; set; }
    public double? S1 { get; set; }
    public double? S2 { get; set; }
    public double? S3 { get; set; }
    public LapReference Previous { get; set; }
    public LapReference BestLap {get;set;}
    public double? Sector(int i){return i==0?S1:i==1?S2:S3;}
    public PersonalRecord Copy(){return new PersonalRecord{Key=Key,Time=Time,S1=S1,S2=S2,S3=S3,BestLap=BestLap==null?null:BestLap.Copy()};}
    static double? Min(double? old,double? next){return next.HasValue&&LapReference.Positive(next.Value)&&(!old.HasValue||next<old)?next:old;}
    public bool Merge(LapReference lap){
        if(lap==null||!LapReference.Positive(lap.Time))return false;
        var time=Min(Time,lap.Time);var s1=Min(S1,lap.S1);var s2=Min(S2,lap.S2);var s3=Min(S3,lap.S3);
        bool changed=time!=Time||s1!=S1||s2!=S2||s3!=S3;if(lap.Time<=time.Value+.0005&&(BestLap==null||lap.Time<BestLap.Time-.0005)){BestLap=lap.Copy();changed=true;}Time=time;S1=s1;S2=s2;S3=s3;return changed;
    }
}
public sealed class PersonalRecordBook {
    public List<PersonalRecord> Entries { get; set; }
    public int Revision { get; set; }
    public PersonalRecordBook(){Entries=new List<PersonalRecord>();}
    public PersonalRecord Get(LiveSample s){
        if(Entries==null)Entries=new List<PersonalRecord>();
        string key=TimingData.Normalize(s.Track)+"|"+TimingData.Normalize(s.CarClass)+"|"+TimingData.Normalize(s.Driver);
        var item=Entries.FirstOrDefault(r=>r.Key==key);
        if(item==null){item=new PersonalRecord{Key=key};Entries.Add(item);Revision++;}return item;
    }
    public void Merge(PersonalRecord record,LapReference lap,bool previous=false){
        if(record.Merge(lap))Revision++;
        if(previous&&lap!=null){record.Previous=lap.Copy();Revision++;}
    }
}
public sealed class LiveLapModel {
    string sessionKey;
    double lapStart=double.NaN,lastSessionTime;
    int completedLaps;
    bool validLap;
    LapReference reference;
    LapReference previous;
    PersonalRecord records;
    public readonly PersonalRecordBook Book;
    LiveLapView finish;
    public LiveLapView LastCompleted {get;private set;}
    public LiveLapView Ideal {get;private set;}
    double finishAt;
    public LiveLapView Current=new LiveLapView();
    public LiveLapModel(PersonalRecordBook book=null){Book=book??new PersonalRecordBook();}
    public LiveLapView Update(LiveSample s,TimingRow online,string expectedTrack,string expectedPlayer,bool showFinishFlash=true,bool compareToPrevious=false){
        Ideal=null;
        if(s==null)return Current=new LiveLapView();
        if(TimingData.Normalize(s.Track)!=TimingData.Normalize(expectedTrack))return Current=new LiveLapView{State="Outra pista"};
        if(TimingData.Normalize(s.Driver)!=TimingData.Normalize(expectedPlayer))return Current=new LiveLapView{State="Outro piloto"};
        string key=s.Track+"|"+s.Car+"|"+s.Session+"|"+s.Slot;
        if(key!=sessionKey || s.SessionTime<lastSessionTime-1){sessionKey=key;lapStart=double.NaN;finish=null;LastCompleted=null;reference=null;}
        lastSessionTime=s.SessionTime;
        bool next=double.IsNaN(lapStart)||Math.Abs(lapStart-s.LapStart)>0.05;
        if(next){
            var record=Book.Get(s);
            bool observed=!double.IsNaN(lapStart)&&s.Laps>completedLaps;
            if(observed&&LapReference.Positive(s.LastLap)){
                LastCompleted=BuildFinished(s,reference,previous,records,compareToPrevious);LastCompleted.Valid=validLap;
                if(validLap){finish=LastCompleted;finishAt=s.SessionTime;Book.Merge(record,LastLap(s),true);}
                else{finish=null;LastCompleted.State="Inválida";LastCompleted.Gap=null;LastCompleted.LapRecord=false;LastCompleted.Deltas=new double?[3];LastCompleted.SectorRecords=new bool[3];}
            }else{
                finish=null;
                if(LastCompleted==null&&s.Laps>0&&LapReference.Positive(s.LastLap)){LastCompleted=BuildFinished(s,null,null,null);Book.Merge(record,LastLap(s),true);}
            }
            Book.Merge(record,LapReference.FromGame(s));
            if(online!=null)Book.Merge(record,new LapReference{Time=online.bestLapTime,S1=online.s1,S2=online.s2,S3=online.s3});
            lapStart=s.LapStart;completedLaps=s.Laps;
            reference=LapReference.Choose(s,online,record);
            previous=record.Previous==null?null:record.Previous.Copy();records=record.Copy();
            validLap=s.CountLapFlag==2&&!s.InPits&&s.Distance>=0;
        }
        Ideal=BuildIdeal(Book.Get(s));
        if(LastCompleted!=null){LastCompleted.Reference=reference;LastCompleted.CompareToPrevious=compareToPrevious;Compare(LastCompleted);}
        if(s.InPits||s.CountLapFlag!=2)validLap=false;
        if(!s.Realtime || s.InPits || s.Distance<0){
            finish=null;
            return Current=new LiveLapView{State=s.InPits?"Nas boxes":!s.Realtime?"Fora da pista":"Saída das boxes",LapNumber=s.Laps+1,Reference=reference};
        }
        if(showFinishFlash&&finish!=null&&s.SessionTime-finishAt<3)return Current=finish;
        var v=new LiveLapView{LapNumber=s.Laps+1,Valid=validLap,State=validLap?"":"Inválida",Reference=reference,Previous=previous,Records=records,CompareToPrevious=compareToPrevious};
        double elapsed=s.SessionTime-s.LapStart;
        if(elapsed<0||elapsed>3600||double.IsNaN(elapsed))return Current=v;
        v.Elapsed=elapsed;
        int sector=s.Sector==0?2:s.Sector==2?1:0;
        if(sector==0)v.Sectors[0]=elapsed;
        else if(LapReference.Positive(s.CurrentS1)){
            v.Sectors[0]=s.CurrentS1;v.Done[0]=true;v.Split=1;
            if(sector==1)v.Sectors[1]=Math.Max(0,elapsed-s.CurrentS1);
            else if(s.CurrentS12>s.CurrentS1){v.Sectors[1]=s.CurrentS12-s.CurrentS1;v.Done[1]=true;v.Split=2;v.Sectors[2]=Math.Max(0,elapsed-s.CurrentS12);}
        }
        Compare(v);
        return Current=v;
    }
    public static LiveLapView BuildIdeal(PersonalRecord record){
        var v=new LiveLapView{State="Melhores setores registados",Valid=true};
        if(record==null)return v;
        double sum=0;bool complete=true;
        for(int i=0;i<3;i++){var sector=record.Sector(i);if(sector.HasValue&&LapReference.Positive(sector.Value)){v.Sectors[i]=sector;v.Done[i]=true;v.SectorRecords[i]=true;sum+=sector.Value;}else complete=false;}
        if(complete){v.Elapsed=sum;v.LapRecord=true;}else v.State="A aguardar os três setores";
        return v;
    }
    static LapReference LastLap(LiveSample s){
        return new LapReference{Time=s.LastLap,S1=LapReference.Positive(s.LastS1)?(double?)s.LastS1:null,
            S2=s.LastS12>s.LastS1&&LapReference.Positive(s.LastS1)?(double?)(s.LastS12-s.LastS1):null,
            S3=s.LastLap>s.LastS12&&LapReference.Positive(s.LastS12)?(double?)(s.LastLap-s.LastS12):null};
    }
    static LiveLapView BuildFinished(LiveSample s,LapReference baseline,LapReference previous,PersonalRecord records,bool compareToPrevious=false){
        var v=new LiveLapView{Finished=true,Valid=true,State="",Elapsed=s.LastLap,LapNumber=s.Laps,Reference=baseline,Previous=previous,Records=records,Split=3,CompareToPrevious=compareToPrevious};
        if(LapReference.Positive(s.LastS1)){v.Sectors[0]=s.LastS1;v.Done[0]=true;}
        if(s.LastS12>s.LastS1&&LapReference.Positive(s.LastS1)){v.Sectors[1]=s.LastS12-s.LastS1;v.Done[1]=true;}
        if(s.LastLap>s.LastS12&&LapReference.Positive(s.LastS12)){v.Sectors[2]=s.LastLap-s.LastS12;v.Done[2]=true;}
        Compare(v);return v;
    }
    static void Compare(LiveLapView v){
        v.Gap=null;v.Deltas=new double?[3];if(!v.Valid)return;
        var baseline=v.CompareToPrevious?v.Previous:v.Reference;
        for(int i=0;i<3;i++)if(v.Done[i]&&v.Sectors[i].HasValue){
            if(baseline!=null&&baseline.Sector(i).HasValue)v.Deltas[i]=v.Sectors[i].Value-baseline.Sector(i).Value;
            if(v.Records!=null&&v.Records.Sector(i).HasValue)v.SectorRecords[i]=v.Sectors[i].Value<v.Records.Sector(i).Value-.0005;
        }
        if(v.Finished&&v.Elapsed.HasValue){
            if(baseline!=null)v.Gap=v.Elapsed.Value-baseline.Time;
            if(v.Records!=null&&v.Records.Time.HasValue)v.LapRecord=v.Elapsed.Value<v.Records.Time.Value-.0005;
        }
        else if(v.Split>0){
            double delta=0;
            for(int i=0;i<v.Split;i++){if(!v.Deltas[i].HasValue)return;delta+=v.Deltas[i].Value;}
            v.Gap=delta;
        }
    }
}
