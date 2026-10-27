// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

public sealed partial class CompanionWindow {
    double? TrainingBest(out string context){
        var sample=trainingRecords.Latest;
        if(sample!=null){
            context=sample.Track+" · "+sample.Driver;string key=TimingData.Normalize(sample.Track)+"|"+TimingData.Normalize(sample.CarClass)+"|"+TimingData.Normalize(sample.Driver);var record=trainingRecords.Book.Entries.FirstOrDefault(x=>x.Key==key);double best=sample.BestLap;if(record!=null&&record.Time.HasValue&&(!TrainingMath.Positive(best)||record.Time.Value<best))best=record.Time.Value;if(TrainingMath.Positive(best))return best;
        }
        var profile=CompetitionProfiles.Selected(settings);var cache=profile==null?null:CompanionStore.Read<CachedSnapshot>(Path.Combine(CompetitionProfiles.DataDirectory(profile),"cache.json"));var row=cache==null||cache.Data==null?null:(cache.Data.rows??new System.Collections.Generic.List<TimingRow>()).FirstOrDefault(x=>TimingData.Normalize(x.driverName)==TimingData.Normalize(driver.Text));
        if(row!=null&&cache.Data.activeRound!=null){context=cache.Data.activeRound.trackName+" · "+row.driverName;string key=TimingData.Normalize(cache.Data.activeRound.trackName)+"|"+TimingData.Normalize(row.vehicleClass)+"|"+TimingData.Normalize(row.driverName);var record=trainingRecords.Book.Entries.FirstOrDefault(x=>x.Key==key);double best=row.bestLapTime;if(record!=null&&record.Time.HasValue&&(!TrainingMath.Positive(best)||record.Time.Value<best))best=record.Time.Value;if(TrainingMath.Positive(best))return best;}
        context="Introduz uma melhor volta válida nas condições de referência.";return null;
    }
    void ShowTrainingHelp(Action<double> apply,bool active){using(var dialog=CreateTrainingHelp(apply,active))dialog.ShowDialog(this);}
    Form CreateTrainingHelp(Action<double> apply,bool active,double? example=null){
        string context;double? known=TrainingBest(out context);if(example.HasValue){known=example;context="Exemplo de cálculo";}
        var dialog=new Form{Text="Preparar um stint",Icon=BrandIcon(),BackColor=GtcTheme.Background,ForeColor=GtcTheme.Text,Font=F(12),AutoScaleMode=AutoScaleMode.None,StartPosition=FormStartPosition.CenterParent,ClientSize=new Size(P(540),P(610)),MinimumSize=new Size(P(500),P(450)),MinimizeBox=false,MaximizeBox=false};
        var stack=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,Padding=new Padding(P(18))};stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true};scroll.Controls.Add(stack);dialog.Controls.Add(scroll);
        Action<string,int,bool> text=(value,size,bold)=>{var label=TextLabel(value,size,bold);label.MaximumSize=new Size(P(475),0);label.Margin=new Padding(0,0,0,P(12));stack.Controls.Add(label);};
        text("Ritmo de corrida a partir da melhor volta",16,true);
        text("Alvo = melhor volta válida × (1 + margem / 100).",12,true);
        text("Como estimativa inicial, experimenta acrescentar 0,5–1,0% ao tempo. É uma margem ajustável, não uma regra universal: combustível, pneus, temperatura e tráfego alteram o ritmo. Confirma o alvo com a média de 10–15 voltas nas condições da corrida.",11,false);
        var calculator=new TableLayoutPanel{AutoSize=true,Dock=DockStyle.Top,ColumnCount=2,BackColor=GtcTheme.Surface,Padding=new Padding(P(12)),Margin=new Padding(0,0,0,P(12))};calculator.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,65));calculator.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,35));stack.Controls.Add(calculator);
        calculator.Controls.Add(TextLabel("Melhor volta válida",11),0,0);calculator.Controls.Add(TextLabel("Margem (%)",11),1,0);
        var best=new TextBox{Text=known.HasValue?TimingData.Lap(known.Value):"",Font=F(12),BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,BorderStyle=BorderStyle.FixedSingle,Dock=DockStyle.Top,Margin=new Padding(0,P(6),P(12),P(8)),AccessibleName="Melhor volta válida para calcular ritmo de corrida"};var margin=new NumericUpDown{Minimum=0,Maximum=10,DecimalPlaces=2,Increment=.1m,Value=.5m,Font=F(12),BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Dock=DockStyle.Top,Margin=new Padding(0,P(6),0,P(8)),AccessibleName="Margem percentual sobre a melhor volta"};calculator.Controls.Add(best,0,1);calculator.Controls.Add(margin,1,1);
        var source=TextLabel(context,10);source.ForeColor=GtcTheme.Muted;source.MaximumSize=new Size(P(445),0);calculator.Controls.Add(source,0,2);calculator.SetColumnSpan(source,2);
        var result=TextLabel("",14,true);result.ForeColor=GtcTheme.Accent;result.Margin=new Padding(0,P(10),0,P(10));calculator.Controls.Add(result,0,3);calculator.SetColumnSpan(result,2);
        var use=Button("Aplicar ao próximo stint",245,true);use.Enabled=!active;calculator.Controls.Add(use,0,4);calculator.SetColumnSpan(use,2);double calculated=0;
        Action calculate=()=>{double lap;if(!TrainingMath.ParsePace(best.Text,out lap)){result.Text="Usa o formato m:ss,mmm.";use.Enabled=false;return;}try{calculated=TrainingMath.RacePace(lap,(double)margin.Value);result.Text="Alvo "+TimingData.Lap(calculated)+" · "+TrainingMath.Delta(calculated-lap);use.Enabled=!active;}catch(Exception ex){result.Text=ex.Message;use.Enabled=false;}};best.TextChanged+=(s,e)=>{source.Text="Referência editável · confirma a pista e as condições.";calculate();};margin.ValueChanged+=(s,e)=>calculate();calculate();
        use.Click+=(s,e)=>{apply(calculated);dialog.Close();};
        if(active)text("Termina o stint em curso para aplicar outro alvo.",11,false);
        text("Como configurar o stint",14,true);
        text("1. Abre o rFactor e prepara o combustível, pneus e condições que vais usar na corrida.\n2. Em Treino, escolhe Voltas ou Minutos e a duração.\n3. Introduz o ritmo alvo e a janela ± de consistência — por exemplo, 1:22,700 ±0,50 s significa 1:22,200 a 1:23,200.\n4. Carrega em Iniciar stint antes da passagem pela meta. A volta de saída fica de fora.\n5. Nos treinos por minutos, termina a volta em que o tempo chega a zero. No final, consulta o gráfico e a média de todas as voltas completas, incluindo inválidas.",11,false);
        text("Dois HUDs, um registo de recordes",14,true);
        text("O HUD de stint acompanha duração, média e consistência. O overlay GTC mantém a classificação e os recordes. As voltas válidas e completas observadas pelo Companion alimentam os mesmos recordes locais de tempo e setores, mesmo com o HUD de treino oculto ou sem um stint ativo. Os registos anteriores são preservados. A tabela online da GTC só inclui as voltas registadas no respetivo servidor.",11,false);
        text("As saídas são estimadas pela posição do carro e não equivalem às penalizações oficiais. Uma falha de telemetria pode tornar uma volta incompleta para efeitos de recordes.",10,false);
        var close=Button("Fechar",110);stack.Controls.Add(close);close.Click+=(s,e)=>dialog.Close();GtcWindowTheme.Attach(dialog);return dialog;
    }
    public void RenderTrainingHelp(string destination){using(var dialog=CreateTrainingHelp(value=>{},false,82.247)){dialog.ShowInTaskbar=false;dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-20000,-20000);dialog.Show();Application.DoEvents();using(var bitmap=new Bitmap(dialog.ClientSize.Width,dialog.ClientSize.Height)){dialog.DrawToBitmap(bitmap,new Rectangle(Point.Empty,dialog.ClientSize));bitmap.Save(destination);}dialog.Close();}}
}
