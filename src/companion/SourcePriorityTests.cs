// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public static class SourcePriorityTests {
    static void Check(bool condition,string message){if(!condition)throw new Exception("Fontes: "+message);}
    sealed class Handler:HttpMessageHandler {
        public int Requests;public string Endpoint;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){Requests++;Endpoint=request.RequestUri.AbsoluteUri;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("{\"activeRound\":{\"id\":\"fixture\",\"trackName\":\"Test track\"},\"rows\":[{\"driverName\":\"Test driver\",\"vehicleClass\":\"Cup\",\"bestLapTime\":90}],\"server\":{\"password\":\"not-persisted-fixture\"}}")});}
    }
    public static void Run(){
        var sample=new LiveSample{Driver="Test driver",CarClass="Cup",Track="Test track",BestLap=85};var book=new PersonalRecordBook();book.Merge(book.Get(sample),new LapReference{Time=80,S1=20,S2=25,S3=35});
        var cache=new CachedSnapshot{UpdatedUtc=DateTime.UtcNow,Data=new TimingSnapshot{activeRound=new RoundInfo{id="fixture",trackName=sample.Track},rows=new List<TimingRow>{new TimingRow{driverName=sample.Driver,vehicleClass="Cup",bestLapTime=90}}}};string context;
        var remote=new DriverHistoryRecord{Id="api",Driver=sample.Driver,Track=sample.Track,Class="Cup",TimingId=110,Source="gtc",UpdatedUtc=DateTime.UtcNow,FirstSeenUtc=DateTime.UtcNow.AddHours(-1),TotalLaps=1};remote.Put(new DriverLapPoint{Number=1,Time=88,Valid=true,Pit=false,Complete=true});var histories=new[]{remote};
        Check(GtcDataPriority.TrainingBest(sample.Driver,sample,new[]{cache},histories,110,book,out context)==90&&context.StartsWith("GTC"),"Classificação GTC antes de voltas da sessão, jogo e recorde local mais rápido");
        Check(GtcDataPriority.TrainingBest(sample.Driver,sample,new CachedSnapshot[0],histories,110,book,out context)==88,"Sessão GTC como segunda fonte online");
        Check(GtcDataPriority.TrainingBest(sample.Driver,sample,new CachedSnapshot[0],histories,111,book,out context)==80&&context.StartsWith("Local"),"Sem GTC correspondente, usar apoio local e respeitar o campeonato");
        var other=new CachedSnapshot{UpdatedUtc=DateTime.UtcNow.AddMinutes(1),Data=new TimingSnapshot{activeRound=new RoundInfo{trackName="Other track"},rows=cache.Data.rows}};
        Check(GtcDataPriority.TrainingBest(sample.Driver,sample,new[]{other,cache},histories,110,book,out context)==90,"Uma cache recente de outra pista não ganha prioridade");
        var emptyRound=new CachedSnapshot{UpdatedUtc=DateTime.UtcNow.AddMinutes(1),Data=new TimingSnapshot{activeRound=new RoundInfo{trackName="Other track"},rows=new List<TimingRow>()}};Check(!GtcDataPriority.TrainingBest(sample.Driver,null,new[]{emptyRound,cache},histories,110,book,out context).HasValue,"Sem jogo aberto, não regressar a outra pista por faltar um piloto na classificação atual");
        cache.Data.rows[0].vehicleClass="GT3";Check(GtcDataPriority.TrainingBest(sample.Driver,sample,new[]{cache},new DriverHistoryRecord[0],110,book,out context)==80,"Não cruzar classes");cache.Data.rows[0].vehicleClass="Cup";
        var local=new DriverHistoryRecord{Id="local",Source="local",FirstSeenUtc=DateTime.UtcNow};Check(GtcDataPriority.Sessions(new[]{local,remote})[0]==remote,"Selecionar a fonte GTC antes de uma observação local mais recente");
        Check(new DriverAnalysisOptions().BackgroundGtc,"A recolha GTC vem ativa por defeito");Check(!CompanionStore.Json.Deserialize<DriverAnalysisOptions>("{\"BackgroundGtc\":false}").BackgroundGtc,"Preservar a opção explícita de desligar a recolha");
        string original=CompanionStore.Root;CompanionStore.Root=Path.Combine(original,"source-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(CompanionStore.Root);
        try{
            var settings=new LaunchSettings();settings.Competitions=new List<CompetitionProfile>{new CompetitionProfile{Name="Test",TimingId=110}};var handler=new Handler();
            Task.Run(async()=>{using(var collector=new HistoryCollector(handler)){await collector.Collect(settings);await collector.Collect(settings);}}).GetAwaiter().GetResult();
            var path=Path.Combine(CompetitionProfiles.DataDirectory(settings.Competitions[0]),"leaderboard-cache.json");var stored=CompanionStore.Read<CachedSnapshot>(path);Check(stored!=null&&stored.Data.rows.Single().bestLapTime==90,"Consultar e guardar a classificação sem exigir piloto ou acesso ao servidor");Check(handler.Requests==1&&handler.Endpoint=="https://api.gtcracecenter.com/livetiming/configs/110/leaderboard/snapshot","Reutilizar a cache e limitar a rota autorizada");Check(!File.ReadAllText(path).Contains("not-persisted-fixture"),"Guardar apenas a classificação desportiva");
        }finally{CompanionStore.Root=original;}
        File.WriteAllText(Path.Combine(original,"source-priority-tests.txt"),"OK: GTC antes de dados locais; cache; pistas, classes e campeonatos separados; apoio offline; ordenação de sessões; preferências preservadas; consulta sem acesso ao servidor; rota limitada e dados desportivos.");
    }
}
