// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

public sealed class TrainingChart:Control {
    TrainingRun run;
    public LapMetric Metric;
    readonly ToolTip tooltip=new ToolTip();
    readonly List<KeyValuePair<PointF,TrainingLap>> dots=new List<KeyValuePair<PointF,TrainingLap>>();
    string lastTip;
    public TrainingRun Run{get{return run;}set{run=value;Invalidate();}}
    public TrainingChart(){DoubleBuffered=true;BackColor=GtcTheme.Surface;ForeColor=GtcTheme.Text;AccessibleName="Tempos por volta ou setor, com referência, consistência e legenda";}
    double? Reference(){return run==null?null:Metric==LapMetric.Lap?(double?)run.Options.Target:DriverLapAnalysis.Value(DriverLapAnalysis.Reference(run.Laps),Metric);}
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);var g=e.Graphics;float scale=g.DpiX/96f;g.ScaleTransform(scale,scale);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        float w=Width/scale,h=Height/scale;dots.Clear();using(var font=GtcTypography.Create(10,false))using(var ink=new SolidBrush(GtcTheme.Text))using(var muted=new SolidBrush(GtcTheme.Muted)){
            if(run==null){g.DrawString("As voltas do stint vão aparecer aqui.",font,muted,20,22);return;}
            var options=run.Options;var baseline=DriverLapAnalysis.Reference(run.Laps);var reference=Reference();var points=run.Laps.Where(l=>DriverLapAnalysis.Value(l,Metric).HasValue).ToList();
            string caption=Metric==LapMetric.Lap?"Alvo do treino "+TimingData.Lap(options.Target)+" · janela ±"+options.Tolerance.ToString("0.00")+" s":baseline==null?"Sem volta válida de referência":DriverLapAnalysis.Name(Metric)+" da melhor volta válida · V"+baseline.Number+": "+DriverLapAnalysis.Format(reference,Metric);
            if(h<180)return;AnalysisLegend.Draw(g,w,h-AnalysisLegend.Height,font,caption,reference.HasValue,false,Metric==LapMetric.Lap);
            if(points.Count==0){g.DrawString(Metric==LapMetric.Lap?"Sem voltas concluídas.":"Sem setores guardados neste stint. Os registos antigos podem não conter setores.",font,muted,new RectangleF(20,22,w-40,h-125));return;}
            double min=points.Min(l=>DriverLapAnalysis.Value(l,Metric).Value),max=points.Max(l=>DriverLapAnalysis.Value(l,Metric).Value);if(reference.HasValue){double window=Metric==LapMetric.Lap?options.Tolerance:0;min=Math.Min(reference.Value-window,min);max=Math.Max(reference.Value+window,max);}double padding=Math.Max(.15,(max-min)*.18);min=Math.Max(0,min-padding);max+=padding;
            var assessment=TrainingAssessment.Evaluate(run);g.DrawString(assessment.Label+" · "+assessment.Description,font,ink,18,12);
            var plot=new RectangleF(76,44,Math.Max(20,w-100),Math.Max(20,h-AnalysisLegend.Height-79));Func<double,float> y=time=>plot.Bottom-(float)((time-min)/(max-min))*plot.Height;int count=Math.Max(2,run.Laps.Count);Func<int,float> x=number=>plot.Left+(number-1)*plot.Width/(count-1);
            using(var grid=new Pen(GtcTheme.Border,1))for(int tick=0;tick<5;tick++){double time=min+(max-min)*tick/4;float line=y(time);g.DrawLine(grid,plot.Left,line,plot.Right,line);string label=DriverLapAnalysis.Format(time,Metric);g.DrawString(label,font,muted,plot.Left-g.MeasureString(label,font).Width-9,line-8);}
            if(reference.HasValue){if(Metric==LapMetric.Lap){double low=reference.Value-options.Tolerance,high=reference.Value+options.Tolerance;using(var band=new SolidBrush(Color.FromArgb(22,GtcTheme.Accent)))g.FillRectangle(band,plot.Left,y(high),plot.Width,y(low)-y(high));using(var pen=new Pen(GtcTheme.Muted,1)){pen.DashStyle=DashStyle.Dash;g.DrawLine(pen,plot.Left,y(low),plot.Right,y(low));g.DrawLine(pen,plot.Left,y(high),plot.Right,y(high));}}using(var goal=new Pen(GtcTheme.Accent,2))g.DrawLine(goal,plot.Left,y(reference.Value),plot.Right,y(reference.Value));}
            int step=Math.Max(1,(int)Math.Ceiling(run.Laps.Count/12d));for(int number=1;number<=Math.Max(1,run.Laps.Count);number+=step)g.DrawString(number.ToString(),font,muted,x(number)-4,plot.Bottom+8);
            foreach(var lap in points){double value=DriverLapAnalysis.Value(lap,Metric).Value;float px=x(lap.Number),py=y(value);using(var point=new SolidBrush(AnalysisLegend.Point(lap.Valid&&lap.CompleteData,value,reference,Metric==LapMetric.Lap?(double?)options.Tolerance:null)))g.FillEllipse(point,px-4,py-4,8,8);if(!lap.Valid||!lap.CompleteData)using(var pen=new Pen(GtcTheme.Text,1.5f)){g.DrawLine(pen,px-5,py-5,px+5,py+5);g.DrawLine(pen,px-5,py+5,px+5,py-5);}if(lap.OffTracks>0)using(var pen=new Pen(GtcTheme.Accent,1.5f))g.DrawEllipse(pen,px-7,py-7,14,14);dots.Add(new KeyValuePair<PointF,TrainingLap>(new PointF(px*scale,py*scale),lap));}
        }
    }
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);var hit=dots.FirstOrDefault(d=>Math.Abs(d.Key.X-e.X)<12&&Math.Abs(d.Key.Y-e.Y)<12);var reference=Reference();string text=hit.Value==null?null:"Volta "+hit.Value.Number+" · "+DriverLapAnalysis.Name(Metric)+" "+DriverLapAnalysis.Format(DriverLapAnalysis.Value(hit.Value,Metric),Metric)+(reference.HasValue?" · "+TrainingMath.Delta(DriverLapAnalysis.Value(hit.Value,Metric).Value-reference.Value):"")+"\nSaídas estimadas: "+hit.Value.OffTracks+(!hit.Value.Valid?" · Inválida":"")+(!hit.Value.CompleteData?" · Telemetria incompleta":"");if(text!=lastTip){lastTip=text;tooltip.SetToolTip(this,text);}}
    public void SaveImage(string path){using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(Point.Empty,Size));bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);}}
    protected override void Dispose(bool disposing){if(disposing)tooltip.Dispose();base.Dispose(disposing);}
}
