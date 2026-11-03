// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;

public static class AppLanguage {
    static readonly Dictionary<string,string> english=Load();static readonly Dictionary<Control,string> originals=new Dictionary<Control,string>();static readonly Dictionary<ToolStripItem,string> menuOriginals=new Dictionary<ToolStripItem,string>();static readonly HashSet<ComboBox> combos=new HashSet<ComboBox>();
    static readonly Dictionary<string,string> prefixes=new Dictionary<string,string>{{"Sobre · v","About · v"},{"Versão ","Version "},{"Criado por ","Created by "},{"Média ","Average "},{"Última ","Last "},{"Alvo ","Target "},{"Volta ","Lap "},{"Faltam ","Remaining "}};
    static bool updating;static string language="pt";static Timer timer;
    static Dictionary<string,string> Load(){try{using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("GtcEnglish"))using(var r=new StreamReader(s)){return new JavaScriptSerializer{MaxJsonLength=4000000}.Deserialize<Dictionary<string,string>>(r.ReadToEnd());}}catch{return new Dictionary<string,string>();}}
    public static string Current{get{return language;}}public static event Action LanguageChanged;
    public static void Set(string value){string next=string.Equals(value,"en",StringComparison.OrdinalIgnoreCase)?"en":"pt";bool changed=language!=next;language=next;Refresh();if(changed&&LanguageChanged!=null)LanguageChanged();}
    public static string T(string text){if(text==null||language!="en")return text;string result;if(english.TryGetValue(text,out result)&&!string.IsNullOrEmpty(result))return result;if(text.EndsWith(" · versão de teste",StringComparison.Ordinal))return text.Substring(0,text.Length-" · versão de teste".Length)+" · test version";foreach(var prefix in prefixes)if(text.StartsWith(prefix.Key,StringComparison.Ordinal))return prefix.Value+text.Substring(prefix.Key.Length);return text;}
    static bool Translatable(Control c){return c is Label||c is ButtonBase||c is GroupBox||c is Form;}
    static void Register(Control c){
        if(c==null||c.IsDisposed)return;if(Translatable(c)&&!originals.ContainsKey(c)){
            originals[c]=c.Text;c.TextChanged+=(s,e)=>{if(updating||c.IsDisposed)return;originals[c]=c.Text;Apply(c);};c.Disposed+=(s,e)=>originals.Remove(c);
        }
        var combo=c as ComboBox;if(combo!=null&&!combos.Contains(combo)){combos.Add(combo);combo.FormattingEnabled=true;combo.Format+=(s,e)=>{var value=e.ListItem as string;if(value!=null)e.Value=T(value);else if(e.ListItem!=null&&e.ListItem.GetType().Name=="Choice")e.Value=T(e.ListItem.ToString());};combo.Disposed+=(s,e)=>combos.Remove(combo);}
        var strip=c as ToolStrip;if(strip!=null)RegisterItems(strip.Items);if(c.ContextMenuStrip!=null)RegisterItems(c.ContextMenuStrip.Items);
        foreach(Control child in c.Controls)Register(child);if(Translatable(c))Apply(c);
    }
    static void Apply(Control c){string text;if(c.IsDisposed||!originals.TryGetValue(c,out text))return;string next=T(text);if(c.Text==next)return;updating=true;try{c.Text=next;}finally{updating=false;}}
    static void RegisterItems(ToolStripItemCollection items){foreach(ToolStripItem item in items){if(!menuOriginals.ContainsKey(item)){menuOriginals[item]=item.Text;item.TextChanged+=(s,e)=>{if(updating||item.IsDisposed)return;menuOriginals[item]=item.Text;ApplyItem(item);};item.Disposed+=(s,e)=>menuOriginals.Remove(item);}ApplyItem(item);var menu=item as ToolStripDropDownItem;if(menu!=null)RegisterItems(menu.DropDownItems);}}
    static void ApplyItem(ToolStripItem item){string text;if(item.IsDisposed||!menuOriginals.TryGetValue(item,out text))return;string next=T(text);if(item.Text==next)return;updating=true;try{item.Text=next;}finally{updating=false;}}
    public static void Refresh(){foreach(var item in originals.Keys.ToArray())Apply(item);foreach(var item in menuOriginals.Keys.ToArray())ApplyItem(item);foreach(Form f in Application.OpenForms.Cast<Form>().ToArray()){InvalidateCombos(f);f.Invalidate(true);}}
    static void InvalidateCombos(Control c){var combo=c as ComboBox;if(combo!=null)combo.Invalidate();foreach(Control child in c.Controls)InvalidateCombos(child);}
    public static void Attach(Control root){Register(root);}
    public static void Watch(){if(timer!=null)return;timer=new Timer{Interval=400};timer.Tick+=(s,e)=>{foreach(Form form in Application.OpenForms.Cast<Form>().ToArray())Register(form);};timer.Start();}
}
