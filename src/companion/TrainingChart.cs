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
    readonly ToolTip tooltip=new ToolTip();
    readonly List<KeyValuePair<PointF,TrainingLap>> dots=new List<KeyValuePair<PointF,TrainingLap>>();
    string lastTip;
    public TrainingRun Run{get{return run;}set{run=value;Invalidate();}}
    public TrainingChart(){DoubleBuffered=true;BackColor=GtcTheme.Surface;ForeColor=GtcTheme.Text;AccessibleName="Gráfico dos tempos por volta, ritmo alvo e janela de consistência";}
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);var g=e.Graphics;float scale=g.DpiX/96f;g.ScaleTransform(scale,scale);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        float w=Width/scale,h=Height/scale;dots.Clear();using(var font=GtcTypography.Create(10,false))using(var bold=GtcTypography.Create(10,true))using(var ink=new SolidBrush(GtcTheme.Text))using(var muted=new SolidBrush(GtcTheme.Muted)){
            if(run==null){g.DrawString("As voltas do stint vão aparecer aqui.",font,muted,20,22);return;}
            var options=run.Options;double low=options.Target-options.Tolerance,high=options.Target+options.Tolerance;
            double minimum=Math.Min(low,run.Laps.Select(l=>l.Time).DefaultIfEmpty(low).Min()),maximum=Math.Max(high,run.Laps.Select(l=>l.Time).DefaultIfEmpty(high).Max());double padding=Math.Max(.25,(maximum-minimum)*.18);minimum=Math.Max(0,minimum-padding);maximum+=padding;
            var plot=new RectangleF(76,18,Math.Max(20,w-100),Math.Max(35,h-75));Func<double,float> y=time=>plot.Bottom-(float)((time-minimum)/(maximum-minimum))*plot.Height;int count=Math.Max(2,run.Laps.Count);Func<int,float> x=number=>plot.Left+(number-1)*plot.Width/(count-1);
            using(var band=new SolidBrush(Color.FromArgb(22,GtcTheme.Accent)))g.FillRectangle(band,plot.Left,y(high),plot.Width,y(low)-y(high));
            using(var grid=new Pen(GtcTheme.Border,1))for(int tick=0;tick<5;tick++){double time=minimum+(maximum-minimum)*tick/4;float line=y(time);g.DrawLine(grid,plot.Left,line,plot.Right,line);string label=TimingData.Lap(time);g.DrawString(label,font,muted,plot.Left-g.MeasureString(label,font).Width-9,line-8);}
            using(var pen=new Pen(GtcTheme.Muted,1)){pen.DashStyle=DashStyle.Dash;g.DrawLine(pen,plot.Left,y(low),plot.Right,y(low));g.DrawLine(pen,plot.Left,y(high),plot.Right,y(high));}
            using(var goal=new Pen(GtcTheme.Accent,2))g.DrawLine(goal,plot.Left,y(options.Target),plot.Right,y(options.Target));
            int step=Math.Max(1,(int)Math.Ceiling(run.Laps.Count/12d));for(int number=1;number<=Math.Max(1,run.Laps.Count);number+=step)g.DrawString(number.ToString(),font,muted,x(number)-4,plot.Bottom+8);
            foreach(var lap in run.Laps){float px=x(lap.Number),py=y(lap.Time);Color color=lap.Time<=options.Target?Color.FromArgb(83,218,154):Color.FromArgb(255,112,126);using(var point=new SolidBrush(color))g.FillEllipse(point,px-4,py-4,8,8);if(!lap.Valid||!lap.CompleteData)using(var pen=new Pen(GtcTheme.Text,1.5f)){g.DrawLine(pen,px-5,py-5,px+5,py+5);g.DrawLine(pen,px-5,py+5,px+5,py-5);}if(lap.OffTracks>0)using(var pen=new Pen(GtcTheme.Accent,1.5f))g.DrawEllipse(pen,px-7,py-7,14,14);dots.Add(new KeyValuePair<PointF,TrainingLap>(new PointF(px*scale,py*scale),lap));}
            string legend="Alvo "+TimingData.Lap(options.Target)+"   ·   Janela ±"+options.Tolerance.ToString("0.00",System.Globalization.CultureInfo.GetCultureInfo("pt-PT"))+" s   ·   × inválida / dados incompletos";g.DrawString(legend,font,ink,plot.Left,h-25);
        }
    }
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);var hit=dots.FirstOrDefault(d=>Math.Abs(d.Key.X-e.X)<12&&Math.Abs(d.Key.Y-e.Y)<12);string text=hit.Value==null?null:"Volta "+hit.Value.Number+" · "+TimingData.Lap(hit.Value.Time)+" · "+TrainingMath.Delta(hit.Value.Time-run.Options.Target)+"\nSaídas estimadas: "+hit.Value.OffTracks+(!hit.Value.Valid?" · Inválida":"")+(!hit.Value.CompleteData?" · Telemetria incompleta":"");if(text!=lastTip){lastTip=text;tooltip.SetToolTip(this,text);}}
    public void SaveImage(string path){using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(Point.Empty,Size));bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);}}
    protected override void Dispose(bool disposing){if(disposing)tooltip.Dispose();base.Dispose(disposing);}
}
