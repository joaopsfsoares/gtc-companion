// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public enum DriverPresence{Unknown,OnTrack,InPits,Paddock}
public sealed class DriverActivityEntry{public string Name,Class;public DriverPresence State;public LiveSample Lap;}
public sealed class DriverActivitySnapshot {
    public string Track;
    public DateTime UpdatedUtc;
    public readonly List<DriverActivityEntry> Drivers=new List<DriverActivityEntry>();
    public DriverPresence Find(TimingRow row,string track,DateTime now){
        if(row==null||now-UpdatedUtc>TimeSpan.FromSeconds(3)||TimingData.Normalize(Track)!=TimingData.Normalize(track))return DriverPresence.Unknown;
        var matches=Drivers.Where(x=>TimingData.Normalize(x.Name)==TimingData.Normalize(row.driverName)).ToList();
        if(matches.Count>1)matches=matches.Where(x=>TimingData.Normalize(x.Class)==TimingData.Normalize(row.vehicleClass)).ToList();
        return matches.Count==1?matches[0].State:DriverPresence.Unknown;
    }
    public static DriverActivitySnapshot Read(byte[] data,int count,DateTime now){
        var result=new DriverActivitySnapshot{Track=Text(data,12,64),UpdatedUtc=now};
        if(count<0||count>128||data.Length<560+count*584)return result;
        for(int i=0;i<count;i++){
            int offset=560+i*584;bool pits=data[offset+198]!=0,player=data[offset+196]!=0;int control=unchecked((sbyte)data[offset+197]);
            var state=control<0?DriverPresence.Unknown:pits?DriverPresence.InPits:player&&data[127]==0?DriverPresence.Paddock:BitConverter.ToDouble(data,offset+104)>=0?DriverPresence.OnTrack:DriverPresence.Paddock;
            result.Drivers.Add(new DriverActivityEntry{Name=Text(data,offset+4,32),Class=Text(data,offset+200,32),State=state,Lap=control<0?null:new LiveSample{
                Track=result.Track,Driver=Text(data,offset+4,32),Car=Text(data,offset+36,64),CarClass=Text(data,offset+200,32),Session=BitConverter.ToInt32(data,76),Slot=BitConverter.ToInt32(data,offset),
                SessionTime=BitConverter.ToDouble(data,80),Realtime=data[127]!=0,InPits=pits,Laps=BitConverter.ToInt16(data,offset+100),Sector=data[offset+102],CountLapFlag=data[offset+506],Distance=BitConverter.ToDouble(data,offset+104),LapStart=BitConverter.ToDouble(data,offset+256),
                CurrentS1=BitConverter.ToDouble(data,offset+176),CurrentS12=BitConverter.ToDouble(data,offset+184),LastS1=BitConverter.ToDouble(data,offset+152),LastS12=BitConverter.ToDouble(data,offset+160),LastLap=BitConverter.ToDouble(data,offset+168),BestLap=BitConverter.ToDouble(data,offset+144),BestS1=BitConverter.ToSingle(data,offset+528),BestS12=BitConverter.ToSingle(data,offset+532)
            }});
        }
        return result;
    }
    static string Text(byte[] data,int offset,int size){int end=offset;while(end<offset+size&&data[end]!=0)end++;return Encoding.UTF8.GetString(data,offset,end-offset);}
}
