// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System.Drawing;
using System.Reflection;
public static class GtcBrand {
    public static Icon Icon(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("GtcIcon"))using(var icon=new Icon(stream))return (Icon)icon.Clone();}
}
