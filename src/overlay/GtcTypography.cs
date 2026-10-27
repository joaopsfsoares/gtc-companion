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
using System.Drawing.Text;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
public static class GtcTypography {
    // Fonts are private to this process, without installation or admin rights.
    static readonly PrivateFontCollection fonts=new PrivateFontCollection();
    static readonly List<IntPtr> memory=new List<IntPtr>();
    static readonly List<IntPtr> registrations=new List<IntPtr>();
    static readonly FontFamily family;
    [DllImport("gdi32.dll")]static extern IntPtr AddFontMemResourceEx(IntPtr data,uint size,IntPtr reserved,ref uint count);
    static GtcTypography(){
        foreach(string resource in new[]{"UbuntuRegular","UbuntuMedium","UbuntuBold"})using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)){
            if(stream==null)throw new InvalidOperationException("Fonte Ubuntu em falta na aplicação.");
            byte[] bytes=new byte[stream.Length];int offset=0,read;while(offset<bytes.Length&&(read=stream.Read(bytes,offset,bytes.Length-offset))>0)offset+=read;
            IntPtr pointer=Marshal.AllocCoTaskMem(bytes.Length);Marshal.Copy(bytes,0,pointer,bytes.Length);memory.Add(pointer);
            uint count=0;IntPtr registration=AddFontMemResourceEx(pointer,(uint)bytes.Length,IntPtr.Zero,ref count);
            if(registration==IntPtr.Zero||count==0)throw new InvalidOperationException("Não foi possível carregar a fonte Ubuntu.");
            registrations.Add(registration);fonts.AddMemoryFont(pointer,bytes.Length);
        }
        family=fonts.Families.First(f=>f.Name=="Ubuntu");
    }
    public static Font Create(float pixels,bool bold=false){return new Font(family,pixels,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel);}
    public static void Verify(){using(var normal=Create(13))using(var bold=Create(13,true))if(normal.Name!="Ubuntu"||bold.Name!="Ubuntu"||!family.IsStyleAvailable(FontStyle.Bold))throw new Exception("Fonte Ubuntu regular/bold não foi carregada.");}
}
