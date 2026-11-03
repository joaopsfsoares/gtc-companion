// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

public sealed partial class CompanionWindow {
    readonly Button[] shortcutButtons=new Button[4];
    Button resetShortcuts;
    Label shortcutStatus;
    bool shortcutBusy;
    DateTime shortcutReplyUtc=DateTime.MinValue;
    Action refreshShortcuts;
    void CreateShortcutControls(TableLayoutPanel contents){
        var card=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,RowCount=1,Padding=new Padding(P(12)),Margin=new Padding(0,0,0,P(14)),BackColor=GtcTheme.Surface};card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,54));card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,46));card.RowStyles.Add(new RowStyle(SizeType.Absolute,P(30)));
        var title=TextLabel("ATALHOS GLOBAIS DO OVERLAY",11,true);title.ForeColor=GtcTheme.Muted;card.Controls.Add(title,0,0);card.SetColumnSpan(title,2);contents.Controls.Add(card);
        var keys=OverlayShortcuts.Normalize(ReadOverlay().Shortcuts).Bindings();
        for(int i=0;i<4;i++){
            int action=i;card.RowCount++;card.RowStyles.Add(new RowStyle(SizeType.Absolute,P(42)));var label=TextLabel(OverlayShortcuts.Names[i],12);label.MaximumSize=new Size(P(250),0);label.Anchor=AnchorStyles.Left;card.Controls.Add(label,0,i+1);
            var button=Button(keys[i].ToString(),220);button.Anchor=AnchorStyles.Right;button.AccessibleName=OverlayShortcuts.Names[i];shortcutButtons[i]=button;card.Controls.Add(button,1,i+1);
            button.Click+=async(s,e)=>{StopCountdown();var current=OverlayShortcuts.Normalize(ReadOverlay().Shortcuts);using(var dialog=CreateShortcutEditor(action,current))if(dialog.ShowDialog(this)==DialogResult.OK){var next=current.Copy();next.Set(action,((ShortcutBinding)dialog.Tag).Copy());await ApplyShortcuts(next);}};
        }
        resetShortcuts=Button("Repor atalhos originais",210);resetShortcuts.Margin=new Padding(0,0,0,P(12));contents.Controls.Add(resetShortcuts);resetShortcuts.Click+=async(s,e)=>await ApplyShortcuts(new OverlayShortcuts());
        shortcutStatus=TextLabel("As alterações são aplicadas sem reiniciar o jogo.",12);shortcutStatus.ForeColor=GtcTheme.Muted;shortcutStatus.MaximumSize=new Size(P(480),0);shortcutStatus.Margin=new Padding(0,0,0,P(10));contents.Controls.Add(shortcutStatus);
        var hint=TextLabel("Seleciona uma ação para escolher a tecla e os modificadores.\nCombinações repetidas ou ocupadas são recusadas.",12);hint.ForeColor=GtcTheme.Muted;hint.MaximumSize=new Size(P(480),0);contents.Controls.Add(hint);
        refreshShortcuts=()=>{
            var current=OverlayShortcuts.Normalize(ReadOverlay().Shortcuts).Bindings();for(int i=0;i<4;i++){shortcutButtons[i].Text=current[i].ToString();shortcutButtons[i].Enabled=!shortcutBusy;}resetShortcuts.Enabled=!shortcutBusy;
            var status=CompanionStore.Read<System.Collections.Generic.Dictionary<string,object>>(Path.Combine(OverlayRoot,"status.json"));object failure;
            if(!shortcutBusy&&File.GetLastWriteTimeUtc(Path.Combine(OverlayRoot,"status.json"))>shortcutReplyUtc&&status!=null&&status.TryGetValue("shortcutError",out failure)&&!string.IsNullOrWhiteSpace(Convert.ToString(failure))){shortcutStatus.Text=Convert.ToString(failure);shortcutStatus.ForeColor=Color.FromArgb(255,112,126);}
        };
    }
    Form CreateShortcutEditor(int action,OverlayShortcuts current){
        var binding=current.Bindings()[action];var dialog=new Form{Text="Alterar atalho",Icon=BrandIcon(),ClientSize=new Size(P(450),P(300)),FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,StartPosition=FormStartPosition.CenterParent,ShowInTaskbar=false,BackColor=GtcTheme.Background,ForeColor=GtcTheme.Text,Font=F(13),AutoScaleMode=AutoScaleMode.None};
        var title=TextLabel(OverlayShortcuts.Names[action],17,true);title.Location=new Point(P(22),P(20));dialog.Controls.Add(title);
        var enabled=new GtcSettingsCheck{Checked=binding.Enabled,Text="",Size=new Size(P(24),P(24)),Location=new Point(P(21),P(62)),BackColor=GtcTheme.Background};dialog.Controls.Add(enabled);var enableLabel=TextLabel("Ativar este atalho",13);enableLabel.Location=new Point(P(54),P(65));dialog.Controls.Add(enableLabel);
        var modifiers=new FlowLayoutPanel{AutoSize=true,FlowDirection=FlowDirection.LeftToRight,WrapContents=false,Location=new Point(P(22),P(106)),Margin=Padding.Empty};dialog.Controls.Add(modifiers);
        Func<string,int,CheckBox> modifier=(name,mask)=>{var check=new BrandCheck{Text=name,Checked=(binding.Modifiers&mask)!=0,AutoSize=true,ForeColor=GtcTheme.Text,BackColor=GtcTheme.Background,Font=F(13),Margin=new Padding(0,0,P(20),0),UseVisualStyleBackColor=false};modifiers.Controls.Add(check);return check;};
        var ctrl=modifier("Ctrl",2);var alt=modifier("Alt",1);var shift=modifier("Shift",4);
        var keyLabel=TextLabel("Tecla",13);keyLabel.Location=new Point(P(22),P(151));dialog.Controls.Add(keyLabel);
        var key=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=P(23),Width=P(190),Location=new Point(P(110),P(145)),Font=F(13),BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,FlatStyle=FlatStyle.Flat};
        var options=ShortcutBinding.SupportedKeys();foreach(var value in options)key.Items.Add(ShortcutBinding.KeyName(value));int selected=options.IndexOf((Keys)binding.Key);key.SelectedIndex=selected<0?0:selected;
        key.DrawItem+=(s,e)=>{if(e.Index<0)return;using(var brush=new SolidBrush(GtcTheme.Input))e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,AppLanguage.T(key.Items[e.Index].ToString()),key.Font,e.Bounds,GtcTheme.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);};dialog.Controls.Add(key);
        Action toggle=()=>{modifiers.Enabled=enabled.Checked;key.Enabled=enabled.Checked;};enabled.CheckedChanged+=(s,e)=>toggle();toggle();
        var error=TextLabel("",12);error.ForeColor=Color.FromArgb(255,112,126);error.MaximumSize=new Size(P(405),0);error.Location=new Point(P(22),P(190));dialog.Controls.Add(error);
        var apply=Button("Aplicar",110,true);apply.Location=new Point(P(318),P(244));dialog.Controls.Add(apply);var cancel=Button("Cancelar",110);cancel.Location=new Point(P(198),P(244));cancel.DialogResult=DialogResult.Cancel;dialog.Controls.Add(cancel);dialog.AcceptButton=apply;dialog.CancelButton=cancel;
        apply.Click+=(s,e)=>{
            var nextBinding=new ShortcutBinding{Key=enabled.Checked?(int)options[key.SelectedIndex]:0,Modifiers=enabled.Checked?((ctrl.Checked?2:0)|(alt.Checked?1:0)|(shift.Checked?4:0)):0};var next=current.Copy();next.Set(action,nextBinding);string validation=next.Validate();
            if(validation!=null){error.Text=validation;return;}dialog.Tag=nextBinding;dialog.DialogResult=DialogResult.OK;dialog.Close();
        };GtcWindowTheme.Attach(dialog);return dialog;
    }
    async Task ApplyShortcuts(OverlayShortcuts next){
        if(shortcutBusy||PreviewOnly)return;StopCountdown();string error=next.Validate();if(error!=null){shortcutStatus.Text=error;return;}
        shortcutBusy=true;shortcutStatus.Text="A verificar e ativar os atalhos…";shortcutStatus.ForeColor=GtcTheme.Muted;refreshShortcuts();
        try{
            Directory.CreateDirectory(OverlayRoot);Directory.CreateDirectory(CompetitionProfiles.PreferencesDirectory);
            if(LaunchEngine.Running("GTC-Timing")){
                string id=Guid.NewGuid().ToString("N");var patch=new OverlayPatch{Shortcuts=next,ShortcutRequestId=id,ShortcutExpiresUtc=DateTime.UtcNow.AddSeconds(5)};string directory=Path.Combine(OverlayRoot,"commands");Directory.CreateDirectory(directory);string target=Path.Combine(directory,DateTime.UtcNow.Ticks.ToString("D19")+"-"+id+".json");File.WriteAllText(target+".tmp",CompanionStore.Json.Serialize(patch));File.Move(target+".tmp",target);
                string resultFile=Path.Combine(OverlayRoot,"shortcut-results",id+".json");ShortcutResult result=null;DateTime deadline=DateTime.UtcNow.AddSeconds(6);
                while(DateTime.UtcNow<deadline&&!IsDisposed){result=CompanionStore.Read<ShortcutResult>(resultFile);if(result!=null)break;await Task.Delay(100);}
                if(result==null){if(!OverlayShortcuts.Normalize(ReadOverlay().Shortcuts).SameAs(next))throw new InvalidOperationException("O overlay não confirmou a alteração. Volta a tentar.");}
                else{if(File.Exists(resultFile))File.Delete(resultFile);if(!result.Success)throw new InvalidOperationException(result.Message);}
            }else{
                if(!GlobalShortcutSet.Probe(next,out error))throw new InvalidOperationException(error);var prefs=ReadOverlay();prefs.Shortcuts=next.Copy();CompanionStore.Write(Path.Combine(CompetitionProfiles.PreferencesDirectory,"settings.json"),prefs);
            }
            shortcutStatus.Text="Atalhos atualizados.";shortcutStatus.ForeColor=Color.FromArgb(90,228,165);Report("Atalhos atualizados e guardados.");
        }catch(Exception ex){if(!IsDisposed){shortcutStatus.Text=ex.Message;shortcutStatus.ForeColor=Color.FromArgb(255,112,126);Report(ex.Message);}}
        finally{shortcutBusy=false;shortcutReplyUtc=DateTime.UtcNow;if(!IsDisposed)refreshShortcuts();}
    }
}
