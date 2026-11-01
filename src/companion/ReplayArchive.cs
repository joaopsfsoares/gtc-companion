// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

public sealed class ArchivedReplay {
    public string Driver {get;set;}
    public string VehicleClass {get;set;}
    public string Track {get;set;}
    public int LapNumber {get;set;}
    public double Time {get;set;}
    public string FileName {get;set;}
    public string Sha256 {get;set;}
}
public static class ReplayArchive {
    // Each managed directory has one manifest and one current replay.  No game
    // replay or other application file is ever removed by this store.
    public static string DirectoryFor(string root,string competitionId,ReplayCandidate c){
        Guid competition;
        if(!Guid.TryParse(competitionId,out competition)||c==null||string.IsNullOrWhiteSpace(c.Track)||string.IsNullOrWhiteSpace(c.Driver))throw new InvalidDataException("Dados de replay incompletos.");
        string key=DriverHistoryBook.Hash(TimingData.Normalize(c.Track)+"|"+TimingData.Normalize(c.VehicleClass)+"|"+TimingData.Normalize(c.Driver)).Substring(0,16);
        return Path.Combine(root,"replays",competition.ToString("N"),key);
    }
    public static ArchivedReplay Save(string root,string competitionId,ReplayCandidate candidate,string source){
        if(candidate==null||!LapReference.Positive(candidate.Time)||candidate.LapNumber<1)throw new InvalidDataException("Volta inválida.");
        string dir=DirectoryFor(root,competitionId,candidate),index=Path.Combine(dir,"best.json");
        Directory.CreateDirectory(dir);
        var old=Read(index);
        if(old!=null&&LapReference.Positive(old.Time)&&old.Time<=candidate.Time&&!string.IsNullOrEmpty(old.FileName)&&Path.GetFileName(old.FileName)==old.FileName){string existing=Path.Combine(dir,old.FileName);if(File.Exists(existing)&&Hash(existing)==old.Sha256)return old;}
        string safe=new string(candidate.Driver.Where(ch=>char.IsLetterOrDigit(ch)||ch==' '||ch=='-'||ch=='_').Take(24).ToArray()).Trim();
        if(safe.Length==0)safe="Piloto";
        string name=safe+" - "+TimingData.Lap(candidate.Time).Replace(':','-')+" - "+Guid.NewGuid().ToString("N").Substring(0,8)+".Vcr";
        string tmp=Path.Combine(dir,Guid.NewGuid().ToString("N")+".tmp"),target=Path.Combine(dir,name);
        try{
            File.Copy(source,tmp,false);
            if(!MatchesReplay(tmp,candidate.Driver,candidate.Track))throw new InvalidDataException("O replay não confirma o piloto e a pista indicados.");
            string sourceHash=Hash(source),copyHash=Hash(tmp);if(sourceHash!=copyHash)throw new IOException("O replay mudou durante a cópia.");
            File.Move(tmp,target);
            var result=new ArchivedReplay{Driver=candidate.Driver,VehicleClass=candidate.VehicleClass,Track=candidate.Track,LapNumber=candidate.LapNumber,Time=candidate.Time,FileName=name,Sha256=copyHash};
            string newIndex=Path.Combine(dir,"index-"+Guid.NewGuid().ToString("N").Substring(0,8)+".tmp");
            try{File.WriteAllText(newIndex,CompanionStore.Json.Serialize(result),new UTF8Encoding(false));if(File.Exists(index))File.Replace(newIndex,index,null);else File.Move(newIndex,index);}finally{if(File.Exists(newIndex))File.Delete(newIndex);}
            // Remove only a file named by the previous manifest in this directory.
            if(old!=null&&!string.IsNullOrEmpty(old.FileName)&&Path.GetFileName(old.FileName)==old.FileName&&!string.Equals(old.FileName,name,StringComparison.OrdinalIgnoreCase)){
                string previous=Path.Combine(dir,old.FileName);if(File.Exists(previous))File.Delete(previous);
            }
            return result;
        }finally{if(File.Exists(tmp))File.Delete(tmp);}
    }
    public static ArchivedReplay Read(string file){try{return CompanionStore.Json.Deserialize<ArchivedReplay>(File.ReadAllText(file));}catch{return null;}}
    static string Hash(string file){using(var stream=File.OpenRead(file))using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
    static bool MatchesReplay(string file,string driver,string track){
        // The driver directory is near the beginning of the decompressed replay.
        using(var stream=File.OpenRead(file))using(var gzip=new GZipStream(stream,CompressionMode.Decompress))using(var buffer=new MemoryStream()){
            byte[] bytes=new byte[16384];int read,total=0;while((read=gzip.Read(bytes,0,bytes.Length))>0){int keep=Math.Min(read,Math.Max(0,4*1024*1024-total));if(keep>0){buffer.Write(bytes,0,keep);total+=keep;}}
            string data=Encoding.GetEncoding(1252).GetString(buffer.ToArray());
            return data.StartsWith("//[[gMb",StringComparison.Ordinal)&&data.Contains("IRSR")&&data.IndexOf(driver,StringComparison.OrdinalIgnoreCase)>=0&&data.IndexOf(track,StringComparison.OrdinalIgnoreCase)>=0;
        }
    }
}
