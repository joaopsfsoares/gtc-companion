// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

public sealed partial class CompanionWindow {
    readonly TrainingModel training=new TrainingModel();
    readonly TrainingRecordCollector trainingRecords=new TrainingRecordCollector();
    PersonalRecordRepository trainingRepository;
    DateTime nextTrainingRecordSync=DateTime.MinValue;
    int storedTrainingRecordRevision=-1;
    readonly Rf2ScoringReader trainingReader=new Rf2ScoringReader();
    readonly System.Windows.Forms.Timer trainingTimer=new System.Windows.Forms.Timer{Interval=200};
    TrainingOptions trainingOptions;
    bool trainingHudRequested,trainingShortcutHidden;
    TrainingHud trainingHud;
    TrainingChart trainingChart;
    ComboBox trainingHistory,trainingMetric,trainingPosition;
    Label trainingSummary,trainingDetails,trainingStatus,trainingScore;
    Action refreshTraining;
    int savedTrainingRevision=-1;DateTime savedTrainingAt=DateTime.MinValue;
    TrainingRun trainingViewCache;DateTime nextTrainingView=DateTime.MinValue;
    sealed class TrainingHistoryItem {
        public TrainingRun Run;
        public override string ToString(){return Run==null?"Stint atual":Run.CreatedUtc.ToLocalTime().ToString("dd/MM HH:mm")+" · "+(Run.Track??"Sem pista")+" · "+Run.Laps.Count+" voltas";}
    }
    void CreateTrainingPage(Panel page){
        foreach(Control control in page.Controls.Cast<Control>().ToArray())control.Dispose();page.AutoScroll=true;
        trainingOptions=CompanionStore.Read<TrainingOptions>(Path.Combine("training","options.json"))??new TrainingOptions();try{trainingOptions.Validate();}catch{trainingOptions=new TrainingOptions();}
        training.Restore(TrainingStore.History().FirstOrDefault());
        trainingRepository=new PersonalRecordRepository(CompanionStore.Root);trainingRepository.Synchronize(trainingRecords.Book);
        StartDriverHistory();
        var root=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,RowCount=8,Margin=Padding.Empty};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));for(int row=0;row<7;row++)root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Absolute,P(240)));page.Controls.Add(root);
        var titleRow=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,Margin=new Padding(0,0,0,P(12))};titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));var title=TextLabel("Stint",19,true);titleRow.Controls.Add(title,0,0);var help=new LinkLabel{Text="Como configurar",AutoSize=true,Font=F(11),LinkColor=GtcTheme.Accent,VisitedLinkColor=GtcTheme.Accent,ActiveLinkColor=GtcTheme.Text,Margin=new Padding(0,P(8),0,0),AccessibleName="Como calcular o ritmo de corrida e configurar o stint"};titleRow.Controls.Add(help,1,0);root.Controls.Add(titleRow,0,0);
        var options=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,BackColor=GtcTheme.Surface,Padding=new Padding(P(12)),Margin=new Padding(0,0,0,P(10))};options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));root.Controls.Add(options,0,1);
        Func<string,Control,Panel> field=(label,control)=>{var panel=new Panel{Height=P(65),Dock=DockStyle.Top,Margin=new Padding(0,0,P(10),0)};var heading=TextLabel(label,11);panel.Controls.Add(heading);control.Location=new Point(0,P(25));control.Width=P(215);panel.Controls.Add(control);return panel;};
        var duration=new FlowLayoutPanel{AutoSize=true,WrapContents=false,Margin=Padding.Empty};var mode=AnalysisChoice("Duração do stint",125);mode.Items.AddRange(new object[]{"Voltas","Minutos"});mode.SelectedIndex=trainingOptions.Minutes?1:0;var amount=new NumericUpDown{Minimum=1,Maximum=999,Value=trainingOptions.Amount,BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Font=F(12),Width=P(75)};duration.Controls.Add(mode);duration.Controls.Add(amount);options.Controls.Add(field("Duração do stint",duration),0,0);
        var target=new TextBox{Text=TimingData.Lap(trainingOptions.Target),BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Font=F(12),BorderStyle=BorderStyle.FixedSingle,AccessibleName="Ritmo alvo em minutos e segundos"};options.Controls.Add(field("Ritmo alvo · m:ss,mmm",target),1,0);
        help.LinkClicked+=(s,e)=>{StopCountdown();ShowTrainingHelp(value=>{target.Text=TimingData.Lap(value);},training.Run!=null&&training.Run.Active);};
        var tolerance=new NumericUpDown{Minimum=.05m,Maximum=30,DecimalPlaces=2,Increment=.05m,Value=(decimal)trainingOptions.Tolerance,BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Font=F(12),AccessibleName="Janela de consistência em segundos"};options.Controls.Add(field("Janela de consistência · ± segundos",tolerance),0,1);
        trainingPosition=AnalysisChoice("Posição do overlay de stint",215);trainingPosition.Items.AddRange(new object[]{"Personalizada","Superior esquerdo","Superior centro","Superior direito","Centro esquerdo","Centro","Centro direito","Inferior esquerdo","Inferior centro","Inferior direito"});trainingPosition.SelectedIndex=Math.Max(0,Math.Min(9,trainingOptions.HudAnchor));options.Controls.Add(field("Posição no ecrã",trainingPosition),1,1);
        var suggestRow=new TableLayoutPanel{AutoSize=true,Dock=DockStyle.Top,ColumnCount=2,BackColor=GtcTheme.Surface,Padding=new Padding(P(10)),Margin=new Padding(0,0,0,P(8))};suggestRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));suggestRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));root.Controls.Add(suggestRow,0,2);var suggestion=TextLabel("A procurar a tua melhor volta…",11);suggestion.MaximumSize=new Size(P(365),0);suggestion.ForeColor=GtcTheme.Muted;suggestion.AccessibleName="Sugestão de parâmetros do stint";suggestRow.Controls.Add(suggestion,0,0);var acceptSuggestion=Button("Aceitar sugestão",150);suggestRow.Controls.Add(acceptSuggestion,1,0);double? suggestedBest=null;double suggestedPace=0;decimal suggestedWindow=0;DateTime nextSuggestion=DateTime.MinValue;
        Action refreshSuggestion=()=>{string context;var best=TrainingBest(out context);suggestedBest=null;if(!best.HasValue){suggestion.Text="Sem melhor volta válida para sugerir parâmetros. "+context;acceptSuggestion.Enabled=false;return;}try{suggestedPace=TrainingMath.RacePace(best.Value,.5);suggestedWindow=(decimal)TrainingMath.SuggestedTolerance(best.Value);suggestedBest=best;suggestion.Text="Recorde "+TimingData.Lap(best.Value)+" · alvo "+TimingData.Lap(suggestedPace)+" ±"+suggestedWindow.ToString("0.00")+" s\n"+context+" · margem 0,5%; janela 0,6%.";acceptSuggestion.Enabled=training.Run==null||!training.Run.Active;}catch(ArgumentException){suggestion.Text="A melhor volta não permite calcular uma sugestão válida.";acceptSuggestion.Enabled=false;}};
        var actions=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Top,WrapContents=true,Margin=new Padding(0,0,0,P(9))};root.Controls.Add(actions,0,3);var begin=Button("Iniciar stint",140,true);var end=Button("Terminar",110);var pilots=Button("Pilotos e stints",165);var export=Button("Guardar gráfico",155);foreach(var button in new[]{begin,end,pilots}){button.Margin=new Padding(0,0,P(6),P(6));actions.Controls.Add(button);}pilots.Click+=(s,e)=>ShowDriverAnalysis();
        var summary=new TableLayoutPanel{AutoSize=true,Dock=DockStyle.Top,ColumnCount=2,Margin=new Padding(0,0,0,P(8))};summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,65));summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,35));trainingSummary=TextLabel("",14,true);trainingSummary.MaximumSize=new Size(P(350),0);trainingSummary.Margin=new Padding(0,0,0,P(5));summary.Controls.Add(trainingSummary,0,0);trainingScore=TextLabel("",14,true);trainingScore.MaximumSize=new Size(P(200),0);trainingScore.ForeColor=GtcTheme.Accent;trainingScore.AccessibleName="Avaliação do stint de zero a cem";summary.Controls.Add(trainingScore,1,0);trainingDetails=TextLabel("",12);trainingDetails.MaximumSize=new Size(P(530),0);summary.Controls.Add(trainingDetails,0,1);summary.SetColumnSpan(trainingDetails,2);root.Controls.Add(summary,0,4);
        trainingStatus=TextLabel("",11);trainingStatus.MaximumSize=new Size(P(530),0);trainingStatus.ForeColor=GtcTheme.Muted;trainingStatus.Margin=new Padding(0,0,0,P(10));root.Controls.Add(trainingStatus,0,5);
        trainingHistory=AnalysisChoice("Histórico de stints",180);trainingHistory.Dock=DockStyle.Top;trainingHistory.Margin=new Padding(0,P(5),P(8),0);trainingMetric=AnalysisChoice("Analisar volta ou setor do treino",140);trainingMetric.Items.AddRange(DriverLapAnalysis.Names);trainingMetric.SelectedIndex=0;trainingMetric.Margin=new Padding(0,0,P(8),0);var historyRow=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=3,Margin=new Padding(0,0,0,P(10))};historyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));historyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));historyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));historyRow.Controls.Add(trainingHistory,0,0);historyRow.Controls.Add(trainingMetric,1,0);historyRow.Controls.Add(export,2,0);root.Controls.Add(historyRow,0,6);
        trainingChart=new TrainingChart{Dock=DockStyle.Fill,Margin=Padding.Empty};root.Controls.Add(trainingChart,0,7);
        trainingMetric.SelectedIndexChanged+=(s,e)=>{trainingChart.Metric=(LapMetric)trainingMetric.SelectedIndex;trainingChart.Invalidate();};
        Action saveOptions=()=>{Directory.CreateDirectory(Path.Combine(CompanionStore.Root,"training"));CompanionStore.Write(Path.Combine("training","options.json"),trainingOptions);};
        acceptSuggestion.Click+=(s,e)=>{if(!suggestedBest.HasValue||training.Run!=null&&training.Run.Active)return;target.Text=TimingData.Lap(suggestedPace);tolerance.Value=suggestedWindow;trainingOptions.Target=suggestedPace;trainingOptions.Tolerance=(double)suggestedWindow;saveOptions();trainingStatus.Text="Sugestão aplicada. Podes ajustar o alvo e a janela antes de iniciar.";};
        trainingPosition.SelectedIndexChanged+=(s,e)=>{trainingOptions.HudAnchor=trainingPosition.SelectedIndex;saveOptions();ApplyTrainingPosition();};
        begin.Click+=(s,e)=>{StopCountdown();try{double pace;if(!TrainingMath.ParsePace(target.Text,out pace))throw new ArgumentException("Indica o ritmo como 1:22,700 ou 1:22.700.");var next=trainingOptions.Copy();next.Minutes=mode.SelectedIndex==1;next.Amount=(int)amount.Value;next.Target=pace;next.Tolerance=(double)tolerance.Value;next.Validate();training.Arm(next);trainingHudRequested=true;trainingOptions=next;saveOptions();TrainingStore.Save(training.Run);RefreshTrainingHistory();refreshTraining();RefreshTrainingHud();}catch(Exception ex){trainingStatus.Text=ex.Message;}};
        end.Click+=(s,e)=>{training.Stop();TrainingStore.Save(training.Run);RefreshTrainingHistory();refreshTraining();RefreshTrainingHud();};
        export.Click+=(s,e)=>{if(trainingChart.Run==null||trainingChart.Run.Laps.Count==0){trainingStatus.Text="Completa uma volta para guardar o gráfico.";return;}using(var dialog=new SaveFileDialog{Filter="Imagem PNG|*.png",FileName="stint-"+trainingChart.Run.CreatedUtc.ToLocalTime().ToString("yyyyMMdd-HHmm")+".png"})if(dialog.ShowDialog(this)==DialogResult.OK)try{trainingChart.SaveImage(dialog.FileName);trainingStatus.Text="Gráfico guardado.";}catch(Exception ex){trainingStatus.Text=ex.Message;}};
        refreshTraining=()=>{
            bool active=training.Run!=null&&training.Run.Active;begin.Enabled=!active;end.Enabled=active;mode.Enabled=amount.Enabled=target.Enabled=tolerance.Enabled=!active;acceptSuggestion.Enabled=suggestedBest.HasValue&&!active;
            var selection=trainingHistory.SelectedItem as TrainingHistoryItem;var run=selection!=null&&selection.Run!=null?selection.Run:training.Run;var profile=CompetitionProfiles.Selected(settings);if(run==null)trainingViewCache=null;else if(trainingViewCache==null||trainingViewCache.Id!=run.Id||trainingViewCache.State!=run.State||trainingViewCache.Laps.Count!=run.Laps.Count||DateTime.UtcNow>=nextTrainingView){trainingViewCache=TrainingHistoryView.Merge(run,driverHistoryService.Collector.Book.Records,profile==null?0:profile.TimingId,DateTime.UtcNow);nextTrainingView=DateTime.UtcNow.AddSeconds(1);}var display=trainingViewCache;if(display!=null){display.Elapsed=run.Elapsed;display.OffTracks=run.OffTracks;}trainingChart.EmptyMessage=display!=null&&!display.GtcMatched&&!PreviewOnly?"Sem sessão GTC correspondente: o gráfico aguarda dados da GTC.":null;trainingChart.Run=display!=null&&!display.GtcMatched&&!PreviewOnly?new TrainingRun{Options=display.Options,State=display.State}:display;
            if(run==null){trainingSummary.Text="Define a duração, o alvo e a janela para começar.";trainingScore.Text="Nota —/100";trainingDetails.Text="";trainingSummary.ForeColor=GtcTheme.Text;trainingStatus.Text="Média de todas as voltas completas, incluindo inválidas. Saídas estimadas: carro além do limite da pista durante pelo menos 0,6 s.";return;}
            string last=display.Laps.Count==0?"—":TimingData.Lap(display.Laps.Last().Time)+" ("+TrainingMath.Delta(display.Laps.Last().Time-display.Options.Target)+")";
            trainingSummary.Text="Média "+TrainingMath.Lap(display.Average)+(display.Average.HasValue?" · "+TrainingMath.Delta(display.Average.Value-display.Options.Target):"");
            var assessment=TrainingAssessment.Evaluate(display);trainingScore.Text=assessment.Label;
            trainingDetails.Text=display.Laps.Count+" voltas · "+TrainingMath.Remaining(display)+" · Tempo "+TrainingMath.Clock(display.Elapsed)+"\nÚltima "+last+"\nNa janela "+display.InWindow+"/"+display.Laps.Count+" ("+display.FasterThanWindow+" mais rápidas) · Válidas "+display.Laps.Count(l=>l.Valid&&l.CompleteData&&!l.PossiblyInvalid)+"/"+display.Laps.Count+" · Saídas estimadas "+display.OffTracks;
            trainingSummary.ForeColor=display.Average.HasValue?AnalysisLegend.Point(true,display.Average.Value,display.Options.Target,display.Options.Tolerance):GtcTheme.Text;
            int online=display.Laps.Count(l=>l.DataSource=="gtc"),local=display.Laps.Count(l=>l.DataSource=="local");string source=!display.GtcMatched?"Sem sessão GTC correspondente · contagem local":"GTC: "+online+" · complemento local: "+local;
            trainingStatus.Text=(run==training.Run?training.Notice:run.Reason)+(string.IsNullOrWhiteSpace(run.RecoveryNote)?"":"\n"+run.RecoveryNote)+"\n"+assessment.Description+"\n"+source+". Média inclui inválidas; saídas são estimadas."+(run.HadTelemetryGap?" Houve falhas de telemetria.":"");
        };
        trainingHistory.SelectedIndexChanged+=(s,e)=>refreshTraining();RefreshTrainingHistory();refreshTraining();
        trainingTimer.Tick+=(s,e)=>{
            bool wasActive=training.Run!=null&&training.Run.Active;var sample=trainingReader.Read();trainingRecords.Observe(sample);if(DateTime.UtcNow>=nextTrainingRecordSync||trainingRecords.Book.Revision!=storedTrainingRecordRevision){nextTrainingRecordSync=DateTime.UtcNow.AddSeconds(1);if(trainingRepository.Synchronize(trainingRecords.Book))storedTrainingRecordRevision=trainingRecords.Book.Revision;}training.Update(sample);if(sample==null&&training.Run!=null&&training.Run.State=="running"&&!LaunchEngine.Running("rFactor2"))training.Stop("O rFactor foi fechado.",true);
            if(training.Run!=null&&(training.Revision!=savedTrainingRevision||training.Run.Active&&(DateTime.UtcNow-savedTrainingAt).TotalSeconds>=5))try{TrainingStore.Save(training.Run);savedTrainingRevision=training.Revision;savedTrainingAt=DateTime.UtcNow;}catch(Exception ex){training.Notice="Não foi possível guardar o stint: "+ex.Message;}
            if(wasActive&&!training.Run.Active){RefreshTrainingHistory();Report(training.Run.State=="completed"?"Objetivo do stint concluído. O gráfico está em Overlay → Stint.":"Stint interrompido: "+training.Run.Reason);}
            ObserveDriverHistory();if(page.Visible){if(DateTime.UtcNow>=nextSuggestion){nextSuggestion=DateTime.UtcNow.AddSeconds(10);refreshSuggestion();}refreshTraining();}RefreshTrainingHud();
        };
        page.VisibleChanged+=async(s,e)=>{if(page.Visible){refreshSuggestion();refreshTraining();if(!PreviewOnly){await history.Collect(settings);if(!IsDisposed&&page.Visible)refreshSuggestion();}}};Shown+=(s,e)=>{if(!PreviewOnly)trainingTimer.Start();};refreshSuggestion();
        FormClosed+=(s,e)=>{trainingTimer.Dispose();trainingReader.Dispose();trainingRepository.Synchronize(trainingRecords.Book);trainingRepository.Dispose();if(!PreviewOnly)try{driverHistoryService.Save(true);}catch{}driverHistoryService.Dispose();if(training.Run!=null&&training.Run.Active){training.Stop("Companion fechado durante o stint.",true);try{TrainingStore.Save(training.Run);}catch{}}if(trainingHud!=null)trainingHud.Close();};
    }
    void RefreshTrainingHistory(){
        if(trainingHistory==null)return;trainingHistory.Items.Clear();trainingHistory.Items.Add(new TrainingHistoryItem());foreach(var run in TrainingStore.History().Where(run=>training.Run==null||run.Id!=training.Run.Id))trainingHistory.Items.Add(new TrainingHistoryItem{Run=run});trainingHistory.SelectedIndex=0;
    }
    void RefreshTrainingHud(){
        if(PreviewOnly)return;
        if(!trainingHudRequested||trainingShortcutHidden){if(trainingHud!=null)trainingHud.Hide();return;}
        if(trainingHud==null||trainingHud.IsDisposed){trainingHud=new TrainingHud(scale,point=>{trainingOptions.HudX=point.X;trainingOptions.HudY=point.Y;trainingOptions.HudAnchor=0;if(trainingPosition!=null&&trainingPosition.SelectedIndex!=0)trainingPosition.SelectedIndex=0;Directory.CreateDirectory(Path.Combine(CompanionStore.Root,"training"));CompanionStore.Write(Path.Combine("training","options.json"),trainingOptions);},locked=>SendOverlay(new OverlayPatch{Locked=locked},true));trainingHud.StartPosition=FormStartPosition.Manual;trainingHud.SetCompact(ReadOverlay().Compact);ApplyTrainingPosition();}
        var overlayPrefs=ReadOverlay();var moveKey=OverlayShortcuts.Normalize(overlayPrefs.Shortcuts).Move;trainingHud.MoveShortcut=moveKey.Enabled?moveKey.ToString():null;
        trainingHud.Opacity=Math.Max(10,Math.Min(100,overlayPrefs.OpacityPercent))/100d;trainingHud.Run=training.Run;trainingHud.Notice=training.Notice;if(!trainingHud.Visible)trainingHud.Show();trainingHud.Invalidate();
    }
    void ShowSharedStint(bool move){if(PreviewOnly)return;trainingHudRequested=true;trainingShortcutHidden=false;RefreshTrainingHud();SetSharedStintMove(move);}
    void SetSharedStintMove(bool move){if(trainingHud!=null&&!trainingHud.IsDisposed)trainingHud.SetLocked(!move);}
    void SetSharedStintOpacity(int opacity){if(trainingOptions==null)return;try{trainingOptions.Opacity=opacity;Directory.CreateDirectory(Path.Combine(CompanionStore.Root,"training"));CompanionStore.Write(Path.Combine("training","options.json"),trainingOptions);if(trainingHud!=null&&!trainingHud.IsDisposed)trainingHud.Opacity=opacity/100d;}catch(Exception ex){Report("Não foi possível guardar a transparência: "+ex.Message);}}
    void ApplyTrainingPosition(){
        if(trainingHud==null||trainingHud.IsDisposed||trainingOptions==null)return;
        bool positioned=trainingOptions.HudX!=-1||trainingOptions.HudY!=-1;var area=positioned?Screen.FromPoint(new Point(trainingOptions.HudX,trainingOptions.HudY)).WorkingArea:Screen.FromControl(this).WorkingArea;
        var desired=positioned?new Point(trainingOptions.HudX,trainingOptions.HudY):new Point(area.Left+(area.Width-trainingHud.Width)/2,area.Top+P(30));
        if(trainingOptions.HudAnchor>0&&trainingOptions.HudAnchor<=9)desired=TimingData.AnchoredPosition(area,trainingHud.Size,trainingOptions.HudAnchor,P(12));
        trainingHud.Location=TimingData.ConstrainPosition(desired,trainingHud.Size,area);
    }
    void ProcessStintCommands(){
        string dir=Path.Combine(CompanionStore.Root,"stint-commands");if(!Directory.Exists(dir))return;
        foreach(string file in Directory.GetFiles(dir,"*.txt").OrderBy(p=>p,StringComparer.Ordinal).Take(32)){
            try{string command=File.ReadAllText(file);if(command=="visible=0"){trainingShortcutHidden=true;RefreshTrainingHud();}else if(command=="visible=1"){trainingHudRequested=true;trainingShortcutHidden=false;RefreshTrainingHud();}else if(command=="locked=0"){trainingHudRequested=true;RefreshTrainingHud();if(trainingHud!=null&&!trainingHud.IsDisposed)trainingHud.SetLocked(false);}else if(command=="locked=1"&&trainingHud!=null&&!trainingHud.IsDisposed)trainingHud.SetLocked(true);else if((command=="compact=0"||command=="compact=1")&&trainingHud!=null&&!trainingHud.IsDisposed){trainingHud.SetCompact(command=="compact=1");ApplyTrainingPosition();}}
            catch(Exception ex){CompanionStore.Log("Atalho do stint: "+ex.Message);}finally{try{File.Delete(file);}catch{}}
        }
    }
    public void PreviewTraining(){
        var run=new TrainingRun{Id="preview",Options=new TrainingOptions{Target=82.7,Tolerance=.5},State="completed",Track="Paul Ricard - 3A",CreatedUtc=DateTime.UtcNow,Elapsed=1242,OffTracks=2,Reason="Stint de demonstração · 15 voltas"};double[] times={83.2,82.9,82.0,82.8,83.35,82.45,82.65,82.75,83.5,82.55,82.4,82.95,82.7,82.6,81.9};for(int i=0;i<times.Length;i++)run.Laps.Add(new TrainingLap{Number=i+1,Time=times[i],Valid=i!=4&&i!=8,CompleteData=true,OffTracks=i==4||i==8?1:0,S1=i==2?(double?)null:18+(i%3)*.05,S2=33+(i%2)*.06,S3=times[i]-18-(i%3)*.05-33-(i%2)*.06});training.Restore(run);RefreshTrainingHistory();refreshTraining();
    }
    public void RenderTrainingHud(string destination){using(var hud=new TrainingHud(scale,point=>{})){hud.Run=training.Run;hud.Notice=training.Notice;hud.StartPosition=FormStartPosition.Manual;hud.Location=new Point(-20000,-20000);hud.Show();Application.DoEvents();using(var bitmap=new Bitmap(hud.ClientSize.Width,hud.ClientSize.Height)){hud.DrawToBitmap(bitmap,new Rectangle(Point.Empty,hud.ClientSize));bitmap.Save(destination);}hud.Close();}}
}
