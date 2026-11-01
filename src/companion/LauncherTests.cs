// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public static class LauncherTests {
    sealed class GameApi:HttpMessageHandler {
        public int Navigation,JoinRequests,JoinStateRequests;
        public bool AlreadyJoined,Accepted,RejectJoin;
        public Uri Requested;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancel){
            string data;int code=200;
            if(request.RequestUri.AbsolutePath=="/navigation/state"){
                Navigation++;bool loading=Navigation<=2;
                data="{\"state\":{\"navigationState\":\"NAV_MAIN_MENU\",\"settingMode\":\""+(AlreadyJoined?"SETTING_MULTIPLAYER":"SETTING_SINGLEPLAYER")+"\"},\"loadingStatus\":{\"loading\":"+(loading?"true":"false")+"}}";
            }else if(request.RequestUri.AbsolutePath=="/rest/multiplayer/join/state"){JoinStateRequests++;data="\""+(Accepted||AlreadyJoined?"JOIN_JOINED_SERVER":"JOIN_NOT_JOINED")+"\"";}
            else if(request.RequestUri.AbsolutePath=="/rest/multiplayer/join"){
                if(Navigation<=2)throw new Exception("Pedido enviado antes do menu terminar de carregar");
                Requested=request.RequestUri;JoinRequests++;Accepted=!RejectJoin;code=RejectJoin?403:204;data="";
            }else throw new Exception("Rota inesperada");
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)code){Content=new StringContent(data)});
        }
    }
    public static void Run(string report){
        GtcTypography.Verify();
        EngineSettingsTests.Run();
        EngineIntegrationTests.Run();
        EngineBackgroundTests.Run();
        ShortcutTests.Run();
        CompetitionTests.Run();
        TelemetryPluginTests.Run();
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(report),"telemetry-plugin-tests.txt"),"OK: bloqueio com jogo aberto; instalação nova; backup da DLL e JSON; preservação de outros plugins e opções; ativação dos buffers; rejeição de JSON inválido e DLL inválida; prevenção de plugins concorrentes; cancelamento quando o jogo abre durante a preparação.");
        SynchronizationContext.SetSynchronizationContext(null);
        ComponentSetupTests.Run();
        TrainingTests.Run();
        DriverHistoryTests.Run();
        SourcePriorityTests.Run();
        LapAnalysisTests.Run();
        RecordSharingTests.Run();
        ReplaySelectionTests.Run();
        ReplayArchiveTests.Run();
        var position=new Preferences{X=1234,Y=876,PositionAnchor=0,Player="Teste"};new OverlayPatch{OpacityPercent=75,PilotsAbove=3,PositionReference=1,PositionReferenceHours=24}.Apply(position);
        if(position.X!=1234||position.Y!=876||position.PositionAnchor!=0||position.OpacityPercent!=75||position.PilotsAbove!=3)throw new Exception("Alterações do Companion não podem substituir a posição manual do overlay");
        var defaults=new LaunchSettings();
        if(!string.IsNullOrEmpty(defaults.ServerHost)||defaults.ServerPort!=0||!string.IsNullOrEmpty(defaults.ServerPassword))throw new Exception("A distribuição não pode incluir dados de acesso predefinidos");
        var access=new LaunchSettings();ServerAccess.ImportLink(access,"http://localhost:5397/rest/multiplayer/join?host=server.example.test&port=12345&password=example%26only%20test");
        if(access.ServerHost!="server.example.test"||access.ServerPort!=12345||access.ServerPassword!="example&only test")throw new Exception("Importação do link e caracteres escapados");
        foreach(string invalid in new[]{"https://untrusted.example.test/rest/multiplayer/join?host=x&port=12345","http://localhost:5397/rest/multiplayer/join?host=x&port=12345&host=y"}){
            bool rejected=false;try{ServerAccess.ImportLink(new LaunchSettings(),invalid);}catch(ArgumentException){rejected=true;}if(!rejected)throw new Exception("Link de entrada não permitido foi aceite");
        }
        string temp=Path.Combine(CompanionStore.Root,"test-fixture");Directory.CreateDirectory(Path.Combine(temp,"Bin64"));Directory.CreateDirectory(Path.Combine(temp,"UserData"));File.WriteAllText(Path.Combine(temp,"Bin64","rFactor2.exe"),"");
        var settings=new LaunchSettings{GameDirectory=temp,Driver="Piloto de teste",CrewChiefPath="missing.exe",TinyPedalPath="missing.exe",StartCrewChief=false,StartTinyPedal=false,ServerHost="server.example.test",ServerPort=12345,ServerPassword="a& b#?"};
        if(LaunchEngine.Validate(settings).Count!=0)throw new Exception("Aplicações opcionais desativadas não podem bloquear o arranque");
        settings.StartCrewChief=true;if(!LaunchEngine.Validate(settings).Any(x=>x.Contains("Crew Chief")))throw new Exception("Aplicação ativada em falta não foi detetada");settings.StartCrewChief=false;
        if(!settings.JoinUrl.EndsWith("password=a%26%20b%23%3F"))throw new Exception("Palavra-passe não está codificada como parâmetro");
        var api=new GameApi();int ready=0;var phases=new List<string>();using(var engine=new LaunchEngine(api,1,4,1)){
            string result=engine.Connect("http://localhost:5397",settings.JoinUrl,false,s=>{},()=>ready++,CancellationToken.None,phase=>phases.Add(phase),()=>true).GetAwaiter().GetResult();
            if(api.JoinRequests!=1||ready!=1||api.Navigation<4||!result.StartsWith("Ligado"))throw new Exception("Sequência de arranque/menu/entrada inválida");
            if(!phases.SequenceEqual(new[]{"process","menu","join","connected"}))throw new Exception("O progresso tem de distinguir processo aberto, menu, pedido e ligação confirmada");
        }
        var menuSettings=new LaunchSettings{GameDirectory=temp};
        if(LaunchEngine.Validate(menuSettings,false,false).Count!=0||LaunchEngine.Validate(menuSettings).Count==0)throw new Exception("Arranque normal deve funcionar sem piloto, campeonato ou acesso; o arranque de servidor continua a exigi-los");
        api=new GameApi();phases.Clear();using(var engine=new LaunchEngine(api,1,4,1)){
            string result=engine.WaitForMenu("http://localhost:5397",false,s=>{},CancellationToken.None,phase=>phases.Add(phase),()=>true).GetAwaiter().GetResult();
            if(api.Navigation<3||api.JoinRequests!=0||api.JoinStateRequests!=0||!result.Contains("menu")||!phases.SequenceEqual(new[]{"process","menu"}))throw new Exception("Arranque sem servidor tem de esperar pelo menu sem consultar ou pedir entrada no servidor");
        }
        api=new GameApi{AlreadyJoined=true};using(var engine=new LaunchEngine(api,1,4,1)){
            string result=engine.WaitForMenu("http://localhost:5397",true,s=>{},CancellationToken.None).GetAwaiter().GetResult();
            if(api.Navigation!=0||api.JoinRequests!=0||api.JoinStateRequests!=0||!result.Contains("sessão atual"))throw new Exception("Arranque normal deve preservar um jogo que já esteja aberto");
        }
        api=new GameApi();using(var engine=new LaunchEngine(api,1,4,1))using(var cancelMenu=new CancellationTokenSource()){
            cancelMenu.Cancel();bool stopped=false;try{engine.WaitForMenu("http://localhost:5397",false,s=>{},cancelMenu.Token).GetAwaiter().GetResult();}catch(OperationCanceledException){stopped=true;}
            if(!stopped||api.Navigation!=0||api.JoinRequests!=0)throw new Exception("Cancelar o arranque normal deve impedir quaisquer pedidos");
        }
        api=new GameApi{RejectJoin=true};phases.Clear();using(var engine=new LaunchEngine(api,1,4,1)){
            bool failed=false;try{engine.Connect("http://localhost:5397",settings.JoinUrl,false,s=>{},()=>{},CancellationToken.None,phase=>phases.Add(phase)).GetAwaiter().GetResult();}catch(InvalidOperationException){failed=true;}
            if(!failed||phases.Contains("connected")||api.JoinRequests!=1)throw new Exception("Um pedido recusado não pode aparecer como ligação concluída");
        }
        api=new GameApi{AlreadyJoined=true};using(var engine=new LaunchEngine(api,1,4,1)){
            string result=engine.Connect("http://localhost:5397",settings.JoinUrl,true,s=>{},()=>{},CancellationToken.None).GetAwaiter().GetResult();
            if(api.JoinRequests!=0||!result.Contains("já está ligado"))throw new Exception("Não deve voltar a entrar quando já está ligado");
        }
        api=new GameApi{AlreadyJoined=true};using(var engine=new LaunchEngine(api,1,4,1)){
            int overlays=0;string result=engine.Connect("http://localhost:5397",settings.JoinUrl,true,s=>{},()=>overlays++,CancellationToken.None,null,null,false).GetAwaiter().GetResult();
            if(overlays!=0||api.JoinRequests!=0||api.Navigation!=0||!result.Contains("sessão atual"))throw new Exception("Ligação existente sem registo local deve ser preservada sem falhar, navegar ou substituir o overlay");
        }
        api=new GameApi();using(var engine=new LaunchEngine(api,1,4,1))using(var cancel=new CancellationTokenSource()){
            cancel.Cancel();bool stopped=false;try{engine.Connect("http://localhost:5397",settings.JoinUrl,true,s=>{},()=>{},cancel.Token).GetAwaiter().GetResult();}catch(OperationCanceledException){stopped=true;}
            if(!stopped||api.JoinRequests!=0)throw new Exception("Cancelar tem de impedir o pedido de entrada");
        }
        // Personal settings must round-trip without shipping a preconfigured user.
        CompanionStore.Write("test-settings.json",settings);var restored=CompanionStore.Read<LaunchSettings>("test-settings.json");
        if(restored.Driver!=settings.Driver||restored.ServerPassword!=settings.ServerPassword||restored.StartTinyPedal)throw new Exception("Persistência de preferências por utilizador");
        File.WriteAllText(report,"OK: distribuição sem dados de acesso; importação do link; rejeição de links inválidos; apps opcionais; validação; codificação da palavra-passe; espera pelo menu; entrada confirmada; servidor já ligado; cancelamento; persistência; arranque sem servidor sem pedidos de entrada; menu sem piloto ou campeonato; sessão existente preservada.");
    }
}
