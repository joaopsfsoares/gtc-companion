// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

public sealed partial class CompanionWindow {
    DriverHistoryService driverHistoryService;
    DriverAnalysisOptions driverAnalysisOptions;
    DateTime nextDriverHistoryFetch=DateTime.MinValue;
    sealed class AnalysisDriver {
        public string Key,Name,Class;
        public override string ToString(){return Name+(string.IsNullOrEmpty(Class)?"":" · "+Class);}
    }
    void StartDriverHistory(){
        driverHistoryService=new DriverHistoryService();driverAnalysisOptions=CompanionStore.Read<DriverAnalysisOptions>(Path.Combine("training","analysis-options.json"))??new DriverAnalysisOptions();
    }
    async void ObserveDriverHistory(){
        driverHistoryService.Collector.Local(trainingReader.Activity,DateTime.UtcNow);
        try{driverHistoryService.Save();}catch{ /* Collection continues if the disk is temporarily unavailable. */ }
        if(driverAnalysisOptions.BackgroundGtc&&DateTime.UtcNow>=nextDriverHistoryFetch){nextDriverHistoryFetch=DateTime.UtcNow.AddSeconds(15);var profile=CompetitionProfiles.Selected(settings);if(profile!=null)await driverHistoryService.Fetch(profile.TimingId);}
    }
    void ShowDriverAnalysis(){StopCountdown();using(var dialog=CreateDriverAnalysis(false))dialog.ShowDialog(this);}
    Form CreateDriverAnalysis(bool preview){
        var dialog=new Form{Text="Pilotos e stints",Icon=BrandIcon(),BackColor=GtcTheme.Background,ForeColor=GtcTheme.Text,Font=F(11),AutoScaleMode=AutoScaleMode.None,StartPosition=FormStartPosition.CenterParent,ClientSize=new Size(P(900),P(640)),MinimumSize=new Size(P(760),P(520))};
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(P(18)),ColumnCount=1,RowCount=7};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));for(int i=0;i<7;i++)root.RowStyles.Add(new RowStyle(i==5?SizeType.Percent:SizeType.AutoSize,i==5?100:0));dialog.Controls.Add(root);
        var heading=TextLabel("Pilotos e stints",19,true);heading.Margin=new Padding(0,0,0,P(5));root.Controls.Add(heading,0,0);
        var intro=TextLabel("Dados GTC prioritários · recolha local de apoio · histórico guardado neste PC",11);intro.ForeColor=GtcTheme.Muted;intro.Margin=new Padding(0,0,0,P(16));root.Controls.Add(intro,0,1);
        var selectors=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=3,Margin=new Padding(0,0,0,P(12))};selectors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,27));selectors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,47));selectors.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,26));root.Controls.Add(selectors,0,2);
        Func<string,int,ComboBox> combo=(name,col)=>{var label=TextLabel(name,10);label.ForeColor=GtcTheme.Muted;label.Margin=new Padding(0,0,0,P(5));selectors.Controls.Add(label,col,0);var control=new ComboBox{Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=P(24),FlatStyle=FlatStyle.Flat,BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Font=F(11),Margin=new Padding(0,0,col==2?0:P(10),0),AccessibleName=name,DropDownWidth=P(col==1?560:320)};control.DrawItem+=(s,e)=>{using(var brush=new SolidBrush((e.State&DrawItemState.Selected)!=0?GtcTheme.AccentSurface:GtcTheme.Input))e.Graphics.FillRectangle(brush,e.Bounds);if(e.Index>=0)TextRenderer.DrawText(e.Graphics,Convert.ToString(control.Items[e.Index]),control.Font,e.Bounds,GtcTheme.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.Left|TextFormatFlags.EndEllipsis);e.DrawFocusRectangle();};selectors.Controls.Add(control,col,1);return control;};
        var drivers=combo("Piloto",0);var sessions=combo("Sessão / origem da recolha",1);var stints=combo("Stint",2);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,WrapContents=true,Margin=new Padding(0,0,0,P(10))};root.Controls.Add(actions,0,3);var fetch=Button("Atualizar GTC",140);fetch.Margin=new Padding(0,0,P(16),0);actions.Controls.Add(fetch);
        var background=new BrandCheck{Text="Recolher GTC em segundo plano",Checked=driverAnalysisOptions.BackgroundGtc,AutoSize=true,Font=F(11),ForeColor=GtcTheme.Text,Margin=new Padding(0,P(7),0,0)};actions.Controls.Add(background);
        var status=TextLabel("",11);status.ForeColor=GtcTheme.Muted;status.MaximumSize=new Size(P(840),0);status.Margin=new Padding(0,0,0,P(10));root.Controls.Add(status,0,4);
        var chart=new DriverAnalysisChart{Dock=DockStyle.Fill,Margin=Padding.Empty,Target=trainingOptions.Target,Tolerance=trainingOptions.Tolerance};root.Controls.Add(chart,0,5);
        var footer=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,WrapContents=true,Margin=new Padding(0,P(12),0,0)};root.Controls.Add(footer,0,6);var target=new BrandCheck{Text="Mostrar alvo do treino",Checked=true,AutoSize=true,Font=F(11),ForeColor=GtcTheme.Text,Margin=new Padding(0,P(7),P(18),0)};var other=new BrandCheck{Text="Incluir outras voltas",AutoSize=true,Font=F(11),ForeColor=GtcTheme.Text,Margin=new Padding(0,P(7),P(18),0)};var export=Button("Guardar gráfico",150);footer.Controls.Add(target);footer.Controls.Add(other);footer.Controls.Add(export);
        bool updating=false,sourceChosen=false;string driverKey="",sessionKey="",stintKey="";
        Func<DriverHistoryRecord,bool> visible=r=>r.Source!="gtc"||r.TimingId==0||CompetitionProfiles.Selected(settings)!=null&&r.TimingId==CompetitionProfiles.Selected(settings).TimingId;
        Action redraw=()=>{var record=sessions.SelectedItem as DriverHistoryRecord;var stint=stints.SelectedItem as DriverStintSlice;chart.Record=record;chart.Stint=stint;chart.ShowTarget=target.Checked;chart.ShowOther=other.Checked;chart.Invalidate();if(record==null)status.Text="Ainda sem voltas. Atualiza a GTC ou mantém o Companion aberto durante a sessão.";else{var available=record.Points(stint);status.Text=(record.Partial?"Histórico parcial":"Histórico disponível")+" · "+record.Laps.Count+"/"+record.TotalLaps+" voltas da sessão · "+available.Count(l=>l.Clean)+" limpas na seleção. Boxes ~: limite aproximado.";}if(driverHistoryService.Error!=null&&!preview)status.Text+="\n"+driverHistoryService.Error;};
        Action refresh=null;
        refresh=()=>{
            if(updating)return;updating=true;
            var records=driverHistoryService.Collector.Book.Records.Where(visible).Where(r=>r.Laps.Count>0).ToList();var list=records.GroupBy(r=>TimingData.Normalize(r.Driver)+"|"+TimingData.Normalize(r.Class)).Select(g=>new AnalysisDriver{Key=g.Key,Name=g.First().Driver,Class=g.First().Class}).OrderBy(d=>d.Name).ToList();
            string dk=string.Join(";",list.Select(d=>d.Key));if(dk!=driverKey){string selected=(drivers.SelectedItem as AnalysisDriver)==null?null:((AnalysisDriver)drivers.SelectedItem).Key;driverKey=dk;drivers.Items.Clear();drivers.Items.AddRange(list.Cast<object>().ToArray());var chosen=list.FirstOrDefault(d=>d.Key==selected)??list.FirstOrDefault(d=>TimingData.Normalize(d.Name)==TimingData.Normalize(settings.Driver))??list.FirstOrDefault();drivers.SelectedItem=chosen;}
            var pilot=drivers.SelectedItem as AnalysisDriver;var history=GtcDataPriority.Sessions(records.Where(r=>pilot!=null&&TimingData.Normalize(r.Driver)+"|"+TimingData.Normalize(r.Class)==pilot.Key));string sk=string.Join(";",history.Select(r=>r.Id));if(sk!=sessionKey){string selected=!sourceChosen||!(sessions.SelectedItem is DriverHistoryRecord)?null:((DriverHistoryRecord)sessions.SelectedItem).Id;sessionKey=sk;sessions.Items.Clear();sessions.Items.AddRange(history.Cast<object>().ToArray());sessions.SelectedItem=history.FirstOrDefault(r=>r.Id==selected)??history.FirstOrDefault();stintKey="";}
            var record=sessions.SelectedItem as DriverHistoryRecord;var slices=record==null?new System.Collections.Generic.List<DriverStintSlice>():record.Stints();string tk=(record==null?"":record.Id)+"|"+string.Join(";",slices.Select(s=>s.Number+":"+s.AfterLap+":"+s.ThroughLap+":"+s.FirstAvailable+":"+s.LastAvailable));if(tk!=stintKey){int selected=(stints.SelectedItem as DriverStintSlice)==null?0:((DriverStintSlice)stints.SelectedItem).Number;stintKey=tk;stints.Items.Clear();stints.Items.AddRange(slices.Cast<object>().ToArray());stints.SelectedItem=slices.FirstOrDefault(s=>s.Number==selected)??slices.FirstOrDefault();}
            updating=false;redraw();
        };
        drivers.SelectedIndexChanged+=(s,e)=>{if(!updating){sourceChosen=false;sessionKey="";refresh();}};sessions.SelectedIndexChanged+=(s,e)=>{if(!updating){sourceChosen=true;stintKey="";refresh();}};stints.SelectedIndexChanged+=(s,e)=>{if(!updating)redraw();};target.CheckedChanged+=(s,e)=>redraw();other.CheckedChanged+=(s,e)=>redraw();
        background.CheckedChanged+=(s,e)=>{if(preview)return;driverAnalysisOptions.BackgroundGtc=background.Checked;Directory.CreateDirectory(Path.Combine(CompanionStore.Root,"training"));CompanionStore.Write(Path.Combine("training","analysis-options.json"),driverAnalysisOptions);nextDriverHistoryFetch=DateTime.MinValue;};
        Func<System.Threading.Tasks.Task> update=async()=>{if(preview)return;var profile=CompetitionProfiles.Selected(settings);if(profile==null||profile.TimingId<=0){status.Text="Escolhe um campeonato com classificação GTC para obter os dados online.";return;}fetch.Enabled=false;fetch.Text="A consultar…";await driverHistoryService.Fetch(profile.TimingId);if(dialog.IsDisposed)return;fetch.Enabled=true;fetch.Text="Atualizar GTC";refresh();};fetch.Click+=async(s,e)=>await update();
        export.Click+=(s,e)=>{if(chart.Record==null)return;using(var file=new SaveFileDialog{Filter="Imagem PNG|*.png",FileName="piloto-stints.png"})if(file.ShowDialog(dialog)==DialogResult.OK)try{chart.SaveImage(file.FileName);}catch(Exception){status.Text="Não foi possível guardar a imagem.";}};
        var timer=new System.Windows.Forms.Timer{Interval=1000};timer.Tick+=(s,e)=>refresh();dialog.Shown+=async(s,e)=>{refresh();if(!preview){timer.Start();await update();}};dialog.FormClosed+=(s,e)=>timer.Dispose();GtcWindowTheme.Attach(dialog);refresh();return dialog;
    }
    public void RenderDriverAnalysis(string destination){
        trainingOptions.Target=82.7;trainingOptions.Tolerance=.5;
        var record=new DriverHistoryRecord{Id="demo",Driver="Piloto de demonstração",Class="Cup",Track="Circuito de demonstração",Source="local",FirstSeenUtc=DateTime.UtcNow,TotalLaps=24};double[] times={82.9,82.5,82.6,82.8,82.4,82.6,82.9,82.5,82.7,83.0,149,106,83.3,82.8,82.9,82.5,82.7,82.6,82.8,82.45,82.7,82.9,82.6,82.8};for(int i=0;i<times.Length;i++)if(i!=6&&i!=7)record.Put(new DriverLapPoint{Number=i+1,Time=times[i],Valid=i!=17,Pit=i==10||i==11,Complete=true});record.Visits.Add(new DriverPitVisit{EntryAfterLap=10,ExitAfterLap=11,ObservedUtc=DateTime.UtcNow,ExitObservedUtc=DateTime.UtcNow});driverHistoryService.Collector.Book.Records.Clear();driverHistoryService.Collector.Book.Records.Add(record);
        using(var dialog=CreateDriverAnalysis(true)){dialog.ShowInTaskbar=false;dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-20000,-20000);dialog.Show();Application.DoEvents();using(var bitmap=new Bitmap(dialog.ClientSize.Width,dialog.ClientSize.Height)){dialog.Controls[0].DrawToBitmap(bitmap,new Rectangle(Point.Empty,dialog.ClientSize));bitmap.Save(destination);}dialog.Close();}
    }
}
