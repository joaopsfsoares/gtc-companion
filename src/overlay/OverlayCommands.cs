// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
public sealed class OverlayPatch {
    public int? PositionAnchor {get;set;}
    public int? OpacityPercent {get;set;}
    public bool? Compact {get;set;}
    public int? PilotsAbove {get;set;}
    public int? PilotsBelow {get;set;}
    public bool? ShowPositionChanges {get;set;}
    public bool? ShowPreviousLap {get;set;}
    public bool? ShowIdealLap {get;set;}
    public bool? ShowDriverIdealLaps {get;set;}
    public int? LapComparison {get;set;}
    public bool? ShowDriverActivity {get;set;}
    public int? PositionReference {get;set;}
    public int? PositionReferenceHours {get;set;}
    public int? RefreshSeconds {get;set;}
    public bool? Locked {get;set;}
    public bool? Visible {get;set;}
    public OverlayShortcuts Shortcuts {get;set;}
    public string ShortcutRequestId {get;set;}
    public DateTime? ShortcutExpiresUtc {get;set;}
    static int Bound(int value,int minimum,int maximum){return Math.Max(minimum,Math.Min(maximum,value));}
    public void Apply(Preferences p){
        if(Shortcuts!=null){var keys=OverlayShortcuts.Normalize(Shortcuts);string error=keys.Validate();if(error!=null)throw new ArgumentException(error);p.Shortcuts=keys;}
        if(PositionAnchor.HasValue)p.PositionAnchor=Bound(PositionAnchor.Value,0,9);
        if(OpacityPercent.HasValue)p.OpacityPercent=Bound(OpacityPercent.Value,10,100);
        if(Compact.HasValue)p.Compact=Compact.Value;
        if(PilotsAbove.HasValue)p.PilotsAbove=Bound(PilotsAbove.Value,0,10);
        if(PilotsBelow.HasValue)p.PilotsBelow=Bound(PilotsBelow.Value,0,10);
        if(LapComparison.HasValue)p.LapComparison=Bound(LapComparison.Value,0,1);
        if(ShowPreviousLap.HasValue)p.ShowPreviousLap=ShowPreviousLap.Value;
        if(ShowIdealLap.HasValue)p.ShowIdealLap=ShowIdealLap.Value;
        if(ShowDriverIdealLaps.HasValue)p.ShowDriverIdealLaps=ShowDriverIdealLaps.Value;
        if(ShowDriverActivity.HasValue)p.ShowDriverActivity=ShowDriverActivity.Value;
        if(ShowPositionChanges.HasValue)p.ShowPositionChanges=ShowPositionChanges.Value;
        if(PositionReference.HasValue)p.PositionReference=Bound(PositionReference.Value,0,2);
        if(PositionReferenceHours.HasValue)p.PositionReferenceHours=Bound(PositionReferenceHours.Value,1,168);
        if(RefreshSeconds.HasValue)p.RefreshSeconds=Bound(RefreshSeconds.Value,5,60);
    }
}
