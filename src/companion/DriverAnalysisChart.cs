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
    public double Target=90,Tolerance=.5;
    public bool ShowTarget=true,ShowOther;
    readonly ToolTip tooltip=new ToolTip();
    readonly List<KeyValuePair<PointF,string>> hits=new List<KeyValuePair<PointF,string>>();
    string lastTip;
    public DriverAnalysisChart(){DoubleBuffered=true;BackColor=GtcTheme.Surface;ForeColor=GtcTheme.Text;AccessibleName="Tempos por volta do piloto, com passagens pelas boxes e separação de stints";}
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);var g=e.Graphics;float scale=g.DpiX/96f;g.ScaleTransform(scale,scale);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        float w=Width/scale,h=Height/scale;hits.Clear();using(var font=GtcTypography.Create(10,false))using(var bold=GtcTypography.Create(12,true))using(var ink=new SolidBrush(GtcTheme.Text))using(var muted=new SolidBrush(GtcTheme.Muted)){
            if(Record==null){g.DrawString("Escolhe um piloto com voltas disponíveis.",font,muted,20,22);return;}
            g.DrawString(Record.Driver+" · "+(Stint==null||Stint.Number==0?"Todos os stints":"Stint "+Stint.Number),bold,ink,18,12);
            var all=Record.Points(Stint);var clean=all.Where(l=>l.Clean).ToList();var points=all.Where(l=>ShowOther||l.Clean).ToList();
            string summary=Record.Track+" · "+(Record.Source=="gtc"?"GTC":"Local")+" · "+all.Count+" voltas disponíveis · média limpa "+(clean.Count==0?"—":TimingData.Lap(clean.Average(l=>l.Time)))+" ("+clean.Count+")";
            g.DrawString(summary,font,muted,18,36);
            if(h<180||w<260)return;
            double low=Target-Tolerance,high=Target+Tolerance;
            double min=points.Select(l=>l.Time).DefaultIfEmpty(Target).Min(),max=points.Select(l=>l.Time).DefaultIfEmpty(Target).Max();if(ShowTarget){min=Math.Min(low,min);max=Math.Max(high,max);}double pad=Math.Max(.3,(max-min)*.15);min=Math.Max(0,min-pad);max+=pad;
            var plot=new RectangleF(78,88,Math.Max(30,w-106),Math.Max(35,h-158));Func<double,float> y=time=>plot.Bottom-(float)((time-min)/(max-min))*plot.Height;
            int from=Stint==null||Stint.Number==0?1:Stint.AfterLap+1,to=Math.Max(from+1,Stint==null||Stint.Number==0?Record.TotalLaps:Stint.ThroughLap);Func<double,float> x=lap=>plot.Left+(float)((lap-from)/(to-from))*plot.Width;
            using(var grid=new Pen(GtcTheme.Border,1))for(int tick=0;tick<5;tick++){double time=min+(max-min)*tick/4;float line=y(time);g.DrawLine(grid,plot.Left,line,plot.Right,line);string label=TimingData.Lap(time);g.DrawString(label,font,muted,plot.Left-g.MeasureString(label,font).Width-9,line-8);}
            if(ShowTarget){using(var band=new SolidBrush(Color.FromArgb(22,GtcTheme.Accent)))g.FillRectangle(band,plot.Left,y(high),plot.Width,y(low)-y(high));using(var pen=new Pen(GtcTheme.Muted,1)){pen.DashStyle=DashStyle.Dash;g.DrawLine(pen,plot.Left,y(low),plot.Right,y(low));g.DrawLine(pen,plot.Left,y(high),plot.Right,y(high));}using(var pen=new Pen(GtcTheme.Accent,2))g.DrawLine(pen,plot.Left,y(Target),plot.Right,y(Target));}
            int step=Math.Max(1,(int)Math.Ceiling((to-from)/12d));for(int lap=from;lap<=to;lap+=step)g.DrawString(lap.ToString(),font,muted,x(lap)-5,plot.Bottom+8);
            int markerIndex=0;foreach(var marker in Record.Markers().Where(m=>m.AtLap>=from-.5&&m.AtLap<=to+.5)){
                float px=Math.Max(plot.Left,Math.Min(plot.Right,x(marker.AtLap)));using(var pen=new Pen(GtcTheme.Accent,1.5f)){pen.DashStyle=marker.Approximate?DashStyle.Dot:DashStyle.Dash;g.DrawLine(pen,px,plot.Top,px,plot.Bottom);}
                string label="PIT"+(marker.Approximate?" ~":"");g.DrawString(label,font,ink,Math.Min(px+3,plot.Right-35),plot.Top-19-(markerIndex++%2)*14);hits.Add(new KeyValuePair<PointF,string>(new PointF(px*scale,(plot.Top-10)*scale),marker.Label+(marker.Approximate?" · limite aproximado":" · entrada observada")));
            }
            foreach(var lap in points){float px=x(lap.Number),py=y(lap.Time);Color color=!lap.Clean?GtcTheme.Muted:!ShowTarget?Color.FromArgb(83,218,154):lap.Time<=Target?Color.FromArgb(83,218,154):Color.FromArgb(255,112,126);using(var brush=new SolidBrush(color))g.FillEllipse(brush,px-4,py-4,8,8);if(!lap.Clean)using(var pen=new Pen(lap.Pit==true?GtcTheme.Accent:GtcTheme.Text,1.4f)){g.DrawLine(pen,px-5,py-5,px+5,py+5);g.DrawLine(pen,px-5,py+5,px+5,py-5);}
                string tip="Volta "+lap.Number+" · "+TimingData.Lap(lap.Time)+(ShowTarget?" · "+TrainingMath.Delta(lap.Time-Target):"")+"\n"+(lap.Pit==true?"Volta com boxes · ":"")+(lap.Valid==false?"Inválida":lap.Clean?"Válida, sem boxes":"Dados / validade incompletos");if(lap.AtUtc.HasValue)tip+=" · "+lap.AtUtc.Value.ToLocalTime().ToString("dd/MM HH:mm:ss");if(lap.S1.HasValue||lap.S2.HasValue||lap.S3.HasValue)tip+="\nS1 "+TrainingMath.Lap(lap.S1)+" · S2 "+TrainingMath.Lap(lap.S2)+" · S3 "+TrainingMath.Lap(lap.S3);hits.Add(new KeyValuePair<PointF,string>(new PointF(px*scale,py*scale),tip));
            }
            if(points.Count==0)g.DrawString("Sem voltas limpas nesta seleção. Ativa «Incluir outras voltas» para ver os dados disponíveis.",font,ink,plot.Left+8,plot.Top+plot.Height/2);
            g.DrawString((ShowTarget?"Alvo "+TimingData.Lap(Target)+" · ±"+Tolerance.ToString("0.00")+" s   ·   ":"")+"PIT boxes · ~ limite aproximado · × outra volta",font,ink,18,h-38);
            g.DrawString("Voltas em falta ficam sem pontos. A média limpa exclui boxes, inválidas e dados incompletos.",font,muted,18,h-20);
        }
    }
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);var hit=hits.FirstOrDefault(d=>Math.Abs(d.Key.X-e.X)<12&&Math.Abs(d.Key.Y-e.Y)<12);string text=hit.Value;if(text!=lastTip){lastTip=text;tooltip.SetToolTip(this,text);}}
    public void SaveImage(string path){using(var bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(Point.Empty,Size));bitmap.Save(path,System.Drawing.Imaging.ImageFormat.Png);}}
    protected override void Dispose(bool disposing){if(disposing)tooltip.Dispose();base.Dispose(disposing);}
}
