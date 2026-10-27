// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
public sealed class GtcSettingsCheck : CheckBox {
    public GtcSettingsCheck(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnPaint(PaintEventArgs e){
        e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        float s=e.Graphics.DpiX/96f,side=16*s,x=(Width-side)/2,y=(Height-side)/2;
        using(var fill=new SolidBrush(Checked?GtcTheme.Accent:GtcTheme.Input))e.Graphics.FillRectangle(fill,x,y,side,side);
        using(var pen=new Pen(GtcTheme.Muted,s))e.Graphics.DrawRectangle(pen,x,y,side,side);
        if(Checked)using(var pen=new Pen(GtcTheme.Background,2*s))e.Graphics.DrawLines(pen,new[]{new PointF(x+3*s,y+8*s),new PointF(x+7*s,y+12*s),new PointF(x+13*s,y+4*s)});
        if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,ClientRectangle);
    }
}
public sealed class GtcSettingsSlider : Control {
    int value;
    public int Minimum {get;set;}
    public int Maximum {get;set;}
    public event EventHandler ValueChanged;
    public int Value {get{return value;}set{int next=Math.Max(Minimum,Math.Min(Maximum,value));if(next==this.value)return;this.value=next;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
    public GtcSettingsSlider(){Maximum=90;TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.Slider;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    protected override void OnPaint(PaintEventArgs e){
        e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;float s=e.Graphics.DpiX/96f,margin=9*s,y=Height/2f;
        float x=margin+(Width-2*margin)*(Value-Minimum)/Math.Max(1,Maximum-Minimum);
        using(var pen=new Pen(GtcTheme.Border,3*s))e.Graphics.DrawLine(pen,margin,y,Width-margin,y);
        using(var pen=new Pen(GtcTheme.Accent,3*s))e.Graphics.DrawLine(pen,margin,y,x,y);
        using(var brush=new SolidBrush(GtcTheme.Accent))e.Graphics.FillEllipse(brush,x-6*s,y-6*s,12*s,12*s);
        if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,ClientRectangle);
    }
    void SetFromMouse(int x){float s=DeviceScale;Value=Minimum+(int)Math.Round((x-9*s)/Math.Max(1,Width-18*s)*(Maximum-Minimum));}
    float DeviceScale {get{using(var g=CreateGraphics())return g.DpiX/96f;}}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();Capture=true;SetFromMouse(e.X);}}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(Capture)SetFromMouse(e.X);}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);Capture=false;}
    protected override bool IsInputKey(Keys keyData){return keyData==Keys.Left||keyData==Keys.Right||keyData==Keys.Home||keyData==Keys.End||base.IsInputKey(keyData);}
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Left)Value--;if(e.KeyCode==Keys.Right)Value++;if(e.KeyCode==Keys.Home)Value=Minimum;if(e.KeyCode==Keys.End)Value=Maximum;}
}
