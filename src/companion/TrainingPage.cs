// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

public sealed partial class CompanionWindow {
    readonly TrainingModel training=new TrainingModel();
    readonly Rf2ScoringReader trainingReader=new Rf2ScoringReader();
    readonly System.Windows.Forms.Timer trainingTimer=new System.Windows.Forms.Timer{Interval=200};
    TrainingOptions trainingOptions;
    bool trainingHudRequested;
    TrainingHud trainingHud;
    TrainingChart trainingChart;
    ComboBox trainingHistory;
    Label trainingSummary,trainingDetails,trainingStatus;
    Action refreshTraining;
    int savedTrainingRevision=-1;DateTime savedTrainingAt=DateTime.MinValue;
    sealed class TrainingHistoryItem {
        public TrainingRun Run;
        public override string ToString(){return Run==null?"Stint atual":Run.CreatedUtc.ToLocalTime().ToString("dd/MM HH:mm")+" · "+(Run.Track??"Sem pista")+" · "+Run.Laps.Count+" voltas";}
    }
    void CreateTrainingPage(Panel page){
        foreach(Control control in page.Controls.Cast<Control>().ToArray())control.Dispose();page.AutoScroll=false;
        trainingOptions=CompanionStore.Read<TrainingOptions>(Path.Combine("training","options.json"))??new TrainingOptions();try{trainingOptions.Validate();}catch{trainingOptions=new TrainingOptions();}
        training.Restore(TrainingStore.History().FirstOrDefault());
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=7,Margin=Padding.Empty};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));for(int row=0;row<6;row++)root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));page.Controls.Add(root);
        var title=TextLabel("Treino de ritmo",19,true);title.Margin=new Padding(0,0,0,P(12));root.Controls.Add(title,0,0);
        var options=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,BackColor=GtcTheme.Surface,Padding=new Padding(P(12)),Margin=new Padding(0,0,0,P(10))};options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));root.Controls.Add(options,0,1);
        Func<string,Control,Panel> field=(label,control)=>{var panel=new Panel{Height=P(65),Dock=DockStyle.Top,Margin=new Padding(0,0,P(10),0)};var heading=TextLabel(label,11);panel.Controls.Add(heading);control.Location=new Point(0,P(25));control.Width=P(215);panel.Controls.Add(control);return panel;};
        var duration=new FlowLayoutPanel{AutoSize=true,WrapContents=false,Margin=Padding.Empty};var mode=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Font=F(12),Width=P(125)};mode.Items.AddRange(new object[]{"Voltas","Minutos"});mode.SelectedIndex=trainingOptions.Minutes?1:0;var amount=new NumericUpDown{Minimum=1,Maximum=999,Value=trainingOptions.Amount,BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Font=F(12),Width=P(75)};duration.Controls.Add(mode);duration.Controls.Add(amount);options.Controls.Add(field("Duração do stint",duration),0,0);
        var target=new TextBox{Text=TimingData.Lap(trainingOptions.Target),BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Font=F(12),BorderStyle=BorderStyle.FixedSingle,AccessibleName="Ritmo alvo em minutos e segundos"};options.Controls.Add(field("Ritmo alvo · m:ss,mmm",target),1,0);
        var tolerance=new NumericUpDown{Minimum=.05m,Maximum=30,DecimalPlaces=2,Increment=.05m,Value=(decimal)trainingOptions.Tolerance,BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Font=F(12),AccessibleName="Janela de consistência em segundos"};options.Controls.Add(field("Janela de consistência · ± segundos",tolerance),0,1);
        var appearance=new FlowLayoutPanel{AutoSize=true,WrapContents=false};var show=new BrandCheck{Text="HUD",Checked=trainingOptions.ShowHud,AutoSize=true,Font=F(12),BackColor=GtcTheme.Surface,ForeColor=GtcTheme.Text,Margin=new Padding(0,P(4),P(10),0)};var opacity=new NumericUpDown{Minimum=40,Maximum=100,Value=Math.Max(40,Math.Min(100,trainingOptions.Opacity)),BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Font=F(12),Width=P(75),AccessibleName="Opacidade do HUD em percentagem"};appearance.Controls.Add(show);appearance.Controls.Add(opacity);options.Controls.Add(field("Overlay de treino · opacidade %",appearance),1,1);
        var actions=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Top,WrapContents=true,Margin=new Padding(0,0,0,P(9))};root.Controls.Add(actions,0,2);var begin=Button("Iniciar stint",140,true);var end=Button("Terminar",110);var move=Button("Mover HUD",120);var export=Button("Guardar gráfico",155);foreach(var button in new[]{begin,end,move}){button.Margin=new Padding(0,0,P(6),P(6));actions.Controls.Add(button);}
        var summary=new TableLayoutPanel{AutoSize=true,Dock=DockStyle.Top,ColumnCount=1,Margin=new Padding(0,0,0,P(8))};summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));trainingSummary=TextLabel("",14,true);trainingSummary.MaximumSize=new Size(P(530),0);trainingSummary.Margin=new Padding(0,0,0,P(5));summary.Controls.Add(trainingSummary);trainingDetails=TextLabel("",12);trainingDetails.MaximumSize=new Size(P(530),0);summary.Controls.Add(trainingDetails);root.Controls.Add(summary,0,3);
        trainingStatus=TextLabel("",11);trainingStatus.MaximumSize=new Size(P(530),0);trainingStatus.ForeColor=GtcTheme.Muted;trainingStatus.Margin=new Padding(0,0,0,P(10));root.Controls.Add(trainingStatus,0,4);
        trainingHistory=new ComboBox{Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Font=F(11),Margin=new Padding(0,P(5),P(8),0),AccessibleName="Histórico de stints"};var historyRow=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,Margin=new Padding(0,0,0,P(10))};historyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));historyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));historyRow.Controls.Add(trainingHistory,0,0);historyRow.Controls.Add(export,1,0);root.Controls.Add(historyRow,0,5);
        trainingChart=new TrainingChart{Dock=DockStyle.Fill,Margin=Padding.Empty};root.Controls.Add(trainingChart,0,6);
        Action saveOptions=()=>{Directory.CreateDirectory(Path.Combine(CompanionStore.Root,"training"));CompanionStore.Write(Path.Combine("training","options.json"),trainingOptions);};
        show.CheckedChanged+=(s,e)=>{trainingOptions.ShowHud=show.Checked;if(show.Checked)trainingHudRequested=true;saveOptions();RefreshTrainingHud();};opacity.ValueChanged+=(s,e)=>{trainingOptions.Opacity=(int)opacity.Value;saveOptions();RefreshTrainingHud();};
        begin.Click+=(s,e)=>{StopCountdown();try{double pace;if(!TrainingMath.ParsePace(target.Text,out pace))throw new ArgumentException("Indica o ritmo como 1:22,700 ou 1:22.700.");var next=trainingOptions.Copy();next.Minutes=mode.SelectedIndex==1;next.Amount=(int)amount.Value;next.Target=pace;next.Tolerance=(double)tolerance.Value;next.Validate();training.Arm(next);trainingHudRequested=true;trainingOptions=next;saveOptions();TrainingStore.Save(training.Run);RefreshTrainingHistory();refreshTraining();RefreshTrainingHud();}catch(Exception ex){trainingStatus.Text=ex.Message;}};
        end.Click+=(s,e)=>{training.Stop();TrainingStore.Save(training.Run);RefreshTrainingHistory();refreshTraining();RefreshTrainingHud();};
        move.Click+=(s,e)=>{StopCountdown();if(training.Run==null){trainingStatus.Text="Inicia um stint para mostrar e mover o HUD.";return;}trainingHudRequested=true;show.Checked=true;RefreshTrainingHud();if(trainingHud!=null)trainingHud.Unlock();};
        export.Click+=(s,e)=>{if(trainingChart.Run==null||trainingChart.Run.Laps.Count==0){trainingStatus.Text="Completa uma volta para guardar o gráfico.";return;}using(var dialog=new SaveFileDialog{Filter="Imagem PNG|*.png",FileName="stint-"+trainingChart.Run.CreatedUtc.ToLocalTime().ToString("yyyyMMdd-HHmm")+".png"})if(dialog.ShowDialog(this)==DialogResult.OK)try{trainingChart.SaveImage(dialog.FileName);trainingStatus.Text="Gráfico guardado.";}catch(Exception ex){trainingStatus.Text=ex.Message;}};
        refreshTraining=()=>{
            bool active=training.Run!=null&&training.Run.Active;begin.Enabled=!active;end.Enabled=active;mode.Enabled=amount.Enabled=target.Enabled=tolerance.Enabled=!active;
            var selection=trainingHistory.SelectedItem as TrainingHistoryItem;var run=selection!=null&&selection.Run!=null?selection.Run:training.Run;trainingChart.Run=run;
            if(run==null){trainingSummary.Text="Define a duração, o alvo e a janela para começar.";trainingDetails.Text="";trainingSummary.ForeColor=GtcTheme.Text;trainingStatus.Text="Média de todas as voltas completas, incluindo inválidas. Saídas estimadas: carro além do limite da pista durante pelo menos 0,6 s.";return;}
            string last=run.Laps.Count==0?"—":TimingData.Lap(run.Laps.Last().Time)+" ("+TrainingMath.Delta(run.Laps.Last().Time-run.Options.Target)+")";
            trainingSummary.Text="Média "+TrainingMath.Lap(run.Average)+(run.Average.HasValue?" · "+TrainingMath.Delta(run.Average.Value-run.Options.Target):"");
            trainingDetails.Text=run.Laps.Count+" voltas · "+TrainingMath.Remaining(run)+" · Tempo "+TrainingMath.Clock(run.Elapsed)+"\nÚltima "+last+"\nNa janela "+run.InWindow+"/"+run.Laps.Count+" · Válidas "+run.Laps.Count(l=>l.Valid&&l.CompleteData)+"/"+run.Laps.Count+" · Saídas estimadas "+run.OffTracks;
            trainingSummary.ForeColor=run.Average.HasValue?(run.Average.Value<=run.Options.Target?Color.FromArgb(83,218,154):Color.FromArgb(255,112,126)):GtcTheme.Text;
            trainingStatus.Text=(run==training.Run?training.Notice:run.Reason)+"\nMédia inclui inválidas · Saídas estimadas, sem equivalência às penalizações."+(run.HadTelemetryGap?" Houve falhas de telemetria.":"");
        };
        trainingHistory.SelectedIndexChanged+=(s,e)=>refreshTraining();RefreshTrainingHistory();refreshTraining();
        trainingTimer.Tick+=(s,e)=>{
            bool wasActive=training.Run!=null&&training.Run.Active;var sample=trainingReader.Read();training.Update(sample);if(sample==null&&training.Run!=null&&training.Run.State=="running"&&!LaunchEngine.Running("rFactor2"))training.Stop("O rFactor foi fechado.",true);
            if(training.Run!=null&&(training.Revision!=savedTrainingRevision||training.Run.Active&&(DateTime.UtcNow-savedTrainingAt).TotalSeconds>=5))try{TrainingStore.Save(training.Run);savedTrainingRevision=training.Revision;savedTrainingAt=DateTime.UtcNow;}catch(Exception ex){training.Notice="Não foi possível guardar o stint: "+ex.Message;}
            if(wasActive&&!training.Run.Active){RefreshTrainingHistory();Report("Stint terminado. O gráfico está na página Treino.");}
            if(page.Visible)refreshTraining();RefreshTrainingHud();
        };
        page.VisibleChanged+=(s,e)=>{if(page.Visible)refreshTraining();};Shown+=(s,e)=>{if(!PreviewOnly)trainingTimer.Start();};
        FormClosed+=(s,e)=>{trainingTimer.Dispose();trainingReader.Dispose();if(training.Run!=null&&training.Run.Active){training.Stop("Companion fechado durante o stint.",true);try{TrainingStore.Save(training.Run);}catch{}}if(trainingHud!=null)trainingHud.Close();};
    }
    void RefreshTrainingHistory(){
        if(trainingHistory==null)return;trainingHistory.Items.Clear();trainingHistory.Items.Add(new TrainingHistoryItem());foreach(var run in TrainingStore.History().Where(run=>training.Run==null||run.Id!=training.Run.Id))trainingHistory.Items.Add(new TrainingHistoryItem{Run=run});trainingHistory.SelectedIndex=0;
    }
    void RefreshTrainingHud(){
        if(PreviewOnly)return;
        if(!trainingHudRequested||!trainingOptions.ShowHud||training.Run==null){if(trainingHud!=null)trainingHud.Hide();return;}
        if(trainingHud==null||trainingHud.IsDisposed){trainingHud=new TrainingHud(scale,point=>{trainingOptions.HudX=point.X;trainingOptions.HudY=point.Y;Directory.CreateDirectory(Path.Combine(CompanionStore.Root,"training"));CompanionStore.Write(Path.Combine("training","options.json"),trainingOptions);});var area=Screen.FromControl(this).WorkingArea;bool positioned=trainingOptions.HudX!=-1||trainingOptions.HudY!=-1;var desired=positioned?new Point(trainingOptions.HudX,trainingOptions.HudY):new Point(area.Left+(area.Width-trainingHud.Width)/2,area.Top+P(30));if(positioned)area=Screen.FromPoint(desired).WorkingArea;trainingHud.StartPosition=FormStartPosition.Manual;trainingHud.Location=TimingData.ConstrainPosition(desired,trainingHud.Size,area);}
        trainingHud.Opacity=trainingOptions.Opacity/100d;trainingHud.Run=training.Run;trainingHud.Notice=training.Notice;if(!trainingHud.Visible)trainingHud.Show();trainingHud.Invalidate();
    }
    public void PreviewTraining(){
        var run=new TrainingRun{Id="preview",Options=new TrainingOptions{Target=82.7,Tolerance=.5},State="completed",Track="Paul Ricard - 3A",CreatedUtc=DateTime.UtcNow,Elapsed=1242,OffTracks=2,Reason="Stint de demonstração · 15 voltas"};double[] times={83.2,82.9,82.6,82.8,83.35,82.45,82.65,82.75,83.5,82.55,82.4,82.95,82.7,82.6,82.85};for(int i=0;i<times.Length;i++)run.Laps.Add(new TrainingLap{Number=i+1,Time=times[i],Valid=i!=4&&i!=8,CompleteData=true,OffTracks=i==4||i==8?1:0});training.Restore(run);RefreshTrainingHistory();refreshTraining();
    }
    public void RenderTrainingHud(string destination){using(var hud=new TrainingHud(scale,point=>{})){hud.Run=training.Run;hud.Notice=training.Notice;hud.StartPosition=FormStartPosition.Manual;hud.Location=new Point(-20000,-20000);hud.Show();Application.DoEvents();using(var bitmap=new Bitmap(hud.ClientSize.Width,hud.ClientSize.Height)){hud.DrawToBitmap(bitmap,new Rectangle(Point.Empty,hud.ClientSize));bitmap.Save(destination);}hud.Close();}}
}
