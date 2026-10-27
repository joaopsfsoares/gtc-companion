// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

public sealed class DriverAnalysisOptions {public bool BackgroundGtc {get;set;}public DriverAnalysisOptions(){BackgroundGtc=true;}}
public sealed class DriverHistoryService:IDisposable {
    public readonly DriverHistoryCollector Collector=new DriverHistoryCollector();
    readonly HttpClient client;
    readonly CancellationTokenSource cancellation=new CancellationTokenSource();
    readonly Dictionary<string,string> signatures=new Dictionary<string,string>();
    readonly Dictionary<string,DateTime> persisted=new Dictionary<string,DateTime>();
    DateTime lastSave=DateTime.MinValue,nextFetch=DateTime.MinValue;
    bool busy,disposed;
    public string Error {get;private set;}
    public DriverHistoryService(HttpMessageHandler handler=null){client=handler==null?new HttpClient():new HttpClient(handler);client.Timeout=TimeSpan.FromSeconds(8);client.MaxResponseContentBufferSize=4000000;foreach(var r in Collector.Book.Records){signatures[r.Id]=Signature(r);persisted[r.Id]=r.UpdatedUtc;}}
    public async Task Fetch(int timingId){
        if(disposed||busy||timingId<=0||DateTime.UtcNow<nextFetch)return;busy=true;nextFetch=DateTime.UtcNow.AddSeconds(15);
        try{
            string endpoint="https://api.gtcracecenter.com/livetiming/snapshot/"+timingId;
            using(var response=await client.GetAsync(endpoint,HttpCompletionOption.ResponseContentRead,cancellation.Token)){
                response.EnsureSuccessStatusCode();string json=await response.Content.ReadAsStringAsync();
                var snapshot=new JavaScriptSerializer{MaxJsonLength=4000000}.Deserialize<DriverTimingSnapshot>(json);if(disposed)return;Collector.Remote(snapshot,timingId,DateTime.UtcNow);Error=null;Save();
            }
        }catch(OperationCanceledException){if(!disposed)Error="A consulta do histórico demorou demasiado. Os dados guardados foram mantidos.";}catch(Exception){if(!disposed)Error="Histórico GTC indisponível. Mantidos os dados guardados e a recolha local.";}finally{busy=false;}
    }
    public void Save(bool force=false){
        if(!force&&DateTime.UtcNow-lastSave<TimeSpan.FromSeconds(5))return;
        foreach(var record in Collector.Book.Records){string signature=Signature(record);string old;DateTime saved;persisted.TryGetValue(record.Id,out saved);if(!signatures.TryGetValue(record.Id,out old)||old!=signature||force&&record.UpdatedUtc>saved){DriverHistoryStore.Save(record);signatures[record.Id]=signature;persisted[record.Id]=record.UpdatedUtc;}}
        lastSave=DateTime.UtcNow;
    }
    static string Signature(DriverHistoryRecord record){var last=record.Visits.LastOrDefault();return record.DataRevision+"|"+record.Laps.Count+"|"+record.TotalLaps+"|"+record.Visits.Count+"|"+(last==null?"":Convert.ToString(last.ExitAfterLap))+"|"+record.LastPitCount+"|"+record.LastInPits;}
    public void Dispose(){disposed=true;cancellation.Cancel();client.Dispose();cancellation.Dispose();}
}
