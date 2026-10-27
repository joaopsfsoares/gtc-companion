// Copyright (C) 2026 GTC Companion contributors.
// This file is part of GTC Companion.
// GTC Companion is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License, version 3, as published
// by the Free Software Foundation.
// GTC Companion is distributed without any warranty. See LICENSE for details.
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

public sealed partial class TimingOverlay : Form {
    static readonly string Root = Environment.GetEnvironmentVariable("GTC_TIMING_DATA") ?? AppDomain.CurrentDomain.BaseDirectory;
    static readonly string PreferencesRoot=Environment.GetEnvironmentVariable("GTC_TIMING_PREFERENCES")??Root;
    static string DataFile(string name){return Path.Combine(name=="settings.json"?PreferencesRoot:Root,name);}
    static readonly string Endpoint = "https://api.gtcracecenter.com/livetiming/configs/"+(Environment.GetEnvironmentVariable("GTC_TIMING_ID")??"110")+"/leaderboard/snapshot";
    readonly JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = 4000000 };
    readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
    readonly System.Windows.Forms.Timer poll = new System.Windows.Forms.Timer { Interval = 10000 };
    readonly System.Windows.Forms.Timer lifecycle = new System.Windows.Forms.Timer { Interval = 1000 };
    readonly System.Windows.Forms.Timer liveTimer = new System.Windows.Forms.Timer { Interval = 100 };
    readonly Rf2ScoringReader scoring = new Rf2ScoringReader();
    readonly LiveLapModel lapModel;
    readonly DriverIdeals driverIdeals;
    readonly PersonalRecordRepository recordRepository;
    DateTime nextRecordSync=DateTime.MinValue;
    int storedRecordRevision=-1;
    DateTime nextRemoteIdeal=DateTime.MinValue;
    string remoteIdealError="";
    int PanelWidth{get{return settings.ShowDriverIdealLaps?792:696;}}
    readonly SessionPositions positions;
    RankingHistory rankingHistory;
    static readonly string HistoryFile="ranking-history-"+(Environment.GetEnvironmentVariable("GTC_TIMING_ID")??"110")+".json";
    string gameRun="";
    bool positionsDirty;
    DateTime positionsSaved=DateTime.MinValue;
    LiveLapView live = new LiveLapView();
    readonly DateTime started = DateTime.UtcNow;
    readonly float scale;
    readonly bool standalone;
    readonly NotifyIcon tray = new NotifyIcon();
    Preferences settings;
    TimingSnapshot snapshot;
    List<TimingRow> rows = new List<TimingRow>();
    DateTime updatedUtc = DateTime.MinValue, missingGameSince = DateTime.MinValue;
    bool busy, locked = true, userHidden, failed, dragging, sawGame,positionReady,finishPressed;
    Point dragOrigin, windowOrigin;
    int offset;
    Form settingsDialog;
    string error = "";
    const int ExTransparent = 0x20, ExToolWindow = 0x80, ExNoActivate = 0x08000000;
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h, int index);
    [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr h, int index, int style);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int height, uint flags);
    protected override bool ShowWithoutActivation { get { return true; } }
    protected override CreateParams CreateParams {
        get { var p = base.CreateParams; p.ExStyle |= ExToolWindow | ExNoActivate | (locked ? ExTransparent : 0); return p; }
    }
    public TimingOverlay(bool independent) {
        standalone = independent;
        settings = Read<Preferences>("settings.json") ?? new Preferences();
        lapModel=new LiveLapModel(Read<PersonalRecordBook>("personal-records.json"));recordRepository=new PersonalRecordRepository(Environment.GetEnvironmentVariable("GTC_COMPANION_DATA")??Root,Environment.GetEnvironmentVariable("GTC_COMPANION_DATA")!=null);SynchronizeRecords(true);driverIdeals=new DriverIdeals(lapModel.Book,Read<GtcRecordCache>("gtc-records.json"));
        positions=Read<SessionPositions>("positions-session.json")??new SessionPositions();
        rankingHistory=Read<RankingHistory>(HistoryFile)??new RankingHistory();
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GTC_TIMING_DRIVER"))) settings.Player = Environment.GetEnvironmentVariable("GTC_TIMING_DRIVER");
        settings.RefreshSeconds=Math.Max(5,Math.Min(60,settings.RefreshSeconds));
        settings.OpacityPercent=Math.Max(10,Math.Min(100,settings.OpacityPercent));
        settings.PositionAnchor=Math.Max(0,Math.Min(9,settings.PositionAnchor));
        settings.PilotsAbove=Math.Max(0,Math.Min(10,settings.PilotsAbove));
        settings.PilotsBelow=Math.Max(0,Math.Min(10,settings.PilotsBelow));
        settings.Shortcuts=OverlayShortcuts.Normalize(settings.Shortcuts);
        settings.PositionReference=Math.Max(0,Math.Min(2,settings.PositionReference));
        if(settings.PositionReferenceHours<1)settings.PositionReferenceHours=24;
        settings.PositionReferenceHours=Math.Min(168,settings.PositionReferenceHours);
        using (var g = Graphics.FromHwnd(IntPtr.Zero)) scale = Math.Max(1, g.DpiX / 96f);
        Text = "GTC Timing";Icon=GtcBrand.Icon();
        FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
        StartPosition = FormStartPosition.Manual; AutoScaleMode = AutoScaleMode.None;
        BackColor = GtcTheme.Background; Opacity = settings.OpacityPercent/100d; DoubleBuffered = true;
        poll.Interval=settings.RefreshSeconds*1000;
        LoadCache();driverIdeals.ObserveLeaderboard(snapshot); ResizeOverlay();
        Rectangle area = Screen.PrimaryScreen.WorkingArea;
        Point preferred = (settings.X!=-1 || settings.Y!=-1)
            ? new Point(settings.X, settings.Y) : new Point(area.Right - Width - (int)(24 * scale), area.Top + (int)(80 * scale));
        settings.X=preferred.X;settings.Y=preferred.Y;positionReady=true;
        ApplyPosition();
        var menu = new ContextMenuStrip();menu.Font=GtcTypography.Create(13*scale);
        shortcutMenus[3]=(ToolStripMenuItem)menu.Items.Add("Definições…", null, (s,e) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        shortcutMenus[0]=(ToolStripMenuItem)menu.Items.Add("Alternar vista compacta / completa", null, (s,e) => ToggleView());
        shortcutMenus[1]=(ToolStripMenuItem)menu.Items.Add("Mover / bloquear overlay", null, (s,e) => ToggleLock());
        shortcutMenus[2]=(ToolStripMenuItem)menu.Items.Add("Mostrar / ocultar", null, (s,e) => ToggleHidden());
        menu.Items.Add("Atualizar agora", null, async (s,e) => await RefreshData());
        menu.Items.Add("Repor posição", null, (s,e) => { settings.PositionAnchor=3;ApplyPosition();SaveSettings(); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Fechar overlay", null, (s,e) => Close());
        tray.Icon = GtcBrand.Icon();UpdateShortcutLabels();
        tray.ContextMenuStrip = menu; tray.Visible = true; tray.DoubleClick += (s,e) => ShowSettings();
        poll.Tick += async (s,e) => await RefreshData();
        lifecycle.Tick += (s,e) => TickLifecycle();
        liveTimer.Tick += (s,e) => {ApplyCommands();UpdateLive();};
        Shown += async (s,e) => { RegisterKeys(); poll.Start(); lifecycle.Start(); liveTimer.Start(); await RefreshData(); };
        FormClosing+=(s,e)=>{if(dragging)FinishMove();};
        FormClosed += (s,e) => { poll.Stop(); lifecycle.Stop(); liveTimer.Stop(); scoring.Dispose();SynchronizeRecords(true);recordRepository.Dispose(); if(settingsDialog!=null)settingsDialog.Close(); SaveSettings(); Write("positions-session.json",positions); if(shortcutRegistration!=null)shortcutRegistration.Dispose();tray.Dispose(); http.Dispose(); };
    }
    T Read<T>(string name) where T : class {
        try { return json.Deserialize<T>(File.ReadAllText(DataFile(name))); } catch { return null; }
    }
    void Write(string name, object value) {
        try { var file = DataFile(name); var temp = file + ".tmp"; File.WriteAllText(temp,json.Serialize(value),new UTF8Encoding(false)); if(File.Exists(file)) File.Replace(temp,file,null); else File.Move(temp,file); } catch { }
    }
    void LoadCache() {
        var cache = new[]{Read<CachedSnapshot>("cache.json"),Read<CachedSnapshot>("leaderboard-cache.json")}.Where(c=>c!=null&&c.Data!=null&&c.Data.activeRound!=null&&c.Data.rows!=null).OrderByDescending(c=>c.UpdatedUtc).FirstOrDefault();
        if (cache == null || cache.Data == null) return;
        snapshot = cache.Data; rows = TimingData.Sorted(snapshot); updatedUtc = cache.UpdatedUtc;
    }
    void SaveSettings() { Write("settings.json",settings); }
    void SynchronizeRecords(bool force=false){if(!force&&DateTime.UtcNow<nextRecordSync&&lapModel.Book.Revision==storedRecordRevision)return;nextRecordSync=DateTime.UtcNow.AddSeconds(1);if(recordRepository.Synchronize(lapModel.Book))storedRecordRevision=lapModel.Book.Revision;}
    async Task RefreshData() {
        if (busy || IsDisposed) return;
        busy = true;
        try {
            using (var response = await http.GetAsync(Endpoint)) {
                response.EnsureSuccessStatusCode();
                var next = json.Deserialize<TimingSnapshot>(await response.Content.ReadAsStringAsync());
                if (next == null || next.activeRound == null || string.IsNullOrWhiteSpace(next.activeRound.trackName) || next.rows == null)
                    throw new InvalidDataException("Resposta sem classificação");
                if(snapshot == null || snapshot.activeRound == null || snapshot.activeRound.id != next.activeRound.id) offset = 0;
                int recordRevision=lapModel.Book.Revision;driverIdeals.ObserveLeaderboard(next);Write("gtc-records.json",driverIdeals.Online);if(lapModel.Book.Revision!=recordRevision)SynchronizeRecords(true);
                snapshot = next; rows = TimingData.Sorted(next); updatedUtc = DateTime.UtcNow; failed = false; error = "";
                if(positions.UpdateRanks(rows,next.activeRound.id))positionsDirty=true;
                rankingHistory=Read<RankingHistory>(HistoryFile)??rankingHistory;
                var collectors=Process.GetProcessesByName("GTC-Companion");bool managedCollector=Environment.GetEnvironmentVariable("GTC_TIMING_MANAGED")=="1"&&collectors.Length>0;foreach(var process in collectors)process.Dispose();
                if(!managedCollector&&rankingHistory.Record(snapshot,rows,updatedUtc))Write(HistoryFile,rankingHistory);
                Write("cache.json",new CachedSnapshot { Data = snapshot, UpdatedUtc = updatedUtc });
                await RefreshRemoteIdeals();
            }
        } catch (Exception ex) { failed = true; error = ex is TaskCanceledException ? "Ligação demorou demasiado" : "Sem ligação à tabela GTC"; }
        finally { busy = false; if(!IsDisposed) { ResizeOverlay(); Invalidate(); } }
    }
    async Task RefreshRemoteIdeals(){
        if(!(settings.ShowIdealLap||settings.ShowDriverIdealLaps||settings.ShowDriverActivity)||DateTime.UtcNow<nextRemoteIdeal)return;
        nextRemoteIdeal=DateTime.UtcNow.AddSeconds(15);
        try{
            string endpoint="https://api.gtcracecenter.com/livetiming/snapshot/"+(Environment.GetEnvironmentVariable("GTC_TIMING_ID")??"110");
            using(var response=await http.GetAsync(endpoint,HttpCompletionOption.ResponseHeadersRead)){
                response.EnsureSuccessStatusCode();if(response.Content.Headers.ContentLength>2000000)throw new InvalidDataException();
                using(var source=await response.Content.ReadAsStreamAsync())using(var memory=new MemoryStream()){
                    var buffer=new byte[16384];int count;using(var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(8))){while((count=await source.ReadAsync(buffer,0,buffer.Length,timeout.Token))>0){if(memory.Length+count>2000000)throw new InvalidDataException();memory.Write(buffer,0,count);}}
                    var remote=json.Deserialize<RemoteLapSnapshot>(Encoding.UTF8.GetString(memory.ToArray()));int revision=lapModel.Book.Revision;
                    driverIdeals.ObserveRemote(remote,snapshot.activeRound.trackName);Write("gtc-records.json",driverIdeals.Online);if(lapModel.Book.Revision!=revision)SynchronizeRecords(true);remoteIdealError="";
                }
            }
        }catch(Exception){remoteIdealError="Histórico remoto indisponível; mantidos os setores já observados.";}
    }
    protected override void WndProc(ref Message m) {
        if(m.Msg == 0x312) { int id = m.WParam.ToInt32(); if(id==701)ToggleView(); if(id==702)ToggleLock(); if(id==703)ToggleHidden(); if(id==704)ShowSettings(); return; }
        if(m.Msg == 0x21) { m.Result = new IntPtr(3); return; }
        base.WndProc(ref m);
    }
    void ToggleView() { settings.Compact = !settings.Compact; offset = 0; ResizeOverlay(); SaveSettings(); Invalidate(); }
    void ToggleHidden() { userHidden = !userHidden; if(userHidden)Hide(); else Show(); }
    void ToggleLock() {
        if(!locked&&dragging)FinishMove();
        locked = !locked;
        finishPressed=false;Capture=false;Cursor=locked?Cursors.Default:Cursors.SizeAll;
        int style = GetWindowLong(Handle,-20);
        SetWindowLong(Handle,-20,locked ? style|ExTransparent : style&~ExTransparent);
        if(userHidden){userHidden=false;Show();} Invalidate();
    }
    List<int> Indexes() { return TimingData.VisibleIndexes(rows,settings.Player,settings.Compact,offset,settings.PilotsAbove,settings.PilotsBelow); }
    LiveLapView PersonalIdeal(){return driverIdeals.Find(rows.FirstOrDefault(row=>TimingData.Normalize(row.driverName)==TimingData.Normalize(settings.Player)),snapshot==null||snapshot.activeRound==null?"":snapshot.activeRound.trackName)??lapModel.Ideal;}
    void UpdateLive() {
        SynchronizeRecords();
        var sample=scoring.Read();
        if(positions.ObserveSession(sample,gameRun))positionsDirty=true;
        var personal=rows.FirstOrDefault(r=>TimingData.Normalize(r.driverName)==TimingData.Normalize(settings.Player));
        string track=snapshot==null||snapshot.activeRound==null?"":snapshot.activeRound.trackName;
        int revision=lapModel.Book.Revision;
        driverIdeals.ObserveLocal(scoring.Activity,track,settings.Player,DateTime.UtcNow);
        live=lapModel.Update(sample,personal,track,settings.Player,false,settings.LapComparison==1,driverIdeals.PreferredRecord(personal,track));
        if(lapModel.Book.Revision!=revision)SynchronizeRecords(true);
        Invalidate();
    }
    void ResizeOverlay() {
        int count = Math.Max(1,Indexes().Count);
        var size = new Size((int)Math.Ceiling(PanelWidth*scale),(int)Math.Ceiling((34 + count*28 + 36+(settings.ShowPreviousLap?28:0)+(settings.ShowIdealLap?28:0))*scale));
        if(IsHandleCreated&&positionReady&&!dragging){var point=DisplayPosition(size);SetBounds(point.X,point.Y,size.Width,size.Height);}
        else Size=size;
    }
    Point DisplayPosition(Size size){
        var preferred=new Point(settings.X,settings.Y);var area=Screen.FromPoint(preferred).WorkingArea;
        var candidate=settings.PositionAnchor>0?TimingData.AnchoredPosition(area,size,settings.PositionAnchor,(int)(12*scale)):preferred;
        return TimingData.ConstrainPosition(candidate,size,area);
    }
    void ApplyPosition() {
        if(!positionReady||dragging)return;
        Location=DisplayPosition(Size);
    }
    void ClampPosition() {
        Rectangle area=Screen.FromPoint(Location).WorkingArea;
        Location=TimingData.ConstrainPosition(Location,Size,area);
    }
    Task JoinGtcServer(Button button,System.Windows.Forms.Label status) {
        button.Enabled=false;status.Visible=true;
        try {
            string companion=Environment.GetEnvironmentVariable("GTC_TIMING_LAUNCHER");
            if(string.IsNullOrEmpty(companion))companion=Path.GetFullPath(Path.Combine(Root,"..","..","GTC-Companion","GTC-Companion.exe"));
            if(!File.Exists(companion))throw new FileNotFoundException();
            Process.Start(new ProcessStartInfo(companion,"--launch"){UseShellExecute=true});
            status.Text="Pedido enviado ao GTC Companion. Acompanha a entrada na janela da aplicação.";
        }catch(Exception){if(!status.IsDisposed)status.Text="Abre o GTC Companion e configura o teu acesso ao servidor.";}
        finally{if(!button.IsDisposed)button.Enabled=true;}
        return Task.FromResult(0);
    }
    void ShowSettings() {
        string companion=Environment.GetEnvironmentVariable("GTC_TIMING_LAUNCHER");
        if(!string.IsNullOrEmpty(companion)&&File.Exists(companion)){Process.Start(new ProcessStartInfo(companion,"--overlay-settings"){UseShellExecute=true});return;}
        if(settingsDialog!=null&&!settingsDialog.IsDisposed){settingsDialog.Activate();return;}
        settingsDialog=CreateSettingsWindow();
        settingsDialog.Show();
    }
    void ApplyCommands(){
        string directory=Path.Combine(Root,"commands");if(!Directory.Exists(directory))return;
        foreach(string file in Directory.GetFiles(directory,"*.json").OrderBy(p=>p,StringComparer.Ordinal).Take(32)){
            string processing=file+".processing";
            try{
                File.Move(file,processing);var patch=json.Deserialize<OverlayPatch>(File.ReadAllText(processing));if(patch==null)continue;
                if(!ApplyShortcutRequest(patch))continue;
                patch.Apply(settings);poll.Interval=settings.RefreshSeconds*1000;Opacity=settings.OpacityPercent/100d;
                if(patch.Locked.HasValue&&locked!=patch.Locked.Value)ToggleLock();
                if(patch.Visible.HasValue&&(!userHidden)!=patch.Visible.Value)ToggleHidden();
                ResizeOverlay();ApplyPosition();SaveSettings();UpdateShortcutLabels();Invalidate();
                if(patch.Shortcuts!=null)ShortcutReply(patch,true,"Atalhos atualizados.");
            }catch(Exception){}finally{try{if(File.Exists(processing))File.Delete(processing);}catch{}}
        }
    }
    void TickLifecycle() {
        string closeRequest=Path.Combine(Root,"request-close.txt");
        try{if(File.Exists(closeRequest)){File.Delete(closeRequest);Close();return;}}catch{}
        string settingsRequest=Path.Combine(Root,"request-settings.txt");
        try{if(File.Exists(settingsRequest)){File.Delete(settingsRequest);ShowSettings();}}catch{}
        if (!standalone) {
            string modePath = Path.Combine(Root,"..","overlay-mode.txt");
            try { if(Environment.GetEnvironmentVariable("GTC_TIMING_MANAGED")!="1" && File.Exists(modePath) && File.ReadAllText(modePath).Trim() != "GTC") { Close(); return; } } catch { }
            var processes = Process.GetProcessesByName("rFactor2");
            DateTime firstStarted=DateTime.MaxValue;string instance="";
            foreach(var p in processes){try{if(p.StartTime<firstStarted){firstStarted=p.StartTime;instance=p.Id+"|"+p.StartTime.ToUniversalTime().Ticks;}}catch{}}
            if(!string.IsNullOrEmpty(instance))gameRun=instance;
            bool gameOpen = processes.Length > 0; foreach(var p in processes)p.Dispose();
            if(gameOpen){sawGame=true;missingGameSince=DateTime.MinValue;}
            else if(missingGameSince==DateTime.MinValue)missingGameSince=DateTime.UtcNow;
            if(!gameOpen && (sawGame || (DateTime.UtcNow-started).TotalSeconds>120) && (DateTime.UtcNow-missingGameSince).TotalSeconds>15){Close();return;}
        }
        if(Visible && (settingsDialog==null || !settingsDialog.Visible))SetWindowPos(Handle,new IntPtr(-1),0,0,0,0,0x13);
        Invalidate();
        if(positionsDirty || (DateTime.UtcNow-positionsSaved).TotalSeconds>=5){Write("positions-session.json",positions);positionsDirty=false;positionsSaved=DateTime.UtcNow;}
        Write("status.json",new { pid=Process.GetCurrentProcess().Id, track=snapshot==null?null:snapshot.activeRound.trackName, rows=rows.Count, lapsPublished=rows.Count(row=>row.lapsAccumulated.HasValue&&row.lapsAccumulated.Value>=0),
            playerPosition=TimingData.FindPlayer(rows,settings.Player)+1, updatedUtc=updatedUtc, stale=IsStale(), error=error,
            visible=Visible, locked=locked, compact=settings.Compact, hotkeys=shortcutRegistration==null?0:shortcutRegistration.Count,shortcutError=shortcutError,x=Left,y=Top,width=Width,height=Height,
            preferredX=settings.X,preferredY=settings.Y,positionAnchor=settings.PositionAnchor,opacityPercent=settings.OpacityPercent,
            pilotsAbove=settings.PilotsAbove,pilotsBelow=settings.PilotsBelow,
            showPositionChanges=settings.ShowPositionChanges,positionBaselineUtc=positions.BaselineUtc,
            remoteIdealError=remoteIdealError,showDriverIdealLaps=settings.ShowDriverIdealLaps,driverIdeals=rows.Select(row=>new{driver=row.driverName,ideal=driverIdeals.Find(row,snapshot==null||snapshot.activeRound==null?"":snapshot.activeRound.trackName)}).ToArray(),showIdealLap=settings.ShowIdealLap,idealLap=PersonalIdeal(),lapComparison=settings.LapComparison,showPreviousLap=settings.ShowPreviousLap,showDriverActivity=settings.ShowDriverActivity,previousLap=lapModel.LastCompleted,liveState=live.State,liveLap=live.LapNumber,liveElapsed=live.Elapsed,liveGap=live.Gap,liveValid=live.Valid,reference=live.Reference,comparisonPreviousLap=live.Previous,liveSectors=live.Sectors,liveDeltas=live.Deltas,sectorRecords=live.SectorRecords,lapRecord=live.LapRecord });
    }
    bool IsStale() { return failed || updatedUtc==DateTime.MinValue || (DateTime.UtcNow-updatedUtc).TotalSeconds>35; }
    protected override void OnMouseDown(MouseEventArgs e) {
        base.OnMouseDown(e); if(locked || e.Button!=MouseButtons.Left)return;
        if(ConcludeBounds().Contains(e.X/scale,e.Y/scale)){finishPressed=true;Capture=true;return;}
        dragging=true;dragOrigin=Cursor.Position;windowOrigin=Location;Capture=true;
    }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if(dragging){Location=new Point(windowOrigin.X+Cursor.Position.X-dragOrigin.X,windowOrigin.Y+Cursor.Position.Y-dragOrigin.Y);settings.PositionAnchor=0;settings.X=Left;settings.Y=Top;} else if(!locked)Cursor=ConcludeBounds().Contains(e.X/scale,e.Y/scale)?Cursors.Hand:Cursors.SizeAll; }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); if(finishPressed){finishPressed=false;Capture=false;if(ConcludeBounds().Contains(e.X/scale,e.Y/scale))ToggleLock();return;}if(dragging)FinishMove(); }
    protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(dragging&&!Capture)FinishMove();}
    void FinishMove(){dragging=false;Capture=false;settings.PositionAnchor=0;ClampPosition();settings.X=Left;settings.Y=Top;SaveSettings();}
    RectangleF ConcludeBounds(){return new RectangleF(PanelWidth-100,3,88,26);}
    protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); if(!locked&&!settings.Compact){offset=Math.Max(0,Math.Min(Math.Max(0,rows.Count-16),offset+(e.Delta<0?3:-3)));ResizeOverlay();Invalidate();} }
    static readonly Color Ink=GtcTheme.Text, Muted=GtcTheme.Muted, GtcOrange=GtcTheme.Accent, Purple=Color.FromArgb(193,133,255), Green=Color.FromArgb(90,228,165), Red=Color.FromArgb(255,112,126);
    static string Delta(double? value) { return !value.HasValue?"—":(value.Value>0?"+":"")+value.Value.ToString("0.000",CultureInfo.GetCultureInfo("pt-PT")); }
    static Color DeltaColor(double? value) { return !value.HasValue?Muted:value.Value < -0.0005?Green:value.Value > 0.0005?Red:Ink; }
    void Label(Graphics g,string text,float x,float y,float width,int size,Color color,bool bold=false,bool right=false,bool center=false) {
        using(var font=GtcTypography.Create(size,bold))
        using(var brush=new SolidBrush(color))
        using(var format=new StringFormat { Alignment=center?StringAlignment.Center:right?StringAlignment.Far:StringAlignment.Near,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap })
            g.DrawString(text,font,brush,new RectangleF(x,y,width,26),format);
    }
    public void PaintPanel(Graphics g) {
        g.SmoothingMode=SmoothingMode.AntiAlias; g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.Clear(BackColor); float height=Height/scale;
        float nameX=settings.ShowPositionChanges?90:52,nameWidth=224-nameX;
        if(locked){
            Label(g,"POS.",12,3,34,10,Muted);Label(g,"PILOTO",nameX,3,nameWidth,10,Muted);
            if(settings.ShowPositionChanges)Label(g,"Δ",52,3,30,10,Muted,false,true);
            Label(g,"VOLTAS",230,3,48,10,Muted,false,true);Label(g,"TEMPO",286,3,88,10,Muted,false,true);Label(g,"GAP",378,3,72,10,Muted,false,true);
            if(settings.ShowDriverIdealLaps)Label(g,"IDEAL",700,3,80,10,Muted,false,true);
            Label(g,"S1",458,3,70,10,Muted,false,true);Label(g,"S2",536,3,70,10,Muted,false,true);Label(g,"S3",614,3,70,10,Muted,false,true);
        }else{
            using(var fill=new SolidBrush(GtcTheme.AccentSurface))g.FillRectangle(fill,1,1,PanelWidth-2,30);
            Label(g,"AJUSTAR POSIÇÃO",12,3,160,11,GtcOrange,true);
            Label(g,MoveHint(),180,3,400,12,Ink);
            var button=ConcludeBounds();using(var fill=new SolidBrush(GtcOrange))g.FillRectangle(fill,button);
            Label(g,"Concluir",button.X,button.Y,button.Width,12,BackColor,true,false,true);
        }
        using(var pen=new Pen(GtcTheme.Border))g.DrawLine(pen,10,31,PanelWidth-10,31);
        var indexes=Indexes(); float y=34; int prev=-1;
        var historicalReference=rankingHistory.Reference(snapshot,settings.PositionReference,settings.PositionReferenceHours,DateTime.UtcNow);
        double best1=TimingData.BestSector(rows,r=>r.s1),best2=TimingData.BestSector(rows,r=>r.s2),best3=TimingData.BestSector(rows,r=>r.s3);
        foreach(int idx in indexes) {
            var row=rows[idx]; bool mine=TimingData.Normalize(row.driverName)==TimingData.Normalize(settings.Player);
            if(mine)using(var fill=new SolidBrush(GtcTheme.AccentSurface))g.FillRectangle(fill,6,y,PanelWidth-12,27);
            else if(idx%2==0)using(var fill=new SolidBrush(GtcTheme.AlternateRow))g.FillRectangle(fill,6,y,PanelWidth-12,27);
            if(prev>=0 && idx>prev+1)using(var pen=new Pen(GtcTheme.Muted)){pen.DashStyle=DashStyle.Dot;g.DrawLine(pen,10,y,PanelWidth-10,y);}
            var c=mine?GtcOrange:Ink;
            Label(g,(idx+1).ToString("00"),12,y,34,14,c,true);
            if(settings.ShowPositionChanges){
                int? change=settings.PositionReference==0?positions.Movement(row,idx+1):RankingHistory.Movement(historicalReference,row,idx+1);
                string movement=!change.HasValue?"—":change>0?"↑"+change:change<0?"↓"+Math.Abs(change.Value):"0";
                Label(g,movement,48,y,34,11,!change.HasValue||change==0?Muted:change>0?Green:Red,true,true);
            }
            float activitySpace=settings.ShowDriverActivity?11:0;
            if(settings.ShowDriverActivity){var presence=driverIdeals.Presence(row,snapshot==null||snapshot.activeRound==null?"":snapshot.activeRound.trackName,scoring.Activity,DateTime.UtcNow);if(presence!=DriverPresence.Unknown)using(var dot=new SolidBrush(presence==DriverPresence.OnTrack?Green:presence==DriverPresence.InPits?GtcOrange:Muted))g.FillEllipse(dot,nameX,y+10,6,6);}
            Label(g,row.driverName,nameX+activitySpace,y,nameWidth-activitySpace,14,c,mine);
            Label(g,TimingData.LapCount(row.lapsAccumulated),230,y,48,13,c,false,true);
            Label(g,TimingData.Lap(row.bestLapTime),286,y,88,14,c,true,true);
            Label(g,idx==0?"—":TimingData.Gap(row.bestLapTime-rows[0].bestLapTime),378,y,72,13,mine?GtcOrange:Muted,false,true);
            bool purple1=TimingData.IsBestSector(row.s1,best1),purple2=TimingData.IsBestSector(row.s2,best2),purple3=TimingData.IsBestSector(row.s3,best3);
            Label(g,TimingData.Sector(row.s1),458,y,70,13,purple1?Purple:c,purple1,true);
            Label(g,TimingData.Sector(row.s2),536,y,70,13,purple2?Purple:c,purple2,true);
            Label(g,TimingData.Sector(row.s3),614,y,70,13,purple3?Purple:c,purple3,true);
            if(settings.ShowDriverIdealLaps){var ideal=driverIdeals.Find(row,snapshot==null||snapshot.activeRound==null?"":snapshot.activeRound.trackName);Label(g,ideal==null||!ideal.Elapsed.HasValue?"—":TimingData.Lap(ideal.Elapsed.Value),700,y,80,13,Muted,false,true);}
            prev=idx;y+=28;
        }
        if(indexes.Count==0){Label(g,snapshot==null?"A carregar…":"Sem voltas registadas",12,y,666,14,Muted);y+=28;}
        y+=5;
        using(var pen=new Pen(GtcTheme.Border))g.DrawLine(pen,10,y-2,PanelWidth-10,y-2);
        PaintLapRow(g,live,"VOLTA ATUAL",nameX,nameWidth,y);
        if(settings.ShowPreviousLap)PaintLapRow(g,lapModel.LastCompleted??new LiveLapView(),"VOLTA ANTERIOR",nameX,nameWidth,y+28);
        if(settings.ShowIdealLap)PaintLapRow(g,PersonalIdeal()??new LiveLapView(),"VOLTA IDEAL",nameX,nameWidth,y+28+(settings.ShowPreviousLap?28:0),true);
        using(var pen=new Pen(GtcTheme.Border))g.DrawRectangle(pen,0,0,PanelWidth-1,height-1);
    }
    void PaintLapRow(Graphics g,LiveLapView value,string caption,float nameX,float nameWidth,float y,bool ideal=false){
        using(var fill=new SolidBrush(GtcTheme.LiveRow))g.FillRectangle(fill,6,y,PanelWidth-12,27);
        Label(g,"—",12,y,34,14,Muted);
        Label(g,caption,nameX,y,nameWidth,14,ideal?Purple:Ink,true);
        Label(g,value.LapNumber>0?value.LapNumber.ToString():"—",230,y,48,13,Ink,false,true);
        Label(g,value.Elapsed.HasValue?TimingData.Lap(value.Elapsed.Value):"—",286,y,88,14,value.LapRecord?Purple:value.Finished?DeltaColor(value.Gap):Ink,true,true);
        Label(g,Delta(value.Gap),378,y,72,13,value.LapRecord?Purple:DeltaColor(value.Gap),true,true);
        for(int i=0;i<3;i++){
            float x=458+i*78;Color color=value.Done[i]?(value.SectorRecords[i]?Purple:DeltaColor(value.Deltas[i])):Muted;
            Label(g,TimingData.Sector(value.Sectors[i]),x,y,70,13,color,value.Done[i],true);
        }
    }
    protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); e.Graphics.ScaleTransform(scale,scale); PaintPanel(e.Graphics); }
    public async Task RenderPreview(string destination,bool demo=false) {
        await RefreshData();
        if(demo)live=new LiveLapView{State="",Valid=true,Finished=true,LapNumber=12,Elapsed=83.2,Gap=-.2,Split=3,Sectors=new double?[]{18.1,33.1,32},Deltas=new double?[]{-.05,-.25,.1},Done=new bool[]{true,true,true},SectorRecords=new bool[]{false,true,false}};
        else UpdateLive();
        using(var bitmap=new Bitmap(Width,Height)) { using(var g=Graphics.FromImage(bitmap)){g.ScaleTransform(scale,scale);PaintPanel(g);} bitmap.Save(destination,System.Drawing.Imaging.ImageFormat.Png); }
    }
    [STAThread] public static int Main(string[] args) {
        ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
        SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        try {
            if(args.Length>1 && args[0]=="--self-test") { SelfTest(args[1]);return 0; }
            if(args.Length>1 && args[0]=="--render-settings") {
                using(var form=new TimingOverlay(true))form.RenderSettings(args[1],args.Length>2?int.Parse(args[2]):0);return 0;
            }
            if(args.Length>1 && args[0]=="--probe") {
                using(var reader=new Rf2ScoringReader()){var sample=reader.Read();File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(new{sample=sample,error=reader.Error}));return sample==null?1:0;}
            }
            if(args.Length>1 && (args[0]=="--render" || args[0]=="--render-full" || args[0]=="--render-lap-demo")) {
                using(var form=new TimingOverlay(true)) { if(args[0]=="--render-full"){form.settings.Compact=false;form.ResizeOverlay();} if(args.Contains("--edit"))form.ToggleLock(); SynchronizationContext.SetSynchronizationContext(null); form.RenderPreview(args[1],args[0]=="--render-lap-demo").GetAwaiter().GetResult(); } return 0;
            }
            Directory.CreateDirectory(Root);
            bool created; using(var mutex=new Mutex(true,"Local\\GTC-Timing",out created)) {
                if(args.Contains("--settings"))File.WriteAllText(Path.Combine(Root,"request-settings.txt"),"");
                if(!created)return 0;
                Application.Run(new TimingOverlay(args.Contains("--standalone")));
            }
            return 0;
        } catch(Exception ex) { try{File.AppendAllText(Path.Combine(Root,"error.log"),DateTime.Now+" "+ex+Environment.NewLine);}catch{} return 1; }
    }
    static void SelfTest(string report) {
        var sample=new TimingSnapshot { rows=new List<TimingRow> {
            new TimingRow { driverName="Pilóto Exemplo",bestLapTime=84.17755126953125 },
            new TimingRow { driverName="Leader",bestLapTime=80.6513671875 },
            new TimingRow { driverName="Invalid",bestLapTime=-1 },
            new TimingRow { driverName="NaN",bestLapTime=double.NaN } } };
        var decoded=new JavaScriptSerializer().Deserialize<TimingSnapshot>("{\"rows\":[{\"driverName\":\"Example\",\"bestLapTime\":80,\"lapsAccumulated\":58},{\"driverName\":\"Other\",\"bestLapTime\":81}]}");
        if(decoded.rows[0].lapsAccumulated!=58||decoded.rows[1].lapsAccumulated.HasValue||TimingData.LapCount(decoded.rows[0].lapsAccumulated)!="58"||TimingData.LapCount(null)!="—"||TimingData.LapCount(-1)!="—"||TimingData.LapCount(0)!="0")throw new Exception("Número de voltas publicadas e dados em falta");
        var list=TimingData.Sorted(sample);
        if(list.Count!=2 || TimingData.FindPlayer(list,"Piloto Exemplo")!=1)throw new Exception("Ordenação / nome acentuado");
        if(TimingData.Lap(list[1].bestLapTime)!="1:24,178" || TimingData.Lap(59.9999)!="1:00,000")throw new Exception("Arredondamento de tempo");
        if(TimingData.Gap(list[1].bestLapTime-list[0].bestLapTime)!="+3,526")throw new Exception("Diferença para líder");
        if(TimingData.Sector(null)!="—" || TimingData.Sector(-1)!="—" || TimingData.Sector(18.0816650390625)!="18,082")throw new Exception("Setores publicados e dados em falta");
        if(TimingData.AnchoredPosition(new Rectangle(0,0,1920,1080),new Size(640,124),3,12)!=new Point(1268,12)
            || TimingData.AnchoredPosition(new Rectangle(0,0,1920,1080),new Size(640,124),5,12)!=new Point(640,478)
            || TimingData.AnchoredPosition(new Rectangle(-1920,0,1920,1080),new Size(640,124),7,12)!=new Point(-1908,944))throw new Exception("Posições no ecrã");
        var many=Enumerable.Range(0,24).Select(i=>new TimingRow {driverName="Driver "+i,bestLapTime=80+i}).ToList();many[19].driverName="Piloto Exemplo";
        var compact=TimingData.VisibleIndexes(many,"Piloto Exemplo",true,0);
        if(!compact.Contains(18)||!compact.Contains(19)||!compact.Contains(20)||compact.Count!=3)throw new Exception("Jogador fora do top 5");
        if(!TimingData.VisibleIndexes(many,"Piloto Exemplo",true,0,3,1).SequenceEqual(new[]{16,17,18,19,20})
            ||!TimingData.VisibleIndexes(many,"Piloto Exemplo",true,0,0,0).SequenceEqual(new[]{19})
            ||TimingData.VisibleIndexes(many,"Driver 0",true,0,5,2).Count!=3)throw new Exception("Quantidade acima e abaixo na vista compacta");
        if(!TimingData.VisibleIndexes(many,"Piloto Exemplo",false,0).Contains(19))throw new Exception("Linha do jogador fixa na vista completa");
        if(TimingData.FindPlayer(many,"Outro")!=-1 || TimingData.VisibleIndexes(many,"Outro",false,20).Count!=4)throw new Exception("Jogador ausente / paginação");
        TestLiveLap();
        SessionFeaturesTests.Run();
        TestPositionChanges();
        TestManualPosition();
        RankingHistory.SelfTest();
        GtcTypography.Verify();
        File.WriteAllText(report,"OK: classificação; setores; posições; volta atual, parciais, invalidade e referência da melhor volta.",Encoding.UTF8);
    }
    static void TestManualPosition(){
        var area=new Rectangle(0,0,3840,2052);var compactSize=new Size(1440,392);var fullSize=new Size(1440,896);
        var prefs=new Preferences{X=2376,Y=1660,PositionAnchor=0};
        var preferred=new Point(prefs.X,prefs.Y);
        if(TimingData.ConstrainPosition(preferred,compactSize,area)!=preferred)throw new Exception("Posição manual compacta");
        var full=TimingData.ConstrainPosition(preferred,fullSize,area);
        if(full!=new Point(2376,1156))throw new Exception("Vista completa deve caber no ecrã");
        var saved=new JavaScriptSerializer().Deserialize<Preferences>(new JavaScriptSerializer().Serialize(prefs));
        if(TimingData.ConstrainPosition(new Point(saved.X,saved.Y),compactSize,area)!=preferred)throw new Exception("Compacta tem de recuperar a posição manual após guardar/reabrir");
        if(TimingData.ConstrainPosition(new Point(-1800,900),new Size(640,392),new Rectangle(-1920,0,1920,1080))!=new Point(-1800,688))throw new Exception("Posição em monitor à esquerda");
    }
    static void TestPositionChanges(){
        var tracker=new SessionPositions();
        var sample=new LiveSample{Track="Paul Ricard - 3A",Session=1,SessionTime=30};
        tracker.ObserveSession(sample,"game1");
        var a=new TimingRow{driverName="João A",vehicleClass="Cup"};var b=new TimingRow{driverName="B",vehicleClass="Cup"};var c=new TimingRow{driverName="C",vehicleClass="Cup"};
        tracker.UpdateRanks(new List<TimingRow>{a,b,c},"round1");
        tracker.UpdateRanks(new List<TimingRow>{c,a,b},"round1");
        if(tracker.Movement(c,1)!=2||tracker.Movement(a,2)!=-1||tracker.Movement(b,3)!=-1)throw new Exception("Ganho/perda desde a primeira classificação");
        if(tracker.Movement(new TimingRow{driverName="Joao A",vehicleClass="Cup"},2)!=-1||tracker.Movement(new TimingRow{driverName="Novo"},4).HasValue)throw new Exception("Identidade e piloto sem posição inicial");
        sample.SessionTime=50;if(tracker.ObserveSession(sample,"game1")||tracker.Movement(c,1)!=2)throw new Exception("Referência da sessão não se mantém");
        sample.SessionTime=1;if(!tracker.ObserveSession(sample,"game1")||tracker.Movement(c,1).HasValue)throw new Exception("Nova sessão reinicia referência");
        tracker.UpdateRanks(new List<TimingRow>{c,a,b},"round1");
        var saved=new JavaScriptSerializer().Deserialize<SessionPositions>(new JavaScriptSerializer().Serialize(tracker));
        if(saved.Movement(c,2)!=-1)throw new Exception("Persistência da referência de posições");
        if(!saved.ObserveSession(sample,"game2")||saved.InitialRanks!=null)throw new Exception("Reinício do jogo reinicia posições");
    }
    static void TestLiveLap(){
        var model=new LiveLapModel();
        var s=new LiveSample{Track="Paul Ricard - 3A",Driver="Piloto Exemplo",Car="Porsche Cup",Session=1,Slot=16,Laps=10,SessionTime=110,LapStart=100,Sector=1,CountLapFlag=2,Distance=500,Realtime=true,BestLap=90,BestS1=20,BestS12=50,LastLap=91,LastS1=20.1,LastS12=50.6};
        var pb=new TimingRow{driverName="Piloto Exemplo",bestLapTime=90,s1=20,s2=30,s3=40};
        Func<LiveLapView> update=()=>model.Update(s,pb,s.Track,s.Driver,true,true);
        var v=update();if(v.Elapsed!=10||v.Gap.HasValue)throw new Exception("Não comparar uma volta parcial com a volta inteira");
        s.SessionTime=120.05;s.Sector=2;s.CurrentS1=20.05;v=update();
        if(Math.Abs(v.Deltas[0].Value+.05)>.00001||v.SectorRecords[0])throw new Exception("Verde melhora a anterior sem bater recorde");
        s.SessionTime=120.2;s.Sector=2;s.CurrentS1=20.2;v=update();
        if(!v.Gap.HasValue||Math.Abs(v.Gap.Value-.1)>.00001||!v.Done[0]||v.Done[1]||v.SectorRecords[0])throw new Exception("Comparação S1 com a anterior");
        s.SessionTime=149.9;s.Sector=0;s.CurrentS12=49.9;v=update();
        if(Math.Abs(v.Sectors[1].Value-29.7)>.00001||Math.Abs(v.Deltas[1].Value+.8)>.00001||Math.Abs(v.Gap.Value+.7)>.00001||!v.SectorRecords[1])throw new Exception("S2 cumulativo, anterior e recorde pessoal");
        s.SessionTime=190;s.LapStart=190;s.Laps=11;s.Sector=1;s.CurrentS1=-1;s.CurrentS12=-1;s.LastS1=20.2;s.LastS12=49.9;s.LastLap=89.9;s.BestLap=89.9;s.BestS1=20.2;s.BestS12=49.9;v=update();
        if(!v.Finished||v.State!=""||Math.Abs(v.Gap.Value+1.1)>.00001||!v.LapRecord||Math.Abs(v.Reference.Time-90)>.0001||Math.Abs(v.Sectors[2].Value-40)>.00001)throw new Exception("Fim da volta bate recorde e preserva a referência GTC prioritária");
        s.SessionTime=194;v=update();if(v.Finished||Math.Abs(v.Reference.Time-90)>.00001||v.Gap.HasValue)throw new Exception("Recorde local não substitui a referência GTC");
        s.CountLapFlag=1;s.Sector=2;s.CurrentS1=20.1;s.SessionTime=210.1;v=update();if(v.Valid||v.Gap.HasValue||v.Deltas[0].HasValue)throw new Exception("Não comparar voltas inválidas");
        if(model.Update(s,pb,"Outra pista",s.Driver).Elapsed.HasValue)throw new Exception("Referência de pista errada");
        s.InPits=true;v=update();if(v.Elapsed.HasValue||v.State!="Nas boxes")throw new Exception("Tempo não corre nas boxes");
    }
}
