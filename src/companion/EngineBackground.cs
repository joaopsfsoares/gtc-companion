// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

// Leaves engine overlays and secondary dialogs alone. Only the main settings
// window is hidden when the Companion has explicitly started that engine.
public static class EngineBackground {
    delegate bool WindowCallback(IntPtr window,IntPtr value);
    public static async Task HideWhenReady(string executable,string titlePrefix){
        for(int attempt=0;attempt<60;attempt++){
            foreach(var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(executable)))using(process){
                try{
                    if(!string.Equals(Path.GetFullPath(process.MainModule.FileName),Path.GetFullPath(executable),StringComparison.OrdinalIgnoreCase))continue;
                    bool found=false;EnumWindows((window,value)=>{uint id;GetWindowThreadProcessId(window,out id);if(id!=process.Id)return true;var title=new StringBuilder(512);GetWindowText(window,title,title.Capacity);if(title.ToString().StartsWith(titlePrefix,StringComparison.OrdinalIgnoreCase)){ShowWindow(window,0);found=true;}return true;},IntPtr.Zero);if(found)return;
                }catch{}
            }
            await Task.Delay(250);
        }
    }
    [DllImport("user32.dll")]static extern bool EnumWindows(WindowCallback callback,IntPtr value);
    [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint processId);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr window,StringBuilder text,int count);
    [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr window,int command);
}
