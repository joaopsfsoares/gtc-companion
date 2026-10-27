// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.Windows.Forms;
public sealed partial class TimingOverlay {
    GlobalShortcutSet shortcutRegistration;
    readonly ToolStripMenuItem[] shortcutMenus=new ToolStripMenuItem[4];
    string shortcutError="";
    void RegisterKeys(){
        settings.Shortcuts=OverlayShortcuts.Normalize(settings.Shortcuts);
        shortcutRegistration=new GlobalShortcutSet(Handle,701);string failure;
        shortcutError=shortcutRegistration.TryApply(settings.Shortcuts,out failure)?"":failure;UpdateShortcutLabels();
    }
    void UpdateShortcutLabels(){
        var keys=OverlayShortcuts.Normalize(settings.Shortcuts).Bindings();
        for(int i=0;i<4;i++)if(shortcutMenus[i]!=null)shortcutMenus[i].Text=OverlayShortcuts.Names[i]+(keys[i].Enabled?" ("+keys[i]+")":"");
        string caption="GTC Companion · "+(keys[3].Enabled?keys[3]+": definições":"Overlay");tray.Text=caption.Length>63?caption.Substring(0,63):caption;
    }
    bool ApplyShortcutRequest(OverlayPatch patch){
        if(patch.Shortcuts==null)return true;
        if(patch.ShortcutExpiresUtc.HasValue&&patch.ShortcutExpiresUtc.Value.ToUniversalTime()<DateTime.UtcNow){ShortcutReply(patch,false,"O pedido expirou. Volta a tentar.");return false;}
        if(shortcutRegistration==null)RegisterKeys();string failure;
        if(!shortcutRegistration.TryApply(patch.Shortcuts,out failure)){shortcutError=failure;ShortcutReply(patch,false,failure);return false;}
        shortcutError="";return true;
    }
    void ShortcutReply(OverlayPatch patch,bool success,string message){
        Guid id;if(!Guid.TryParse(patch.ShortcutRequestId,out id))return;
        string directory=Path.Combine(Root,"shortcut-results");Directory.CreateDirectory(directory);
        Write(Path.Combine("shortcut-results",id.ToString("N")+".json"),new ShortcutResult{Success=success,Message=message});
    }
    string MoveHint(){var move=OverlayShortcuts.Normalize(settings.Shortcuts).Move;return move.Enabled?move+" para terminar":"Usa Concluir para terminar";}
}
