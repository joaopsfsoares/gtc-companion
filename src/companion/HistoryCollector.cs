// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public sealed class HistoryCollector:IDisposable {
    readonly HttpClient http=new HttpClient{Timeout=TimeSpan.FromSeconds(10)};
    readonly CancellationTokenSource cancel=new CancellationTokenSource();
    bool busy;
    public async Task Collect(LaunchSettings settings){
        if(busy||!settings.Configured)return;busy=true;
        try{
            foreach(var profile in settings.Competitions.Where(p=>p.TimingId>0).Select(p=>p.Copy()).ToArray()){
            try{int id=profile.TimingId;
            using(var response=await http.GetAsync("https://api.gtcracecenter.com/livetiming/configs/"+id+"/leaderboard/snapshot",cancel.Token)){
                response.EnsureSuccessStatusCode();var snapshot=CompanionStore.Json.Deserialize<TimingSnapshot>(await response.Content.ReadAsStringAsync());
                if(snapshot==null||snapshot.activeRound==null||snapshot.rows==null||string.IsNullOrWhiteSpace(snapshot.activeRound.trackName))throw new InvalidDataException();
                string directory=CompetitionProfiles.DataDirectory(profile);Directory.CreateDirectory(directory);string file=Path.Combine(directory,"ranking-history-"+id+".json");RankingHistory history=null;
                try{history=CompanionStore.Json.Deserialize<RankingHistory>(File.ReadAllText(file));}catch{}
                history=history??new RankingHistory();if(history.Record(snapshot,TimingData.Sorted(snapshot),DateTime.UtcNow))CompanionStore.Write(file,history);
            }
            }catch(OperationCanceledException){throw;}catch(Exception){CompanionStore.Log("Histórico: classificação temporariamente indisponível.");}
            }
        }catch(OperationCanceledException){}catch(Exception){CompanionStore.Log("Histórico: não foi possível recolher a classificação GTC.");}finally{busy=false;}
    }
    public void Dispose(){cancel.Cancel();http.Dispose();cancel.Dispose();}
}
