// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public static class DriverHistoryTests {
    static void Check(bool value,string text){if(!value)throw new Exception("Histórico de pilotos: "+text);}
    static DriverLapPoint Lap(int n,bool pit=false){return new DriverLapPoint{Number=n,Time=82+n*.01,Valid=true,Pit=pit,Complete=true};}
    static DriverTimingSnapshot Snapshot(){return new DriverTimingSnapshot{sourceSessionId="session-test-only",sessionInfo=new DriverTimingSession{trackName="Test circuit",sessionCode=1,currentEventTime=1000},entries=new List<DriverTimingEntry>{new DriverTimingEntry{driverName="Test Driver",vehicleClass="Cup",lapsCompleted=15,inPits=false,pitStops=0,lastLapTime=83,lapHistory=new List<DriverTimingLap>{new DriverTimingLap{lapNumber=2,lapTime=82,invalid=false,pit=false},new DriverTimingLap{lapNumber=3,lapTime=82.1,invalid=false,pit=false},new DriverTimingLap{lapNumber=15,lapTime=82.3,invalid=false,pit=false}}}}};}
    static DriverActivitySnapshot Activity(double time,double start,int laps,bool pits=false,int stops=0){var activity=new DriverActivitySnapshot{Track="Test circuit",UpdatedUtc=DateTime.UtcNow};activity.Drivers.Add(new DriverActivityEntry{Name="Other Driver",Class="Cup",State=pits?DriverPresence.InPits:DriverPresence.OnTrack,PitStops=stops,Lap=new LiveSample{Track=activity.Track,Driver="Other Driver",CarClass="Cup",Car="Car",Session=1,Slot=2,SessionTime=time,LapStart=start,Laps=laps,LastLap=82,LastS1=25,LastS12=55,CountLapFlag=2,Realtime=false,InPits=pits}});return activity;}
    sealed class Handler:HttpMessageHandler {
        public int Requests;public string Endpoint,Body;public HttpStatusCode Status=HttpStatusCode.OK;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellation){Requests++;Endpoint=request.RequestUri.AbsoluteUri;return Task.FromResult(new HttpResponseMessage(Status){Content=new StringContent(Body??"{}",Encoding.UTF8,"application/json")});}
    }
    public static void Run(){
        string original=CompanionStore.Root;string root=Path.Combine(original,"driver-history-tests-"+Guid.NewGuid().ToString("N"));CompanionStore.Root=root;Directory.CreateDirectory(root);
        try{
            var collector=new DriverHistoryCollector();var snapshot=Snapshot();collector.Remote(snapshot,110,DateTime.UtcNow);var record=collector.Book.Records.Single();Check(record.Laps.Count==3&&record.Partial,"Preservar o histórico parcial");Check(record.Markers().Count==0&&record.Stints().Count==2,"Saltos de volta não são boxes nem separam stints");int revision=record.DataRevision;collector.Remote(snapshot,110,DateTime.UtcNow);Check(record.Laps.Count==3&&record.DataRevision==revision,"Consulta repetida não duplica nem altera as voltas");
            snapshot.entries[0].lapHistory.Add(new DriverTimingLap{lapNumber=10,lapTime=120,invalid=false,pit=true});snapshot.entries[0].lapHistory.Add(new DriverTimingLap{lapNumber=11,lapTime=110,invalid=false,pit=true});collector.Remote(snapshot,110,DateTime.UtcNow);Check(record.Markers().Count==1&&record.Stints().Count==3,"Agrupar as voltas de uma passagem pelas boxes");Check(record.Stints()[2].AfterLap==11,"Novo stint após a última volta de boxes");Check(record.Laps.Count(l=>l.Clean)==3,"Excluir boxes da média limpa");
            snapshot.entries[0].lapHistory.Add(new DriverTimingLap{lapNumber=14,lapTime=84});collector.Remote(snapshot,110,DateTime.UtcNow);Check(!record.Laps.First(l=>l.Number==14).Clean,"Não inventar validade quando não é fornecida");
            snapshot.entries[0].lapsCompleted=16;snapshot.entries[0].lastLapTime=81;collector.Remote(snapshot,110,DateTime.UtcNow);Check(!record.Laps.Last().Complete&&!record.Laps.Last().Clean,"Última volta sem flags mantém validade desconhecida");
            snapshot.sessionInfo.currentEventTime=5;collector.Remote(snapshot,110,DateTime.UtcNow);Check(collector.Book.Records.Count==2,"Reinício de sessão cria histórico separado");collector.Remote(snapshot,111,DateTime.UtcNow);Check(collector.Book.Records.Count==3,"Campeonatos não misturam sessões");
            var visits=new DriverHistoryRecord{TotalLaps=12};for(int n=1;n<=12;n++)visits.Put(Lap(n));var now=DateTime.UtcNow;visits.PitState(false,0,4,now);visits.PitState(true,0,4,now);visits.PitState(true,0,4,now);Check(visits.Visits.Count==1&&!visits.Markers()[0].SplitAfter.HasValue,"Entrada contínua tem só uma marca e aguarda saída");visits.PitState(false,1,5,now);Check(visits.Visits.Count==1&&visits.Visits[0].ExitAfterLap==5&&!visits.Visits[0].Approximate,"Entrada e saída observadas definem um stint");
            var approximate=new DriverHistoryRecord{TotalLaps=5};approximate.PitState(false,0,5,now);approximate.PitState(false,1,9,now);Check(approximate.Visits.Single().Approximate&&approximate.Visits[0].EntryAfterLap==5&&approximate.Visits[0].ExitAfterLap==9,"Contador de boxes sem transição dá intervalo aproximado");
            var local=new DriverHistoryCollector();local.Local(Activity(10,0,0),now);local.Local(Activity(82.1,82,1),now);for(int t=83;t<164;t++)local.Local(Activity(t,82,1),now);local.Local(Activity(164.1,164,2),now);var localRecord=local.Book.Records.Single();Check(localRecord.Laps.Last().Clean&&localRecord.Laps.Last().Number==2,"Acompanhar outros pilotos mesmo com o jogador no menu");
            local.Local(Activity(245,164,2),now);local.Local(Activity(246.1,246,3),now);Check(!localRecord.Laps.Last().Complete,"Uma falha de leitura não produz uma volta completa");local.Local(Activity(1,0,0),now);Check(local.Book.Records.Count==2,"Reinício local não mistura voltas");
            var bytes=new byte[1144];Encoding.UTF8.GetBytes("Remote").CopyTo(bytes,564);BitConverter.GetBytes((short)3).CopyTo(bytes,752);BitConverter.GetBytes(10d).CopyTo(bytes,664);var native=DriverActivitySnapshot.Read(bytes,1,now);Check(native.Drivers.Single().PitStops==3&&native.Drivers[0].State==DriverPresence.OnTrack,"Leitura do contador nativo de boxes e presença remota");
            DriverHistoryStore.Save(record);var loaded=DriverHistoryStore.Load().Single();Check(loaded.Laps.Count==record.Laps.Count&&loaded.TimingId==110&&loaded.StreamKey==record.StreamKey,"Persistência do histórico desportivo");
            var json=new JavaScriptSerializer().Serialize(Snapshot());var handler=new Handler{Body=json.Substring(0,json.Length-1)+",\"server\":{\"password\":\"private-test-value\"},\"fuel\":55}"};
            Task.Run(async()=>{using(var service=new DriverHistoryService(handler)){await service.Fetch(110);await service.Fetch(110);Check(service.Error==null&&handler.Requests==1,"Consulta com intervalo mínimo de 15 segundos");Check(handler.Endpoint=="https://api.gtcracecenter.com/livetiming/snapshot/110","Consultar apenas o endpoint autorizado");service.Save(true);}}).GetAwaiter().GetResult();Check(!Directory.GetFiles(Path.Combine(root,"training","drivers"),"*.json").Any(f=>File.ReadAllText(f).Contains("private-test-value")),"Não guardar acessos ou dados alheios à análise");
            var failure=new Handler{Status=HttpStatusCode.ServiceUnavailable};Task.Run(async()=>{using(var service=new DriverHistoryService(failure)){int before=service.Collector.Book.Records.Count;await service.Fetch(110);Check(service.Error!=null&&service.Collector.Book.Records.Count==before,"Falha de rede mantém o histórico guardado");}}).GetAwaiter().GetResult();
            var oversized=new Handler{Body=new string(' ',4000001)};Task.Run(async()=>{using(var service=new DriverHistoryService(oversized)){int before=service.Collector.Book.Records.Count;await service.Fetch(110);Check(service.Error!=null&&service.Collector.Book.Records.Count==before,"Limitar a resposta a 4 MB e manter a cache");}}).GetAwaiter().GetResult();
            var nullable=new JavaScriptSerializer().Deserialize<DriverTimingEntry>("{\"lapsCompleted\":1.0,\"lastLapTime\":null}");Check(nullable.lapsCompleted==1&&!nullable.lastLapTime.HasValue,"Aceitar números da API e tempos indisponíveis");
            File.WriteAllText(Path.Combine(original,"driver-history-tests.txt"),"OK: histórico parcial; voltas esparsas; boxes observadas/aproximadas; stints; validade desconhecida; múltiplos pilotos; falhas de telemetria; reinício; persistência; isolamento por campeonato; consulta limitada; cache em falha; dados desportivos apenas.");
        }finally{CompanionStore.Root=original;}
    }
}
