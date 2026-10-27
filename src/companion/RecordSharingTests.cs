// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public static class RecordSharingTests {
    static void Check(bool condition,string message){if(!condition)throw new Exception("Recordes: "+message);}
    static LiveSample Sample(double time,double start,int laps,double last=82,int flag=2){return new LiveSample{Track="Test track",Car="Test car",CarClass="Cup",Driver="Test driver",Session=1,Slot=1,Realtime=true,Distance=100,SessionTime=time,LapStart=start,Laps=laps,LastLap=last,LastS1=20,LastS12=49,CountLapFlag=flag};}
    public static void Run(){
        Check(Math.Abs(TrainingMath.RacePace(100,.5)-100.5)<.00001&&Math.Abs(TrainingMath.RacePace(82.247,1)-83.06947)<.00001,"Cálculo percentual do ritmo");
        var collector=new TrainingRecordCollector();collector.Observe(Sample(10,0,0));collector.Observe(Sample(80.1,80,1));for(int t=81;t<162;t++)collector.Observe(Sample(t,80,1));collector.Observe(Sample(162.1,162,2));
        Check(collector.Book.Entries.Count==1&&collector.Book.Entries[0].Time==82&&collector.Book.Entries[0].BestLap.S2==29,"Capturar uma volta completa válida sem precisar de um stint ativo");
        for(int t=163;t<244;t++)collector.Observe(Sample(t,162,2,82,1));collector.Observe(Sample(244.1,244,3,50));Check(collector.Book.Entries[0].Time==82,"Uma volta inválida não pode melhorar recordes");
        collector.Observe(Sample(250,244,3));collector.Observe(Sample(300,244,3));collector.Observe(Sample(304.1,304,4,60));Check(collector.Book.Entries[0].Time==82,"Uma volta sem observação completa não pode melhorar recordes");
        string root=Path.Combine(CompanionStore.Root,"shared-record-tests-"+Guid.NewGuid().ToString("N")),legacy=Path.Combine(root,"competitions","fixture","timing-1","personal-records.json");Directory.CreateDirectory(Path.GetDirectoryName(legacy));
        var initial=new PersonalRecordBook();var sample=Sample(1,0,0);initial.Merge(initial.Get(sample),new LapReference{Time=90,S1=20,S2=30,S3=40},true);initial.Entries[0].PreviousAtUtc=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);string original=CompanionStore.Json.Serialize(initial);File.WriteAllText(legacy,original);
        using(var first=new PersonalRecordRepository(root))using(var second=new PersonalRecordRepository(root)){
            var a=new PersonalRecordBook();var b=new PersonalRecordBook();Check(first.Synchronize(a)&&second.Synchronize(b)&&a.Entries.Single().Time==90,"Importar os registos existentes");
            a.Merge(a.Get(sample),new LapReference{Time=85,S1=19,S2=31,S3=35},true);a.Entries[0].PreviousAtUtc=new DateTime(2026,1,1,0,0,1,DateTimeKind.Utc);Check(first.Synchronize(a),"Guardar o primeiro processo");
            b.Merge(b.Get(sample),new LapReference{Time=86,S1=18,S2=32,S3=36},true);b.Entries[0].PreviousAtUtc=new DateTime(2026,1,1,0,0,2,DateTimeKind.Utc);Check(second.Synchronize(b),"Fundir um segundo processo com uma cópia antiga");
            var loaded=new PersonalRecordBook();Check(first.Synchronize(loaded),"Reabrir os recordes partilhados");var record=loaded.Entries.Single();Check(record.Time==85&&record.S1==18&&record.S2==30&&record.S3==35,"Preservar os mínimos de ambos os escritores");
            var reference=LapReference.Choose(sample,null,record);Check(reference.Time==85&&reference.S1==19&&reference.S2==31&&reference.S3==35,"Manter os setores da volta recorde separados dos recordes individuais");
            Check(record.Previous.Time==86,"Preservar a volta anterior mais recente ao fundir processos");
            Check(File.ReadAllText(legacy)==original,"A migração não altera os ficheiros anteriores");
            var c=new PersonalRecordBook();var d=new PersonalRecordBook();var other=Sample(1,0,0);other.Driver="Other driver";c.Merge(c.Get(sample),new LapReference{Time=84},false);d.Merge(d.Get(other),new LapReference{Time=88},false);
            Parallel.Invoke(()=>{for(int i=0;i<100&&!first.Synchronize(c);i++)Thread.Sleep(1);},()=>{for(int i=0;i<100&&!second.Synchronize(d);i++)Thread.Sleep(1);});
            var concurrent=new PersonalRecordBook();Check(first.Synchronize(concurrent)&&concurrent.Entries.Count==2&&concurrent.Entries.Single(x=>x.Key==a.Entries[0].Key).Time==84,"Dois processos não podem perder melhorias concorrentes");
            File.WriteAllText(first.FilePath,"{invalid");Check(!first.Synchronize(concurrent)&&File.ReadAllText(first.FilePath)=="{invalid","Não substituir silenciosamente um histórico danificado");
        }
        File.WriteAllText(Path.Combine(CompanionStore.Root,"record-sharing-tests.txt"),"OK: cálculo de ritmo; voltas válidas sem stint; rejeição de inválidas e falhas; migração preservada; escrita concorrente; setores emparelhados; ficheiro danificado preservado.");
    }
}
