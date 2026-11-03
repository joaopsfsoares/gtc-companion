// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

public sealed partial class CompanionWindow {
    ComboBox competitions;
    Button addCompetition,editCompetition,competitionTable;
    Button competitionPage;
    bool syncingProfiles;
    void CreateCompetitionHeader(Panel header){
        CompetitionProfiles.EnsureSelected(settings);
        var title=TextLabel("COMPANION",14,true);title.Location=new Point(P(196),P(10));header.Controls.Add(title);
        competitions=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=P(23),BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Font=F(12),FlatStyle=FlatStyle.Flat,Location=new Point(P(196),P(36)),AccessibleName="Campeonato selecionado"};
        competitions.DrawItem+=(s,e)=>{if(e.Index<0)return;using(var brush=new SolidBrush(GtcTheme.Input))e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,AppLanguage.T(competitions.Items[e.Index].ToString()),competitions.Font,e.Bounds,GtcTheme.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};header.Controls.Add(competitions);
        addCompetition=Button("+ Adicionar",100);editCompetition=Button("…",30);editCompetition.AccessibleName="Editar campeonato";competitionTable=Button("Tabela ↗",90);
        header.Controls.Add(addCompetition);header.Controls.Add(editCompetition);header.Controls.Add(competitionTable);
        Action layout=()=>{competitionTable.Location=new Point(header.Width-P(24)-competitionTable.Width,P(32));editCompetition.Location=new Point(competitionTable.Left-P(6)-editCompetition.Width,P(32));addCompetition.Location=new Point(editCompetition.Left-P(6)-addCompetition.Width,P(32));competitions.Width=Math.Max(P(150),addCompetition.Left-P(8)-competitions.Left);};header.Resize+=(s,e)=>layout();layout();
        competitionTable.Click+=(s,e)=>{StopCountdown();if(settings.TimingId>0)ProcessUrl("https://esports.gtcracecenter.com/livetiming/"+settings.TimingId);};
        addCompetition.Click+=(s,e)=>EditCompetition(true);editCompetition.Click+=(s,e)=>EditCompetition(false);
        competitions.SelectedIndexChanged+=(s,e)=>{
            if(syncingProfiles||building)return;StopCountdown();var next=competitions.SelectedItem as CompetitionProfile;if(next==null||next.Id==settings.SelectedCompetitionId)return;
            if(!Save(false)){RefreshCompetitions();return;}
            settings.SelectedCompetitionId=next.Id;System.IO.File.Delete(System.IO.Path.Combine(CompanionStore.Root,"launcher-state.json"));
            launchStates.Clear();progressSteps.Visible=false;progressLayout(false);SyncCompetition();CompanionStore.Write("settings.json",settings);
            Report("Campeonato selecionado: "+next.Name+". Será usado no próximo arranque.");
        };
        RefreshCompetitions();
    }
    void ProcessUrl(string url){System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url){UseShellExecute=true});}
    void RefreshCompetitions(){
        syncingProfiles=true;try{
            competitions.Items.Clear();foreach(var profile in settings.Competitions)competitions.Items.Add(profile);var selected=CompetitionProfiles.EnsureSelected(settings);bool has=selected!=null;
            if(has)competitions.SelectedItem=selected;else{competitions.Items.Add("Sem campeonatos");competitions.SelectedIndex=0;}
            competitions.Enabled=has&&!starting;editCompetition.Enabled=has&&!starting;competitionTable.Enabled=settings.TimingId>0;
            if(competitionPage!=null)competitionPage.Visible=has&&!string.IsNullOrWhiteSpace(selected.PageUrl);
            if(timing!=null)timing.Enabled=has;if(serverLink!=null)serverLink.Enabled=has;start.Enabled=has&&!starting;menuStart.Enabled=!starting;AcceptButton=has?start:menuStart;
        }finally{syncingProfiles=false;}

    }
    void SyncCompetition(){
        syncingProfiles=true;try{timing.Value=Math.Max(timing.Minimum,Math.Min(timing.Maximum,settings.TimingId));serverLink.Clear();linkEditing=!CompetitionProfiles.HasAccess(CompetitionProfiles.EnsureSelected(settings));refreshLink();RefreshCompetitions();}finally{syncingProfiles=false;}
        if(refreshOverlay!=null)refreshOverlay();
    }
    void EditCompetition(bool add){
        if(starting||shortcutBusy)return;StopCountdown();if(add){if(!Save(false))return;}else ReadInputs();
        var original=CompetitionProfiles.EnsureSelected(settings);if(!add&&original==null)return;var candidate=add?new CompetitionProfile{Name=""}:original.Copy();
        using(var dialog=CreateCompetitionEditor(add,candidate)){
            var result=dialog.ShowDialog(this);if(result==DialogResult.Abort){RemoveCompetition(original);return;}if(result!=DialogResult.OK)return;
        }
        if(add)settings.Competitions.Add(candidate);else settings.Competitions[settings.Competitions.IndexOf(original)]=candidate;
        settings.SelectedCompetitionId=candidate.Id;CompanionStore.Write("settings.json",settings);launchStates.Clear();progressSteps.Visible=false;progressLayout(false);SyncCompetition();
        if(add)((Action<int>)Tag)(2);Report(add?"Campeonato adicionado. Cola o respetivo link de entrada em Servidor.":"Campeonato guardado. As alterações serão usadas no próximo arranque.");
    }
    void RemoveCompetition(CompetitionProfile original){
        if(original==null||starting)return;
        try{
            if(!CompetitionProfiles.Remove(settings,original.Id))return;
            launchStates.Clear();progressSteps.Visible=false;progressLayout(false);SyncCompetition();
            Report(settings.Competitions.Count==0?"Campeonato eliminado. Usa + Adicionar para criar outro.":"Campeonato eliminado. Selecionado: "+CompetitionProfiles.Selected(settings).Name+".");
        }catch(Exception){Report("Não foi possível eliminar o campeonato. Volta a tentar.");}
    }
    sealed class CompetitionDialog:Form {public bool SuppressActivation;protected override bool ShowWithoutActivation{get{return SuppressActivation;}}}
    Form CreateCompetitionEditor(bool add,CompetitionProfile candidate){
        var dialog=new CompetitionDialog{SuppressActivation=PreviewOnly,Text=add?"Adicionar campeonato":"Editar campeonato",Icon=BrandIcon(),BackColor=GtcTheme.Background,ForeColor=GtcTheme.Text,Font=F(12),AutoScaleMode=AutoScaleMode.None,ClientSize=new Size(P(520),P(290)),FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,ShowInTaskbar=false,StartPosition=FormStartPosition.CenterParent};
            var panel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,Padding=new Padding(P(22)),AutoSize=false};panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));dialog.Controls.Add(panel);
            Func<string,string,TextBox> field=(label,value)=>{var caption=TextLabel(label,12);caption.Margin=new Padding(0,0,0,P(5));panel.Controls.Add(caption);var box=new TextBox{Text=value??"",Dock=DockStyle.Top,BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Font=F(13),BorderStyle=BorderStyle.FixedSingle,Margin=new Padding(0,0,0,P(13)),AccessibleName=label};panel.Controls.Add(box);return box;};
            var name=field("Nome do campeonato",candidate.Name);name.MaxLength=80;
            var page=field("Página do campeonato (opcional)",candidate.PageUrl);
            var timingLink=field("Ligação da classificação GTC (opcional)",candidate.TimingId>0?"https://esports.gtcracecenter.com/livetiming/"+candidate.TimingId:"");
            var hint=TextLabel("O link de entrada no servidor é guardado na página Servidor.",11);hint.ForeColor=GtcTheme.Muted;hint.MaximumSize=new Size(P(470),0);hint.Margin=new Padding(0,0,0,P(12));panel.Controls.Add(hint);
            var actions=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,FlowDirection=FlowDirection.RightToLeft};panel.Controls.Add(actions);var ok=Button(add?"Adicionar":"Guardar",110,true);var close=Button("Cancelar",100);close.Margin=new Padding(0,0,P(8),0);actions.Controls.Add(ok);actions.Controls.Add(close);dialog.AcceptButton=ok;dialog.CancelButton=close;close.DialogResult=DialogResult.Cancel;
            if(!add){
                var remove=Button("Eliminar",100);remove.Name="DeleteCompetition";remove.AccessibleName="Eliminar campeonato";remove.ForeColor=Color.FromArgb(255,112,126);remove.Margin=new Padding(0,0,P(26),0);actions.Controls.Add(remove);
                remove.Click+=(s,e)=>{var original=CompetitionProfiles.Selected(settings);if(original==null)return;
                    if(MessageBox.Show(dialog,"Eliminar o campeonato \""+original.Name+"\" da lista?\n\nO link guardado será removido. O histórico local e a sessão em curso são mantidos.","Eliminar campeonato",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)==DialogResult.Yes)dialog.DialogResult=DialogResult.Abort;
                };
            }
            ok.Click+=(s,e)=>{try{candidate.Name=name.Text.Trim();candidate.PageUrl=page.Text.Trim();candidate.TimingId=CompetitionProfiles.ParseTimingLink(timingLink.Text);string error=CompetitionProfiles.ValidateProfile(settings,candidate);if(error!=null)throw new ArgumentException(error);dialog.DialogResult=DialogResult.OK;}catch(ArgumentException ex){hint.Text=ex.Message;hint.ForeColor=Color.FromArgb(255,112,126);}};
        GtcWindowTheme.Attach(dialog);return dialog;

    }
    public void RenderCompetitionEditor(string path,bool edit=false){
        using(var dialog=CreateCompetitionEditor(!edit,edit?CompetitionProfiles.Selected(settings).Copy():new CompetitionProfile{Name=""})){
            dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-20000,-20000);dialog.Show();Application.DoEvents();
            using(var bitmap=new Bitmap(dialog.ClientSize.Width,dialog.ClientSize.Height)){dialog.Controls[0].DrawToBitmap(bitmap,new Rectangle(Point.Empty,dialog.ClientSize));bitmap.Save(path);}dialog.Close();
        }
    }

}
