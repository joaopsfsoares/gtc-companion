// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;

public static class ReplaySelectionTests {
    static void Check(bool value,string message){if(!value)throw new Exception("Replays: "+message);}
    static DriverTimingSnapshot Snapshot(string session,int total,params DriverTimingLap[] laps){return new DriverTimingSnapshot{sourceSessionId=session,sessionInfo=new DriverTimingSession{trackName="Estoril",sessionCode=1},entries=new List<DriverTimingEntry>{new DriverTimingEntry{driverName="Outro Piloto",vehicleClass="Cup",lapsCompleted=total,lapHistory=new List<DriverTimingLap>(laps)}}};}
    static DriverTimingLap Lap(int number,double time,bool? invalid=false,bool? pit=false){return new DriverTimingLap{lapNumber=number,lapTime=time,invalid=invalid,pit=pit};}
    public static void Run(){
        var selection=new ReplaySelection();
        var presence=Snapshot("a",0);presence.entries[0].inPits=true;
        Check(!ReplaySelection.HasOtherCarOnTrack(presence,"Meu Nome"),"piloto nas boxes não ativa gravação");
        presence.entries[0].inPits=null;Check(!ReplaySelection.HasOtherCarOnTrack(presence,"Meu Nome"),"estado desconhecido não ativa gravação");
        presence.entries[0].inPits=false;Check(ReplaySelection.HasOtherCarOnTrack(presence,"Meu Nome"),"piloto em pista ativa deteção");
        Check(!ReplaySelection.HasOtherCarOnTrack(presence,"Outro Piloto"),"o próprio piloto não ativa gravação");
        Check(selection.Observe(Snapshot("a",4,Lap(4,90)),100,"Meu Nome").Count==0,"voltas anteriores ao arranque ignoradas");
        Check(selection.Observe(Snapshot("a",5,Lap(5,95)),100,"Meu Nome").Count==0,"ignorar volta que pode ter começado antes da gravação");
        var candidates=selection.Observe(Snapshot("a",6,Lap(6,95)),100,"Meu Nome");
        Check(candidates.Count==1&&candidates[0].Time==95&&candidates[0].LapNumber==6,"selecionar volta nova mais rápida que o utilizador");
        selection.SetSaved("Estoril","Outro Piloto","Cup",95);
        Check(selection.Observe(Snapshot("a",7,Lap(7,96)),100,"Meu Nome").Count==0,"manter melhor replay existente");
        Check(selection.Observe(Snapshot("a",8,Lap(8,94,true)),100,"Meu Nome").Count==0,"não guardar volta inválida");
        Check(selection.Observe(Snapshot("a",9,Lap(9,93,null)),100,"Meu Nome").Count==0,"não assumir que volta sem validade é válida");
        Check(selection.Observe(Snapshot("a",10,Lap(10,92,false,true)),100,"Meu Nome").Count==0,"não guardar volta de boxes");
        Check(selection.Observe(Snapshot("a",11,Lap(11,94)),100,"Meu Nome").Count==1,"aceitar melhoria do mesmo piloto");
        Check(selection.Observe(Snapshot("b",1,Lap(1,90)),100,"Meu Nome").Count==0,"nova sessão começa com referência limpa");
        Check(selection.Observe(Snapshot("b",2,Lap(2,90)),null,"Meu Nome").Count==0,"sem melhor volta de referência não inferir comparação");
        var delayed=new ReplaySelection();delayed.Observe(Snapshot("c",0),100,"Meu Nome");delayed.Observe(Snapshot("c",1),100,"Meu Nome");
        Check(delayed.Observe(Snapshot("c",2),100,"Meu Nome").Count==0,"aguardar dados de volta em atraso");
        Check(delayed.Observe(Snapshot("c",2,Lap(2,88)),100,"Meu Nome").Count==1,"aceitar detalhes que chegaram depois do contador");
    }
}
