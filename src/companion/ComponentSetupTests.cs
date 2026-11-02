// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public static class ComponentSetupTests {
    sealed class Publisher:HttpMessageHandler {
        public readonly Dictionary<string,byte[]> Files=new Dictionary<string,byte[]>();
        public string Feed;public int Downloads,Feeds;public bool FailDownloads;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancel){
            cancel.ThrowIfCancellationRequested();byte[] data;int status=200;
            if(request.RequestUri.AbsolutePath=="/downloads.php"){Feeds++;data=Encoding.UTF8.GetBytes(Feed);}
            else{Downloads++;if(FailDownloads){status=503;data=new byte[0];}else if(!Files.TryGetValue(Path.GetFileName(request.RequestUri.AbsolutePath),out data))throw new Exception("Download inesperado");}
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status){RequestMessage=request,Content=new ByteArrayContent(data)});
        }
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static byte[] Zip(string marker,int version,bool audio){
        using(var stream=new MemoryStream()){
            using(var zip=new ZipArchive(stream,ZipArchiveMode.Create,true)){
                string prefix=marker.Contains("driver_names")?"driver_names/":"";
                if(prefix!="")using(var output=zip.CreateEntry("updates.txt").Open())output.WriteByte(1);
                using(var writer=new StreamWriter(zip.CreateEntry(prefix+Path.GetFileName(marker)).Open()))writer.Write(version);
                using(var output=zip.CreateEntry(prefix+"voice/numbers/point0/2.lnk").Open())output.WriteByte(1);
                if(audio)using(var output=zip.CreateEntry("voice/radio_check/test.wav").Open())output.Write(new byte[64],0,64);
                else using(var output=zip.CreateEntry(prefix+(marker.Contains("personalisations")?"Fixture/test.wav":"Fixture.wav")).Open())output.Write(new byte[64],0,64);
            }return stream.ToArray();
        }
    }
    public static void Run(){
        string root=Path.Combine(CompanionStore.Root,"component-fixture-"+Guid.NewGuid().ToString("N")),prefix="fixture-"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(root);
        var publisher=new Publisher();string[] groups={"voice-messages","driver-names","personalisations"};var xml=new StringBuilder("<item><sounds language='en'>");
        for(int i=0;i<3;i++){
            string name=prefix+"-"+i+".zip";int version=i==0?2:i+1;publisher.Files[name]=Zip(CrewVoices.VersionFiles[i],i==0?0:version,i==0);
            xml.Append("<"+groups[i]+" latest-version='"+version+"'><update-pack updates-from-version='-1' url='https://thecrewchief.org/downloads/"+name+"'/>");
            if(i==0){string update=prefix+"-update.zip";publisher.Files[update]=Zip(CrewVoices.VersionFiles[0],2,true);xml.Append("<update-pack updates-from-version='0' url='https://thecrewchief.org/downloads/"+update+"'/>");}xml.Append("</"+groups[i]+">");
        }
        publisher.Feed=xml.Append("</sounds></item>").ToString();
        using(var client=new HttpClient(publisher)){
            Action<string> ignore=message=>{};string first=Path.Combine(root,"first");CrewVoices.Install(first,ignore,CancellationToken.None,client,()=>false).GetAwaiter().GetResult();
            Require(CrewVoices.Ready(first)&&publisher.Downloads==4,"Instalar vozes, nomes e personalizações até às versões atuais");
            Require(!File.Exists(Path.Combine(first,"voice","numbers","point0","2.lnk")),"Ignorar atalhos incluídos nos pacotes oficiais");
            Require(File.Exists(Path.Combine(first,"driver_names","Fixture.wav"))&&File.Exists(Path.Combine(first,"personalisations","Fixture","test.wav")),"Extrair cada pacote para a pasta esperada pelo Crew Chief");
            CrewVoices.Install(first,ignore,CancellationToken.None,client,()=>true).GetAwaiter().GetResult();Require(publisher.Downloads==4,"Não descarregar nem escrever pacotes atualizados com motor aberto");
            string second=Path.Combine(root,"second");CrewVoices.Install(second,ignore,CancellationToken.None,client,()=>false).GetAwaiter().GetResult();Require(CrewVoices.Ready(second)&&publisher.Downloads==4,"Reutilizar os downloads concluídos da cache");
            using(var cancel=new CancellationTokenSource()){
                bool cancelled=false;string target=Path.Combine(root,"cancelled");try{CrewVoices.Install(target,message=>{if(message.StartsWith("A preparar"))cancel.Cancel();},cancel.Token,client,()=>false).GetAwaiter().GetResult();}catch(OperationCanceledException){cancelled=true;}
                Require(cancelled&&CrewVoices.Version(target,CrewVoices.VersionFiles[0])==-1,"Cancelar antes de modificar a pasta de vozes");
            }
            bool blocked=false;try{CrewVoices.Install(Path.Combine(root,"running"),ignore,CancellationToken.None,client,()=>true).GetAwaiter().GetResult();}catch(InvalidOperationException){blocked=true;}Require(blocked,"Não instalar vozes com o motor aberto");
            string bad=prefix+"-missing.zip";publisher.Feed="<item><sounds language='en'><voice-messages latest-version='2'><update-pack updates-from-version='-1' url='https://thecrewchief.org/downloads/"+bad+"'/></voice-messages></sounds></item>";publisher.FailDownloads=true;
            string failed=Path.Combine(root,"failed");bool rejected=false;try{CrewVoices.Install(failed,ignore,CancellationToken.None,client,()=>false).GetAwaiter().GetResult();}catch(HttpRequestException){rejected=true;}Require(rejected&&!CrewVoices.Ready(failed),"Não marcar uma transferência falhada como instalada");
            Require(!Directory.GetFiles(Path.Combine(CompanionStore.Root,"downloads"),prefix+"*.partial").Any(),"Limpar transferências parciais");
            publisher.Feed="<!DOCTYPE item [<!ENTITY xxe SYSTEM 'file:///invalid'>]><item>&xxe;</item>";rejected=false;try{CrewVoices.Install(failed,ignore,CancellationToken.None,client,()=>false).GetAwaiter().GetResult();}catch(System.Xml.XmlException){rejected=true;}Require(rejected,"Não resolver entidades XML externas");
        }
        // The plugin installation must work from the complete package without a browser.
        string game=Path.Combine(root,"game");Directory.CreateDirectory(Path.Combine(game,"Bin64"));Directory.CreateDirectory(Path.Combine(game,"UserData"));File.WriteAllText(Path.Combine(game,"Bin64","rFactor2.exe"),"");
        TelemetryPlugin.InstallIncluded(game,()=>false);Require(TelemetryPlugin.Ready(game),"Instalar e ativar o plugin incluído");TelemetryPlugin.InstallIncluded(game,()=>true);
        File.WriteAllText(Path.Combine(root,"result.txt"),"OK: instalação offline do plugin; vozes, nomes e personalizações; versões; cache; cancelamento; motor aberto; falha de rede; XML seguro.");
    }
}
