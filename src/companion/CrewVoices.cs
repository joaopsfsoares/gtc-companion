// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

public static class CrewVoices {
    static bool Audio(string path){try{return Directory.Exists(path)&&Directory.EnumerateFiles(path,"*.wav",SearchOption.AllDirectories).Any(file=>new FileInfo(file).Length>44);}catch{return false;}}
    public static string SoundRoot(string executable,IEnumerable<EngineSetting> options){
        var preference=options==null?null:options.FirstOrDefault(x=>x.Key=="override_default_sound_pack_location");string chosen=preference==null?"":Convert.ToString(preference.Value);
        var migration=CompanionStore.Read<EngineMigration>("engines-migration.json");var candidates=new[]{chosen,string.IsNullOrEmpty(executable)?"":Path.Combine(Path.GetDirectoryName(executable),"sounds"),migration==null||string.IsNullOrEmpty(migration.OriginalCrew)?"":Path.Combine(Path.GetDirectoryName(migration.OriginalCrew),"sounds"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CrewChiefV4","sounds")};
        return candidates.FirstOrDefault(path=>!string.IsNullOrWhiteSpace(path)&&Directory.Exists(Path.Combine(path,"voice")))??candidates.Last();
    }
    public static List<string> Installed(string root,string key){
        var result=new List<string>();
        if(key=="chief_name"){
            if(Audio(Path.Combine(root,"voice","radio_check")))result.Add("Jim (default)");string alt=Path.Combine(root,"alt");if(Directory.Exists(alt))foreach(string directory in Directory.GetDirectories(alt))if(Audio(Path.Combine(directory,"voice")))result.Add(Path.GetFileName(directory));
        }else if(key=="spotter_name"){
            if(Audio(Path.Combine(root,"voice","spotter")))result.Add("Jim (default)");string voice=Path.Combine(root,"voice");if(Directory.Exists(voice))foreach(string directory in Directory.GetDirectories(voice,"spotter_*"))if(Audio(directory))result.Add(Path.GetFileName(directory).Substring(8));
        }else if(key=="PERSONALISATION_NAME"){
            string names=Path.Combine(root,"driver_names"),personal=Path.Combine(root,"personalisations");if(Directory.Exists(names))result.AddRange(Directory.GetFiles(names,"*.wav").Select(Path.GetFileNameWithoutExtension));if(Directory.Exists(personal))result.AddRange(Directory.GetDirectories(personal).Where(Audio).Select(Path.GetFileName));
        }
        return result.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x,StringComparer.CurrentCultureIgnoreCase).ToList();
    }
    public static int Version(string directory,string name){try{int number;return int.TryParse(File.ReadAllText(Path.Combine(directory,name)).Trim(),NumberStyles.Integer,CultureInfo.InvariantCulture,out number)?number:-1;}catch{return -1;}}
    public static void ExtractAudio(string archive,string destination){
        string root=Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;Directory.CreateDirectory(root);
        using(var zip=ZipFile.OpenRead(archive))foreach(var entry in zip.Entries){
            if(string.IsNullOrEmpty(entry.Name))continue;string file=Path.GetFullPath(Path.Combine(root,entry.FullName.Replace('/',Path.DirectorySeparatorChar)));
            if(!file.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("O pacote contém um caminho inválido.");
            string extension=Path.GetExtension(file).ToLowerInvariant();if(!new[]{".wav",".txt",".csv",".json",".xml",".ogg",".ini"}.Contains(extension))throw new InvalidDataException("O pacote contém um tipo de ficheiro inesperado.");
            var parent=new DirectoryInfo(Path.GetDirectoryName(file));while(parent!=null&&parent.FullName.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase)){if(parent.Exists&&(parent.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("A pasta de destino contém uma ligação não suportada.");parent=parent.Parent;}
            Directory.CreateDirectory(Path.GetDirectoryName(file));entry.ExtractToFile(file,true);
        }
    }
    public static async Task Install(string destination,Action<string> progress,CancellationToken cancel){
        if(LaunchEngine.Running("CrewChiefV4"))throw new InvalidOperationException("Fecha o motor Crew Chief antes de instalar ou atualizar as vozes.");
        using(var client=new HttpClient{Timeout=TimeSpan.FromMinutes(30)}){
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GTC-Companion/0.7.4");var xml=new XmlDocument{XmlResolver=null};xml.LoadXml(await client.GetStringAsync("https://thecrewchief.org/downloads.php?do=downloadxml"));
            string[] groups={"voice-messages","driver-names"},versions={"sound_pack_version_info.txt",Path.Combine("driver_names","driver_names_version_info.txt")};
            for(int kind=0;kind<groups.Length;kind++){
                var group=xml.SelectSingleNode("/item/sounds[@language='en']/"+groups[kind]);if(group==null)throw new InvalidDataException("A lista oficial de vozes não tem o formato esperado.");int latest=int.Parse(group.Attributes["latest-version"].Value,CultureInfo.InvariantCulture);
                for(int attempt=0;attempt<64;attempt++){
                    cancel.ThrowIfCancellationRequested();int before=Version(destination,versions[kind]);if(before>=latest)break;
                    var pack=group.SelectNodes("update-pack").Cast<XmlNode>().Where(x=>int.Parse(x.Attributes["updates-from-version"].Value,CultureInfo.InvariantCulture)<=before).OrderByDescending(x=>int.Parse(x.Attributes["updates-from-version"].Value,CultureInfo.InvariantCulture)).FirstOrDefault();if(pack==null)throw new InvalidDataException("Não foi possível escolher o pacote oficial.");
                    // The publisher documents this HTTPS mirror for its legacy VPS URLs.
                    var original=new Uri(pack.Attributes["url"].Value);string filename=Path.GetFileName(original.AbsolutePath);if(!filename.EndsWith(".zip",StringComparison.OrdinalIgnoreCase)||filename.IndexOfAny(Path.GetInvalidFileNameChars())>=0)throw new InvalidDataException("Nome de pacote inválido.");
                    string url="https://thecrewchief.org/downloads/"+Uri.EscapeDataString(filename);string cache=Path.Combine(CompanionStore.Root,"downloads");Directory.CreateDirectory(cache);string file=Path.Combine(cache,filename),temp=file+".partial";
                    progress("A descarregar "+(kind==0?"vozes":"nomes")+": "+filename+"…");
                    try{
                        using(var response=await client.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,cancel)){
                            response.EnsureSuccessStatusCode();if(response.RequestMessage.RequestUri.Scheme!="https"||!response.RequestMessage.RequestUri.Host.Equals("thecrewchief.org",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("O servidor de descarga mudou.");
                            using(var input=await response.Content.ReadAsStreamAsync())using(var output=File.Create(temp)){var bytes=new byte[131072];long total=0;int read;long? length=response.Content.Headers.ContentLength;DateTime update=DateTime.MinValue;while((read=await input.ReadAsync(bytes,0,bytes.Length,cancel))>0){await output.WriteAsync(bytes,0,read,cancel);total+=read;if(DateTime.UtcNow-update>TimeSpan.FromSeconds(1)){progress("A descarregar "+filename+": "+(length.HasValue?(total*100/Math.Max(1,length.Value))+"%":(total/1048576)+" MB"));update=DateTime.UtcNow;}}}
                        }
                        if(File.Exists(file))File.Delete(file);File.Move(temp,file);cancel.ThrowIfCancellationRequested();progress("A instalar "+filename+"…");await Task.Run(()=>ExtractAudio(file,destination),cancel);
                        if(Version(destination,versions[kind])<=before)throw new InvalidDataException("O pacote não confirmou uma versão mais recente. Os ficheiros descarregados foram mantidos.");
                    }finally{if(File.Exists(temp))File.Delete(temp);}
                }
                if(Version(destination,versions[kind])<latest)throw new InvalidDataException("A atualização não ficou concluída.");
            }
            progress("Vozes e nomes instalados. Recarrega as preferências para escolher.");
        }
    }
}
