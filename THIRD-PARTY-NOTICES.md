# Licensing and third-party resources

The original GTC Companion and GTC Timing overlay code is made available under the GNU General Public License, version 3 (SPDX: `GPL-3.0-only`). Copyright (C) 2026 GTC Companion contributors. The full text is in `LICENSE`.

## Ubuntu Fonts

Ubuntu Regular, Medium and Bold fonts retain the Ubuntu Font License 1.0 and their original notices. The license is at `FONT-LICENCE.txt` in the application package and at `assets/fonts/FONT-LICENCE.txt` in the source code. These fonts are not relicensed under GPLv3.

## GTC Race Center Identity

The name, logos and other elements of the GTC Race Center identity belong to their respective owners. Our code license does not grant rights to these trademarks or graphic resources. The application is a community project, with no official affiliation declared.

Source of graphic elements: https://esports.gtcracecenter.com/

## Classification and external applications

Rating data is queried from the GTC API and is not part of the code licensed in this repository. No personal ratings or access settings are included in distribution packages.

rFactor 2 and Steam are installed separately and maintain their own conditions. The distribution includes TinyPedal 2.52.0 (GPL-3.0-or-later) and Crew Chief 4.19.5.8 (MIT). TinyPedal's Python library contains Companion changes; the modified sources are listed in `SOURCE.zip/assets/tinypedal-live`. Licenses and notices for projects and their dependencies are maintained. Crew Chief voice packs are not included. The MIT license of Crew Chief's code does not change the terms of its own sounds or third-party components.

The native TinyPedal and Crew Chief pages use the configuration formats from these projects. The TinyPedal reference schema was derived from the official definitions of version 2.52.0, Copyright (C) 2022-2026 TinyPedal developers, under GPL-3.0-or-later. The editable copy of the schema is in `assets/engines/tiny-defaults.json`, included in the corresponding source code. The license is at `TINYPEDAL-LICENSE.txt` in the package and at `assets/engines/TINYPEDAL-LICENSE.txt` in the source code.

The Crew Chief reference schema was derived from `CrewChiefV4/Properties/Settings.settings`, Copyright (c) 2019-2024 Britton IT Ltd, under MIT. It is in `assets/engines/crew-defaults.json`. The copyright notice and MIT License are preserved in `CREWCHIEF-LICENSE.txt` in the package and in `assets/engines/CREWCHIEF-LICENSE.txt` in the source code. Revisions and links for the two projects are in `assets/engines/PROVENANCE.json`.

The telemetry reader implements the rFactor2SharedMemoryMapPlugin 3.7 shared memory format in C#. The field definitions consulted are identified in the code (`TonyWhitley/pyRfactor2SharedMemory`, `rF2data.py`). The package includes plugin 3.7.15.1, without changes, with GPLv3 license, original README and provenance in `components/rF2SharedMemoryMap`. Corresponding sources from revision `48aa12dbb68849923870acd8e68044c46c3d83eb` are included in the package in `third-party-source/rF2SharedMemoryMap-3.7.15.1-source.zip`. Installation is optional and preserves existing plugins and preferences. Official project: https://github.com/TheIronWolfModding/rF2SharedMemoryMapPlugin

## Engine sources and libraries

`third-party-source/README.md` identifies the corresponding versions and sources. The TinyPedal ZIP includes the code and submodules in pinned revisions. Qt, PySide2 and shiboken maintain LGPLv3 and the original replaceable shared libraries; no block is imposed on its replacement or debugging of modified versions. The PSF, BSD, MIT and GPL warnings for the remaining components remain in the original folders. Companion code and scripts are in `SOURCE.zip`. The remaining plugins distributed in the engine directories maintain the installation of their respective projects.
