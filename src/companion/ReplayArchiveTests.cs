// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
public static class ReplayArchiveTests {
    static void Check(bool value,string message){if(!value)throw new Exception("Arquivo de replays: "+message);}
    static void Fixture(string file,string pilot){using(var stream=File.Create(file))using(var gzip=new GZipStream(stream,CompressionMode.Compress))using(var writer=new StreamWriter(gzip,Encoding.ASCII))writer.Write("//[[gMb1.002f ]]\nIRSR Estoril "+pilot);}
    public static void Run(){
        string root=Path.Combine(CompanionStore.Root,"replay-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try{
            string competition=Guid.NewGuid().ToString("N"),source=Path.Combine(root,"session.Vcr");Fixture(source,"Outro Piloto");
            var c=new ReplayCandidate{Driver="Outro Piloto",Track="Estoril",VehicleClass="Cup",LapNumber=3,Time=90};
            var a=ReplayArchive.Save(root,competition,c,source);string dir=ReplayArchive.DirectoryFor(root,competition,c),first=Path.Combine(dir,a.FileName);
            Check(File.Exists(first)&&File.Exists(source),"guardar cópia sem mexer no replay original");
            c.Time=91;Check(ReplayArchive.Save(root,competition,c,source).FileName==a.FileName,"rejeitar tempo mais lento");
            c.Time=89;Fixture(source,"Piloto errado");bool rejected=false;try{ReplayArchive.Save(root,competition,c,source);}catch(InvalidDataException){rejected=true;}
            Check(rejected&&File.Exists(first)&&ReplayArchive.Read(Path.Combine(dir,"best.json")).FileName==a.FileName,"falha não pode apagar a melhor anterior");
            Fixture(source,"Outro Piloto");var b=ReplayArchive.Save(root,competition,c,source);Check(File.Exists(Path.Combine(dir,b.FileName))&&!File.Exists(first)&&Directory.GetFiles(dir,"*.Vcr").Length==1,"substituir apenas após cópia confirmada");
        }finally{string parent=Path.GetFullPath(CompanionStore.Root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;string target=Path.GetFullPath(root);if(!target.StartsWith(parent,StringComparison.OrdinalIgnoreCase)||!Path.GetFileName(target).StartsWith("replay-tests-",StringComparison.Ordinal))throw new IOException("Pasta de teste inesperada.");Directory.Delete(target,true);}
    }
}
