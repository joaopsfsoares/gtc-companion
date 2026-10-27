// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Windows.Forms;
public static class ShortcutTests {
    static OverlayShortcuts Empty(){var result=new OverlayShortcuts();for(int i=0;i<4;i++)result.Set(i,new ShortcutBinding());return result;}
    public static void Run(){
        var defaults=new OverlayShortcuts();if(defaults.Validate()!=null||defaults.Move.ToString()!="Ctrl + Alt + O")throw new Exception("Atalhos originais e apresentação");
        var command=CompanionStore.Json.Deserialize<OverlayPatch>(CompanionStore.Json.Serialize(new OverlayPatch{Shortcuts=defaults,ShortcutRequestId=Guid.NewGuid().ToString("N"),ShortcutExpiresUtc=DateTime.UtcNow.AddSeconds(5)}));if(command.Shortcuts==null||!command.Shortcuts.SameAs(defaults))throw new Exception("Serialização dos atalhos entre Companion e overlay");
        var migrated=CompanionStore.Json.Deserialize<Preferences>("{\"X\":1200,\"Y\":800}");if(migrated.Shortcuts==null||!migrated.Shortcuts.SameAs(defaults))throw new Exception("Preferências antigas têm de manter os atalhos originais");
        var duplicate=defaults.Copy();duplicate.Settings=duplicate.View.Copy();if(duplicate.Validate()==null)throw new Exception("Atalhos repetidos têm de ser recusados");
        var invalid=defaults.Copy();invalid.Move=new ShortcutBinding{Key=(int)Keys.A,Modifiers=0};if(invalid.Validate()==null)throw new Exception("Uma letra sem modificador não pode ser global");
        var disabled=defaults.Copy();disabled.Move=new ShortcutBinding();if(disabled.Validate()!=null)throw new Exception("Desativar uma ação tem de ser possível");
        using(var window=new ShortcutProbeWindow())using(var foreignWindow=new ShortcutProbeWindow())using(var owner=new GlobalShortcutSet(window.Handle,4400))using(var foreign=new GlobalShortcutSet(foreignWindow.Handle,4500)){
            var original=new OverlayShortcuts();for(int i=0;i<4;i++)original.Set(i,new ShortcutBinding{Key=(int)Keys.F20+i,Modifiers=7});string error;
            if(!owner.TryApply(original,out error))throw new Exception("Não foi possível reservar as combinações de teste: "+error);
            var occupied=Empty();occupied.View=new ShortcutBinding{Key=(int)Keys.F19,Modifiers=7};if(!foreign.TryApply(occupied,out error))throw new Exception("Falha na preparação do conflito de teste");
            var replacement=original.Copy();replacement.View=occupied.View.Copy();if(owner.TryApply(replacement,out error)||!owner.Current.SameAs(original)||owner.Count!=4)throw new Exception("Um conflito deve repor todos os atalhos anteriores");
            if(GlobalShortcutSet.Probe(original,out error))throw new Exception("Os atalhos anteriores deixaram de estar registados após o conflito");
            foreign.Dispose();if(!owner.TryApply(replacement,out error)||!owner.Current.SameAs(replacement))throw new Exception("Uma combinação livre deve substituir o atalho em execução");
            var released=Empty();released.View=original.View.Copy();if(!GlobalShortcutSet.Probe(released,out error))throw new Exception("A combinação substituída não foi libertada");
        }
    }
}
