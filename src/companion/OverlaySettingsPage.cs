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
using System.IO;
using System.Linq;
using System.Windows.Forms;

public sealed partial class CompanionWindow {
    public void ShowOverlaySettings(int tab=0){((Action<int>)Tag)(3);selectOverlayTab(tab);}
    bool syncingOverlay;
    Action refreshOverlay;
    Action<int> selectOverlayTab;
    Action<bool> overlayStintLayout;
    int currentOverlayTab;
    readonly System.Windows.Forms.Timer overlayTimer=new System.Windows.Forms.Timer{Interval=500};
    string OverlayRoot{get{return CompetitionProfiles.ControlDirectory(settings);}}
    Preferences ReadOverlay(){return CompanionStore.Read<Preferences>(Path.Combine(CompetitionProfiles.PreferencesDirectory,"settings.json"))??new Preferences{Player=settings.Driver};}
    void SendOverlay(OverlayPatch patch,bool forceQueue=false){
        if(syncingOverlay||building||PreviewOnly)return;
        try{
            Directory.CreateDirectory(OverlayRoot);Directory.CreateDirectory(CompetitionProfiles.PreferencesDirectory);
            if(forceQueue||LaunchEngine.Running("GTC-Timing")){
                string directory=Path.Combine(OverlayRoot,"commands");Directory.CreateDirectory(directory);
                string target=Path.Combine(directory,DateTime.UtcNow.Ticks.ToString("D19")+"-"+Guid.NewGuid().ToString("N")+".json");
                File.WriteAllText(target+".tmp",CompanionStore.Json.Serialize(patch));File.Move(target+".tmp",target);
            }else{
                var prefs=ReadOverlay();patch.Apply(prefs);prefs.Player=settings.Driver;CompanionStore.Write(Path.Combine(CompetitionProfiles.PreferencesDirectory,"settings.json"),prefs);
            }
            Report("Definições do overlay guardadas automaticamente.");
        }catch(Exception){Report("Não foi possível guardar as definições do overlay.");}
    }
    void OpenOverlay(bool move){
        StopCountdown();ReadInputs();if(string.IsNullOrWhiteSpace(settings.Driver)){Report("Indica primeiro o teu nome de piloto.");return;}
        try{LaunchEngine.StartOverlay(settings,true);if(move)SendOverlay(new OverlayPatch{Locked=false,Visible=true},true);else SendOverlay(new OverlayPatch{Visible=true},true);ShowSharedStint(move);var keys=OverlayShortcuts.Normalize(ReadOverlay().Shortcuts);Report(move?"Arrasta os overlays. "+(keys.Move.Enabled?keys.Move+" termina o ajuste.":"Usa o botão Concluir para terminar."):"Overlays abertos para pré-visualização.");}
        catch(Exception ex){Report(ex.Message);}
    }
    void CreateOverlayPage(TableLayoutPanel stack,Panel stintPage){
        var prefs=ReadOverlay();syncingOverlay=true;
        stack.Dock=DockStyle.Fill;stack.AutoSize=false;stack.RowCount=3;stack.RowStyles.Clear();stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));stack.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var tabs=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Top,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Margin=new Padding(0,0,0,P(12))};stack.Controls.Add(tabs);
        var host=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};stack.Controls.Add(host);
        var pages=new Panel[5];var contents=new TableLayoutPanel[5];var nav=new Button[5];string[] names={"Aparência","Classificação","Estado","Atalhos","Stint"};
        for(int i=0;i<5;i++){
            int index=i;pages[i]=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Visible=false};contents[i]=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,Margin=Padding.Empty};contents[i].ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));pages[i].Controls.Add(contents[i]);host.Controls.Add(pages[i]);
            nav[i]=Button(names[i],104);nav[i].Margin=new Padding(0,0,P(5),0);tabs.Controls.Add(nav[i]);nav[i].Click+=(s,e)=>selectOverlayTab(index);
        }
        pages[4].Controls.Clear();pages[4].AutoScroll=false;stintPage.Dock=DockStyle.Fill;stintPage.Visible=true;pages[4].Controls.Add(stintPage);
        selectOverlayTab=index=>{currentOverlayTab=index;for(int i=0;i<5;i++){pages[i].Visible=i==index;nav[i].BackColor=i==index?GtcTheme.AccentSurface:GtcTheme.Input;nav[i].ForeColor=i==index?GtcTheme.Accent:GtcTheme.Text;}pages[index].BringToFront();if(currentPage==3){if(overlayStintLayout!=null)overlayStintLayout(index==4);ResizeForPage(3);}if(refreshOverlay!=null)refreshOverlay();};
        Func<int,string,TableLayoutPanel> card=(page,title)=>{
            var c=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,RowCount=1,Padding=new Padding(P(12)),Margin=new Padding(0,0,0,P(12)),BackColor=GtcTheme.Surface};c.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,54));c.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,46));c.RowStyles.Add(new RowStyle(SizeType.Absolute,P(23)));
            var label=TextLabel(title,11,true);label.ForeColor=GtcTheme.Muted;c.Controls.Add(label,0,0);c.SetColumnSpan(label,2);contents[page].Controls.Add(c);return c;
        };
        Action<TableLayoutPanel,string,Control> row=(cardPanel,title,control)=>{
            int index=cardPanel.RowCount++;cardPanel.RowStyles.Add(new RowStyle(SizeType.Absolute,P(36)));var label=TextLabel(title,12);label.Anchor=AnchorStyles.Left;label.MaximumSize=new Size(P(235),0);cardPanel.Controls.Add(label,0,index);control.Anchor=AnchorStyles.Right;control.Margin=Padding.Empty;control.AccessibleName=title;cardPanel.Controls.Add(control,1,index);
        };
        Func<int,int,int,NumericUpDown> number=(value,min,max)=>new NumericUpDown{Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,value)),Width=P(78),Font=F(13),TextAlign=HorizontalAlignment.Right,BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,BorderStyle=BorderStyle.FixedSingle};
        Func<bool,CheckBox> check=value=>new GtcSettingsCheck{Checked=value,AutoSize=false,Size=new Size(P(24),P(24)),BackColor=GtcTheme.Surface,Cursor=Cursors.Hand};
        Func<object[],int,ComboBox> choice=(options,index)=>{
            var combo=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=P(22),Width=P(195),BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Font=F(12),FlatStyle=FlatStyle.Flat};combo.Items.AddRange(options);combo.SelectedIndex=Math.Max(0,Math.Min(options.Length-1,index));
            combo.DrawItem+=(s,e)=>{if(e.Index<0)return;using(var brush=new SolidBrush(GtcTheme.Input))e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,combo.Items[e.Index].ToString(),combo.Font,e.Bounds,combo.Enabled?GtcTheme.Text:GtcTheme.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);};return combo;
        };
        var appearance=card(0,"POSIÇÃO E TRANSPARÊNCIA");
        var position=choice(new object[]{"Personalizada","Superior esquerdo","Superior centro","Superior direito","Centro esquerdo","Centro","Centro direito","Inferior esquerdo","Inferior centro","Inferior direito"},prefs.PositionAnchor);
        row(appearance,"Posição no ecrã",position);position.SelectedIndexChanged+=(s,e)=>SendOverlay(new OverlayPatch{PositionAnchor=position.SelectedIndex});
        var sliderPanel=new TableLayoutPanel{ColumnCount=2,RowCount=1,Width=P(195),Height=P(34),Margin=Padding.Empty};sliderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));sliderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,P(43)));
        var percent=TextLabel((100-prefs.OpacityPercent)+"%",12);percent.Dock=DockStyle.Fill;percent.TextAlign=ContentAlignment.MiddleRight;
        var slider=new GtcSettingsSlider{Minimum=0,Maximum=90,Value=Math.Max(0,Math.Min(90,100-prefs.OpacityPercent)),Dock=DockStyle.Fill,BackColor=GtcTheme.Surface};sliderPanel.Controls.Add(slider,0,0);sliderPanel.Controls.Add(percent,1,0);row(appearance,"Transparência",sliderPanel);
        slider.ValueChanged+=(s,e)=>{percent.Text=slider.Value+"%";SendOverlay(new OverlayPatch{OpacityPercent=100-slider.Value});if(!syncingOverlay&&!building&&!PreviewOnly)SetSharedStintOpacity(100-slider.Value);};
        var actions=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Top,FlowDirection=FlowDirection.LeftToRight,WrapContents=true,Margin=new Padding(0,0,0,P(10))};contents[0].Controls.Add(actions);
        var preview=Button("Mostrar overlay",150);preview.Margin=new Padding(0,0,P(8),0);actions.Controls.Add(preview);preview.Click+=(s,e)=>OpenOverlay(false);
        var move=Button("Mover overlay",150);actions.Controls.Add(move);bool unlocked=false;move.Click+=(s,e)=>{if(unlocked){SendOverlay(new OverlayPatch{Locked=true},true);SetSharedStintMove(false);}else OpenOverlay(true);};
        var movementHint=TextLabel("Arrasta cada overlay para uma posição personalizada.\nCtrl+Alt+O termina o ajuste; Ctrl+Alt+G muda a vista.",12);movementHint.ForeColor=GtcTheme.Muted;movementHint.MaximumSize=new Size(P(470),0);contents[0].Controls.Add(movementHint);
        var view=card(1,"VISTA COMPACTA");var compact=check(prefs.Compact);row(view,"Ativar vista compacta",compact);var above=number(prefs.PilotsAbove,0,10);var below=number(prefs.PilotsBelow,0,10);row(view,"Pilotos acima",above);row(view,"Pilotos abaixo",below);
        Action nearby=()=>{above.Enabled=compact.Checked;below.Enabled=compact.Checked;};nearby();compact.CheckedChanged+=(s,e)=>{nearby();SendOverlay(new OverlayPatch{Compact=compact.Checked});};above.ValueChanged+=(s,e)=>SendOverlay(new OverlayPatch{PilotsAbove=(int)above.Value});below.ValueChanged+=(s,e)=>SendOverlay(new OverlayPatch{PilotsBelow=(int)below.Value});
        var details=card(1,"VOLTAS E PRESENÇA EM PISTA");var lapComparison=choice(new object[]{"Recorde pessoal","Volta anterior"},prefs.LapComparison);row(details,"Comparar diferencial com",lapComparison);lapComparison.SelectedIndexChanged+=(s,e)=>SendOverlay(new OverlayPatch{LapComparison=lapComparison.SelectedIndex});var previousLap=check(prefs.ShowPreviousLap);row(details,"Mostrar volta anterior",previousLap);previousLap.CheckedChanged+=(s,e)=>SendOverlay(new OverlayPatch{ShowPreviousLap=previousLap.Checked});var idealLap=check(prefs.ShowIdealLap);row(details,"Mostrar volta ideal",idealLap);idealLap.CheckedChanged+=(s,e)=>SendOverlay(new OverlayPatch{ShowIdealLap=idealLap.Checked});var driverIdeals=check(prefs.ShowDriverIdealLaps);row(details,"Ideal observada de cada piloto",driverIdeals);driverIdeals.CheckedChanged+=(s,e)=>SendOverlay(new OverlayPatch{ShowDriverIdealLaps=driverIdeals.Checked});var activity=check(prefs.ShowDriverActivity);row(details,"Indicadores nos nomes",activity);activity.CheckedChanged+=(s,e)=>SendOverlay(new OverlayPatch{ShowDriverActivity=activity.Checked});
        var legend=TextLabel("Volta ideal: soma dos melhores setores registados pela app.\nInclui voltas válidas do histórico GTC e da telemetria local.\nPode não incluir melhores setores históricos não observados.\nVerde: em pista · laranja: boxes · cinzento: paddock.\nSem ponto: sem dados locais atuais desse piloto.",11);legend.ForeColor=GtcTheme.Muted;legend.MaximumSize=new Size(P(470),0);contents[1].Controls.Add(legend);
        var positions=card(1,"LUGARES GANHOS OU PERDIDOS");var movement=check(prefs.ShowPositionChanges);row(positions,"Mostrar variação",movement);
        var reference=choice(new object[]{"Início da sessão","Últimas X horas","Ontem às 00:00"},prefs.PositionReference);row(positions,"Comparar desde",reference);var hours=number(prefs.PositionReferenceHours<1?24:prefs.PositionReferenceHours,1,168);row(positions,"Número de horas",hours);
        Action references=()=>{reference.Enabled=movement.Checked;hours.Enabled=movement.Checked&&reference.SelectedIndex==1;};references();movement.CheckedChanged+=(s,e)=>{references();SendOverlay(new OverlayPatch{ShowPositionChanges=movement.Checked});};reference.SelectedIndexChanged+=(s,e)=>{references();SendOverlay(new OverlayPatch{PositionReference=reference.SelectedIndex});};hours.ValueChanged+=(s,e)=>SendOverlay(new OverlayPatch{PositionReferenceHours=(int)hours.Value});
        var update=card(2,"CLASSIFICAÇÃO E ATUALIZAÇÃO");var interval=number(prefs.RefreshSeconds,5,60);row(update,"Atualizar a cada (segundos)",interval);interval.ValueChanged+=(s,e)=>SendOverlay(new OverlayPatch{RefreshSeconds=(int)interval.Value});
        timing=number(settings.TimingId,0,1000000);row(update,"Identificador da tabela GTC",timing);
        var stateCard=card(2,"ESTADO DO OVERLAY");
        Func<string,Label> statusLabel=title=>{var label=TextLabel("—",12);label.AutoSize=false;label.Size=new Size(P(195),P(30));label.TextAlign=ContentAlignment.MiddleRight;row(stateCard,title,label);return label;};
        var state=statusLabel("Estado");var championship=statusLabel("Campeonato em execução");var last=statusLabel("Última consulta");var track=statusLabel("Pista");var live=statusLabel("Telemetria");var baseline=statusLabel("Referência de posições");
        CreateShortcutControls(contents[3]);
        DateTime nextDetails=DateTime.MinValue;
        refreshOverlay=()=>{
            if(syncingOverlay||IsDisposed)return;syncingOverlay=true;
            try{
                var current=ReadOverlay();position.SelectedIndex=Math.Max(0,Math.Min(9,current.PositionAnchor));if(trainingHud!=null&&!trainingHud.IsDisposed&&trainingHud.IsCompact!=current.Compact){trainingHud.SetCompact(current.Compact);ApplyTrainingPosition();}
                var shortcuts=OverlayShortcuts.Normalize(current.Shortcuts);movementHint.Text="Arrasta cada overlay para uma posição personalizada.\n"+(shortcuts.Move.Enabled?shortcuts.Move+" termina o ajuste.":"Usa Concluir para terminar o ajuste.")+(shortcuts.View.Enabled?" "+shortcuts.View+" muda a vista.":"");if(refreshShortcuts!=null)refreshShortcuts();
                if(!slider.Capture)slider.Value=Math.Max(0,Math.Min(90,100-current.OpacityPercent));
                lapComparison.SelectedIndex=Math.Max(0,Math.Min(1,current.LapComparison));previousLap.Checked=current.ShowPreviousLap;idealLap.Checked=current.ShowIdealLap;driverIdeals.Checked=current.ShowDriverIdealLaps;activity.Checked=current.ShowDriverActivity;compact.Checked=current.Compact;above.Value=Math.Max(0,Math.Min(10,current.PilotsAbove));below.Value=Math.Max(0,Math.Min(10,current.PilotsBelow));movement.Checked=current.ShowPositionChanges;reference.SelectedIndex=Math.Max(0,Math.Min(2,current.PositionReference));hours.Value=Math.Max(1,Math.Min(168,current.PositionReferenceHours));interval.Value=Math.Max(5,Math.Min(60,current.RefreshSeconds));nearby();references();
                var status=CompanionStore.Read<Dictionary<string,object>>(Path.Combine(OverlayRoot,"status.json"));bool running=LaunchEngine.Running("GTC-Timing");
                Func<string,string> value=key=>status!=null&&status.ContainsKey(key)&&status[key]!=null?Convert.ToString(status[key]):"—";
                unlocked=running&&value("locked").Equals("False",StringComparison.OrdinalIgnoreCase);move.Text=unlocked?"Terminar ajuste":"Mover overlay";
                state.Text=running?"Em execução":"Fechado";state.ForeColor=running?Color.FromArgb(90,228,165):GtcTheme.Muted;
                var active=CompetitionProfiles.RunningOverlay(settings);championship.Text=active==null?"—":active.Name;
                last.Text=running&&status!=null&&status.ContainsKey("updatedUtc")&&status["updatedUtc"] is DateTime?((DateTime)status["updatedUtc"]).ToLocalTime().ToString("HH:mm:ss"):"—";track.Text=value("track");live.Text=running?value("liveState"):"—";
                if(DateTime.UtcNow>=nextDetails){nextDetails=DateTime.UtcNow.AddSeconds(10);DateTime at=DateTime.MinValue;
                    if(current.PositionReference==0){var session=CompanionStore.Read<Dictionary<string,object>>(Path.Combine(OverlayRoot,"positions-session.json"));if(session!=null&&session.ContainsKey("BaselineUtc")&&session["BaselineUtc"] is DateTime)at=(DateTime)session["BaselineUtc"];}
                    else{var cache=CompanionStore.Read<CachedSnapshot>(Path.Combine(OverlayRoot,"cache.json"));var history=CompanionStore.Read<RankingHistory>(Path.Combine(OverlayRoot,"ranking-history-"+settings.TimingId+".json"));var point=history==null?null:history.Reference(cache==null?null:cache.Data,current.PositionReference,current.PositionReferenceHours,DateTime.UtcNow);if(point!=null)at=point.AtUtc;}
                    baseline.Text=at==DateTime.MinValue?"Histórico insuficiente":at.ToLocalTime().ToString("dd/MM HH:mm");
                }
            }finally{syncingOverlay=false;}
        };
        overlayTimer.Tick+=(s,e)=>{if(currentPage==3&&!PreviewOnly)refreshOverlay();};Shown+=(s,e)=>overlayTimer.Start();FormClosed+=(s,e)=>overlayTimer.Dispose();
        syncingOverlay=false;selectOverlayTab(0);
    }
}
