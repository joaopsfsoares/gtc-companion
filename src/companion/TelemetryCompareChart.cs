// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

public sealed class TelemetryCompareChart:Control {
    public TelemetryLap First,Second;public int Metric;public double Progress;public event Action<double> ProgressChanged;
    static readonly Color Orange=Color.FromArgb(255,139,24),Blue=Color.FromArgb(110,182,255);
    public TelemetryCompareChart(){DoubleBuffered=true;BackColor=GtcTheme.Surface;Height=500;AccessibleName="Mapa e gráficos das duas voltas";}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Y>Height*.52&&ProgressChanged!=null)ProgressChanged(Math.Max(0,Math.Min(1,(e.X-45.0)/Math.Max(1,Width-60.0))));}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;string problem=TelemetryLap.Problem(First,Second);using(var f=(Font)Font.Clone())using(var ink=new SolidBrush(GtcTheme.Text))using(var muted=new SolidBrush(GtcTheme.Muted)){
        if(problem!=null){g.DrawString(AppLanguage.T(problem),f,muted,new RectangleF(16,16,Math.Max(1,Width-32),120));return;}
        float w=Width,h=Height;var all=First.Points.Concat(Second.Points).ToList();double minx=all.Min(p=>p.X),maxx=all.Max(p=>p.X),minz=all.Min(p=>p.Z),maxz=all.Max(p=>p.Z);float mapH=Math.Max(140,h*.48f);double scale=Math.Min((w-40)/Math.Max(1,maxx-minx),(mapH-35)/Math.Max(1,maxz-minz));
        Func<TelemetryPoint,PointF> map=p=>new PointF((float)(w/2+(p.X-(minx+maxx)/2)*scale),(float)(mapH/2-(p.Z-(minz+maxz)/2)*scale));
        Action<TelemetryLap,Color> drawMap=(lap,col)=>{var points=lap.Points.Where((p,i)=>i%Math.Max(1,lap.Points.Count/1600)==0).Select(map).ToArray();if(points.Length>1)using(var pen=new Pen(col,1.5f))g.DrawLines(pen,points);var pos=map(lap.At(Progress));using(var brush=new SolidBrush(col))g.FillEllipse(brush,pos.X-5,pos.Y-5,10,10);};drawMap(First,Orange);drawMap(Second,Blue);
        var plot=new RectangleF(45,mapH+32,Math.Max(20,w-60),Math.Max(40,h-mapH-66));double low=Metric==2?-1:0,high=1;using(var grid=new Pen(GtcTheme.Border)){for(int k=0;k<=4;k++){float y=plot.Top+plot.Height*k/4;g.DrawLine(grid,plot.Left,y,plot.Right,y);double value=high-(high-low)*k/4;g.DrawString((value*100).ToString("0")+"%",f,muted,0,y-8);}}
        Action<TelemetryLap,Color> draw=(lap,col)=>{var points=Enumerable.Range(0,301).Select(i=>{double t=i/300.0;var p=lap.At(t);double v=Metric==0?p.Throttle:Metric==1?p.Brake:p.Steering;return new PointF(plot.Left+(float)t*plot.Width,plot.Bottom-(float)((v-low)/(high-low))*plot.Height);}).ToArray();using(var pen=new Pen(col,1.8f))g.DrawLines(pen,points);};draw(First,Orange);draw(Second,Blue);
        using(var cursor=new Pen(Color.White,1))g.DrawLine(cursor,plot.Left+(float)Progress*plot.Width,plot.Top,plot.Left+(float)Progress*plot.Width,plot.Bottom);
        g.DrawString("0%",f,muted,plot.Left,plot.Bottom+6);g.DrawString(AppLanguage.T("Progresso aproximado na pista"),f,muted,plot.Left+plot.Width/2-95,plot.Bottom+6);g.DrawString("100%",f,muted,plot.Right-38,plot.Bottom+6);
    }}
}
