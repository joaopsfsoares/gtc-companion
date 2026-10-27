// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public sealed class ShortcutBinding {
    public int Key {get;set;}
    public int Modifiers {get;set;}
    public bool Enabled {get{return Key!=0;}}
    public ShortcutBinding Copy(){return new ShortcutBinding{Key=Key,Modifiers=Modifiers};}
    public override string ToString(){
        if(!Enabled)return "Sem atalho";var parts=new List<string>();if((Modifiers&2)!=0)parts.Add("Ctrl");if((Modifiers&1)!=0)parts.Add("Alt");if((Modifiers&4)!=0)parts.Add("Shift");parts.Add(KeyName((Keys)Key));return string.Join(" + ",parts);
    }
    public static string KeyName(Keys key){int value=(int)key;if(value>=(int)Keys.D0&&value<=(int)Keys.D9)return (value-(int)Keys.D0).ToString();if(value>=(int)Keys.NumPad0&&value<=(int)Keys.NumPad9)return "Num "+(value-(int)Keys.NumPad0);if(key==Keys.Space)return "Espaço";return key.ToString();}
    public static List<Keys> SupportedKeys(){
        var list=new List<Keys>();foreach(var range in new[]{new[]{(int)Keys.A,(int)Keys.Z},new[]{(int)Keys.D0,(int)Keys.D9},new[]{(int)Keys.F1,(int)Keys.F24},new[]{(int)Keys.NumPad0,(int)Keys.NumPad9}})for(int key=range[0];key<=range[1];key++)if(key!=(int)Keys.F12)list.Add((Keys)key);list.Add(Keys.Space);return list;
    }
    public string Validate(){
        if(!Enabled)return Modifiers==0?null:"Um atalho desativado não pode ter modificadores.";
        if((Modifiers&~7)!=0||Modifiers<0||!SupportedKeys().Contains((Keys)Key))return "Escolhe uma tecla e modificadores válidos.";
        if(Modifiers==0&&!(Key>=(int)Keys.F1&&Key<=(int)Keys.F24))return "Para letras e números, seleciona Ctrl, Alt ou Shift.";
        return null;
    }
}
public sealed class OverlayShortcuts {
    public ShortcutBinding View {get;set;}
    public ShortcutBinding Move {get;set;}
    public ShortcutBinding Visibility {get;set;}
    public ShortcutBinding Settings {get;set;}
    public OverlayShortcuts(){View=new ShortcutBinding{Key=(int)Keys.G,Modifiers=3};Move=new ShortcutBinding{Key=(int)Keys.O,Modifiers=3};Visibility=new ShortcutBinding{Key=(int)Keys.H,Modifiers=3};Settings=new ShortcutBinding{Key=(int)Keys.S,Modifiers=3};}
    public static readonly string[] Names={"Alternar vista compacta/completa","Mover ou bloquear overlay","Mostrar ou ocultar overlay","Abrir definições"};
    public ShortcutBinding[] Bindings(){return new[]{View,Move,Visibility,Settings};}
    public void Set(int index,ShortcutBinding binding){if(index==0)View=binding;if(index==1)Move=binding;if(index==2)Visibility=binding;if(index==3)Settings=binding;}
    public OverlayShortcuts Copy(){var copy=new OverlayShortcuts();var source=Bindings();for(int i=0;i<4;i++)copy.Set(i,source[i]==null?null:source[i].Copy());return copy;}
    public static OverlayShortcuts Normalize(OverlayShortcuts source){var result=source==null?new OverlayShortcuts():source.Copy();var defaults=new OverlayShortcuts().Bindings();var bindings=result.Bindings();for(int i=0;i<4;i++)if(bindings[i]==null)result.Set(i,defaults[i]);return result;}
    public string Validate(){
        var bindings=Bindings();var seen=new Dictionary<string,int>();for(int i=0;i<4;i++){
            if(bindings[i]==null)return "Há um atalho por configurar.";string error=bindings[i].Validate();if(error!=null)return Names[i]+": "+error;
            if(!bindings[i].Enabled)continue;string key=bindings[i].Modifiers+":"+bindings[i].Key;int previous;
            if(seen.TryGetValue(key,out previous))return "A combinação "+bindings[i]+" já está atribuída a «"+Names[previous]+"».";seen.Add(key,i);
        }return null;
    }
    public bool SameAs(OverlayShortcuts other){if(other==null)return false;var a=Bindings();var b=other.Bindings();for(int i=0;i<4;i++)if(a[i]==null||b[i]==null||a[i].Key!=b[i].Key||a[i].Modifiers!=b[i].Modifiers)return false;return true;}
}
public sealed class ShortcutResult {
    public bool Success {get;set;}
    public string Message {get;set;}
}
public sealed class GlobalShortcutSet:IDisposable {
    readonly IntPtr window;
    readonly int firstId;
    readonly List<int> registered=new List<int>();
    public OverlayShortcuts Current {get;private set;}
    public int Count {get{return registered.Count;}}
    [DllImport("user32.dll",SetLastError=true)]static extern bool RegisterHotKey(IntPtr handle,int id,uint modifiers,uint key);
    [DllImport("user32.dll")]static extern bool UnregisterHotKey(IntPtr handle,int id);
    public GlobalShortcutSet(IntPtr handle,int first){window=handle;firstId=first;}
    void Clear(){foreach(int id in registered)UnregisterHotKey(window,id);registered.Clear();}
    bool Register(OverlayShortcuts shortcuts,out string error){
        error=null;var keys=shortcuts.Bindings();for(int i=0;i<4;i++){
            if(!keys[i].Enabled)continue;
            if(RegisterHotKey(window,firstId+i,0x4000u|(uint)keys[i].Modifiers,(uint)keys[i].Key))registered.Add(firstId+i);
            else{error="Não foi possível ativar "+keys[i]+" para «"+OverlayShortcuts.Names[i]+"». A combinação pode estar a ser usada pelo Windows ou por outra aplicação.";return false;}
        }return true;
    }
    public bool TryApply(OverlayShortcuts next,out string error){
        next=OverlayShortcuts.Normalize(next);error=next.Validate();if(error!=null)return false;
        var previous=Current;Clear();if(Register(next,out error)){Current=next;return true;}
        Clear();if(previous!=null){string restoreError;if(!Register(previous,out restoreError))error+=" Também não foi possível reativar todos os atalhos anteriores.";}
        return false;
    }
    public static bool Probe(OverlayShortcuts shortcuts,out string error){using(var owner=new ShortcutProbeWindow())using(var set=new GlobalShortcutSet(owner.Handle,3700))return set.TryApply(shortcuts,out error);}
    public void Dispose(){Clear();}
}
public sealed class ShortcutProbeWindow:NativeWindow,IDisposable {
    public ShortcutProbeWindow(){CreateHandle(new CreateParams{Caption="GTC shortcut availability"});}
    public void Dispose(){DestroyHandle();}
}
