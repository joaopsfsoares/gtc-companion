// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

// Both processes merge under a shared file-specific lock. Older scoped record
// books remain untouched and are imported, so migration does not erase history.
public sealed class PersonalRecordRepository:IDisposable {
    readonly string root,file;
    readonly Mutex gate;
    public string Error {get;private set;}
    public string FilePath{get{return file;}}
    public PersonalRecordRepository(string dataRoot,bool shared=true){
        root=Path.GetFullPath(dataRoot);file=Path.Combine(root,shared?Path.Combine("records","personal-records.json"):"personal-records.json");
        using(var hash=SHA256.Create()){string key=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(file.ToUpperInvariant()))).Replace("-","").Substring(0,24);gate=new Mutex(false,"Local\\GTC-LapRecords-"+key);}
    }
    static JavaScriptSerializer Json(){return new JavaScriptSerializer{MaxJsonLength=4000000};}
    static PersonalRecordBook Read(string path){
        if(!System.IO.File.Exists(path))return null;
        try{var book=Json().Deserialize<PersonalRecordBook>(System.IO.File.ReadAllText(path));if(book==null||book.Entries==null)throw new InvalidDataException();return book;}
        catch(Exception ex){throw new InvalidDataException("Não foi possível ler um ficheiro de recordes; o original foi preservado.",ex);}
    }
    IEnumerable<string> Legacy(){
        foreach(string path in new[]{Path.Combine(root,"personal-records.json"),Path.Combine(root,"overlay","personal-records.json")})if(path!=file&&System.IO.File.Exists(path))yield return path;
        string competitions=Path.Combine(root,"competitions");if(Directory.Exists(competitions))foreach(string path in Directory.GetFiles(competitions,"personal-records.json",SearchOption.AllDirectories))yield return path;
    }
    static double? Min(double? a,double? b){return b.HasValue&&LapReference.Positive(b.Value)&&(!a.HasValue||!LapReference.Positive(a.Value)||b.Value<a.Value)?b:a;}
    static int Sectors(LapReference lap){return lap==null?0:new[]{lap.S1,lap.S2,lap.S3}.Count(x=>x.HasValue&&LapReference.Positive(x.Value));}
    public static bool Merge(PersonalRecordBook target,PersonalRecordBook source){
        if(source==null||source.Entries==null)return false;if(target.Entries==null)target.Entries=new List<PersonalRecord>();bool changed=false;var json=Json();
        foreach(var incoming in source.Entries.Where(x=>x!=null&&!string.IsNullOrWhiteSpace(x.Key))){
            var current=target.Entries.FirstOrDefault(x=>x!=null&&x.Key==incoming.Key);if(current==null){current=new PersonalRecord{Key=incoming.Key};target.Entries.Add(current);changed=true;}
            string before=json.Serialize(current);current.Time=Min(current.Time,incoming.Time);current.S1=Min(current.S1,incoming.S1);current.S2=Min(current.S2,incoming.S2);current.S3=Min(current.S3,incoming.S3);
            var lap=incoming.BestLap;
            if(lap!=null&&LapReference.Positive(lap.Time)){
                current.Time=Min(current.Time,lap.Time);
                if(current.BestLap==null||lap.Time<current.BestLap.Time-.0005||(Math.Abs(lap.Time-current.BestLap.Time)<.0005&&Sectors(lap)>Sectors(current.BestLap)))current.BestLap=lap.Copy();
            }
            if(incoming.Previous!=null&&LapReference.Positive(incoming.Previous.Time)&&(current.Previous==null||incoming.PreviousAtUtc>current.PreviousAtUtc)){current.Previous=incoming.Previous.Copy();current.PreviousAtUtc=incoming.PreviousAtUtc;}
            if(before!=json.Serialize(current))changed=true;
        }
        if(changed)target.Revision++;return changed;
    }
    public bool Synchronize(PersonalRecordBook local){
        bool locked=false;
        try{
            try{locked=gate.WaitOne(0);}catch(AbandonedMutexException){locked=true;}
            if(!locked){Error="Recordes em utilização; a sincronização será repetida.";return false;}
            var disk=Read(file)??new PersonalRecordBook();bool changed=Merge(disk,local);
            foreach(string legacy in Legacy()){
                // A damaged legacy import must not block healthy central records.
                try{changed=Merge(disk,Read(legacy))||changed;}catch(InvalidDataException){}
            }
            Merge(local,disk);
            if(changed||!System.IO.File.Exists(file)&&disk.Entries.Count>0){
                Directory.CreateDirectory(Path.GetDirectoryName(file));string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
                try{System.IO.File.WriteAllText(temp,Json().Serialize(disk),new UTF8Encoding(false));if(System.IO.File.Exists(file))System.IO.File.Replace(temp,file,null);else System.IO.File.Move(temp,file);}finally{if(System.IO.File.Exists(temp))System.IO.File.Delete(temp);}
            }
            Error=null;return true;
        }catch(Exception ex){Error=ex.Message;return false;}finally{if(locked)gate.ReleaseMutex();}
    }
    public void Dispose(){gate.Dispose();}
}
