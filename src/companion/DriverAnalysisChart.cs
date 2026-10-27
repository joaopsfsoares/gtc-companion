// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

public sealed class DriverAnalysisChart:Control {
    public DriverHistoryRecord Record;
    public DriverStintSlice Stint;
    public LapMetric Metric;
    public int SelectedLap;
    public event Action<int> LapSelected;
    public double Target=90,Tolerance=.5;
    public bool ShowTarget=true,ShowOther;
    readonly ToolTip tooltip=new ToolTip();
    readonly List<KeyValuePair<PointF,string>> hits=new List<KeyValuePair<PointF,string>>();
    readonly Dictionary<int,PointF> dots=new Dictionary<int,PointF>();
    string lastTip;
    public DriverAnalysisChart(){DoubleBuffered=true;BackColor=GtcTheme.Surface;ForeColor=GtcTheme.Text;AccessibleName="Tempos por volta ou setor, com boxes, stints e seleção de uma volta";}
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);var g=e.Graphics;float scale=g.DpiX/96f;g.ScaleTransform(scale,scale);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        float w=Width/scale,h=Height/scale;hits.Clear();dots.Clear();using(var font=GtcTypography.Create(10,false))using(var bold=GtcTypography.Create(12,true))using(var ink=new SolidBrush(GtcTheme.Text))using(var muted=new SolidBrush(GtcTheme.Muted)){
            if(Record==null){g.DrawString("Escolhe um piloto com voltas disponíveis.",font,muted,20,22);return;}
            var all=Record.Points(Stint);var clean=all.Where(l=>l.Clean&&DriverLapAnalysis.Value(l,Metric).HasValue).ToList();var points=all.Where(l=>(ShowOther||l.Clean)&&DriverLapAnalysis.Value(l,Metric).HasValue).ToList();var baseline=DriverLapAnalysis.Reference(all);
            double? reference=ShowTarget?(Metric==LapMetric.Lap?(double?)Target:DriverLapAnalysis.Value(baseline,Metric)):null;
            string caption=Metric==LapMetric.Lap?"Alvo do treino "+TimingData.Lap(Target)+" · janela ±"+Tolerance.ToString("0.00")+" s":baseline==null?"Sem volta limpa de referência":DriverLapAnalysis.Name(Metric)+" da melhor volta limpa · V"+baseline.Number+": "+DriverLapAnalysis.Format(DriverLapAnalysis.Value(baseline,Metric),Metric);
            if(!ShowTarget)caption="Comparação desativada";
            g.DrawString(Record.Driver+" · "+DriverLapAnalysis.Name(Metric)+" · "+(Stint==null||Stint.Number==0?"Todos os stints":"Stint "+Stint.Number),bold,ink,18,12);
            g.DrawString(Record.Track+" · "+(Record.Source=="gtc"?"GTC":"Local")+" · "+clean.Count+"/"+all.Count+" tempos limpos · média "+DriverLapAnalysis.Format(DriverLapAnalysis.Mean(all,Metric),Metric),font,muted,18,36);
            if(h<210||w<300)return;AnalysisLegend.Draw(g,w,h-AnalysisLegend.Height,font,caption,reference.HasValue,true,Metric==LapMetric.Lap);
            if(points.Count==0){g.DrawString(Metric==LapMetric.Lap?"Sem voltas limpas. Ativa «Incluir outras voltas» para consultar as restantes.":"Sem tempos deste setor na seleção. Os setores ausentes não são estimados.",font,ink,new RectangleF(24,85,w-48,h-190));return;}
            double min=points.Min(l=>DriverLapAnalysis.Value(l,Metric).Value),max=points.Max(l=>DriverLapAnalysis.Value(l,Metric).Value);if(reference.HasValue){double window=Metric==LapMetric.Lap?Tolerance:0;min=Math.Min(reference.Value-window,min);max=Math.Max(reference.Value+window,max);}double pad=Math.Max(.15,(max-min)*.15);min=Math.Max(0,min-pad);max+=pad;
            var plot=new RectangleF(78,88,Math.Max(30,w-106),Math.Max(20,h-AnalysisLegend.Height-128));Func<double,float> y=time=>plot.Bottom-(float)((time-min)/(max-min))*plot.Height;
            int from=Stint==null||Stint.Number==0?1:Stint.AfterLap+1,to=Math.Max(from+1,Stint==null||Stint.Number==0?Record.TotalLaps:Stint.ThroughLap);Func<double,float> x=lap=>plot.Left+(float)((lap-from)/(to-from))*plot.Width;
            using(var grid=new Pen(GtcTheme.Border,1))for(int tick=0;tick<5;tick++){double time=min+(max-min)*tick/4;float line=y(time);g.DrawLine(grid,plot.Left,line,plot.Right,line);string label=DriverLapAnalysis.Format(time,Metric);g.DrawString(label,font,muted,plot.Left-g.MeasureString(label,font).Width-9,line-8);}
            if(reference.HasValue){if(Metric==LapMetric.Lap){double low=reference.Value-Tolerance,high=reference.Value+Tolerance;using(var band=new SolidBrush(Color.FromArgb(22,GtcTheme.Accent)))g.FillRectangle(band,plot.Left,y(high),plot.Width,y(low)-y(high));using(var pen=new Pen(GtcTheme.Muted,1)){pen.DashStyle=DashStyle.Dash;g.DrawLine(pen,plot.Left,y(low),plot.Right,y(low));g.DrawLine(pen,plot.Left,y(high),plot.Right,y(high));}}using(var pen=new Pen(GtcTheme.Accent,2))g.DrawLine(pen,plot.Left,y(reference.Value),plot.Right,y(reference.Value));}
            int step=Math.Max(1,(int)Math.Ceiling((to-from)/12d));for(int lap=from;lap<=to;lap+=step)g.DrawString(lap.ToString(),font,muted,x(lap)-5,plot.Bottom+8);
            int markerIndex=0;foreach(var marker in Record.Markers().Where(m=>m.AtLap>=from-.5&&m.AtLap<=to+.5)){float px=Math.Max(plot.Left,Math.Min(plot.Right,x(marker.AtLap)));using(var pen=new Pen(GtcTheme.Accent,1.5f)){pen.DashStyle=marker.Approximate?DashStyle.Dot:DashStyle.Dash;g.DrawLine(pen,px,plot.Top,px,plot.Bottom);}g.DrawString("PIT"+(marker.Approximate?" ~":""),font,ink,Math.Min(px+3,plot.Right-35),plot.Top-19-(markerIndex++%2)*14);hits.Add(new KeyValuePair<PointF,string>(new PointF(px*scale,(plot.Top-10)*scale),marker.Label));}
            foreach(var lap in points){double value=DriverLapAnalysis.Value(lap,Metric).Value;float px=x(lap.Number),py=y(value);using(var brush=new SolidBrush(AnalysisLegend.Point(lap.Clean,value,reference,Metric==LapMetric.Lap?(double?)Tolerance:null)))g.FillEllipse(brush,px-4,py-4,8,8);if(!lap.Clean)using(var pen=new Pen(lap.Pit==true?GtcTheme.Accent:GtcTheme.Text,1.4f)){g.DrawLine(pen,px-5,py-5,px+5,py+5);g.DrawLine(pen,px-5,py+5,px+5,py-5);}if(lap.Number==SelectedLap)using(var pen=new Pen(GtcTheme.Text,1.5f))g.DrawEllipse(pen,px-8,py-8,16,16);
                string tip="Volta "+lap.Number+" · "+DriverLapAnalysis.Name(Metric)+" "+DriverLapAnalysis.Format(value,Metric)+(reference.HasValue?" · "+TrainingMath.Delta(value-reference.Value):"")+"\n"+(lap.Pit==true?"Volta com boxes · ":"")+(lap.Valid==false?"Inválida":lap.Clean?"Válida, sem boxes":"Dados / validade incompletos");if(lap.AtUtc.HasValue)tip+=" · "+lap.AtUtc.Value.ToLocalTime().ToString("dd/MM HH:mm:ss");tip+="\nClica para consultar esta volta.";hits.Add(new KeyValuePair<PointF,string>(new PointF(px*scale,py*scale),tip));dots[lap.Number]=new PointF(px*scale,py*scale);
            }
        }
    }
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);var hit=hits.FirstOrDefault(d=>Math.Abs(d.Key.X-e.X)<12&&Math.Abs(d.Key.Y-e.Y)<12);string text=hit.Value;if(text!=lastTip){lastTip=text;tooltip.SetToolTip(this,text);}}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;var hit=dots.FirstOrDefault(d=>Math.Abs(d.Value.X-e.X)<12&&Math.Abs(d.Value.Y-e.Y)<12);if(hit.Key>0&&LapSelected!=null)LapSelected(hit.Key);}
    public void SaveImage(string path){using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(Point.Empty,Size));bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);}}
    protected override void Dispose(bool disposing){if(disposing)tooltip.Dispose();base.Dispose(disposing);}
}
