# GTC Companion

Windows application to prepare rFactor 2 sessions and monitor GTC Race Center standings.

Created by **João Soares** · GPLv3 · Windows 10/11

**[Download the application](https://github.com/joaopsfsoares/gtc-companion/releases)** — open the latest version and choose `GTC-Companion-<version>.zip`. `Source code` files are for development.

## Installation

1. Extract the complete ZIP to a writable folder and open **GTC-Companion.exe**.
2. In **Session**, confirm the driver name, rFactor 2 folder and automatic start.
3. In **Session → Installation → Install Components…**, install the Crew Chief telemetry plugin and audio packages.
4. On the **TinyPedal** and **Crew Chief** pages, choose whether you want to open each engine with the session. Engines are included in ZIP; They do not require separate installation or configuration of their paths.
5. The **Crew Chief → Install Voices** button opens the same wizard to verify or complete the installation.

Requirements: Steam, rFactor 2 and .NET Framework 4.8. The app does not require Python or Node installed. Cars and tracks must be installed in the game.

### rFactor 2 Telemetry

The ZIP includes the plugin **rF2 Shared Memory Map 3.7.15.1**, with its licence and source code. The wizard copies and activates the plugin automatically: no need to search for a DLL or open download pages. rFactor must be closed to change the plugin installation.

In the same wizard, **Crew Chief · voices, names and customizations** gets the official service packages. The installed versions are checked and only the necessary packages are downloaded. Completed downloads are cached; If you cancel or there is a failure, press **Install / Verify** again. Installing the audio files requires the Crew Chief engine to be stopped. The choice of voice and name remains in the Crew Chief settings and is not overridden by the installer.

When components are missing, normal opening of Companion displays this wizard before the session starts automatically. You can choose the components or close the wizard and configure the application. **Install components…** is always available in Session.

Installing the plugin preserves other preferences and saves copies of replaced files in `UserData/player/CompanionPluginBackups`. Invalid configuration or other potentially active shared memory plugin blocks the operation. No competing plugins are installed. After installing, open the game and enter the track to confirm the telemetry.

If the DLL does not appear in the game, check the Visual C++ 2013 Runtime in `Support/Runtimes`, according to the [official TinyPedal guide](https://github.com/TinyPedal/TinyPedal#setup-for-rfactor-2). The plugin origin, version, and hashes are in `assets/plugins/RF2-PROVENANCE.json`.

GTC standings work without this plugin. Live laps, car presence and widgets depend on compatible local telemetry.

## Championships and start

At the top of the window, choose a championship or use **+Add**. Each championship has its own name, page, GTC standings and server access. The **…** menu allows you to edit or delete the championship. The selection prepares the next start and preserves the current session.

On the **Server** page, open the GTC website, log in and copy the entry link provided for your competition. Paste it into **Input Link** and save. No servers or passwords are included in the package; each user configures their access.

In **Replays → Automatic replays**, you can activate GTC session monitoring. When another driver is on the track, the Companion tries to open rFactor as a spectator, as long as the game is closed. The comparison uses your best GTC lap from the same track and class; completed and valid laps observed after switching on are considered. Companion saves the entire session as a replay, with the driver's name and time, preserving the best recording of each driver, track and class. The file is archived after you close rFactor and the game finishes writing it. Keeps the Companion open; If you close it in the meantime, the pending scan will resume when you open it again. rFactor needs to have replay recording enabled and the server to allow spectators.

If the game is already open, monitoring waits so as not to interrupt the ongoing session. If it is not possible to identify a single replay of the session or confirm the driver and track, the files are kept and the app shows the reason. The specific lap is not cut from the full replay.

- **Enter the server** opens the selected applications and rFactor, waits for the menu and requests the connection to the configured server. Distinguishes sent request from confirmed connection.
- **Open without server** allows you to train without configuring a championship, driver or server access. Open the selected applications and the game; This mode does not open the GTC overlay.
- If the game or engines are already opened, they are reused. An existing connection is preserved. To change servers, first return to the game menu.
- Automatic start-up begins after four seconds and can be canceled or disabled in **Session**.

Uses borderless window mode for overlays to appear over the game. Closing the Companion does not close the game or engines.

## TinyPedal and Crew Chief

Companion's own pages edit original engine settings:

- **TinyPedal:** widgets, position, opacity, scale, colors, units and modules. The **Tires / brakes** section brings together the most used controls. Search, groups and pagination help you find the other options. On a new installation, the default profile includes map, radar, tire and brake wear and temperatures; Existing profiles are preserved.
- **Crew Chief:** profiles, installed names and voices, volumes, audio devices, microphone, voice recognition, spotter and messages.
- **Save changes:** in TinyPedal, valid changes are automatically saved and the active profile is updated with the engine open. Other profiles and global settings are pending until the engine closes. On the Crew Chief, changes are pending as long as the engine is open. Conflicts with external changes require reloading and review; When applying changes with the engines closed, copies are saved in `backups/engines`.

Includes TinyPedal **2.52.0** and Crew Chief **4.19.5.8**. TinyPedal's Python library contains an integration to apply changes to the active profile while the engine is open; the respective sources are in `assets/tinypedal-live`. Audio packages are downloaded by the installation wizard. Some tools continue to belong to the original applications.

When starting up in the Companion, the Crew Chief's main window remains in the background while the engine remains active. The loading window and the main window are handled separately; other engine windows remain available for functions that still belong to the original application.

## Sorting Overlay

The overlay displays positions, names, accumulated laps, differences and sectors of the GTC classification. The settings are in **Overlay**: appearance, classification, status and shortcuts.

- Complete and compact views, with configurable number of drivers above and below.
- Position and transparency saved between uses.
- Current lap, optional previous lap and optional ideal lap, with the same size as the riders' lines.
- Differential compared to **Personal record** (default) or **Previous lap**. The GTC classification reference takes priority over local times, even if a local record is faster. Without corresponding GTC reference, uses local record. The sectors belong to the same lap.
- Personal ideal calculated by the sum of the best valid sectors available in GTC. Missing sectors can use local records for support. The optional column **Observed ideal for each driver** follows the same rule and may not represent the entire history.
- Points of presence with priority for the explicit status of the GTC session: green on the track, orange in the pits and gray in the paddock. Remote data older than 30 s, missing flags or unambiguously unmatched fall back on fresh local telemetry; without a valid source, no point appears.
- Places gained/lost since the start of the session, in the last X hours or since yesterday at 00:00.

Green indicates improvement, red loss and purple records/best sectors. The ideal return is identified in purple. Unavailable data appears as **—**.

| Home shortcut | Action |
|---|---|
| Ctrl + Alt + S | Overlay settings |
| Ctrl + Alt + G | Toggle compact/full view |
| Ctrl + Alt + O | Move/lock |
| Ctrl + Alt + H | Show/hide |

Combinations can be changed in **Overlay → Shortcuts**. Changes apply to open overlay. Repeated or unavailable combinations are refused.

### Data and history

Position history is collected once per minute while the Companion or overlay is open and is retained for eight days, separated by championship, round and track. Data from before collection cannot be recovered. A comparison by period uses the last record prior to the chosen moment, with a tolerance of 15 minutes; without enough data shows **—**.

The ideals give priority to sectors with valid laps from the classification and the GTC session, stored in a cache separate from the local records. The overlay queries `/livetiming/snapshot/{id}` with an ideal view or active driver status at most once every 15 seconds. Driver analysis uses the same source when requested or with background collection active. Only the necessary sports data is saved. A failure of this source keeps the GTC data saved and allows local support. Tracks, classes, drivers, rounds and championships are not interchangeable.

The rating is also collected by Companion without requiring server access or open overlay. The two responses already used by the application remain the only GTC sources; new routes are not consulted. The saved data prevents new queries with each interaction. Preferences, engine installation, current lap telemetry and off-track estimates continue to use the necessary local sources.

Preferences and history are, by default, in `%LOCALAPPDATA%\GTC-Companion`. Each user maintains their own history. Do not share the data folder or input link. The app does not verify registrations; the organization and the server control access authorization.

## Pace training

In **Overlay → Stint**, choose the duration in **laps** or **minutes**, the **target pace** (`m:ss,mmm`) and the **consistency window** in seconds for each side of the target. The optional suggestion uses the best lap available, with priority given to GTC: target 0.5% above the record and window ±0.6% of the record, rounded to hundredths, with a minimum of 0.10 s. These are adjustable estimates; confirms reference, track and conditions. **Start stint** prepares the count for the next pass through the finish line; the exit lap is left out. The training works in singleplayer or on a server, without needing a GTC rating.

In **Overlay → Appearance**, the same transparency control, the Show and Move buttons and the view, visibility and movement shortcuts act on the standings and stint overlays. The position of each can be adjusted separately; When the movement ends, the chosen position is preserved.

In **How ​​to Set Up**, the calculator uses **best valid lap × (1 + margin / 100)**. The reference comes first from the GTC classification and, in its absence, from the available valid laps of the GTC session; only then use the game and local records. The origin appears next to the reference and can be edited. The 0.5–1.0% margin is an adjustable initial estimate, not a universal rule. Uses a representative reference and confirms the target with 10–15 laps on fuel, tire and race track conditions. The **Apply to next stint** button fills the target, without changing an ongoing stint. The help explains the setup, the initial walkthrough of the goal, and reading the result.

The training HUD and classification overlay have separate displays and **shared local records**. As long as the Companion is open, observed completed and valid laps feed into time and sector records, even without active stint or with the HUD hidden. Invalid, partial laps, in pits or with observation errors do not improve records. Existing records are imported without deleting previous files. The synchronization between processes preserves the improvements of both and keeps the record lap sectors and the best individual sectors separate.

The common record is in `records/personal-records.json` in the data folder, by track, class and driver. GTC's **online** classification continues to only show laps recorded by the server; a singleplayer lap can improve the local record, but is not sent to GTC.

During the stint, the HUD shows laps completed, laps/time remaining, average, differential to target, last lap, laps meeting window, estimated exits, and score from 0 to 100. Average and differential use all completed laps, including invalid ones. Green means within target ± tolerance; loaded green means below the fast limit and also counts as compliance with the window; red means above slow limit. The duration and pace settings are fixed during the stint.

The score adds up to **70 pace points** and **30 cleanliness points**. The pace receives 70 if the average is equal to or less than the target + tolerance, without penalizing faster times. Above this limit, you lose points linearly until zero when the additional difference reaches 5% of the target. Cleanliness loses 10 points per off-track per 10 laps, up to a maximum of 30 points. For example, 15 laps at pace with one exit gives 93/100; with three off-tracks, 80/100. The result is rounded to the nearest integer. It is a training assessment, not a GTC rating; Estimated off-tracks do not equate to official penalties. The score is provisional during training, partial on an objective interrupted or completed manually and estimated if there were telemetry failures. No score is shown without complete laps. The histories are evaluated with the same rules, without changing the saved times and off-tracks.

In minute training, the counter reaches zero and waits for the next pass at the finish line, counting only completed laps. A trip to the pits, change of car/track/session or skipping laps interrupts the stint, preserving the summary. A transient negative distance at the finish line does not end the training. A loss of driving status puts the collection on pause and allows you to resume in the same session, without announcing the objective as completed. In the absence of telemetry, progress is suspended. Laps with incomplete data are flagged. **Objective completed**, **Stint interrupted** and **Manually finished** have different indications.

The graph allows you to choose **Complete lap**, **Sector 1**, **Sector 2** or **Sector 3**. In the complete lap, the orange line represents the target and the dashed lines delimit the window: green identifies times within the target ± tolerance; solid green identifies times below the fast limit, also accepted; red identifies times above the slow threshold. In the sectors, the reference is the corresponding sector of the best valid lap of the stint: green means equal or faster and red means slower. The lap target and tolerance are not divided among the three sectors. The legend and note appear on the chart and in the PNG; gray ones with a cross are invalid or incomplete and the orange circle marks an estimated output. Hover over a point for details. **Save Chart** exports a PNG image. The selector allows you to review the last 30 stints; local records are in `training/stints` in the data folder.

The new training sessions save the sectors of complete laps with times confirmed by telemetry. Previous histories may not have sectors; missing fields appear as **—** and are not estimated. The main training average continues to use all completed laps, including invalid ones.

**Move HUD** allows you to drag it; double-clicking locks it again. The position and opacity are saved. Locked mode lets clicks through to the game.

### Drivers and stints

**Overlay → Stint → Drivers and stints** displays graphics of other drivers. Choose the **driver**, the **GTC session**, **all stints** or a specific stint and **Complete Lap / Sector 1 / Sector 2 / Sector 3**. GTC times and sectors prevail. Missing local laps are used as a supplement only when driver, track, class, lap number, weather and time permit identification of a single corresponding session. A cross indicates invalidity, incomplete data or possible invalidity; consult the point to distinguish. Lack of time in the GTC does not prove that a lap is invalid. In your stint graph, with no unambiguously associated GTC session, the local data keeps count, but is not displayed as a GTC graph. Available tire data appears discreetly on the graph; Zeros or missing fields are not shown as confirmed wear. **Show reference** displays the target and training window for the full lap; in the sectors, it presents the corresponding sector of the best clean lap of the selection. Selection does not change your stints or shared records.

- The **PIT** lines indicate entries into the observed pits. The observed output defines the start of the next stint. **~** marks indicate an approximate limit, obtained from a pitted lap or from an increase in the stop counter between two readings. With no exit observed, the passage remains open.
- The **clean average** uses only the laps available, valid, complete and without pits. **Include other laps** also shows invalid and incomplete pit laps, marked with a cross. These laps are not part of the clean average.
- The numbers on the axis are the actual laps of the session. Missing laps or sectors are left without points and **are not interpreted as stops**. Hover your mouse over the points and PIT marks to see the details. Click on a point or use the lap selector to see the total time, S1, S2, S3 and the differences by sector for the best clean lap of the selection. The white circle marks the selected lap. **Save graphic** exports PNG with the caption.

Local telemetry tracks the cars in the session as a source of support while the Companion is open, even without an active training stint. **Update GTC** consults only the session snapshot of the selected championship, with a minimum interval of 15 seconds. **Collect GTC in the background** allows you to continue the query with this window closed, while the Companion is open; the option is active in new configurations and can be turned off. Previously saved preferences are respected.

The snapshot can only contain a few drivers and part of their laps. The app shows available coverage and retains previous data when the source fails. It does not recover all past sessions nor invent missing times or stops. The sessions and championships are separate; opening the app again starts a new local observation window. Session records are located in `training/drivers` in the data folder on this PC and are not included in the distributed ZIP.

**track departures are estimated**, they are not official server infractions. An event is counted when the center of the car remains more than 1 m beyond the threshold reported by telemetry for 0.6 s. Another event is only counted after returning indoors for 0.6 s. Boxes and menus do not count. Tracking and telemetry failures may affect this estimate.

## Version and updates

The **About** page lets you select Portuguese or English and presents the installed version, credits, licenses and engine versions as links to official projects. **See updates** opens versions published on GitHub, including testing versions. Does not install updates automatically.

**Copy version information** copies application and engine numbers to help with diagnosis, without including personal settings or access.

Test versions still need validation on a PC without previous configurations. Current binaries do not have a digital signature. The `.sha256` file for each version allows you to verify the integrity of the ZIP.

## License and development

The original Companion and overlay code is under **GNU GPL version 3** (`GPL-3.0-only`). The full text is in [LICENSE](LICENSE). The program is provided without warranty. TinyPedal, Crew Chief and their dependencies maintain their respective licenses, described in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

The package includes `SOURCE.zip`, the engines in `components`, and the corresponding sources/notices in `third-party-source`. To compile on Windows with .NET Framework:

```powershell
powershell -File .\build.ps1 -Test
```

The script gets the components in the pinned versions when necessary. The optional digital signature is documented in [SIGNING.md](SIGNING.md). Publication rules without private data are in [SECURITY.md](SECURITY.md).

Community project, no official affiliation declared. The GTC identity belongs to its respective holders. Ubuntu Typography maintains the Ubuntu Font License 1.0. rFactor 2 and Steam are installed separately and maintain their own conditions.

The driver's own lap telemetry is saved locally as a CSV in the `telemetry\laps` subfolder of the Companion data folder (denoted by `local-data.txt`, where available) while the Companion and rFactor 2 are open. Includes raw and filtered position and commands; Pedal values ​​are fractions of 0 to 1, direction of -1 to 1. A recording started halfway through the lap is marked as partial. This personal data is not sent to the repository.

During spectate, the Companion identifies the camera car through the plugin's Graphics channel and records the laps observed in `telemetry\laps\observados`. The channel is temporarily activated if necessary; identification and recording stop when the camera changes or the data is no longer reliable.

The Replays tab allows you to compare two complete laps in CSV, with map and accelerator, brake and steering graphs. Normalized distance alignment is approximate; Incomplete laps are not shown as comparable. In Overlay it is possible to show or hide each overlay independently. Hold down Ctrl to see the configured shortcuts above the visible overlays.
