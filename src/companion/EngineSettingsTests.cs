// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

public static class EngineSettingsTests {
    public static void Run(){
        string saved=CompanionStore.Root;string temp=Path.Combine(saved,"engine-fixture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);CompanionStore.Root=temp;
        try{
            string file=Path.Combine(temp,"preset.json");File.WriteAllText(file,"{\"radar\":{\"enable\":false,\"opacity\":0.9,\"unknown\":42},\"future\":{\"data\":[1,2]}}");
            var pending=new EnginePendingSettings{Engine="tiny",Changes=new List<EngineSettingChange>{new EngineSettingChange{File=file,Keys=new[]{"radar","enable"},Before=false,Value=true}}};CompanionStore.Write("engine-pending-tiny.json",pending);
            var acknowledged=new EnginePendingSettings{Changes=new List<EngineSettingChange>{new EngineSettingChange{File=file,Keys=new[]{"radar","enable"},Before=false,Value=true}}};
            CompanionStore.Write("engine-live-tiny-ack.json",acknowledged);EngineSettingsQueue.ReconcileTiny();if(EngineSettingsQueue.Pending("tiny").Changes.Count!=0)throw new Exception("A confirmação do TinyPedal deve remover a alteração aplicada.");
            pending.Changes[0].Value=false;CompanionStore.Write("engine-pending-tiny.json",pending);CompanionStore.Write("engine-live-tiny-ack.json",acknowledged);EngineSettingsQueue.ReconcileTiny();var later=EngineSettingsQueue.Pending("tiny").Changes.Single();if(!(bool)later.Before||(bool)later.Value)throw new Exception("Uma edição posterior à confirmação deve permanecer pendente.");
            pending.Changes[0].Value=true;CompanionStore.Write("engine-pending-tiny.json",pending);
            // An unrelated external modification must survive the merge.
            File.WriteAllText(file,"{\"radar\":{\"enable\":false,\"opacity\":0.8,\"unknown\":42},\"future\":{\"data\":[1,2]}}");
            EngineSettingsQueue.Apply("tiny",()=>true);if(EngineSettingsQueue.Pending("tiny").Changes.Count!=1||!File.ReadAllText(file).Contains("\"enable\":false"))throw new Exception("Não escrever sobre um motor em execução.");
            {
                EngineSettingsQueue.Apply("tiny",()=>false);var data=CompanionStore.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(file));var radar=(Dictionary<string,object>)data["radar"];
                if(!(bool)radar["enable"]||Convert.ToDouble(radar["opacity"])!=.8||Convert.ToInt32(radar["unknown"])!=42||!data.ContainsKey("future"))throw new Exception("Guardar definições não pode perder campos ou alterações externas.");
                if(EngineSettingsQueue.Pending("tiny").Changes.Count!=0)throw new Exception("A fila aplicada deve ficar vazia.");
                pending.Changes[0].Before=false;pending.Changes[0].Value=false;CompanionStore.Write("engine-pending-tiny.json",pending);bool refused=false;try{EngineSettingsQueue.Apply("tiny",()=>false);}catch(IOException){refused=true;}if(!refused)throw new Exception("Alterações concorrentes ao mesmo campo devem ser detetadas.");
            }
            string xmlFile=Path.Combine(temp,"user.config");File.WriteAllText(xmlFile,"<?xml version=\"1.0\"?><configuration><userSettings><CrewChiefV4.Properties.Settings><setting name=\"PERSONALISATION_NAME\" serializeAs=\"String\"><value>Soares</value></setting><setting name=\"untouched\" serializeAs=\"String\"><value>keep</value></setting></CrewChiefV4.Properties.Settings></userSettings></configuration>");
            var xmlPending=new EnginePendingSettings{Engine="crew",Changes=new List<EngineSettingChange>{new EngineSettingChange{File=xmlFile,Xml=true,Keys=new[]{"messages_volume"},Before=null,Value="0.75"}}};CompanionStore.Write("engine-pending-crew.json",xmlPending);EngineSettingsQueue.Apply("crew",()=>false);
            var xml=new XmlDocument{XmlResolver=null};xml.Load(xmlFile);if(xml.SelectSingleNode("//setting[@name='PERSONALISATION_NAME']/value").InnerText!="Soares"||xml.SelectSingleNode("//setting[@name='untouched']/value").InnerText!="keep"||xml.SelectSingleNode("//setting[@name='messages_volume']/value").InnerText!="0.75")throw new Exception("Preferências globais devem preservar nome e campos desconhecidos.");
            if(Directory.GetFiles(Path.Combine(temp,"backups","engines")).Length<2)throw new Exception("Guardar deve criar cópias de segurança.");
            string profiles=Path.Combine(temp,"profiles");Directory.CreateDirectory(profiles);File.WriteAllText(Path.Combine(profiles,"defaultSettings.json"),"{\"userSettings\":{\"enable_spotter\":true,\"#enable_spotter\":\"description\",\"unknown\":123}}");
            var session=EngineSettingsSession.Crew(Path.Combine(temp,"CrewChiefV4.exe"),null,xmlFile,profiles);if(session.Dirty||session.Options.Any(x=>x.Key.StartsWith("#")))throw new Exception("Ler valores predefinidos em falta não pode criar alterações ou tratar comentários como preferências.");string originalXml=File.ReadAllText(xmlFile);session.Save();if(File.ReadAllText(xmlFile)!=originalXml)throw new Exception("Guardar sem alterações não deve escrever preferências globais.");
            if(!EngineSettingsSession.Resource("CrewDefaults").ContainsKey("enable_spotter"))throw new Exception("Metadados do Crew Chief em falta.");
            var tiny=EngineSettingsSession.Resource("TinyDefaults");if(!tiny.ContainsKey("preset"))throw new Exception("Metadados do TinyPedal em falta.");
            string fresh=Path.Combine(temp,"tiny-fresh");Directory.CreateDirectory(fresh);File.WriteAllText(Path.Combine(fresh,"classes.json"),"{}");
            if(!BundledEngines.EnsureTinyDefault(fresh))throw new Exception("Criar o perfil predefinido mesmo quando há ficheiros auxiliares.");
            string defaultFile=Path.Combine(fresh,"default.json");var preset=CompanionStore.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(defaultFile));
            foreach(string key in new[]{"track_map","radar","tyre_wear","tyre_temperature","brake_temperature"})if(!(bool)((Dictionary<string,object>)preset[key])["enable"])throw new Exception("A predefinição deve ativar "+key+".");
            File.WriteAllText(defaultFile,"personalizado");if(BundledEngines.EnsureTinyDefault(fresh)||File.ReadAllText(defaultFile)!="personalizado")throw new Exception("Não substituir um default existente, mesmo se inválido.");
            string custom=Path.Combine(temp,"tiny-custom");Directory.CreateDirectory(custom);File.WriteAllText(Path.Combine(custom,"meu.json"),"{\"overlay\":{}}");if(BundledEngines.EnsureTinyDefault(custom)||File.Exists(Path.Combine(custom,"default.json")))throw new Exception("Preservar um perfil com outro nome.");
        }finally{CompanionStore.Root=saved;}
    }
}
