// Copyright (C) 2026 GTC Companion contributors.
// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

public sealed partial class CompanionWindow {
    sealed class SetupProgress:Control {
        readonly System.Windows.Forms.Timer animation=new System.Windows.Forms.Timer{Interval=60};
        int value,phase;ProgressBarStyle style;
        public SetupProgress(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);animation.Tick+=(s,e)=>{phase=(phase+3)%130;Invalidate();};}
        public int Value{get{return value;}set{this.value=value;Invalidate();}}
        public ProgressBarStyle Style{get{return style;}set{style=value;animation.Enabled=value==ProgressBarStyle.Marquee;Invalidate();}}
        protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(GtcTheme.Input);using(var brush=new SolidBrush(GtcTheme.Accent)){if(style==ProgressBarStyle.Marquee)e.Graphics.FillRectangle(brush,(phase-30)*Width/100,0,Width*30/100,Height);else e.Graphics.FillRectangle(brush,0,0,Width*value/100,Height);}}
        protected override void Dispose(bool disposing){if(disposing)animation.Dispose();base.Dispose(disposing);}
    }
    bool settingUp;
    void CreateTelemetryCard(TableLayoutPanel card){
        var status=TextLabel("",12);status.MaximumSize=new Size(P(470),0);status.Margin=new Padding(0,0,0,P(10));card.Controls.Add(status);
        Action refresh=()=>status.Text=TelemetryPlugin.Status(game.Text.Trim().Trim('"'));refresh();game.TextChanged+=(s,e)=>refresh();
        var setup=Button("Instalar componentes…",220);card.Controls.Add(setup);setup.Click+=(s,e)=>{StopCountdown();ShowTelemetrySetup(game.Text.Trim().Trim('"'));refresh();};
    }
    void ShowTelemetrySetup(string directory){
        if(settingUp)return;settingUp=true;StopCountdown();try{using(var dialog=CreateTelemetrySetup(directory))dialog.ShowDialog(this);}finally{settingUp=false;}
    }
    public void RenderTelemetrySetup(string destination){
        using(var dialog=CreateTelemetrySetup(game.Text.Trim().Trim('"'))){dialog.ShowInTaskbar=false;dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-20000,-20000);dialog.Show();Application.DoEvents();using(var bitmap=new Bitmap(dialog.ClientSize.Width,dialog.ClientSize.Height)){dialog.DrawToBitmap(bitmap,new Rectangle(Point.Empty,dialog.ClientSize));bitmap.Save(destination);}dialog.Close();}
    }
    Form CreateTelemetrySetup(string directory){
        var dialog=new Form{Text="Instalação do GTC Companion",Icon=BrandIcon(),BackColor=GtcTheme.Background,ForeColor=GtcTheme.Text,Font=F(12),AutoScaleMode=AutoScaleMode.None,StartPosition=FormStartPosition.CenterParent,ClientSize=new Size(P(510),P(395)),MinimumSize=new Size(P(480),P(380)),MaximizeBox=false,MinimizeBox=false};
        var stack=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(P(20))};dialog.Controls.Add(stack);
        Action<Control> add=control=>{control.Margin=new Padding(0,0,0,P(12));stack.Controls.Add(control);};
        var title=TextLabel("Preparar o rFactor 2",17,true);add(title);
        var hint=TextLabel("Escolhe os componentes. O launcher instala o plugin incluído e descarrega os pacotes de áudio oficiais.",12);hint.MaximumSize=new Size(P(455),0);add(hint);
        var plugin=new BrandCheck{Text="Plugin de telemetria · 3.7.15.1",Checked=true,AutoSize=true,Font=F(12),ForeColor=GtcTheme.Text,BackColor=GtcTheme.Background};add(plugin);
        var pluginStatus=TextLabel(TelemetryPlugin.Ready(directory)?"Já instalado e ativado.":"Incluído no ZIP · instala com o rFactor fechado.",11);pluginStatus.MaximumSize=new Size(P(455),0);pluginStatus.ForeColor=GtcTheme.Muted;add(pluginStatus);
        string crewExecutable=crewPath.Text.Trim().Trim('"');
        var voices=new BrandCheck{Text="Crew Chief · vozes, nomes e personalizações",Checked=File.Exists(crewExecutable),Enabled=File.Exists(crewExecutable),AutoSize=true,Font=F(12),ForeColor=GtcTheme.Text,BackColor=GtcTheme.Background};add(voices);
        var voicesStatus=TextLabel("Verifica as versões e descarrega apenas os pacotes em falta. O motor deve estar parado durante a instalação.",11);voicesStatus.MaximumSize=new Size(P(455),0);voicesStatus.ForeColor=GtcTheme.Muted;add(voicesStatus);
        var progress=new SetupProgress{Width=P(455),Height=P(8),Style=ProgressBarStyle.Blocks};add(progress);
        var state=TextLabel("Os componentes instalados e as preferências são preservados.",11);state.MaximumSize=new Size(P(455),0);add(state);
        var actions=new FlowLayoutPanel{AutoSize=true,WrapContents=false};add(actions);var install=Button("Instalar / verificar",200,true);install.Margin=new Padding(0,0,P(10),0);actions.Controls.Add(install);var close=Button("Fechar",110);actions.Controls.Add(close);CancellationTokenSource operation=null;
        close.Click+=(s,e)=>{if(operation!=null){operation.Cancel();state.Text="A cancelar. Os downloads concluídos ficam guardados.";}else dialog.Close();};
        install.Click+=async(s,e)=>{
            if(operation!=null)return;if(!plugin.Checked&&!voices.Checked){state.Text="Seleciona pelo menos um componente.";return;}
            operation=new CancellationTokenSource();install.Enabled=false;plugin.Enabled=false;voices.Enabled=false;close.Text="Cancelar";progress.Value=0;progress.Style=ProgressBarStyle.Marquee;state.Text="A preparar a instalação…";
            try{await ComponentSetup.Run(directory,crewExecutable,plugin.Checked,voices.Checked,(id,message)=>{if(dialog.IsDisposed)return;if(id=="plugin")pluginStatus.Text=message;else voicesStatus.Text=message;},operation.Token);state.Text="Instalação concluída. Podes iniciar a sessão.";progress.Value=100;Report("Componentes instalados e verificados.");}
            catch(OperationCanceledException){if(voices.Checked)voicesStatus.Text="Instalação interrompida.";state.Text="Instalação cancelada. Podes continuar de onde ficou.";}
            catch(UnauthorizedAccessException){if(voices.Checked)voicesStatus.Text="Instalação interrompida.";state.Text="Sem permissão para escrever os ficheiros. Os componentes já instalados foram mantidos.";}
            catch(Exception ex){if(voices.Checked)voicesStatus.Text="Instalação interrompida.";state.Text=ex.Message;}
            finally{operation.Dispose();operation=null;if(!dialog.IsDisposed){install.Enabled=true;install.Text="Verificar novamente";plugin.Enabled=true;voices.Enabled=File.Exists(crewExecutable);close.Text="Fechar";progress.Style=ProgressBarStyle.Blocks;}}
        };
        dialog.FormClosing+=(s,e)=>{if(operation!=null){operation.Cancel();e.Cancel=true;state.Text="A aguardar o cancelamento…";}};
        GtcWindowTheme.Attach(dialog);return dialog;
    }
}
