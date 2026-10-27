// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

public static class AppInfo {
    public const string ReleasesUrl="https://github.com/joaopsfsoares/gtc-companion/releases";
    public static string Version{get{return Application.ProductVersion;}}
    public static string EngineVersion(string name,string executable){
        try{var manifest=CompanionStore.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(BundledEngines.Root,"manifest.json")));return Convert.ToString(((Dictionary<string,object>)manifest[name])["version"]);}catch{}
        try{return FileVersionInfo.GetVersionInfo(executable).ProductVersion??"—";}catch{return "—";}
    }
    public static string Summary(){return "GTC Companion "+Version+" (teste)"+Environment.NewLine+"TinyPedal "+EngineVersion("TinyPedal",BundledEngines.Tiny)+Environment.NewLine+"Crew Chief "+EngineVersion("CrewChief",BundledEngines.Crew);}
}
public sealed partial class CompanionWindow {
    void CreateAboutPage(TableLayoutPanel stack){
        Func<string,TableLayoutPanel> card=title=>{var result=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,BackColor=GtcTheme.Surface,Padding=new Padding(P(14)),Margin=new Padding(0,0,0,P(12))};result.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));var heading=TextLabel(title,11,true);heading.ForeColor=GtcTheme.Muted;heading.Margin=new Padding(0,0,0,P(10));result.Controls.Add(heading);stack.Controls.Add(result);return result;};
        Action<TableLayoutPanel,string,bool> line=(panel,text,bold)=>{var label=TextLabel(text,bold?19:12,bold);label.MaximumSize=new Size(P(495),0);label.Margin=new Padding(0,0,0,P(9));panel.Controls.Add(label);};
        var identity=card("GTC COMPANION");line(identity,"Versão "+AppInfo.Version,true);line(identity,"Criado por João Soares",false);line(identity,"Versão de teste · GPLv3",false);
        var copy=Button("Copiar informação da versão",250);identity.Controls.Add(copy);copy.Click+=(s,e)=>{try{Clipboard.SetText(AppInfo.Summary());Report("Informação da versão copiada.");}catch{Report("Não foi possível copiar a informação.");}};
        var updates=new LinkLabel{Text="Ver atualizações ↗",AccessibleName="Ver versões e atualizações do GTC Companion no GitHub",AutoSize=true,Font=F(12),LinkColor=GtcTheme.Accent,VisitedLinkColor=GtcTheme.Accent,ActiveLinkColor=GtcTheme.Text,LinkBehavior=LinkBehavior.HoverUnderline,Margin=new Padding(0,P(12),0,0),Cursor=Cursors.Hand};identity.Controls.Add(updates);updates.LinkClicked+=(s,e)=>{StopCountdown();try{ProcessUrl(AppInfo.ReleasesUrl);}catch{Report("Não foi possível abrir a página de atualizações no navegador.");}};
        var components=card("MOTORES INCLUÍDOS");
        Action<string,string> engineLink=(text,url)=>{var link=new LinkLabel{Text=text,AccessibleName=text+", abrir repositório oficial",AutoSize=true,Font=F(12),LinkColor=GtcTheme.Accent,VisitedLinkColor=GtcTheme.Accent,ActiveLinkColor=GtcTheme.Text,LinkBehavior=LinkBehavior.HoverUnderline,Margin=new Padding(0,0,0,P(9)),Cursor=Cursors.Hand};components.Controls.Add(link);link.LinkClicked+=(s,e)=>{StopCountdown();try{ProcessUrl(url);}catch{Report("Não foi possível abrir o repositório no navegador.");}};};
        engineLink("TinyPedal "+AppInfo.EngineVersion("TinyPedal",BundledEngines.Tiny),"https://github.com/TinyPedal/TinyPedal");
        engineLink("Crew Chief "+AppInfo.EngineVersion("CrewChief",BundledEngines.Crew),"https://gitlab.com/mr_belowski/CrewChiefV4");
        var notices=card("LICENÇAS E AVISOS");line(notices,"© 2026 GTC Companion contributors. Projeto da comunidade. Os motores e recursos de terceiros mantêm as respetivas licenças.",false);
        var actions=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Top,WrapContents=true,Margin=Padding.Empty};notices.Controls.Add(actions);
        foreach(var item in new[]{new[]{"GPLv3","LICENSE"},new[]{"Avisos de terceiros","THIRD-PARTY-NOTICES.md"}}){string file=item[1];var button=Button(item[0],file=="LICENSE"?85:185);button.Margin=new Padding(0,0,P(8),0);actions.Controls.Add(button);button.Click+=(s,e)=>{string target=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,file);if(!File.Exists(target)){Report("Avisos disponíveis no pacote completo da aplicação.");return;}try{Process.Start(new ProcessStartInfo(target){UseShellExecute=true});}catch{Report("Não foi possível abrir o ficheiro de licença.");}};}
    }
}
