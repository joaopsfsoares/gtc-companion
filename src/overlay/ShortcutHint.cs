// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public sealed class ShortcutHint : IDisposable {
    [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
    readonly Form owner; HintWindow popup; string previous;
    public ShortcutHint(Form owner){this.owner=owner;}
    public void Update(OverlayShortcuts shortcuts){
        bool pressed=(GetAsyncKeyState(0x11)&0x8000)!=0;
        if(!pressed||owner.IsDisposed||!owner.Visible){if(popup!=null&&!popup.IsDisposed)popup.Hide();return;}
        if(popup==null||popup.IsDisposed)popup=new HintWindow();
        var keys=OverlayShortcuts.Normalize(shortcuts);string text=Format(keys);if(previous!=text){popup.HintText=text;popup.Invalidate();previous=text;}
        var area=Screen.FromControl(owner).WorkingArea;popup.Location=new Point(Math.Max(area.Left,Math.Min(area.Right-popup.Width,owner.Left)),Math.Max(area.Top,Math.Min(area.Bottom-popup.Height,owner.Top)));
        if(!popup.Visible)popup.Show();
    }
    public static string Format(OverlayShortcuts keys){var items=OverlayShortcuts.Normalize(keys).Bindings();var names=new[]{"Vista compacta / completa","Mover / bloquear","Mostrar / ocultar ambos","Definições"};string result=AppLanguage.T("ATALHOS · CTRL PREMIDO")+"\n";for(int i=0;i<items.Length;i++)result+=AppLanguage.T(names[i])+"   "+AppLanguage.T(items[i].ToString())+(i<items.Length-1?"\n":"");return result;}
    public void Dispose(){if(popup!=null){popup.Close();popup.Dispose();popup=null;}}
    sealed class HintWindow:Form {
        public string HintText="";
        protected override bool ShowWithoutActivation{get{return true;}}
        protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x80|0x20|0x08000000;return p;}}
        public HintWindow(){float scale;using(var g=Graphics.FromHwnd(IntPtr.Zero))scale=Math.Max(1,g.DpiX/96f);FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;BackColor=GtcTheme.Background;Opacity=.97;DoubleBuffered=true;ClientSize=new Size((int)(370*scale),(int)(126*scale));}
        protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=new IntPtr(3);return;}base.WndProc(ref m);}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var font=GtcTypography.Create(10*e.Graphics.DpiX/96f))using(var brush=new SolidBrush(GtcTheme.Text))e.Graphics.DrawString(HintText,font,brush,new RectangleF(10,7,Width-20,Height-14));using(var pen=new Pen(GtcTheme.Accent,2))e.Graphics.DrawRectangle(pen,1,1,Width-3,Height-3);}
    }
}
