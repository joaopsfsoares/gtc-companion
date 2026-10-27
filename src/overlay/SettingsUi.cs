// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UiLabel=System.Windows.Forms.Label;

public sealed class GtcSettingsWindow : Form {
    public bool PreviewOnly;
    protected override bool ShowWithoutActivation {get{return PreviewOnly;}}
}
public sealed partial class TimingOverlay {
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    Form CreateSettingsWindow() {
        Func<int,int> px=n=>(int)Math.Round(n*scale);
        Color background=GtcTheme.Background,surface=GtcTheme.Surface,input=GtcTheme.Input,line=GtcTheme.Border;
        var fonts=new List<Font>();
        Func<int,bool,Font> font=(size,bold)=>{var f=GtcTypography.Create(size*scale,bold);fonts.Add(f);return f;};
        Font normal=font(13,false),small=font(11,false),section=font(11,true),heading=font(22,true);
        var dialog=new GtcSettingsWindow {Text="Overlay GTC — Definições",Icon=GtcBrand.Icon(),StartPosition=FormStartPosition.CenterScreen,
            ClientSize=new Size(px(800),px(620)),MinimumSize=new Size(px(760),px(580)),TopMost=true,
            AutoScaleMode=AutoScaleMode.None,Font=normal,BackColor=background,ForeColor=Ink};
        dialog.HandleCreated+=(s,e)=>{try{int dark=1;DwmSetWindowAttribute(dialog.Handle,20,ref dark,4);}catch{}};
        Func<string,Font,Color,UiLabel> text=(value,f,c)=>new UiLabel{Text=value,Font=f,ForeColor=c,BackColor=Color.Transparent,AutoSize=true};
        Func<string,int,bool,Button> button=(value,width,primary)=>{
            var b=new Button {Text=value,Size=new Size(px(width),px(38)),FlatStyle=FlatStyle.Flat,
                BackColor=primary?GtcOrange:input,ForeColor=primary?background:Ink,Font=font(12,true),
                Cursor=Cursors.Hand,UseVisualStyleBackColor=false,Margin=Padding.Empty};
            b.FlatAppearance.BorderSize=primary?0:1;b.FlatAppearance.BorderColor=line;
            b.FlatAppearance.MouseOverBackColor=primary?GtcTheme.AccentHover:GtcTheme.Hover;return b;
        };
        var root=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Margin=Padding.Empty,Padding=Padding.Empty};
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,px(94)));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,px(64)));dialog.Controls.Add(root);
        var header=new Panel {Dock=DockStyle.Fill,Margin=Padding.Empty};root.Controls.Add(header,0,0);
        var brand=text("OVERLAY GTC",section,GtcOrange);brand.Location=new Point(px(24),px(13));header.Controls.Add(brand);
        var title=text("Definições",heading,Ink);title.Location=new Point(px(24),px(34));header.Controls.Add(title);
        var table=button("Tabela GTC ↗",115,false);var join=button("Entrar no servidor",160,true);header.Controls.Add(table);header.Controls.Add(join);
        Action headerLayout=()=>{join.Location=new Point(header.ClientSize.Width-px(24)-join.Width,px(30));table.Location=new Point(join.Left-px(10)-table.Width,px(30));};
        header.Resize+=(s,e)=>headerLayout();headerLayout();header.Paint+=(s,e)=>{using(var pen=new Pen(line))e.Graphics.DrawLine(pen,0,header.Height-1,header.Width,header.Height-1);};
        var footer=new Panel {Dock=DockStyle.Fill,Margin=Padding.Empty};root.Controls.Add(footer,0,2);
        var status=new UiLabel {Text="Alterações guardadas automaticamente.",Font=small,ForeColor=Muted,Location=new Point(px(24),px(10)),TextAlign=ContentAlignment.MiddleLeft};
        var done=button("Concluído",106,false);footer.Controls.Add(status);footer.Controls.Add(done);done.Click+=(s,e)=>dialog.Close();
        dialog.AcceptButton=done;dialog.CancelButton=done;footer.TabIndex=2;
        Action footerLayout=()=>{done.Location=new Point(footer.ClientSize.Width-px(24)-done.Width,px(13));status.Size=new Size(Math.Max(px(100),done.Left-px(42)),px(45));};
        footer.Resize+=(s,e)=>footerLayout();footerLayout();footer.Paint+=(s,e)=>{using(var pen=new Pen(line))e.Graphics.DrawLine(pen,0,0,footer.Width,0);};
        join.Click+=async(s,e)=>await JoinGtcServer(join,status);
        table.Click+=(s,e)=>{try{Process.Start(new ProcessStartInfo("https://esports.gtcracecenter.com/livetiming/110"){UseShellExecute=true});}catch{status.Text="Não foi possível abrir o navegador.";}};
        var body=new TableLayoutPanel {Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty,Padding=Padding.Empty};
        body.TabIndex=1;
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,px(162)));body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));body.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.Controls.Add(body,0,1);
        var sidebar=new Panel {Dock=DockStyle.Fill,BackColor=GtcTheme.Sidebar,Padding=new Padding(px(12),px(20),px(12),px(16)),Margin=Padding.Empty};body.Controls.Add(sidebar,0,0);
        var navigation=new FlowLayoutPanel {Dock=DockStyle.Top,AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=Padding.Empty,Padding=Padding.Empty};sidebar.Controls.Add(navigation);
        var keys=new UiLabel {Dock=DockStyle.Bottom,Height=px(105),Font=small,ForeColor=Muted,Text="ATALHOS\n\nVista   Ctrl+Alt+G\nMover   Ctrl+Alt+O\nOcultar Ctrl+Alt+H",BackColor=Color.Transparent};sidebar.Controls.Add(keys);
        var host=new Panel {Dock=DockStyle.Fill,Padding=new Padding(px(24),px(18),px(24),px(12)),Margin=Padding.Empty};body.Controls.Add(host,1,0);
        var pages=new List<Panel>();var stacks=new List<TableLayoutPanel>();var navButtons=new List<Button>();
        string[] names={"Aparência","Classificação","Sessão"};
        for(int i=0;i<names.Length;i++){
            var page=new Panel {Dock=DockStyle.Fill,AutoScroll=true,Visible=false,Margin=Padding.Empty};
            var stack=new TableLayoutPanel {Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,Margin=Padding.Empty,Padding=Padding.Empty};stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            var pageTitle=text(names[i],font(18,true),Ink);pageTitle.Margin=new Padding(0,0,0,px(17));stack.Controls.Add(pageTitle);
            page.Controls.Add(stack);host.Controls.Add(page);pages.Add(page);stacks.Add(stack);
            var nav=button(names[i],138,false);nav.Height=px(42);nav.TextAlign=ContentAlignment.MiddleLeft;nav.Padding=new Padding(px(12),0,0,0);nav.Margin=new Padding(0,0,0,px(7));nav.FlatAppearance.BorderSize=0;navigation.Controls.Add(nav);navButtons.Add(nav);
        }
        Action<int> selectPage=index=>{
            for(int i=0;i<pages.Count;i++){pages[i].Visible=i==index;navButtons[i].BackColor=i==index?GtcTheme.AccentSurface:sidebar.BackColor;navButtons[i].ForeColor=i==index?GtcOrange:Muted;}
            pages[index].BringToFront();
        };
        for(int i=0;i<navButtons.Count;i++){int index=i;navButtons[i].Click+=(s,e)=>selectPage(index);}
        dialog.Tag=selectPage;
        Func<int,string,TableLayoutPanel> card=(page,name)=>{
            var c=new TableLayoutPanel {AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,ColumnCount=2,RowCount=1,Padding=new Padding(px(16),px(12),px(16),px(10)),Margin=new Padding(0,0,0,px(12)),Dock=DockStyle.Top,BackColor=surface};
            c.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,58));c.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,42));
            var caption=text(name,section,Muted);caption.Margin=new Padding(0,0,0,px(8));c.Controls.Add(caption,0,0);c.SetColumnSpan(caption,2);c.RowStyles.Add(new RowStyle(SizeType.Absolute,px(30)));
            c.Paint+=(s,e)=>{using(var pen=new Pen(line))e.Graphics.DrawRectangle(pen,0,0,c.ClientSize.Width-1,c.ClientSize.Height-1);};stacks[page].Controls.Add(c);return c;
        };
        Action<TableLayoutPanel,string,string,Control> row=(cardPanel,label,hint,control)=>{
            int idx=cardPanel.RowCount;cardPanel.RowCount=idx+1;cardPanel.RowStyles.Add(new RowStyle(SizeType.Absolute,px(string.IsNullOrEmpty(hint)?46:62)));
            var labels=new TableLayoutPanel {ColumnCount=1,RowCount=string.IsNullOrEmpty(hint)?1:2,AutoSize=true,Anchor=AnchorStyles.Left,Margin=new Padding(0,0,px(10),0),Padding=Padding.Empty};
            labels.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));var main=text(label,normal,Ink);main.Margin=Padding.Empty;labels.Controls.Add(main,0,0);
            if(!string.IsNullOrEmpty(hint)){var description=text(hint,small,Muted);description.MaximumSize=new Size(px(270),0);description.Margin=new Padding(0,px(4),0,0);labels.Controls.Add(description,0,1);}
            cardPanel.Controls.Add(labels,0,idx);control.Anchor=AnchorStyles.Right;control.Margin=Padding.Empty;control.AccessibleName=label;cardPanel.Controls.Add(control,1,idx);
        };
        Func<bool,CheckBox> check=value=>new GtcSettingsCheck{Checked=value,AutoSize=false,Size=new Size(px(24),px(24)),BackColor=surface,ForeColor=Ink,Cursor=Cursors.Hand,Text=""};
        Func<int,int,int,NumericUpDown> number=(value,min,max)=>new NumericUpDown {Value=value,Minimum=min,Maximum=max,Width=px(82),Font=normal,TextAlign=HorizontalAlignment.Right,BackColor=input,ForeColor=Ink,BorderStyle=BorderStyle.FixedSingle};
        var placement=card(0,"POSIÇÃO");
        var position=new ComboBox {DropDownStyle=ComboBoxStyle.DropDownList,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=px(22),Width=px(202),FlatStyle=FlatStyle.Flat,Font=normal,BackColor=input,ForeColor=Ink};
        position.Items.AddRange(new object[]{"Personalizada","Superior esquerdo","Superior centro","Superior direito","Centro esquerdo","Centro","Centro direito","Inferior esquerdo","Inferior centro","Inferior direito"});position.SelectedIndex=settings.PositionAnchor;
        position.DrawItem+=(s,e)=>{if(e.Index<0)return;bool selected=(e.State&DrawItemState.Selected)!=0;using(var fill=new SolidBrush(selected?GtcTheme.AccentSurface:input))e.Graphics.FillRectangle(fill,e.Bounds);TextRenderer.DrawText(e.Graphics,position.Items[e.Index].ToString(),normal,e.Bounds,selected?GtcOrange:Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);};
        position.SelectedIndexChanged+=(s,e)=>{settings.PositionAnchor=position.SelectedIndex;ApplyPosition();SaveSettings();};row(placement,"Posição no ecrã",null,position);
        var unlock=check(!locked);unlock.CheckedChanged+=(s,e)=>{if(locked==unlock.Checked)ToggleLock();};row(placement,"Mover livremente","Desbloqueia para arrastar a tabela.",unlock);
        var appearance=card(0,"TRANSPARÊNCIA");
        var transparencyPanel=new TableLayoutPanel {ColumnCount=2,RowCount=1,Width=px(202),Height=px(40),Margin=Padding.Empty};transparencyPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));transparencyPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,px(43)));
        var percent=new UiLabel {Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleRight,Text=(100-settings.OpacityPercent)+"%",Font=normal,ForeColor=Ink,Margin=Padding.Empty};
        var transparency=new GtcSettingsSlider {Dock=DockStyle.Fill,Minimum=0,Maximum=90,Value=100-settings.OpacityPercent,Margin=Padding.Empty,BackColor=surface,AccessibleName="Transparência"};
        transparencyPanel.Controls.Add(transparency,0,0);transparencyPanel.Controls.Add(percent,1,0);
        transparency.ValueChanged+=(s,e)=>{settings.OpacityPercent=100-transparency.Value;Opacity=settings.OpacityPercent/100d;percent.Text=transparency.Value+"%";SaveSettings();};row(appearance,"Transparência","0% opaco · 90% transparente",transparencyPanel);
        var view=card(1,"VISTA COMPACTA");
        var compact=check(settings.Compact);row(view,"Ativar vista compacta","Mostra a tua posição e os pilotos próximos.",compact);
        var above=number(settings.PilotsAbove,0,10);var below=number(settings.PilotsBelow,0,10);
        row(view,"Pilotos acima",null,above);row(view,"Pilotos abaixo",null,below);
        Action updateNearby=()=>{above.Enabled=compact.Checked;below.Enabled=compact.Checked;};
        compact.CheckedChanged+=(s,e)=>{if(settings.Compact!=compact.Checked)ToggleView();updateNearby();};updateNearby();
        above.ValueChanged+=(s,e)=>{settings.PilotsAbove=(int)above.Value;ResizeOverlay();SaveSettings();Invalidate();};
        below.ValueChanged+=(s,e)=>{settings.PilotsBelow=(int)below.Value;ResizeOverlay();SaveSettings();Invalidate();};
        var lapsCard=card(1,"VOLTAS E PRESENÇA");var comparison=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=px(202),Font=normal,BackColor=input,ForeColor=Ink,FlatStyle=FlatStyle.Flat};comparison.Items.AddRange(new object[]{"Recorde pessoal","Volta anterior"});comparison.SelectedIndex=Math.Max(0,Math.Min(1,settings.LapComparison));row(lapsCard,"Comparar diferencial com",null,comparison);comparison.SelectedIndexChanged+=(s,e)=>{settings.LapComparison=comparison.SelectedIndex;SaveSettings();UpdateLive();};var previousLap=check(settings.ShowPreviousLap);row(lapsCard,"Mostrar volta anterior","Mantém a última volta concluída debaixo da atual.",previousLap);previousLap.CheckedChanged+=(s,e)=>{settings.ShowPreviousLap=previousLap.Checked;ResizeOverlay();SaveSettings();Invalidate();};var idealLap=check(settings.ShowIdealLap);row(lapsCard,"Mostrar volta ideal","Soma dos teus melhores setores registados.",idealLap);idealLap.CheckedChanged+=(s,e)=>{settings.ShowIdealLap=idealLap.Checked;ResizeOverlay();SaveSettings();Invalidate();};var driverIdeals=check(settings.ShowDriverIdealLaps);row(lapsCard,"Ideal observada de cada piloto","Setores de voltas válidas GTC e locais; pode estar incompleta.",driverIdeals);driverIdeals.CheckedChanged+=(s,e)=>{settings.ShowDriverIdealLaps=driverIdeals.Checked;ResizeOverlay();SaveSettings();Invalidate();};var presence=check(settings.ShowDriverActivity);row(lapsCard,"Indicadores nos nomes","Verde: pista; laranja: boxes; cinzento: paddock.",presence);presence.CheckedChanged+=(s,e)=>{settings.ShowDriverActivity=presence.Checked;SaveSettings();Invalidate();};
        var movementCard=card(1,"POSIÇÕES");var movement=check(settings.ShowPositionChanges);
        movement.CheckedChanged+=(s,e)=>{settings.ShowPositionChanges=movement.Checked;SaveSettings();Invalidate();};row(movementCard,"Lugares ganhos ou perdidos","Mostra a variação junto à posição.",movement);
        var referenceMode=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=px(22),Width=px(202),Font=normal,BackColor=input,ForeColor=Ink,FlatStyle=FlatStyle.Flat};
        referenceMode.DrawItem+=(s,e)=>{if(e.Index<0)return;using(var fill=new SolidBrush(input))e.Graphics.FillRectangle(fill,e.Bounds);TextRenderer.DrawText(e.Graphics,referenceMode.Items[e.Index].ToString(),normal,e.Bounds,referenceMode.Enabled?Ink:Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);};
        referenceMode.Items.AddRange(new object[]{"Início da sessão","Últimas X horas","Ontem às 00:00"});referenceMode.SelectedIndex=settings.PositionReference;
        row(movementCard,"Comparar desde",null,referenceMode);
        var referenceHours=number(settings.PositionReferenceHours,1,168);row(movementCard,"Número de horas",null,referenceHours);
        Action updateReference=()=>{referenceMode.Enabled=movement.Checked;referenceHours.Enabled=movement.Checked&&referenceMode.SelectedIndex==1;};
        referenceMode.SelectedIndexChanged+=(s,e)=>{settings.PositionReference=referenceMode.SelectedIndex;updateReference();SaveSettings();Invalidate();};
        referenceHours.ValueChanged+=(s,e)=>{settings.PositionReferenceHours=(int)referenceHours.Value;SaveSettings();Invalidate();};movement.CheckedChanged+=(s,e)=>updateReference();updateReference();
        var historyHint=text("O histórico é recolhido nesta app. Sem registos suficientes, aparece —.",small,Muted);historyHint.MaximumSize=new Size(px(500),0);historyHint.Margin=new Padding(0,0,0,px(10));stacks[1].Controls.Add(historyHint);
        foreach(var compactCard in new[]{view,movementCard}){
            compactCard.RowStyles[0].Height=px(22);
            for(int i=1;i<compactCard.RowStyles.Count;i++)compactCard.RowStyles[i].Height=px(compactCard.RowStyles[i].Height>px(50)?48:36);
        }
        var refresh=card(2,"ATUALIZAÇÃO");var interval=number(settings.RefreshSeconds,5,60);
        interval.ValueChanged+=(s,e)=>{settings.RefreshSeconds=(int)interval.Value;poll.Interval=settings.RefreshSeconds*1000;SaveSettings();};row(refresh,"Intervalo em segundos",null,interval);
        var connection=card(2,"ESTADO DA SESSÃO");
        Func<string,UiLabel> valueLabel=value=>new UiLabel {Text=value,Font=normal,ForeColor=Ink,AutoSize=true,MaximumSize=new Size(px(202),0),TextAlign=ContentAlignment.MiddleRight};
        UiLabel connectionValue=valueLabel("—"),updateValue=valueLabel("—"),trackValue=valueLabel("—"),driverValue=valueLabel(settings.Player),lapValue=valueLabel("—"),referenceValue=valueLabel("—"),baselineValue=valueLabel("—");
        Action<string,Control> stateRow=(name,value)=>{int idx=connection.RowCount;row(connection,name,null,value);connection.RowStyles[idx].Height=px(30);};
        stateRow("Ligação à GTC",connectionValue);stateRow("Última consulta",updateValue);stateRow("Pista",trackValue);stateRow("Piloto",driverValue);stateRow("Volta atual",lapValue);stateRow("Melhor pessoal",referenceValue);stateRow("Posições registadas desde",baselineValue);
        var timer=new System.Windows.Forms.Timer {Interval=1000};
        Action updateValues=()=>{
            connectionValue.Text=IsStale()?"Sem atualização":"Ligado";connectionValue.ForeColor=IsStale()?GtcOrange:Green;
            updateValue.Text=updatedUtc==DateTime.MinValue?"—":updatedUtc.ToLocalTime().ToString("HH:mm:ss");trackValue.Text=snapshot==null?"—":snapshot.activeRound.trackName;
            lapValue.Text=string.IsNullOrEmpty(live.State)?"A receber dados":live.State;referenceValue.Text=live.Reference==null?"—":TimingData.Lap(live.Reference.Time);
            var point=rankingHistory.Reference(snapshot,settings.PositionReference,settings.PositionReferenceHours,DateTime.UtcNow);
            baselineValue.Text=settings.PositionReference==0?(positions.BaselineUtc==DateTime.MinValue?"A aguardar":positions.BaselineUtc.ToLocalTime().ToString("dd/MM HH:mm")):(point==null?"Histórico insuficiente":point.AtUtc.ToLocalTime().ToString("dd/MM HH:mm"));
            if(compact.Checked!=settings.Compact){compact.Checked=settings.Compact;updateNearby();}if(unlock.Checked==locked)unlock.Checked=!locked;
        };
        timer.Tick+=(s,e)=>updateValues();updateValues();timer.Start();
        selectPage(0);dialog.FormClosed+=(s,e)=>{SaveSettings();settingsDialog=null;};
        dialog.Disposed+=(s,e)=>{timer.Dispose();foreach(var f in fonts)f.Dispose();};
        GtcWindowTheme.Attach(dialog);return dialog;
    }
    void RenderSettings(string destination,int page){
        using(var window=CreateSettingsWindow()){
            ((GtcSettingsWindow)window).PreviewOnly=true;window.ShowInTaskbar=false;window.StartPosition=FormStartPosition.Manual;window.Location=new Point(-20000,-20000);
            window.Show();Application.DoEvents();((Action<int>)window.Tag)(page);window.PerformLayout();Application.DoEvents();
            using(var bitmap=new Bitmap(window.ClientSize.Width,window.ClientSize.Height)){
                // Capture this application's own settings surface without activating it.
                window.Controls[0].DrawToBitmap(bitmap,new Rectangle(Point.Empty,window.ClientSize));bitmap.Save(destination,System.Drawing.Imaging.ImageFormat.Png);
            }
            window.Close();
        }
    }
}
