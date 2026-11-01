// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Linq;

public static class TrainingAssessmentTests {
    static void Check(bool value,string message){if(!value)throw new Exception("Nota do stint: "+message);}
    static TrainingRun Run(int count,double time,int exits=0){return new TrainingRun{State="completed",Options=new TrainingOptions{Target=80,Tolerance=.5,Amount=count},OffTracks=exits,Laps=Enumerable.Range(1,count).Select(n=>new TrainingLap{Number=n,Time=time,Valid=true,CompleteData=true}).ToList()};}
    public static void Run(){
        Check(!TrainingAssessment.Evaluate(null).Score.HasValue&&!TrainingAssessment.Evaluate(new TrainingRun()).Score.HasValue,"Não atribuir nota antes da primeira volta");
        Check(TrainingMath.WithinWindow(70,80,.5)&&TrainingMath.WithinWindow(80.5,80,.5)&&!TrainingMath.WithinWindow(80.501,80,.5),"Cumprir o limite superior, mesmo abaixo do inferior");
        foreach(double invalid in new[]{-1d,0,double.NaN,double.PositiveInfinity})Check(!TrainingMath.WithinWindow(invalid,80,.5),"Um tempo inválido não conta como volta rápida");
        var fast=Run(15,75);Check(fast.InWindow==15&&fast.FasterThanWindow==15&&TrainingAssessment.Evaluate(fast).Score==100,"Mais rápido mantém os 100 pontos e conta no objetivo");
        Check(TrainingAssessment.Evaluate(Run(15,80.5)).Score==100,"Média no limite lento recebe 70 pontos de ritmo");
        Check(TrainingAssessment.Evaluate(Run(15,82.5)).Score==65&&TrainingAssessment.Evaluate(Run(15,84.5)).Score==30,"Penalizar só a diferença acima do limite, até 5% do alvo");
        Check(TrainingAssessment.Evaluate(Run(15,80,1)).Score==93&&TrainingAssessment.Evaluate(Run(15,80,3)).Score==80,"Saídas estimadas normalizadas pela duração do stint");
        Check(TrainingAssessment.Evaluate(Run(10,80,1)).Score==TrainingAssessment.Evaluate(Run(20,80,2)).Score,"O mesmo número de saídas por volta produz a mesma nota");
        Check(TrainingAssessment.Evaluate(Run(10,200,10)).Score==0,"Limitar a nota entre zero e cem");
        var invalidLap=Run(2,80);invalidLap.Laps[1].Time=90;invalidLap.Laps[1].Valid=false;Check(TrainingAssessment.Evaluate(invalidLap).Score==30,"A média mantém as voltas inválidas");
        var lost=Run(10,80);lost.Laps[0].CompleteData=false;Check(TrainingAssessment.Evaluate(lost).Estimated&&TrainingAssessment.Evaluate(lost).Label.Contains("estimada"),"Assinalar a avaliação com telemetria incompleta");
        lost.Laps[0].Time=double.NaN;Check(!TrainingAssessment.Evaluate(lost).Score.HasValue,"Um tempo não finito não produz uma nota enganadora");
        var partial=Run(3,80);partial.State="running";Check(TrainingAssessment.Evaluate(partial).Label.Contains("provisória"),"Nota provisória durante o stint");partial.State="interrupted";Check(TrainingAssessment.Evaluate(partial).Label.Contains("parcial"),"Uma parte do objetivo não é um stint concluído");
        var old=Run(15,80);old.Laps[0].OffTracks=1;Check(TrainingAssessment.Evaluate(old).Score==93,"Recuperar saídas por volta nos registos antigos sem contar duas vezes");old.OffTracks=1;Check(TrainingAssessment.Evaluate(old).Score==93,"O total e as saídas por volta não se somam");
    }
}
