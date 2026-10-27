// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

public sealed partial class CompanionWindow {
    void CreateTelemetryCard(TableLayoutPanel card){
        var status=TextLabel("",12);status.MaximumSize=new Size(P(470),0);status.Margin=new Padding(0,0,0,P(10));card.Controls.Add(status);
        Action refresh=()=>status.Text=TelemetryPlugin.Status(game.Text.Trim().Trim('"'));refresh();game.TextChanged+=(s,e)=>refresh();
        var setup=Button("Configurar telemetria…",220);card.Controls.Add(setup);setup.Click+=(s,e)=>{StopCountdown();ShowTelemetrySetup(game.Text.Trim().Trim('"'));refresh();};
    }
    void ShowTelemetrySetup(string directory){
        using(var dialog=CreateTelemetrySetup(directory))dialog.ShowDialog(this);
    }
    public void RenderTelemetrySetup(string destination){
        using(var dialog=CreateTelemetrySetup(game.Text.Trim().Trim('"'))){dialog.ShowInTaskbar=false;dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-20000,-20000);dialog.Show();Application.DoEvents();using(var bitmap=new Bitmap(dialog.ClientSize.Width,dialog.ClientSize.Height)){dialog.DrawToBitmap(bitmap,new Rectangle(Point.Empty,dialog.ClientSize));bitmap.Save(destination);}dialog.Close();}
    }
    Form CreateTelemetrySetup(string directory){
        var dialog=new Form{Text="Telemetria do rFactor 2",Icon=BrandIcon(),BackColor=GtcTheme.Background,ForeColor=GtcTheme.Text,Font=F(12),AutoScaleMode=AutoScaleMode.None,StartPosition=FormStartPosition.CenterParent,ClientSize=new Size(P(510),P(400)),MinimumSize=new Size(P(480),P(380)),MaximizeBox=false,MinimizeBox=false};
        {
            var stack=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(P(20))};dialog.Controls.Add(stack);
            Action<string,bool> text=(value,bold)=>{var label=TextLabel(value,bold?14:12,bold);label.MaximumSize=new Size(P(455),0);label.Margin=new Padding(0,0,0,P(12));stack.Controls.Add(label);};
            text("rF2 Shared Memory Map",true);
            var status=TextLabel(TelemetryPlugin.Status(directory),12);status.MaximumSize=new Size(P(455),0);status.Margin=new Padding(0,0,0,P(14));stack.Controls.Add(status);
            text("1. Descarrega o plugin no projeto oficial e extrai o ZIP.",false);
            var download=new LinkLabel{Text="Descarregar no projeto oficial ↗",AutoSize=true,Font=F(12),LinkColor=GtcTheme.Accent,ActiveLinkColor=GtcTheme.Text,VisitedLinkColor=GtcTheme.Accent,LinkBehavior=LinkBehavior.HoverUnderline,Margin=new Padding(0,0,0,P(16))};stack.Controls.Add(download);download.LinkClicked+=(s,e)=>{try{ProcessUrl(TelemetryPlugin.ProjectUrl);}catch{status.Text="Não foi possível abrir o navegador. Consulta o link no README do pacote.";}};
            text("2. Fecha o rFactor. Seleciona o ficheiro rFactor2SharedMemoryMapPlugin64.dll de 64 bits.",false);
            var choose=Button("Selecionar DLL…",180);choose.Margin=new Padding(0,0,0,P(8));stack.Controls.Add(choose);
            string selected=null;var file=TextLabel("Nenhum ficheiro selecionado",11);file.ForeColor=GtcTheme.Muted;file.MaximumSize=new Size(P(455),0);file.Margin=new Padding(0,0,0,P(14));stack.Controls.Add(file);
            choose.Click+=(s,e)=>{using(var picker=new OpenFileDialog{Title="Selecionar plugin extraído do pacote oficial",Filter="Plugin rFactor 2|rFactor2SharedMemoryMapPlugin64.dll",CheckFileExists=true})if(picker.ShowDialog(dialog)==DialogResult.OK){try{TelemetryPlugin.ValidateBinary(picker.FileName);selected=picker.FileName;file.Text=Path.GetFileName(selected);}catch(Exception ex){selected=null;file.Text=ex.Message;}}};
            text("3. Instala e ativa o plugin. A configuração existente é preservada e é criada uma cópia de segurança. Depois abre o jogo e entra em pista.",false);
            var install=Button("Instalar e ativar",180,true);install.Margin=new Padding(0,0,0,P(12));stack.Controls.Add(install);
            install.Click+=(s,e)=>{
                if(selected==null){status.Text="Seleciona primeiro a DLL do pacote oficial.";return;}
                try{string backup=TelemetryPlugin.Install(directory,selected,()=>LaunchEngine.Running("rFactor2"));status.Text="Plugin instalado e ativado. Abre o jogo para usar a telemetria.";Report("Telemetria configurada. A cópia de segurança está em UserData/player/CompanionPluginBackups.");CompanionStore.Log("Plugin instalado; cópia de segurança em "+backup);}
                catch(UnauthorizedAccessException){status.Text="Sem permissão para escrever na pasta do jogo. Consulta o guia oficial para a instalação manual.";}
                catch(Exception ex){status.Text=ex.Message;}
            };
            GtcWindowTheme.Attach(dialog);return dialog;
        }
    }
}
