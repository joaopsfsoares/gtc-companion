// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Linq;
using System.Windows.Forms;

public sealed class TrainingHud:Form {
    [DllImport("user32.dll")]static extern int GetWindowLong(IntPtr window,int index);
    [DllImport("user32.dll")]static extern int SetWindowLong(IntPtr window,int index,int value);
    readonly float scale;readonly Action<Point> moved;
    bool locked=true,dragging;Point mouseStart,windowStart;
    public TrainingRun Run;public string Notice;
    protected override bool ShowWithoutActivation{get{return true;}}
    protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x80|0x08000000|(locked?0x20:0);return p;}}
    public TrainingHud(float dpi,Action<Point> onMove){scale=dpi;moved=onMove;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;BackColor=GtcTheme.Background;DoubleBuffered=true;AutoScaleMode=AutoScaleMode.None;ClientSize=new Size((int)(480*scale),(int)(153*scale));MouseDoubleClick+=(s,e)=>Lock();}
    public void Unlock(){locked=false;if(IsHandleCreated)SetWindowLong(Handle,-20,GetWindowLong(Handle,-20)&~0x20);Invalidate();}
    void Lock(){if(dragging){dragging=false;Capture=false;moved(Location);}locked=true;if(IsHandleCreated)SetWindowLong(Handle,-20,GetWindowLong(Handle,-20)|0x20);Invalidate();}
    protected override void WndProc(ref Message message){if(message.Msg==0x21){message.Result=new IntPtr(3);return;}base.WndProc(ref message);}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(locked||e.Button!=MouseButtons.Left)return;dragging=true;mouseStart=Cursor.Position;windowStart=Location;Capture=true;}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging)Location=new Point(windowStart.X+Cursor.Position.X-mouseStart.X,windowStart.Y+Cursor.Position.Y-mouseStart.Y);}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(dragging){dragging=false;Capture=false;moved(Location);}}
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);var g=e.Graphics;g.ScaleTransform(scale,scale);g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        Action<string,int,int,int,Color,bool> text=(value,x,y,size,color,bold)=>{using(var font=GtcTypography.Create(size,bold))using(var brush=new SolidBrush(color))g.DrawString(value,font,brush,x,y);};
        text(!locked?"ARRASTA · DUPLO CLIQUE PARA BLOQUEAR":"TREINO DE RITMO",14,10,10,GtcTheme.Accent,true);
        if(Run==null)return;
        if(Run.State=="armed"){text("A aguardar passagem pela meta",14,36,18,GtcTheme.Text,true);text("A volta de saída não conta.",14,77,11,GtcTheme.Muted,false);text("Alvo "+TimingData.Lap(Run.Options.Target)+" · "+Run.Options.Amount+(Run.Options.Minutes?" minutos":" voltas"),14,113,12,GtcTheme.Accent,true);return;}
        string count=Run.Options.Minutes?Run.Laps.Count+" voltas · "+TrainingMath.Remaining(Run):Run.Laps.Count+" / "+Run.Options.Amount+" voltas · "+TrainingMath.Remaining(Run);
        text(Run.Active?count:(Run.State=="completed"?"OBJETIVO CONCLUÍDO":Run.State=="interrupted"?"STINT INTERROMPIDO":"TERMINADO MANUALMENTE")+" · "+Run.Laps.Count+" voltas",14,34,13,Run.State=="interrupted"?GtcTheme.Accent:GtcTheme.Text,true);
        text("Média "+TrainingMath.Lap(Run.Average),14,59,20,GtcTheme.Text,true);
        if(Run.Average.HasValue)text(TrainingMath.Delta(Run.Average.Value-Run.Options.Target),325,65,14,AnalysisLegend.Point(true,Run.Average.Value,Run.Options.Target,Run.Options.Tolerance),true);
        var last=Run.Laps.LastOrDefault();if(last!=null)text("Última "+TimingData.Lap(last.Time)+"  "+TrainingMath.Delta(last.Time-Run.Options.Target),14,94,12,AnalysisLegend.Point(last.Valid&&last.CompleteData,last.Time,Run.Options.Target,Run.Options.Tolerance),false);
        text("Alvo "+TimingData.Lap(Run.Options.Target)+"  ·  Na janela "+Run.InWindow+"/"+Run.Laps.Count+"  ·  Saídas* "+Run.OffTracks,14,123,11,GtcTheme.Muted,false);
        if(Run.Active&&Notice!=null&&Notice.StartsWith("A aguardar telemetria"))text("Sem telemetria",335,10,10,Color.FromArgb(255,112,126),true);
        if(Run.Active&&Notice!=null&&Notice.StartsWith("Treino em pausa"))text("EM PAUSA",365,10,10,GtcTheme.Accent,true);
    }
}
