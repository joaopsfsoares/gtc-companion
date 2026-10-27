// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
[assembly:AssemblyTitle("GTC Companion")]
[assembly:AssemblyProduct("GTC Companion")]
[assembly:AssemblyVersion("0.7.4.0")]
[assembly:AssemblyFileVersion("0.7.4.0")]
[assembly:AssemblyInformationalVersion("0.7.4")]

public sealed partial class CompanionWindow:Form {
    public bool PreviewOnly;
    public bool BackgroundStart;
    protected override bool ShowWithoutActivation{get{return PreviewOnly||BackgroundStart;}}
    readonly LaunchSettings settings;
    readonly Panel body=new Panel();
    readonly Label message=new Label();
    readonly Button start=new Button(),save=new Button(),cancel=new Button(),menuStart=new Button();
    readonly System.Windows.Forms.Timer countdown=new System.Windows.Forms.Timer{Interval=1000};
    readonly System.Windows.Forms.Timer requests=new System.Windows.Forms.Timer{Interval=500};
    readonly System.Windows.Forms.Timer historyTimer=new System.Windows.Forms.Timer{Interval=60000};
    readonly HistoryCollector history=new HistoryCollector();
    TextBox driver,game,crewPath,tinyPath,serverLink;
    NumericUpDown timing;
    CheckBox crew,tiny,automatic,borderless;
    CancellationTokenSource operation;
    bool starting,building=true,linkEditing;
    Action refreshLink;
    int seconds=4;
    readonly float scale;
    int P(int value){return (int)Math.Round(value*scale);}
    Font F(int size,bool bold=false){return GtcTypography.Create(size*scale,bold);}
    sealed class BrandCheck:CheckBox {
        public BrandCheck(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
        protected override void OnPaint(PaintEventArgs e){
            e.Graphics.Clear(BackColor);float s=e.Graphics.DpiX/96f,side=15*s,x=0,y=(Height-side)/2;
            using(var brush=new SolidBrush(Checked?GtcTheme.Accent:GtcTheme.Input))e.Graphics.FillRectangle(brush,x,y,side,side);
            using(var pen=new Pen(GtcTheme.Muted,s))e.Graphics.DrawRectangle(pen,x,y,side,side);
            if(Checked)using(var pen=new Pen(GtcTheme.Background,2*s))e.Graphics.DrawLines(pen,new[]{new PointF(3*s,y+8*s),new PointF(6*s,y+11*s),new PointF(12*s,y+4*s)});
            TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle((int)(23*s),0,Width-(int)(23*s),Height),ForeColor,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,ClientRectangle);
        }
        public override Size GetPreferredSize(Size proposedSize){var size=TextRenderer.MeasureText(Text,Font);float s=Font.Size/13f;return new Size(size.Width+(int)(26*s),Math.Max(size.Height,(int)(20*s)));}
    }
    Label TextLabel(string value,int size=13,bool bold=false){return new Label{Text=value,AutoSize=true,Font=F(size,bold),ForeColor=GtcTheme.Text,BackColor=Color.Transparent,Margin=Padding.Empty};}
    Button Button(string value,int width,bool primary=false){var b=new Button{Text=value,Width=P(width),Height=P(36),FlatStyle=FlatStyle.Flat,BackColor=primary?GtcTheme.Accent:GtcTheme.Input,ForeColor=primary?GtcTheme.Background:GtcTheme.Text,Font=F(12,true),Cursor=Cursors.Hand,UseVisualStyleBackColor=false,Margin=Padding.Empty};b.FlatAppearance.BorderColor=GtcTheme.Border;b.FlatAppearance.BorderSize=primary?0:1;return b;}
    void StyleButton(Button b,string value,int width,bool primary){b.Text=value;b.Width=P(width);b.Height=P(38);b.FlatStyle=FlatStyle.Flat;b.BackColor=primary?GtcTheme.Accent:GtcTheme.Input;b.ForeColor=primary?GtcTheme.Background:GtcTheme.Text;b.Font=F(12,true);b.Cursor=Cursors.Hand;b.FlatAppearance.BorderColor=GtcTheme.Border;b.FlatAppearance.BorderSize=primary?0:1;}
    void AddRepositoryLink(TableLayoutPanel card,string application,string url){
        var heading=card.Controls[0];card.Controls.Remove(heading);
        var header=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,RowCount=1,Margin=heading.Margin};
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        heading.Margin=Padding.Empty;header.Controls.Add(heading,0,0);
        var link=new LinkLabel{Text="Repositório ↗",AccessibleName="Repositório oficial do "+application,AutoSize=true,Anchor=AnchorStyles.Top|AnchorStyles.Right,Font=F(11),BackColor=Color.Transparent,LinkColor=GtcTheme.Accent,ActiveLinkColor=GtcTheme.Text,VisitedLinkColor=GtcTheme.Accent,LinkBehavior=LinkBehavior.HoverUnderline,Margin=Padding.Empty,Cursor=Cursors.Hand};
        link.LinkClicked+=(s,e)=>{StopCountdown();try{ProcessUrl(url);}catch(Exception){Report("Não foi possível abrir o repositório do "+application+" no navegador.");}};
        header.Controls.Add(link,1,0);card.Controls.Add(header,0,0);
    }
    public CompanionWindow(LaunchSettings value,bool configure){
        settings=value;using(var g=Graphics.FromHwnd(IntPtr.Zero))scale=Math.Max(1,g.DpiX/96f);
        Text="GTC Companion";Icon=BrandIcon();BackColor=GtcTheme.Background;ForeColor=GtcTheme.Text;Font=F(13);AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(P(760),P(510));MinimumSize=new Size(P(720),P(365));
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Margin=Padding.Empty};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,P(78)));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,P(102)));Controls.Add(root);
        var header=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};root.Controls.Add(header,0,0);
        using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("GtcMark")){var picture=new PictureBox{Image=new Bitmap(stream),SizeMode=PictureBoxSizeMode.Zoom,Location=new Point(P(24),P(22)),Size=new Size(P(150),P(44))};header.Controls.Add(picture);}
        CreateCompetitionHeader(header);
        var content=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Margin=Padding.Empty};content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,P(142)));content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));root.Controls.Add(content,0,1);
        var navHost=new Panel{Dock=DockStyle.Fill,BackColor=GtcTheme.Sidebar,Margin=Padding.Empty};content.Controls.Add(navHost,0,0);
        var nav=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(P(12),P(20),P(12),0),BackColor=GtcTheme.Sidebar,Margin=Padding.Empty};navHost.Controls.Add(nav);
        body.Dock=DockStyle.Fill;body.Padding=new Padding(P(18),P(14),P(18),P(10));body.Margin=Padding.Empty;content.Controls.Add(body,1,0);
        var pages=new Panel[7];var stacks=new TableLayoutPanel[7];var buttons=new Button[7];string[] names={"Sessão","Aplicações","Servidor","Overlay","TinyPedal","Crew Chief","Sobre"};
        Action<int> selectPage=page=>{StopCountdown();currentPage=page;save.Visible=page<3;for(int j=0;j<pages.Length;j++){pages[j].Visible=j==page;buttons[j].BackColor=j==page?GtcTheme.AccentSurface:GtcTheme.Sidebar;buttons[j].ForeColor=j==page?GtcTheme.Accent:GtcTheme.Muted;}pages[page].BringToFront();ResizeForPage(page);if(page==3&&refreshOverlay!=null)refreshOverlay();if(!starting&&launchStates.Count==0)Report(page==6?"GTC Companion · "+AppInfo.Version+" · versão de teste":page>=4?"Configura os motores nesta página e guarda as alterações.":page==3?"Alterações do overlay guardadas automaticamente.":settings.Configured?"Pronto para iniciar a sessão.":page==2?"Cola o link de entrada da tua competição.":"Confirma o teu nome e as aplicações que queres abrir.");};
        for(int i=0;i<pages.Length;i++){int page=i;pages[i]=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Visible=false};stacks[i]=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,Margin=Padding.Empty};stacks[i].ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));var title=TextLabel(names[i],19,true);title.Margin=new Padding(0,0,0,P(12));stacks[i].Controls.Add(title);pages[i].Controls.Add(stacks[i]);body.Controls.Add(pages[i]);buttons[i]=Button(names[i],118);buttons[i].Height=P(40);buttons[i].TextAlign=ContentAlignment.MiddleLeft;buttons[i].Padding=new Padding(P(10),0,0,0);buttons[i].Margin=new Padding(0,0,0,P(6));buttons[i].FlatAppearance.BorderSize=0;if(i<6)nav.Controls.Add(buttons[i]);else{buttons[i].Text="Sobre · v"+AppInfo.Version;buttons[i].AccessibleName="Sobre o GTC Companion, versão "+AppInfo.Version;buttons[i].Dock=DockStyle.Bottom;buttons[i].Height=P(34);buttons[i].Font=F(10);buttons[i].Padding=new Padding(P(24),0,0,0);navHost.Controls.Add(buttons[i]);}buttons[i].Click+=(s,e)=>selectPage(page);}
        Tag=selectPage;
        Func<int,string,TableLayoutPanel> card=(index,title)=>{var c=new TableLayoutPanel{Dock=DockStyle.Top,ColumnCount=1,AutoSize=true,BackColor=GtcTheme.Surface,Padding=new Padding(P(14)),Margin=new Padding(0,0,0,P(12))};c.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));var heading=TextLabel(title,11,true);heading.ForeColor=GtcTheme.Muted;heading.Margin=new Padding(0,0,0,P(10));c.Controls.Add(heading);stacks[index].Controls.Add(c);return c;};
        Action<TableLayoutPanel,string,Control> row=(c,label,control)=>{var l=TextLabel(label,12);l.Margin=new Padding(0,P(6),0,P(6));c.Controls.Add(l);control.Dock=DockStyle.Top;control.Margin=new Padding(0,0,0,P(8));c.Controls.Add(control);};
        Func<string,TextBox> input=text=>new TextBox{Text=text??"",BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,BorderStyle=BorderStyle.FixedSingle,Font=F(13),Height=P(28)};
        Func<string,bool,CheckBox> check=(text,isChecked)=>new BrandCheck{Text=text,Checked=isChecked,AutoSize=true,ForeColor=GtcTheme.Text,BackColor=GtcTheme.Surface,Font=F(13),Margin=new Padding(0,P(6),0,P(8)),UseVisualStyleBackColor=false};
        Func<TextBox,bool,Control> path=(box,isFolder)=>{var panel=new TableLayoutPanel{ColumnCount=2,RowCount=1,Height=P(34),Margin=Padding.Empty};panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,P(89)));box.Dock=DockStyle.Fill;box.Margin=new Padding(0,P(4),P(8),0);panel.Controls.Add(box,0,0);var browse=Button("Procurar…",89);panel.Controls.Add(browse,1,0);browse.Click+=(s,e)=>{StopCountdown();if(isFolder){using(var dialog=new FolderBrowserDialog{Description="Pasta do rFactor 2 (Bin64 e UserData)",SelectedPath=box.Text})if(dialog.ShowDialog(this)==DialogResult.OK)box.Text=dialog.SelectedPath;}else{using(var dialog=new OpenFileDialog{Filter="Aplicação (*.exe)|*.exe",FileName=box.Text,CheckFileExists=true})if(dialog.ShowDialog(this)==DialogResult.OK)box.Text=dialog.FileName;}};return panel;};
        var identity=card(0,"PILOTO E JOGO");driver=input(settings.Driver);row(identity,"Nome na classificação GTC",driver);game=input(settings.GameDirectory);row(identity,"Pasta do rFactor 2",path(game,true));
        var behavior=card(0,"AO ABRIR A APLICAÇÃO");automatic=check("Iniciar a sessão automaticamente",settings.AutoStart);borderless=check("Usar modo sem bordas para os overlays",settings.Borderless);behavior.Controls.Add(automatic);behavior.Controls.Add(borderless);
        var crewCard=card(1,"CREW CHIEF");AddRepositoryLink(crewCard,"Crew Chief","https://gitlab.com/mr_belowski/CrewChiefV4");crew=check("Abrir o Crew Chief com a sessão",settings.StartCrewChief);crewCard.Controls.Add(crew);crewPath=input(settings.CrewChiefPath);var crewIncluded=TextLabel(File.Exists(BundledEngines.Crew)?"Motor incluído · 4.19.5.8":"Extrai o pacote completo para incluir o motor.",12);crewIncluded.ForeColor=GtcTheme.Muted;crewIncluded.Margin=new Padding(0,0,0,P(8));crewCard.Controls.Add(crewIncluded);var configureCrew=Button("Configurar Crew Chief",205);crewCard.Controls.Add(configureCrew);configureCrew.Click+=(s,e)=>selectPage(5);
        var tinyCard=card(1,"TINYPEDAL");AddRepositoryLink(tinyCard,"TinyPedal","https://github.com/TinyPedal/TinyPedal");tiny=check("Abrir o TinyPedal com a sessão",settings.StartTinyPedal);tinyCard.Controls.Add(tiny);tinyPath=input(settings.TinyPedalPath);var tinyIncluded=TextLabel(File.Exists(BundledEngines.Tiny)?"Motor incluído · 2.52.0":"Extrai o pacote completo para incluir o motor.",12);tinyIncluded.ForeColor=GtcTheme.Muted;tinyIncluded.Margin=new Padding(0,0,0,P(8));tinyCard.Controls.Add(tinyIncluded);var configureTiny=Button("Configurar TinyPedal",205);tinyCard.Controls.Add(configureTiny);configureTiny.Click+=(s,e)=>selectPage(4);
        var info=TextLabel("Os motores são opcionais e vêm no pacote. Não precisas de escolher caminhos.\nAs vozes do Crew Chief são geridas na respetiva página.",12);info.ForeColor=GtcTheme.Muted;info.MaximumSize=new Size(P(510),0);stacks[1].Controls.Add(info);
        var access=card(2,"ACESSO À COMPETIÇÃO");
        linkEditing=string.IsNullOrWhiteSpace(settings.ServerHost)||settings.ServerPort<1;
        var stored=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Top,WrapContents=false,Margin=new Padding(0,0,0,P(12))};access.Controls.Add(stored);
        var storedText=TextLabel("Link guardado",14,true);storedText.ForeColor=Color.FromArgb(90,228,165);storedText.Margin=new Padding(0,P(8),P(18),0);stored.Controls.Add(storedText);var replace=Button("Substituir",110);stored.Controls.Add(replace);
        var editor=new TableLayoutPanel{ColumnCount=1,AutoSize=true,Dock=DockStyle.Top,Margin=Padding.Empty};editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));access.Controls.Add(editor);
        serverLink=input("");serverLink.UseSystemPasswordChar=true;row(editor,"Link de entrada",serverLink);
        var cancelReplace=Button("Manter link guardado",190);cancelReplace.Margin=new Padding(0,0,0,P(10));editor.Controls.Add(cancelReplace);
        var accessHint=TextLabel("Copia o link da tua competição no site GTC.\nO acesso fica guardado neste computador.",12);accessHint.ForeColor=GtcTheme.Muted;accessHint.MaximumSize=new Size(P(470),0);accessHint.Margin=new Padding(0,P(3),0,P(14));access.Controls.Add(accessHint);
        refreshLink=()=>{stored.Visible=!linkEditing;editor.Visible=linkEditing;cancelReplace.Visible=linkEditing&&!string.IsNullOrWhiteSpace(settings.ServerHost);accessHint.Text=linkEditing?"Copia o link da tua competição no site GTC.\nO acesso fica guardado neste computador.":"O acesso está guardado neste computador.";if(currentPage==2)ResizeForPage(2);};
        replace.Click+=(s,e)=>{StopCountdown();linkEditing=true;serverLink.Clear();refreshLink();serverLink.Focus();Report("Cola o novo link e guarda para substituir o acesso atual.");};
        cancelReplace.Click+=(s,e)=>{linkEditing=false;serverLink.Clear();refreshLink();Report("O link guardado foi mantido.");};refreshLink();
        var sites=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,WrapContents=true};access.Controls.Add(sites);
        var website=Button("Abrir site GTC ↗",155);website.Margin=new Padding(0,0,P(8),P(6));sites.Controls.Add(website);
        competitionPage=Button("Página do campeonato ↗",230);sites.Controls.Add(competitionPage);competitionPage.Visible=CompetitionProfiles.Selected(settings)!=null&&!string.IsNullOrWhiteSpace(CompetitionProfiles.Selected(settings).PageUrl);competitionPage.Click+=(s,e)=>{StopCountdown();var selected=CompetitionProfiles.EnsureSelected(settings);if(selected!=null&&!string.IsNullOrWhiteSpace(selected.PageUrl))ProcessUrl(selected.PageUrl);};
        website.Click+=(s,e)=>{StopCountdown();Process.Start(new ProcessStartInfo("https://esports.gtcracecenter.com/campeonatos"){UseShellExecute=true});};
        CreateOverlayPage(stacks[3]);
        CreateEnginePage(pages[4],"tiny",()=>tinyPath.Text.Trim().Trim('"'));
        CreateEnginePage(pages[5],"crew",()=>crewPath.Text.Trim().Trim('"'));
        CreateAboutPage(stacks[6]);
        StartEngineSettings();
        var footer=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};root.Controls.Add(footer,0,2);message.Font=F(12);message.ForeColor=GtcTheme.Muted;message.Location=new Point(P(24),P(8));message.Text=settings.Configured?"Pronto para iniciar a sessão.":"Confirma o teu nome e as aplicações que queres abrir.";footer.Controls.Add(message);
        StyleButton(start,"Entrar no servidor",170,true);StyleButton(menuStart,"Abrir sem servidor",170,false);StyleButton(save,"Guardar",100,false);StyleButton(cancel,"Cancelar arranque",145,false);footer.Controls.Add(start);footer.Controls.Add(save);footer.Controls.Add(cancel);footer.Controls.Add(menuStart);cancel.Visible=false;
        Action footerLayout=()=>{int y=progressSteps!=null&&progressSteps.Visible?111:61;start.Location=new Point(footer.Width-P(24)-start.Width,P(y));save.Location=new Point(start.Left-P(10)-save.Width,P(y));cancel.Location=new Point(P(24),P(y));menuStart.Location=cancel.Location;message.Size=new Size(footer.Width-P(48),P(43));if(progressSteps!=null)progressSteps.Width=footer.Width-P(48);};footer.Resize+=(s,e)=>footerLayout();
        CreateProgress(footer,visible=>{root.RowStyles[2].Height=P(visible?154:102);ResizeForPage(currentPage);footerLayout();});footerLayout();
        menuStart.Click+=async(s,e)=>await Launch(false);start.Click+=async(s,e)=>await Launch();save.Click+=(s,e)=>{StopCountdown();if(Save(false))Report("Definições guardadas.");};cancel.Click+=(s,e)=>{StopCountdown();if(operation!=null)operation.Cancel();};AcceptButton=start;
        foreach(var box in new[]{driver,game,crewPath,tinyPath,serverLink})box.TextChanged+=(s,e)=>Changed();foreach(var c in new[]{automatic,borderless,crew,tiny})c.CheckedChanged+=(s,e)=>Changed();timing.ValueChanged+=(s,e)=>{if(building||syncingOverlay||syncingProfiles||CompetitionProfiles.Selected(settings)==null)return;settings.TimingId=(int)timing.Value;CompanionStore.Write("settings.json",settings);RefreshCompetitions();Report("Tabela guardada. Será aplicada no próximo arranque do overlay.");};
        countdown.Tick+=async(s,e)=>{seconds--;if(seconds<=0){StopCountdown();await Launch();}else Report("Arranque automático em "+seconds+" segundos. Seleciona uma página para configurar.");};
        requests.Tick+=async(s,e)=>{string file=Path.Combine(CompanionStore.Root,"request.txt");try{if(!File.Exists(file))return;string request=File.ReadAllText(file);File.Delete(file);if(request=="quit"){Close();return;}BackgroundStart=false;WindowState=FormWindowState.Normal;Show();Activate();StopCountdown();if(request=="overlay-settings")selectPage(3);if(request=="launch"&&!starting)await Launch();if(request=="launch-menu"&&!starting)await Launch(false);}catch(Exception ex){CompanionStore.Log(ex.Message);}};requests.Start();
        historyTimer.Tick+=async(s,e)=>await history.Collect(settings);
        Shown+=async(s,e)=>{if(!PreviewOnly){historyTimer.Start();await history.Collect(settings);}};
        Shown+=(s,e)=>{if(!PreviewOnly&&settings.Configured&&CompetitionProfiles.HasAccess(CompetitionProfiles.EnsureSelected(settings))&&settings.AutoStart&&!configure){cancel.Visible=true;Report("Arranque automático em 4 segundos. Seleciona uma página para configurar.");countdown.Start();}};
        FormClosing+=(s,e)=>{StopCountdown();if(operation!=null)operation.Cancel();};FormClosed+=(s,e)=>{countdown.Dispose();requests.Dispose();historyTimer.Dispose();history.Dispose();if(operation!=null)operation.Dispose();};
        GtcWindowTheme.Attach(this);selectPage(0);building=false;RefreshCompetitions();if(CompetitionProfiles.Selected(settings)==null)Report("Usa + Adicionar para criar o teu primeiro campeonato.");
    }
    void Changed(){if(!building&&!syncingProfiles){StopCountdown();Report("Existem alterações por guardar.");}}
    void StopCountdown(){countdown.Stop();if(!starting)cancel.Visible=false;}
    void ReadInputs(){settings.Driver=driver.Text.Trim();settings.GameDirectory=game.Text.Trim().Trim('"');settings.StartCrewChief=crew.Checked;settings.CrewChiefPath=crewPath.Text.Trim().Trim('"');settings.StartTinyPedal=tiny.Checked;settings.TinyPedalPath=tinyPath.Text.Trim().Trim('"');settings.AutoStart=automatic.Checked;settings.Borderless=borderless.Checked;if(CompetitionProfiles.Selected(settings)!=null)settings.TimingId=(int)timing.Value;}
    bool Save(bool requireAccess=true,bool ignoreLinkDraft=false){
        ReadInputs();
        if(requireAccess&&CompetitionProfiles.Selected(settings)==null){Report("Adiciona primeiro um campeonato.");return false;}
        if(!ignoreLinkDraft&&CompetitionProfiles.Selected(settings)!=null&&linkEditing&&(!string.IsNullOrWhiteSpace(serverLink.Text)||requireAccess))try{ServerAccess.ImportLink(settings,serverLink.Text);}catch(ArgumentException ex){Report(ex.Message);return false;}
        var errors=LaunchEngine.Validate(settings);if(requireAccess&&errors.Count>0){Report(string.Join(" ",errors));return false;}
        if(LaunchEngine.Validate(settings,false).Count==0)settings.Configured=true;CompanionStore.Write("settings.json",settings);
        if(!ignoreLinkDraft){linkEditing=!CompetitionProfiles.HasAccess(CompetitionProfiles.EnsureSelected(settings));serverLink.Clear();refreshLink();}return true;
    }
    void Report(string text){if(IsDisposed)return;message.Text=text;PersistLaunchState();}
    public async Task Launch(bool joinServer=true){
        if(starting)return;StopCountdown();if(!Save(joinServer,!joinServer))return;launchJoinServer=joinServer;starting=true;BeginLaunchProgress();menuStart.Visible=false;menuStart.Enabled=false;body.Enabled=false;competitions.Enabled=false;addCompetition.Enabled=false;editCompetition.Enabled=false;start.Enabled=false;save.Enabled=false;cancel.Visible=true;cancel.Text="Cancelar";if(operation!=null)operation.Dispose();operation=new CancellationTokenSource();
        try{using(var engine=new LaunchEngine()){string result=await engine.Run(settings,text=>{Report(text);CompanionStore.Log(text);},operation.Token,SetLaunchStep,joinServer);launchState=result.StartsWith("Entrada enviada")?"action-required":result.Contains("já está ligado")?"existing-connection":"complete";Report(result);CompanionStore.Log(result);}}
        catch(OperationCanceledException){launchState="cancelled";Report("Arranque cancelado. As aplicações já abertas continuam a funcionar.");}
        catch(Exception ex){launchState="failed";Report(ex.Message);CompanionStore.Log(ex.GetType().Name+": "+ex.Message);}
        finally{if(!IsDisposed){starting=false;body.Enabled=true;competitions.Enabled=true;addCompetition.Enabled=true;editCompetition.Enabled=true;start.Enabled=true;save.Enabled=true;cancel.Visible=false;start.Text="Entrar no servidor";menuStart.Visible=true;menuStart.Enabled=true;RefreshCompetitions();PersistLaunchState();}}
    }
    public static Icon BrandIcon(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("GtcIcon"))using(var icon=new Icon(stream))return (Icon)icon.Clone();}
}
public static class CompanionProgram {
    [DllImport("user32.dll")]static extern bool SetProcessDPIAware();
    [STAThread]public static int Main(string[] args){
        ServiceSetup();SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        try{
            string inherited=Environment.GetEnvironmentVariable("GTC_COMPANION_DATA");if(!string.IsNullOrEmpty(inherited))CompanionStore.Root=inherited;
            string local=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"local-data.txt");if(File.Exists(local))CompanionStore.Root=Path.GetFullPath(File.ReadAllText(local).Trim());
            int dir=Array.IndexOf(args,"--data-dir");if(dir>=0&&dir+1<args.Length)CompanionStore.Root=Path.GetFullPath(args[dir+1]);Directory.CreateDirectory(CompanionStore.Root);
            if(args.Length>1&&args[0]=="--self-test"){LauncherTests.Run(args[1]);return 0;}
            var settings=CompetitionProfiles.Load(!args.Contains("--render")&&!args.Contains("--validate"));AppDiscovery.Fill(settings);
            if(args.Contains("--stop-overlay")){string root=CompetitionProfiles.ControlDirectory(settings);Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,"request-close.txt"),"");return 0;}
            if(args.Contains("--overlay-only")){LaunchEngine.StartOverlay(settings,false);return 0;}
            if(args.Length>1&&args[0]=="--validate"){CompanionStore.Write(Path.GetFullPath(args[1]),new{errors=LaunchEngine.Validate(settings),game=settings.GameDirectory,crewChief=settings.StartCrewChief,tinyPedal=settings.StartTinyPedal,driver=settings.Driver,steam=AppDiscovery.SteamExe(),data=CompanionStore.Root});return LaunchEngine.Validate(settings).Count==0?0:1;}
            if(args.Length>1&&args[0]=="--render"){
                using(var window=new CompanionWindow(settings,true)){window.PreviewOnly=true;window.ShowInTaskbar=false;window.StartPosition=FormStartPosition.Manual;window.Location=new Point(-20000,-20000);window.Show();if(args.Contains("--page-about"))((Action<int>)window.Tag)(6);if(args.Contains("--page-apps"))((Action<int>)window.Tag)(1);if(args.Contains("--page-tiny"))((Action<int>)window.Tag)(4);if(args.Contains("--page-chief"))((Action<int>)window.Tag)(5);if(args.Contains("--page-server"))((Action<int>)window.Tag)(2);if(args.Contains("--page-overlay"))window.ShowOverlaySettings();if(args.Contains("--page-overlay-classification"))window.ShowOverlaySettings(1);if(args.Contains("--page-overlay-state"))window.ShowOverlaySettings(2);if(args.Contains("--page-shortcuts"))window.ShowOverlaySettings(3);if(args.Contains("--page-progress"))window.RestoreProgressPreview();Application.DoEvents();if(args.Contains("--page-competition-editor")){window.RenderCompetitionEditor(args[1],args.Contains("--edit-competition"));window.Close();return 0;}using(var bitmap=new Bitmap(window.ClientSize.Width,window.ClientSize.Height)){window.Controls[0].DrawToBitmap(bitmap,new Rectangle(Point.Empty,window.ClientSize));bitmap.Save(args[1]);}window.Close();}return 0;
            }
            bool created;string key="Local\\GTC-Companion-"+Environment.UserName;
            using(var mutex=new Mutex(true,key,out created)){
                if(!created){File.WriteAllText(Path.Combine(CompanionStore.Root,"request.txt"),args.Contains("--launch-menu")?"launch-menu":args.Contains("--launch")?"launch":args.Contains("--overlay-settings")?"overlay-settings":"settings");return 0;}
                using(var window=new CompanionWindow(settings,args.Contains("--settings")||args.Contains("--overlay-settings")||args.Contains("--background")||args.Contains("--launch-menu"))){if(args.Contains("--background")){window.BackgroundStart=true;window.WindowState=FormWindowState.Minimized;}if(args.Contains("--launch-menu"))window.Shown+=async(s,e)=>await window.Launch(false);if(args.Contains("--launch"))window.Shown+=async(s,e)=>await window.Launch();if(args.Contains("--overlay-settings"))window.Shown+=(s,e)=>window.ShowOverlaySettings();Application.Run(window);}
            }return 0;
        }catch(Exception ex){CompanionStore.Log(ex.ToString());if(!args.Contains("--self-test")&&!args.Contains("--validate")&&!args.Contains("--render"))MessageBox.Show(ex.Message,"GTC Companion",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
    }
    static void ServiceSetup(){System.Net.ServicePointManager.SecurityProtocol=System.Net.SecurityProtocolType.Tls12;}
}
