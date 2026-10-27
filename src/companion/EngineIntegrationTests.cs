// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;
public static class EngineIntegrationTests {
    static int Controls(Control control){return 1+control.Controls.Cast<Control>().Sum(child=>Controls(child));}
    public static void Run(){
        string root=Path.Combine(CompanionStore.Root,"engine-integration-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        string jim=Path.Combine(root,"voice","radio_check","test"),jerry=Path.Combine(root,"alt","Jerry","voice","test"),empty=Path.Combine(root,"alt","Missing","voice");Directory.CreateDirectory(jim);Directory.CreateDirectory(jerry);Directory.CreateDirectory(empty);File.WriteAllBytes(Path.Combine(jim,"1.wav"),new byte[64]);File.WriteAllBytes(Path.Combine(jerry,"1.wav"),new byte[64]);
        var voices=CrewVoices.Installed(root,"chief_name");if(voices.Count!=2||!voices.Contains("Jim (default)")||!voices.Contains("Jerry")||voices.Contains("Missing"))throw new Exception("Não apresentar vozes sem ficheiros de áudio instalados.");
        string zip=Path.Combine(root,"bad.zip");using(var archive=ZipFile.Open(zip,ZipArchiveMode.Create)){using(var writer=new StreamWriter(archive.CreateEntry("../outside.wav").Open()))writer.Write("invalid");}bool rejected=false;try{CrewVoices.ExtractAudio(zip,Path.Combine(root,"extract"));}catch(InvalidDataException){rejected=true;}if(!rejected||File.Exists(Path.Combine(root,"outside.wav")))throw new Exception("Os pacotes de áudio não podem escrever fora da pasta de destino.");
        var settings=new EngineSettingsSession{Engine="tiny",Profile="default.json"};settings.Profiles.Add("default.json");for(int i=0;i<100;i++)settings.Options.Add(new EngineSetting{Group="widget_"+i,Key="enable",Initial=false,Change=new EngineSettingChange{File="fixture.json",Keys=new[]{"widget_"+i,"enable"},Before=false,Value=false}});
        using(var page=new NativeEnginePage("tiny",()=>"fixture.exe",()=>{},1,profile=>settings)){page.LoadSettings();if(Controls(page)>140)throw new Exception("A página deve criar apenas os controlos visíveis, e não todos os widgets.");}
    }
}
