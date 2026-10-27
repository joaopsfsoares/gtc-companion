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
    internal enum WindowObservation { NotFound,AlreadyHidden,HiddenNow }
    internal static bool MainTitle(string title,string titlePrefix){
        if(string.IsNullOrWhiteSpace(title))return false;
        // Crew Chief shows a separate loading window before its main form.
        // Matching that splash made the previous one-shot search stop too soon.
        string expected=titlePrefix.Equals("Crew Chief",StringComparison.OrdinalIgnoreCase)?"Crew Chief - Active Profile:":titlePrefix;
        return title.StartsWith(expected,StringComparison.OrdinalIgnoreCase);
    }
    internal static async Task<bool> Watch(Func<WindowObservation> observe,Func<Task> delay,int attempts,int quietChecks){
        bool seen=false;int quiet=quietChecks;
        for(int attempt=0;attempt<attempts;attempt++){
            var observation=observe();
            if(observation==WindowObservation.HiddenNow){seen=true;quiet=quietChecks;}
            else if(observation==WindowObservation.AlreadyHidden){seen=true;if(--quiet<=0)return true;}
            else if(seen)quiet=quietChecks;
            await delay();
        }
        return false;
    }
    static WindowObservation Observe(string executable,string titlePrefix){
        WindowObservation result=WindowObservation.NotFound;
        foreach(var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(executable)))using(process){
            try{
                if(!string.Equals(Path.GetFullPath(process.MainModule.FileName),Path.GetFullPath(executable),StringComparison.OrdinalIgnoreCase))continue;
                EnumWindows((window,value)=>{uint id;GetWindowThreadProcessId(window,out id);if(id!=process.Id||GetWindow(window,4)!=IntPtr.Zero)return true;var title=new StringBuilder(512);GetWindowText(window,title,title.Capacity);if(!MainTitle(title.ToString(),titlePrefix))return true;if(IsWindowVisible(window)){ShowWindow(window,0);result=WindowObservation.HiddenNow;}else if(result==WindowObservation.NotFound)result=WindowObservation.AlreadyHidden;return true;},IntPtr.Zero);
            }catch{}
        }
        return result;
    }
    public static Task<bool> HideWhenReady(string executable,string titlePrefix){return Watch(()=>Observe(executable,titlePrefix),()=>Task.Delay(250),240,40);}
    public static async Task HideTinyOnce(string executable){
        // Preserve TinyPedal's existing behaviour; its overlay windows are
        // separate from its main window and must not be watched continuously.
        for(int attempt=0;attempt<60;attempt++){
            foreach(var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(executable)))using(process){
                try{if(!string.Equals(Path.GetFullPath(process.MainModule.FileName),Path.GetFullPath(executable),StringComparison.OrdinalIgnoreCase))continue;bool found=false;EnumWindows((window,value)=>{uint id;GetWindowThreadProcessId(window,out id);if(id!=process.Id)return true;var title=new StringBuilder(512);GetWindowText(window,title,title.Capacity);if(title.ToString().StartsWith("TinyPedal",StringComparison.OrdinalIgnoreCase)){ShowWindow(window,0);found=true;}return true;},IntPtr.Zero);if(found)return;}catch{}
            }
            await Task.Delay(250);
        }
    }
    public static void RequestHidden(string executable,string titlePrefix){
        // Do not delay launching the game while an engine loads its UI.
        Task.Run(async()=>{try{if(!await HideWhenReady(executable,titlePrefix))CompanionStore.Log("Não foi possível ocultar a janela principal de "+titlePrefix+".");}catch(Exception ex){CompanionStore.Log("Não foi possível ocultar a janela principal de "+titlePrefix+": "+ex.Message);}});
    }
    [DllImport("user32.dll")]static extern bool EnumWindows(WindowCallback callback,IntPtr value);
    [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint processId);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr window,StringBuilder text,int count);
    [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr window,int command);
    [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")]static extern IntPtr GetWindow(IntPtr window,uint command);
}
