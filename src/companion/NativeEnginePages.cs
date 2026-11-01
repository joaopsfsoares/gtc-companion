// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

public sealed class NativeEnginePage:UserControl {
    readonly string engine,name;
    readonly Func<string> executable;
    readonly Action changed;
    readonly Func<string,EngineSettingsSession> loader;
    readonly float scale;
    readonly Label status;
    readonly ComboBox profile,group;
    readonly TextBox search;
    readonly Panel viewport=new Panel{Dock=DockStyle.Fill,AutoScroll=true};
    readonly TableLayoutPanel contents=new TableLayoutPanel{Dock=DockStyle.Top,ColumnCount=1,AutoSize=true,Margin=Padding.Empty};
    readonly Button widgets,preferences,start,save;
    readonly Button previousPage,nextPage;
    readonly Label pageLabel;
    int pageIndex;
    readonly ToolTip hints=new ToolTip{AutoPopDelay=12000};
    readonly System.Windows.Forms.Timer searchDelay=new System.Windows.Forms.Timer{Interval=180};
    readonly System.Windows.Forms.Timer stateTimer=new System.Windows.Forms.Timer{Interval=2000};
    EngineSettingsSession session;
    CancellationTokenSource voiceDownload;
    readonly HashSet<EngineSetting> invalid=new HashSet<EngineSetting>();
    bool loading,widgetView;
    int P(int value){return (int)Math.Round(value*scale);}
    Font F(int size,bool bold=false){return GtcTypography.Create(size*scale,bold);}
    static readonly Dictionary<string,string> Titles=new Dictionary<string,string>{
        {"audio","Áudio e engenheiro"},{"spotter","Spotter"},{"fuel","Combustível"},{"voice","Reconhecimento de voz"},{"messages","Mensagens"},{"rfactor","rFactor 2"},{"advanced","Avançado"},
        {"overlay","Comportamento dos overlays"},{"units","Unidades"},{"relative","Pilotos próximos"},{"standings","Classificação"},{"radar","Radar"},{"track_map","Mapa da pista"},{"timing","Tempos"},{"sectors","Setores"},{"pedal","Pedais"},{"tyre_pressure","Pressão dos pneus"},{"tyre_temperature","Temperatura dos pneus"},{"tyre_wear","Desgaste dos pneus"},{"laps_and_position","Voltas e posição"},
        {"global/application","Aplicação"},{"global/compatibility","Compatibilidade"},{"global/telemetry","Telemetria"},{"global/user_path","Pastas"},
        {"enable","Ativar"},{"position_x","Posição horizontal"},{"position_y","Posição vertical"},{"opacity","Opacidade"},{"global_scale","Escala"},{"font_name","Tipo de letra"},{"font_size","Tamanho do texto"},{"font_weight","Espessura do texto"},{"update_interval","Intervalo de atualização (ms)"},{"fixed_position","Bloquear posição"},{"auto_hide","Ocultar fora de pista"},{"show_at_startup","Mostrar janela original ao iniciar"},{"minimize_to_tray","Minimizar para a área de notificação"},{"PERSONALISATION_NAME","Nome usado pelo engenheiro"},{"chief_name","Voz do engenheiro"},{"spotter_name","Voz do spotter"},{"messages_volume","Volume das mensagens"},{"background_volume","Volume de fundo"},{"VOICE_OPTION","Modo de reconhecimento de voz"},{"NAUDIO_DEVICE_GUID_MESSAGES","Saída das mensagens"},{"NAUDIO_DEVICE_GUID_BACKGROUND","Saída do áudio de fundo"},{"NAUDIO_RECORDING_DEVICE_GUID","Microfone"},
        {"enable_spotter","Ativar spotter"},{"enable_fuel_messages","Informações de combustível"},{"enable_driver_names","Dizer nomes dos pilotos"},{"use_sweary_messages","Permitir palavrões"},{"minimize_on_startup","Iniciar em segundo plano"},
        {"acceleration","Aceleração"},{"battery","Bateria"},{"brake_bias","Repartição de travagem"},{"brake_performance","Desempenho dos travões"},{"brake_pressure","Pressão de travagem"},{"brake_temperature","Temperatura dos travões"},{"brake_wear","Desgaste dos travões"},{"cruise","Velocidade de cruzeiro"},{"damage","Danos"},{"damage_stats","Detalhes dos danos"},{"deltabest","Delta da melhor volta"},{"deltabest_extended","Delta detalhado"},{"differential","Diferencial"},{"electric_motor","Motor elétrico"},{"elevation","Elevação"},{"engine","Motor"},{"engine_temperature","Temperatura do motor"},{"flag","Bandeiras"},{"force","Forças"},{"friction_circle","Círculo de aderência"},{"fuel_energy_saver","Poupança de combustível e energia"},{"gear","Mudança"},{"heading","Direção"},{"instrument","Instrumentos"},{"lap_time_history","Histórico de voltas"},{"lift_and_coast_led","Indicador de lift and coast"},{"navigation","Navegação"},{"onboard_setting","Regulações do carro"},{"pace_notes","Notas de condução"},{"pit_stop_estimate","Estimativa de paragem"},{"push_to_pass","Push to pass"},{"rake_angle","Inclinação do carro"},{"relative_finish_order","Previsão da classificação"},{"ride_height","Altura do carro"},{"rivals","Rivais"},{"roll_angle","Rolamento"},{"rpm_led","Indicador de rotações"},{"session","Sessão"},{"slip_angle","Ângulo de deriva"},{"slip_ratio","Deslizamento das rodas"},{"speedometer","Velocímetro"},{"steering_angle","Ângulo do volante"},{"steering_meter","Direção"},{"steering_wheel","Volante"},{"stint_history","Histórico de stints"},{"suspension_force","Força da suspensão"},{"suspension_position","Posição da suspensão"},{"suspension_travel","Curso da suspensão"},{"system_performance","Desempenho do sistema"},{"track_clock","Relógio da pista"},{"track_notes","Notas da pista"},{"traffic","Tráfego"},{"trailing","Rasto"},{"tyre_carcass","Carcaça dos pneus"},{"tyre_deflection","Deformação dos pneus"},{"tyre_inner_layer","Interior dos pneus"},{"tyre_load","Carga dos pneus"},{"virtual_energy","Energia virtual"},{"weather","Meteorologia"},{"weather_forecast","Previsão meteorológica"},{"weight_distribution","Distribuição de peso"},{"wheel_camber","Camber"},{"wheel_dimension","Dimensões das rodas"},{"wheel_toe","Convergência"},{"wind_direction","Direção do vento"}
    };
    public static string Title(string key){string value;if(Titles.TryGetValue(key,out value))return value;string text=key.Replace("global/","").Replace("_"," ");return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text);}
    Button MakeButton(string text,bool primary=false){var button=new Button{Text=text,AutoSize=true,Height=P(34),Padding=new Padding(P(10),P(4),P(10),P(4)),BackColor=primary?GtcTheme.Accent:GtcTheme.Input,ForeColor=primary?GtcTheme.Background:GtcTheme.Text,Font=F(11,true),FlatStyle=FlatStyle.Flat,Cursor=Cursors.Hand,Margin=new Padding(0,0,P(8),0)};button.FlatAppearance.BorderColor=GtcTheme.Border;return button;}
    ComboBox MakeChoice(){var combo=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Font=F(12),Width=P(210),Margin=new Padding(0,0,P(8),0),DrawMode=DrawMode.OwnerDrawFixed,ItemHeight=P(22)};combo.DrawItem+=(s,e)=>{if(e.Index<0)return;using(var brush=new SolidBrush(GtcTheme.Input))e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,combo.GetItemText(combo.Items[e.Index]),combo.Font,e.Bounds,GtcTheme.Text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};return combo;}
    sealed class Choice{public string Id,Caption;public override string ToString(){return Caption;}}
    public NativeEnginePage(string id,Func<string> path,Action onChange,float dpi,Func<string,EngineSettingsSession> settingsLoader=null,Action installComponents=null,CheckBox launchOption=null){
        engine=id;name=id=="tiny"?"TinyPedal":"Crew Chief";executable=path;changed=onChange;loader=settingsLoader;scale=dpi;Dock=DockStyle.Fill;BackColor=GtcTheme.Background;widgetView=id=="tiny";
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=8,Margin=Padding.Empty};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));for(int i=0;i<6;i++)root.RowStyles.Add(new RowStyle(SizeType.AutoSize));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(root);
        var heading=new Label{Text=name,Font=F(19,true),AutoSize=true,ForeColor=GtcTheme.Text,Margin=new Padding(0,0,0,P(10))};root.Controls.Add(heading,0,0);
        if(launchOption!=null){
            var launchRow=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,WrapContents=true,Margin=new Padding(0,0,0,P(8))};
            launchOption.BackColor=GtcTheme.Background;launchOption.Margin=new Padding(0,P(3),P(18),P(3));launchRow.Controls.Add(launchOption);
            bool included=File.Exists(id=="tiny"?BundledEngines.Tiny:BundledEngines.Crew);
            var version=new LinkLabel{Text=included?(id=="tiny"?"Motor incluído · 2.52.0 ↗":"Motor incluído · 4.19.5.8 ↗"):"Motor em falta · repositório ↗",AutoSize=true,Font=F(11),LinkColor=GtcTheme.Accent,ActiveLinkColor=GtcTheme.Text,VisitedLinkColor=GtcTheme.Accent,LinkBehavior=LinkBehavior.HoverUnderline,Margin=new Padding(0,P(5),0,0),Cursor=Cursors.Hand};
            version.LinkClicked+=(s,e)=>{changed();try{Process.Start(new ProcessStartInfo(id=="tiny"?"https://github.com/TinyPedal/TinyPedal":"https://gitlab.com/mr_belowski/CrewChiefV4"){UseShellExecute=true});}catch(Exception ex){status.Text="Não foi possível abrir o repositório: "+ex.Message;}};launchRow.Controls.Add(version);root.Controls.Add(launchRow,0,1);
        }
        var tabs=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Margin=new Padding(0,0,0,P(12))};root.Controls.Add(tabs,0,2);widgets=MakeButton("Widgets");preferences=MakeButton("Definições");if(id=="tiny"){tabs.Controls.Add(widgets);tabs.Controls.Add(preferences);}start=MakeButton("Iniciar");tabs.Controls.Add(start);save=MakeButton("Guardar",true);tabs.Controls.Add(save);
        if(id=="crew"){
            var stop=MakeButton("Parar");tabs.Controls.Add(stop);stop.Click+=async(s,e)=>{changed();if(!EngineSettingsQueue.Running("crew"))return;try{using(var command=Process.Start(new ProcessStartInfo(executable(),"-c_exit"){UseShellExecute=true,WindowStyle=ProcessWindowStyle.Hidden})){}for(int n=0;n<60&&EngineSettingsQueue.Running("crew");n++)await Task.Delay(250);BundledEngines.Prepare();EngineSettingsQueue.ApplyReady();UpdateStatus();}catch(Exception ex){status.Text=ex.Message;}};
            var voices=MakeButton("Instalar vozes");tabs.Controls.Add(voices);voices.Click+=async(s,e)=>{changed();if(installComponents!=null){installComponents();Render();return;}if(voiceDownload!=null){voiceDownload.Cancel();return;}if(EngineSettingsQueue.Running("crew")){status.Text="Para o motor antes de instalar ou atualizar as vozes.";return;}voiceDownload=new CancellationTokenSource();voices.Text="Cancelar descarga";try{await CrewVoices.Install(CrewVoices.SoundRoot(executable(),session==null?null:session.Options),text=>status.Text=text,voiceDownload.Token);LoadSettings(session==null?null:session.Profile);}catch(OperationCanceledException){status.Text="Descarga cancelada.";}catch(Exception ex){status.Text=ex.Message;}finally{voiceDownload.Dispose();voiceDownload=null;voices.Text="Instalar vozes";}};
        }

        var selectors=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Margin=new Padding(0,0,0,P(10)),WrapContents=true};root.Controls.Add(selectors,0,3);
        profile=MakeChoice();profile.AccessibleName="Perfil do "+name;profile.FormattingEnabled=true;profile.Format+=(s,e)=>{string value=Path.GetFileNameWithoutExtension(Convert.ToString(e.Value));e.Value=value=="default"||value=="defaultSettings"?"Perfil predefinido":value;};group=MakeChoice();group.AccessibleName="Categoria de definições";selectors.Controls.Add(profile);selectors.Controls.Add(group);var reload=MakeButton("Recarregar");selectors.Controls.Add(reload);reload.Click+=(s,e)=>{if(session!=null&&session.Dirty){status.Text="Guarda as alterações antes de recarregar.";return;}LoadSettings(session==null?null:session.Profile);};
        search=new TextBox{Font=F(12),BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,BorderStyle=BorderStyle.FixedSingle,Dock=DockStyle.Top,AccessibleName="Pesquisar no "+name,Margin=new Padding(0,0,0,P(8))};search.HandleCreated+=(s,e)=>SendMessage(search.Handle,0x1501,new IntPtr(1),"Pesquisar widgets ou definições…");root.Controls.Add(search,0,4);hints.SetToolTip(search,"Pesquisar widgets ou definições");
        status=new Label{Font=F(11),ForeColor=GtcTheme.Muted,AutoSize=true,MaximumSize=new Size(P(760),0),Margin=new Padding(0,0,0,P(12))};root.Controls.Add(status,0,5);viewport.Controls.Add(contents);contents.ColumnCount=2;contents.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));contents.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));root.Controls.Add(viewport,0,6);
        var paging=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,Margin=new Padding(0,P(6),0,0)};previousPage=MakeButton("←");nextPage=MakeButton("→");previousPage.AccessibleName="Página anterior";nextPage.AccessibleName="Página seguinte";pageLabel=new Label{AutoSize=true,ForeColor=GtcTheme.Muted,Font=F(11),Margin=new Padding(P(8),P(9),P(14),0)};paging.Controls.Add(previousPage);paging.Controls.Add(pageLabel);paging.Controls.Add(nextPage);root.Controls.Add(paging,0,7);previousPage.Click+=(s,e)=>{pageIndex=Math.Max(0,pageIndex-1);Render();};nextPage.Click+=(s,e)=>{pageIndex++;Render();};
        widgets.Click+=(s,e)=>{widgetView=true;pageIndex=0;Render();};preferences.Click+=(s,e)=>{widgetView=false;pageIndex=0;Render();};
        profile.SelectedIndexChanged+=(s,e)=>{if(loading)return;if(session!=null&&session.Dirty){loading=true;profile.SelectedItem=session.Profile;loading=false;status.Text="Guarda as alterações antes de escolher outro perfil.";return;}LoadSettings(Convert.ToString(profile.SelectedItem));};
        group.SelectedIndexChanged+=(s,e)=>{if(!loading){pageIndex=0;Render();}};search.TextChanged+=(s,e)=>{pageIndex=0;searchDelay.Stop();searchDelay.Start();};searchDelay.Tick+=(s,e)=>{searchDelay.Stop();Render();};
        save.Click+=(s,e)=>{changed();try{if(session==null||invalid.Count>0)return;session.Save();UpdateStatus();}catch(Exception ex){status.Text=ex.Message;}};
        start.Click+=async(s,e)=>await StartEngine();VisibleChanged+=(s,e)=>{if(Visible&&session==null)LoadSettings();};stateTimer.Tick+=(s,e)=>{if(Visible&&session!=null)UpdateStatus();};stateTimer.Start();
    }
    public void LoadSettings(string selected=null){
        loading=true;try{
            session=loader!=null?loader(selected):engine=="tiny"?EngineSettingsSession.Tiny(executable(),selected):EngineSettingsSession.Crew(executable(),selected);
            profile.Items.Clear();profile.Items.AddRange(session.Profiles.Cast<object>().ToArray());if(!profile.Items.Contains(session.Profile))profile.Items.Add(session.Profile);profile.SelectedItem=session.Profile;
            group.Items.Clear();foreach(string key in session.Options.Select(x=>x.Group).Where(x=>!x.StartsWith("_")).Distinct().OrderBy(Title))group.Items.Add(new Choice{Id=key,Caption=Title(key)});
            string preferred=engine=="tiny"?"overlay":"audio";int index=group.Items.Cast<Choice>().ToList().FindIndex(x=>x.Id==preferred);if(group.Items.Count>0)group.SelectedIndex=Math.Max(0,index);
            UpdateStatus();Render();
        }catch(Exception ex){session=null;status.Text=ex.Message;contents.Controls.Clear();}finally{loading=false;}
    }
    void UpdateStatus(){
        if(session==null||voiceDownload!=null)return;int count=EngineSettingsQueue.Pending(engine).Changes.Count;bool running=EngineSettingsQueue.Running(engine);start.Enabled=!running;start.Text=running?"Motor ativo":"Iniciar";save.Enabled=session.Dirty;
        if(invalid.Count>0){save.Enabled=false;status.Text="Corrige os valores assinalados antes de guardar.";return;}
        if(session.Dirty){status.Text="Existem alterações por guardar.";return;}
        if(!string.IsNullOrEmpty(session.Warning)){status.Text=session.Warning;return;}
        var pending=EngineSettingsQueue.Pending(engine);status.Text=pending.Error??(count>0?count+" alterações guardadas. Serão aplicadas quando o motor estiver fechado.":running?"Motor ativo. Configura os overlays e as preferências nesta página.":"Definições prontas. Inicia o motor para as utilizar.");
    }
    async Task StartEngine(){
        changed();if(session!=null&&session.Dirty){status.Text="Guarda as alterações antes de iniciar o motor.";return;}string exe=executable();if(!File.Exists(exe)){status.Text="Pacote incompleto. Extrai novamente a aplicação com a pasta components.";return;}
        try{BundledEngines.Prepare();EngineSettingsQueue.Apply(engine);Process.Start(new ProcessStartInfo(exe,engine=="crew"?"-game RF2":""){WorkingDirectory=Path.GetDirectoryName(exe),UseShellExecute=true,WindowStyle=engine=="crew"?ProcessWindowStyle.Hidden:ProcessWindowStyle.Minimized});status.Text="A iniciar o motor…";if(engine=="crew")EngineBackground.RequestHidden(exe,"Crew Chief");else await EngineBackground.HideTinyOnce(exe);UpdateStatus();}catch(Exception ex){status.Text=ex.Message;}
    }
    void Render(){
        viewport.SuspendLayout();contents.SuspendLayout();invalid.Clear();foreach(Control control in contents.Controls.Cast<Control>().ToArray())control.Dispose();contents.RowStyles.Clear();contents.RowCount=0;
        widgets.BackColor=widgetView?GtcTheme.AccentSurface:GtcTheme.Input;widgets.ForeColor=widgetView?GtcTheme.Accent:GtcTheme.Text;preferences.BackColor=!widgetView?GtcTheme.AccentSurface:GtcTheme.Input;preferences.ForeColor=!widgetView?GtcTheme.Accent:GtcTheme.Text;group.Visible=!widgetView;
        var all=new List<EngineSetting>();string query=search.Text.Trim();
        if(session!=null){
            if(widgetView)all=session.Options.Where(x=>x.Key=="enable"&&!x.Group.StartsWith("global/")&&!x.Group.StartsWith("module_")&&x.Group!="pace_notes_playback").OrderByDescending(x=>Convert.ToBoolean(x.Value)).ThenBy(x=>Title(x.Group)).ToList();
            else{var selection=group.SelectedItem as Choice;all=session.Options.Where(x=>selection!=null&&x.Group==selection.Id&&!x.Key.StartsWith("#")).ToList();}
            if(query.Length>0)all=all.Where(x=>Title(widgetView?x.Group:x.Key).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0||(widgetView?x.Group:x.Key).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0).ToList();
        }
        int size=widgetView?10:6,pages=Math.Max(1,(all.Count+size-1)/size);pageIndex=Math.Max(0,Math.Min(pageIndex,pages-1));
        foreach(var option in all.Skip(pageIndex*size).Take(size)){if(widgetView)WidgetRow(option);else SettingRow(option);}
        pageLabel.Text=all.Count==0?"Sem resultados":(pageIndex+1)+" / "+pages+" · "+all.Count+(widgetView?" widgets":" definições");previousPage.Enabled=pageIndex>0;nextPage.Enabled=pageIndex+1<pages;viewport.AutoScrollPosition=Point.Empty;
        contents.ResumeLayout(true);viewport.ResumeLayout(true);
    }
    TableLayoutPanel Card(int columns){var card=new TableLayoutPanel{ColumnCount=columns,RowCount=1,Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(P(8)),BackColor=GtcTheme.Surface,Margin=new Padding(0,0,P(6),P(6))};card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));for(int i=1;i<columns;i++)card.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));contents.Controls.Add(card);return card;}
    Label Label(string text){return new Label{Text=text,Font=F(12),AutoSize=true,ForeColor=GtcTheme.Text,Anchor=AnchorStyles.Left,Margin=new Padding(0,P(5),P(10),P(5)),MaximumSize=new Size(P(225),0)};}
    void WidgetRow(EngineSetting option){
        var card=Card(3);var caption=Label(Title(option.Group));caption.MaximumSize=new Size(P(155),0);card.Controls.Add(caption,0,0);var check=new GtcSettingsCheck{Checked=Convert.ToBoolean(option.Value),Size=new Size(P(28),P(28)),BackColor=GtcTheme.Surface,AccessibleName="Ativar "+Title(option.Group),Cursor=Cursors.Hand,Margin=new Padding(P(3),P(3),P(5),0)};card.Controls.Add(check,1,0);check.CheckedChanged+=(s,e)=>{option.Value=check.Checked;changed();UpdateStatus();};
        var edit=MakeButton("⚙");hints.SetToolTip(edit,"Definições de "+Title(option.Group));edit.AccessibleName="Definições de "+Title(option.Group);card.Controls.Add(edit,2,0);edit.Click+=(s,e)=>{widgetView=false;pageIndex=0;search.Clear();group.SelectedItem=group.Items.Cast<Choice>().First(x=>x.Id==option.Group);Render();};
    }
    void SettingRow(EngineSetting option){
        var card=Card(1);var label=Label(Title(option.Key));hints.SetToolTip(label,(option.Hint??option.Key).Replace("\\r\\n",Environment.NewLine));card.Controls.Add(label,0,0);
        Control editor;
        if(option.Value is bool){var check=new GtcSettingsCheck{Checked=(bool)option.Value,Size=new Size(P(28),P(28)),BackColor=GtcTheme.Surface,Cursor=Cursors.Hand};check.CheckedChanged+=(s,e)=>Set(option,check.Checked);editor=check;}
        else if(option.Key=="chief_name"||option.Key=="spotter_name"||option.Key=="PERSONALISATION_NAME"){
            var combo=MakeChoice();combo.Width=P(220);string soundRoot=CrewVoices.SoundRoot(executable(),session.Options);var installed=CrewVoices.Installed(soundRoot,option.Key);foreach(string voice in installed)combo.Items.Add(new Choice{Id=voice,Caption=voice});var selected=combo.Items.Cast<Choice>().FirstOrDefault(x=>x.Id.Equals(Convert.ToString(option.Value),StringComparison.OrdinalIgnoreCase));if(installed.Count==0){selected=new Choice{Id=Convert.ToString(option.Value),Caption="Sem vozes instaladas"};combo.Items.Add(selected);combo.Enabled=false;}combo.SelectedItem=selected;combo.SelectedIndexChanged+=(s,e)=>{if(combo.SelectedItem is Choice)Set(option,((Choice)combo.SelectedItem).Id);};hints.SetToolTip(combo,installed.Count==0?"Usa Instalar vozes para obter os pacotes oficiais.":"Só são apresentadas vozes instaladas.");editor=combo;
        }
        else if(option.Key=="messages_volume"||option.Key=="background_volume"){
            var panel=new TableLayoutPanel{ColumnCount=2,RowCount=1,Width=P(220),Height=P(32),Margin=Padding.Empty};panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,P(48)));decimal value;decimal.TryParse(Convert.ToString(option.Value),NumberStyles.Float,CultureInfo.InvariantCulture,out value);
            var percent=Label(((int)(value*100))+"%");percent.MaximumSize=Size.Empty;percent.Dock=DockStyle.Fill;percent.TextAlign=ContentAlignment.MiddleRight;var slider=new GtcSettingsSlider{Minimum=0,Maximum=100,Value=(int)(value*100),Dock=DockStyle.Fill,BackColor=GtcTheme.Surface,AccessibleName=Title(option.Key)};panel.Controls.Add(slider,0,0);panel.Controls.Add(percent,1,0);slider.ValueChanged+=(s,e)=>{percent.Text=slider.Value+"%";Set(option,(slider.Value/100m).ToString(CultureInfo.InvariantCulture));};editor=panel;
        }else if(option.Key=="VOICE_OPTION"){
            var combo=MakeChoice();combo.Width=P(220);string[] values={"DISABLED","HOLD","TOGGLE","ALWAYS_ON","TRIGGER_WORD","NOT_PRESSED"},captions={"Desativado","Manter botão premido","Alternar com botão","Sempre a ouvir","Palavra de ativação","Ouvir quando o botão está solto"};for(int i=0;i<values.Length;i++)combo.Items.Add(new Choice{Id=values[i],Caption=captions[i]});var current=combo.Items.Cast<Choice>().FirstOrDefault(x=>x.Id.Equals(Convert.ToString(option.Value),StringComparison.OrdinalIgnoreCase));if(current==null){current=new Choice{Id=Convert.ToString(option.Value),Caption=Convert.ToString(option.Value)};combo.Items.Add(current);}combo.SelectedItem=current;combo.SelectedIndexChanged+=(s,e)=>Set(option,((Choice)combo.SelectedItem).Id);editor=combo;
        }else if(option.Key.StartsWith("NAUDIO_")){
            var combo=MakeChoice();combo.Width=P(220);combo.Items.Add(new Choice{Id="",Caption="Dispositivo predefinido do Windows"});foreach(var device in AudioDevices(option.Key.Contains("RECORDING")))combo.Items.Add(device);string selected=Convert.ToString(option.Value);var match=combo.Items.Cast<Choice>().FirstOrDefault(x=>x.Id.Equals(selected,StringComparison.OrdinalIgnoreCase));if(match==null){match=new Choice{Id=selected,Caption="Dispositivo guardado"};combo.Items.Add(match);}combo.SelectedItem=match;combo.SelectedIndexChanged+=(s,e)=>Set(option,((Choice)combo.SelectedItem).Id);editor=combo;
        }else if(option.Value is int||option.Value is long||option.Value is decimal||option.Value is double||option.Value is float){
            decimal value=Convert.ToDecimal(option.Value,CultureInfo.InvariantCulture);var number=new NumericUpDown{Minimum=-1000000000m,Maximum=1000000000m,DecimalPlaces=option.Value is int||option.Value is long?0:4,Increment=option.Key=="opacity"?.05m:1m,Value=Math.Max(-1000000000m,Math.Min(1000000000m,value)),Width=P(160),BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Font=F(12),BorderStyle=BorderStyle.FixedSingle};
            if(option.Key=="opacity"){number.Minimum=0;number.Maximum=1;}number.ValueChanged+=(s,e)=>Set(option,number.DecimalPlaces==0?(object)(long)number.Value:(object)number.Value);editor=number;
        }else{
            bool complex=option.Value is IDictionary||option.Value is IList;var text=new TextBox{Text=complex?CompanionStore.Json.Serialize(option.Value):Convert.ToString(option.Value),Width=P(220),BackColor=GtcTheme.Input,ForeColor=GtcTheme.Text,Font=F(12),BorderStyle=BorderStyle.FixedSingle,Multiline=complex,Height=P(complex?65:28)};
            text.TextChanged+=(s,e)=>{try{object value=complex?CompanionStore.Json.DeserializeObject(text.Text):(object)text.Text;if(complex&&!(value is IDictionary)&&!(value is IList))throw new FormatException();if(option.Key=="messages_volume"||option.Key=="background_volume"){decimal volume;if(!decimal.TryParse(text.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out volume)&&!decimal.TryParse(text.Text,NumberStyles.Float,CultureInfo.CurrentCulture,out volume))throw new FormatException();if(volume<0||volume>1)throw new FormatException();value=volume.ToString(CultureInfo.InvariantCulture);}invalid.Remove(option);Set(option,value);text.ForeColor=GtcTheme.Text;}catch{invalid.Add(option);status.Text="O valor não é válido. Mantém o formato original.";text.ForeColor=Color.FromArgb(255,112,126);save.Enabled=false;}};editor=text;
        }
        editor.AccessibleName=Title(option.Key);editor.Width=Math.Min(editor.Width,P(220));editor.Anchor=AnchorStyles.Left;editor.Margin=new Padding(0,P(3),0,P(3));card.Controls.Add(editor,0,1);hints.SetToolTip(editor,option.Hint??option.Key);
    }
    void Set(EngineSetting option,object value){option.Value=value;changed();UpdateStatus();}
    static IEnumerable<Choice> AudioDevices(bool input){
        var result=new List<Choice>();try{using(var root=Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\"+(input?"Capture":"Render"))){if(root!=null)foreach(string name in root.GetSubKeyNames())using(var device=root.OpenSubKey(name))using(var properties=device.OpenSubKey("Properties")){if(Convert.ToInt32(device.GetValue("DeviceState",0))!=1||properties==null)continue;string caption=Convert.ToString(properties.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},14",""));if(caption.Length>0)result.Add(new Choice{Id=(input?"{0.0.1.00000000}.":"{0.0.0.00000000}.")+name,Caption=caption});}}}catch{}return result;
    }
    protected override void Dispose(bool disposing){if(disposing){if(voiceDownload!=null)voiceDownload.Cancel();searchDelay.Dispose();stateTimer.Dispose();hints.Dispose();}base.Dispose(disposing);}
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr window,uint message,IntPtr value,string text);
}

public sealed partial class CompanionWindow {
    readonly System.Windows.Forms.Timer engineSettingsTimer=new System.Windows.Forms.Timer{Interval=5000};
    void CreateEnginePage(Panel page,string id,Func<string> path,CheckBox launchOption){foreach(Control control in page.Controls.Cast<Control>().ToArray())control.Dispose();page.AutoScroll=false;page.Controls.Add(new NativeEnginePage(id,path,StopCountdown,scale,null,()=>ShowTelemetrySetup(game.Text.Trim().Trim('"')),launchOption));}
    void StartEngineSettings(){Shown+=(s,e)=>{if(!PreviewOnly){engineSettingsTimer.Tick+=(sender,args)=>{try{BundledEngines.Prepare();EngineSettingsQueue.ApplyReady();}catch(Exception ex){CompanionStore.Log("Preferências dos motores: "+ex.Message);}};engineSettingsTimer.Start();}};FormClosed+=(s,e)=>engineSettingsTimer.Dispose();}
}
