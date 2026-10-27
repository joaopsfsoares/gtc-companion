// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public static class GtcWindowTheme {
    [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    [DllImport("uxtheme.dll",CharSet=CharSet.Unicode)]static extern int SetWindowTheme(IntPtr window,string application,string classes);
    static int ColorRef(Color color){return color.R|(color.G<<8)|(color.B<<16);}
    static void Apply(Control control){
        if(!control.IsHandleCreated)return;
        try{
            if(control is Form){int enabled=1;DwmSetWindowAttribute(control.Handle,20,ref enabled,4);int caption=ColorRef(GtcTheme.Background),text=ColorRef(GtcTheme.Text),border=ColorRef(GtcTheme.Border);DwmSetWindowAttribute(control.Handle,35,ref caption,4);DwmSetWindowAttribute(control.Handle,36,ref text,4);DwmSetWindowAttribute(control.Handle,34,ref border,4);}
            if(control is ScrollableControl)SetWindowTheme(control.Handle,"DarkMode_Explorer",null);
        }catch(DllNotFoundException){}catch(EntryPointNotFoundException){}
    }
    public static void Attach(Control root){
        Apply(root);root.HandleCreated+=(s,e)=>Apply(root);root.ControlAdded+=(s,e)=>Attach(e.Control);
        foreach(Control child in root.Controls)Attach(child);
    }
}
