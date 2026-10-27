// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
public sealed partial class CompanionWindow {
    FlowLayoutPanel progressSteps;
    readonly Dictionary<string,string> launchStates=new Dictionary<string,string>();
    readonly Dictionary<string,Label> launchLabels=new Dictionary<string,Label>();
    Action<bool> progressLayout;
    string launchState="idle";
    bool launchJoinServer=true;
    DateTime launchStarted;
    int currentPage;
    void ResizeForPage(int index){
        int[] overlayHeights={530,630,650,650};int[] heights={700,540,linkEditing?450:420,overlayHeights[currentOverlayTab],720,720,800,700};int height=heights[Math.Max(0,Math.Min(7,index))]+(progressSteps!=null&&progressSteps.Visible?52:0);
        var area=Screen.FromControl(this).WorkingArea;ClientSize=new Size(Math.Max(P(720),ClientSize.Width),Math.Min(P(height),area.Height-P(50)));
        if(IsHandleCreated&&Location.X>-10000&&Location.Y>-10000)Location=new Point(Math.Max(area.Left,Math.Min(Left,area.Right-Width)),Math.Max(area.Top,Math.Min(Top,area.Bottom-Height)));
    }
    void CreateProgress(Panel footer,Action<bool> layout){
        progressLayout=layout;progressSteps=new FlowLayoutPanel{Location=new Point(P(24),P(52)),Height=P(45),AutoSize=false,WrapContents=false,FlowDirection=FlowDirection.LeftToRight,Visible=false,Margin=Padding.Empty};footer.Controls.Add(progressSteps);
        string[] ids={"crew","tiny","game","server","overlay"},names={"Crew Chief","TinyPedal","rFactor 2","Servidor","Overlay"};
        for(int i=0;i<ids.Length;i++){var label=new Label{Text=names[i]+"\nA aguardar",Font=F(11),ForeColor=GtcTheme.Muted,Size=new Size(P(126),P(42)),Margin=new Padding(0,0,P(5),0),Tag=names[i]};launchLabels.Add(ids[i],label);progressSteps.Controls.Add(label);}
    }
    void SetLaunchStep(string id,string state){
        launchStates[id]=state;Label label;if(launchLabels.TryGetValue(id,out label)){
            string caption=state=="ready"?"Pronto":state=="opened"?"Aberto":state=="working"?(id=="server"?"A ligar…":"A abrir…"):state=="off"?"Desativado":state=="not-requested"?"Não solicitado":state=="existing"?"Já ligado":state=="action"?"Confirmar no jogo":state=="failed"?"Falhou":state=="cancelled"?"Cancelado":"A aguardar";
            label.Text=label.Tag+"\n"+caption;label.ForeColor=state=="ready"||state=="existing"?Color.FromArgb(90,228,165):state=="failed"?Color.FromArgb(255,112,126):state=="working"||state=="opened"||state=="action"?GtcTheme.Accent:GtcTheme.Muted;
        }
        PersistLaunchState();
    }
    void BeginLaunchProgress(){
        launchState="running";launchStarted=DateTime.UtcNow;launchStates.Clear();progressSteps.Visible=true;progressLayout(true);
        foreach(string id in new[]{"crew","tiny","game","server","overlay"})SetLaunchStep(id,!launchJoinServer&&id=="server"?"not-requested":!launchJoinServer&&id=="overlay"||id=="crew"&&!settings.StartCrewChief||id=="tiny"&&!settings.StartTinyPedal?"off":"pending");
    }
    void PersistLaunchState(){
        if(PreviewOnly||launchStates.Count==0)return;
        try{CompanionStore.Write("launcher-state.json",new{state=launchState,mode=launchJoinServer?"server":"menu",startedUtc=launchStarted,updatedUtc=DateTime.UtcNow,steps=launchStates,message=message.Text,running=starting});}catch{}
    }
    public void RestoreProgressPreview(){
        var saved=CompanionStore.Read<Dictionary<string,object>>("launcher-state.json");if(saved==null)return;BeginLaunchProgress();
        object values;if(saved.TryGetValue("steps",out values)){var steps=values as Dictionary<string,object>;if(steps!=null)foreach(var pair in steps)SetLaunchStep(pair.Key,Convert.ToString(pair.Value));}
        if(saved.ContainsKey("message"))Report(Convert.ToString(saved["message"]));
    }
}
