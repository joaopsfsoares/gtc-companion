// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;

public sealed class TimingRow {
    public string driverName { get; set; }
    public double bestLapTime { get; set; }
    public string vehicleClass { get; set; }
    public int? lapsAccumulated { get; set; }
    public double? s1 { get; set; }
    public double? s2 { get; set; }
    public double? s3 { get; set; }
}
public sealed class RoundInfo {
    public string id { get; set; }
    public string trackName { get; set; }
}
public sealed class TimingSnapshot {
    public List<TimingRow> rows { get; set; }
    public RoundInfo activeRound { get; set; }
}
public sealed class Preferences {
    public int X { get; set; }
    public int Y { get; set; }
    public bool Compact { get; set; }
    public string Player { get; set; }
    public int RefreshSeconds { get; set; }
    public int OpacityPercent { get; set; }
    public int PositionAnchor { get; set; }
    public int PilotsAbove { get; set; }
    public int PilotsBelow { get; set; }
    public bool ShowPositionChanges { get; set; }
    public bool ShowPreviousLap {get;set;}
    public bool ShowIdealLap {get;set;}
    public bool ShowDriverIdealLaps {get;set;}
    public int LapComparison {get;set;}
    public bool ShowDriverActivity {get;set;}
    public int PositionReference {get;set;}
    public int PositionReferenceHours {get;set;}
    public OverlayShortcuts Shortcuts {get;set;}
    public Preferences() { ShowPreviousLap=true;ShowDriverActivity=true;X=-1;Y=-1;Compact=true;Player="";RefreshSeconds=10;OpacityPercent=94;PilotsAbove=1;PilotsBelow=1;PositionReferenceHours=24;Shortcuts=new OverlayShortcuts(); }
}
public sealed class CachedSnapshot {
    public TimingSnapshot Data { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
public static class TimingData {
    public static string Normalize(string name) {
        var text = new StringBuilder();
        foreach (char c in (name ?? "").Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))
                text.Append(char.ToLowerInvariant(c));
        return text.ToString();
    }
    public static List<TimingRow> Sorted(TimingSnapshot snapshot) {
        return (snapshot.rows ?? new List<TimingRow>())
            .Where(r => r != null && !string.IsNullOrWhiteSpace(r.driverName) && r.bestLapTime > 0 && !double.IsNaN(r.bestLapTime) && !double.IsInfinity(r.bestLapTime))
            .OrderBy(r => r.bestLapTime).ThenBy(r => r.driverName, StringComparer.Ordinal).ToList();
    }
    public static int FindPlayer(List<TimingRow> rows, string player) {
        return rows.FindIndex(r => Normalize(r.driverName) == Normalize(player));
    }
    public static string Lap(double seconds) {
        long ms = (long)Math.Round(seconds * 1000, MidpointRounding.AwayFromZero);
        return (ms / 60000).ToString() + ":" + ((ms / 1000) % 60).ToString("00") + "," + (ms % 1000).ToString("000");
    }
    public static string Gap(double seconds) { return "+" + seconds.ToString("0.000", CultureInfo.GetCultureInfo("pt-PT")); }
    public static string LapCount(int? count) { return count.HasValue&&count.Value>=0?count.Value.ToString(CultureInfo.InvariantCulture):"—"; }
    public static string Sector(double? seconds) {
        return seconds.HasValue && seconds.Value > 0 && !double.IsNaN(seconds.Value) && !double.IsInfinity(seconds.Value)
            ? seconds.Value.ToString("0.000",CultureInfo.GetCultureInfo("pt-PT")) : "—";
    }
    public static double BestSector(IEnumerable<TimingRow> rows,Func<TimingRow,double?> select) {
        return rows.Select(select).Where(v=>v.HasValue && v.Value>0 && !double.IsNaN(v.Value) && !double.IsInfinity(v.Value))
            .Select(v=>v.Value).DefaultIfEmpty(double.PositiveInfinity).Min();
    }
    public static bool IsBestSector(double? value,double best) {
        return value.HasValue && value.Value>0 && !double.IsInfinity(best) && Math.Abs(value.Value-best)<0.000001;
    }
    public static Point AnchoredPosition(Rectangle area,Size size,int anchor,int padding) {
        int col=(anchor-1)%3,row=(anchor-1)/3;
        int x=area.Left+padding+Math.Max(0,area.Width-size.Width-2*padding)*col/2;
        int y=area.Top+padding+Math.Max(0,area.Height-size.Height-2*padding)*row/2;
        return new Point(x,y);
    }
    public static Point ConstrainPosition(Point preferred,Size size,Rectangle area) {
        return new Point(Math.Max(area.Left,Math.Min(preferred.X,area.Right-size.Width)),Math.Max(area.Top,Math.Min(preferred.Y,area.Bottom-size.Height)));
    }
    public static List<int> VisibleIndexes(List<TimingRow> rows, string player, bool compact, int offset, int above=1, int below=1) {
        int mine = FindPlayer(rows, player);
        if (!compact) {
            var full = Enumerable.Range(0, rows.Count).Skip(offset).Take(16).ToList();
            if(mine >= 0 && !full.Contains(mine)) { if(full.Count==16)full.RemoveAt(15);full.Add(mine);full.Sort(); }
            return full;
        }
        above=Math.Max(0,Math.Min(10,above));below=Math.Max(0,Math.Min(10,below));
        int first=mine>=0?Math.Max(0,mine-above):0;
        int last=mine>=0?Math.Min(rows.Count-1,mine+below):Math.Min(rows.Count-1,above+below);
        return Enumerable.Range(first,Math.Max(0,last-first+1)).ToList();
    }
}
