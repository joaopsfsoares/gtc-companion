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
    public static readonly string[] VersionFiles={"sound_pack_version_info.txt",Path.Combine("driver_names","driver_names_version_info.txt"),Path.Combine("personalisations","personalisations_version_info.txt")};
    static readonly SemaphoreSlim InstallGate=new SemaphoreSlim(1,1);
    public static bool Ready(string root){return Audio(Path.Combine(root,"voice","radio_check"))&&VersionFiles.All(name=>Version(root,name)>=0);}
    static string EntryPath(string root,string name){
        string file=Path.GetFullPath(Path.Combine(root,name.Replace('/',Path.DirectorySeparatorChar).Replace('\\',Path.DirectorySeparatorChar)));
        if(!file.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("O pacote contém um caminho inválido.");
        if(!new[]{".wav",".txt",".csv",".json",".xml",".ogg",".ini"}.Contains(Path.GetExtension(file).ToLowerInvariant()))throw new InvalidDataException("O pacote contém um tipo de ficheiro inesperado.");return file;
    }
    public static void ExtractAudio(string archive,string destination){ExtractAudio(archive,destination,CancellationToken.None);}
    static void ExtractAudio(string archive,string destination,CancellationToken cancel){
        string root=Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        using(var zip=ZipFile.OpenRead(archive)){
            // Validate every entry before extracting any file.
            foreach(var entry in zip.Entries){if(!string.IsNullOrEmpty(entry.Name))EntryPath(root,entry.FullName);}
            foreach(var entry in zip.Entries){
                cancel.ThrowIfCancellationRequested();if(string.IsNullOrEmpty(entry.Name))continue;string file=EntryPath(root,entry.FullName);
                CheckLinks(root,file);Directory.CreateDirectory(Path.GetDirectoryName(file));entry.ExtractToFile(file,true);
            }
        }
    }
    static void CheckLinks(string root,string file){
        var parent=new DirectoryInfo(Path.GetDirectoryName(file));while(parent!=null&&parent.FullName.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase)){if(parent.Exists&&(parent.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("A pasta de destino contém uma ligação não suportada.");parent=parent.Parent;}
    }
    static int ArchiveVersion(string file,string marker){
        using(var zip=ZipFile.OpenRead(file)){
            string root=Path.GetFullPath(Path.Combine(CompanionStore.Root,"archive-validation"))+Path.DirectorySeparatorChar;
            foreach(var entry in zip.Entries)if(!string.IsNullOrEmpty(entry.Name))EntryPath(root,entry.FullName);
            var version=zip.Entries.FirstOrDefault(entry=>entry.FullName.Replace('\\','/').Equals(marker.Replace('\\','/'),StringComparison.OrdinalIgnoreCase));
            if(version==null||version.Length>1024)return -1;
            using(var reader=new StreamReader(version.Open())){int number;return int.TryParse(reader.ReadToEnd().Trim(),NumberStyles.Integer,CultureInfo.InvariantCulture,out number)?number:-1;}
        }
    }
    static async Task Download(HttpClient client,string url,string file,Action<string> progress,CancellationToken cancel){
        string partial=file+".partial";
        try{
            using(var response=await client.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,cancel)){
                response.EnsureSuccessStatusCode();CheckOfficial(response);
                using(var input=await response.Content.ReadAsStreamAsync())using(var output=File.Create(partial)){
                    var bytes=new byte[131072];long total=0;int read;long? length=response.Content.Headers.ContentLength;DateTime updated=DateTime.MinValue;
                    while((read=await input.ReadAsync(bytes,0,bytes.Length,cancel))>0){await output.WriteAsync(bytes,0,read,cancel);total+=read;if(total>4L*1024*1024*1024)throw new InvalidDataException("O pacote excede o tamanho esperado.");if(DateTime.UtcNow-updated>TimeSpan.FromSeconds(1)){progress("A descarregar "+Path.GetFileName(file)+": "+(length.HasValue?(total*100/Math.Max(1,length.Value))+"%":(total/1048576)+" MB"));updated=DateTime.UtcNow;}}
                    if(length.HasValue&&total!=length.Value)throw new IOException("O download ficou incompleto. Volta a tentar.");
                }
            }
            cancel.ThrowIfCancellationRequested();if(File.Exists(file))File.Delete(file);File.Move(partial,file);
        }finally{if(File.Exists(partial))File.Delete(partial);}
    }
    static void CheckOfficial(HttpResponseMessage response){if(response.RequestMessage==null||response.RequestMessage.RequestUri.Scheme!="https"||!response.RequestMessage.RequestUri.Host.Equals("thecrewchief.org",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("O servidor de descarga mudou.");}
    public static async Task Install(string destination,Action<string> progress,CancellationToken cancel,HttpClient suppliedClient=null,Func<bool> engineRunning=null){
        if(!await InstallGate.WaitAsync(0,cancel))throw new InvalidOperationException("Já existe uma instalação de vozes em curso.");
        var client=suppliedClient??new HttpClient{Timeout=TimeSpan.FromMinutes(30)};var running=engineRunning??(()=>LaunchEngine.Running("CrewChiefV4"));
        try{
            progress("A verificar vozes, nomes e personalizações…");client.DefaultRequestHeaders.UserAgent.ParseAdd("GTC-Companion/"+AppInfo.Version);
            var xml=new XmlDocument{XmlResolver=null};
            using(var response=await client.GetAsync("https://thecrewchief.org/downloads.php?do=downloadxml",cancel)){
                response.EnsureSuccessStatusCode();CheckOfficial(response);string text=await response.Content.ReadAsStringAsync();cancel.ThrowIfCancellationRequested();if(text.Length>2000000)throw new InvalidDataException("A lista oficial excede o tamanho esperado.");
                using(var reader=XmlReader.Create(new StringReader(text),new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null}))xml.Load(reader);
            }
            string[] groups={"voice-messages","driver-names","personalisations"};
            for(int kind=0;kind<groups.Length;kind++){
                var group=xml.SelectSingleNode("/item/sounds[@language='en']/"+groups[kind]);if(group==null)throw new InvalidDataException("A lista oficial de vozes não tem o formato esperado.");int latest=int.Parse(group.Attributes["latest-version"].Value,CultureInfo.InvariantCulture);
                for(int attempt=0;attempt<64;attempt++){
                    cancel.ThrowIfCancellationRequested();int before=Version(destination,VersionFiles[kind]);
                    if(kind==0&&!Audio(Path.Combine(destination,"voice","radio_check")))before=-1;
                    if(before>=latest)break;
                    if(running())throw new InvalidOperationException("Fecha o motor Crew Chief antes de instalar as vozes. Os componentes já instalados foram mantidos.");
                    var pack=group.SelectNodes("update-pack").Cast<XmlNode>().Where(x=>int.Parse(x.Attributes["updates-from-version"].Value,CultureInfo.InvariantCulture)<=before).OrderByDescending(x=>int.Parse(x.Attributes["updates-from-version"].Value,CultureInfo.InvariantCulture)).FirstOrDefault();if(pack==null)throw new InvalidDataException("Não foi possível escolher o pacote oficial.");
                    var original=new Uri(pack.Attributes["url"].Value);string filename=Path.GetFileName(original.AbsolutePath);if(!System.Text.RegularExpressions.Regex.IsMatch(filename,@"\A[a-zA-Z0-9_-]+\.zip\z"))throw new InvalidDataException("Nome de pacote inválido.");
                    string url="https://thecrewchief.org/downloads/"+Uri.EscapeDataString(filename),cache=Path.Combine(CompanionStore.Root,"downloads");Directory.CreateDirectory(cache);string file=Path.Combine(cache,filename);int cached=-1;
                    string marker=Path.GetFileName(VersionFiles[kind]);
                    try{if(File.Exists(file))cached=ArchiveVersion(file,marker);}catch(InvalidDataException){}catch(IOException){}
                    if(cached<=before||cached>latest){await Download(client,url,file,progress,cancel);cached=ArchiveVersion(file,marker);}
                    else progress("A reutilizar o pacote descarregado: "+filename);
                    if(cached<=before||cached>latest)throw new InvalidDataException("O pacote não confirmou uma versão válida. Volta a tentar a instalação.");
                    string staging=Path.Combine(cache,"voice-stage-"+Guid.NewGuid().ToString("N"));
                    try{
                        progress("A preparar "+filename+"…");await Task.Run(()=>ExtractAudio(file,staging,cancel),cancel);cancel.ThrowIfCancellationRequested();
                        if(running())throw new InvalidOperationException("O Crew Chief foi aberto entretanto. Fecha o motor e volta a tentar; o download foi guardado.");
                        string target=kind==0?destination:Path.Combine(destination,kind==1?"driver_names":"personalisations");
                        progress("A instalar "+filename+"…");await Task.Run(()=>Merge(staging,target));
                    }finally{if(Directory.Exists(staging))Directory.Delete(staging,true);}
                    if(Version(destination,VersionFiles[kind])<=before)throw new InvalidDataException("A instalação do pacote não ficou concluída. Volta a tentar.");
                }
                if(Version(destination,VersionFiles[kind])<latest)throw new InvalidDataException("A atualização não ficou concluída.");
            }
            progress("Vozes, nomes e personalizações prontos.");
        }finally{if(suppliedClient==null)client.Dispose();InstallGate.Release();}
    }
    static void Merge(string staging,string destination){
        string root=Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        // Commit version markers last. Interrupted merges remain retryable from cache.
        foreach(string source in Directory.GetFiles(staging,"*",SearchOption.AllDirectories).OrderBy(file=>file.EndsWith("version_info.txt",StringComparison.OrdinalIgnoreCase)?1:0)){
            string relative=source.Substring(staging.Length).TrimStart(Path.DirectorySeparatorChar),target=EntryPath(root,relative);CheckLinks(root,target);Directory.CreateDirectory(Path.GetDirectoryName(target));string temp=target+".gtc-install";
            try{File.Copy(source,temp,true);if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);}finally{if(File.Exists(temp))File.Delete(temp);}
        }
    }
}
