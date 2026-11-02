# Licenciamento e recursos de terceiros

O código original do GTC Companion e do overlay GTC Timing é disponibilizado sob a GNU General Public License, versão 3 (SPDX: `GPL-3.0-only`). Copyright (C) 2026 GTC Companion contributors. O texto integral está em `LICENSE`.

## Fontes Ubuntu

As fontes Ubuntu Regular, Medium e Bold mantêm a Ubuntu Font Licence 1.0 e os seus avisos originais. A licença está em `FONT-LICENCE.txt` no pacote da aplicação e em `assets/fonts/FONT-LICENCE.txt` no código-fonte. Estas fontes não são relicenciadas sob GPLv3.

## Identidade GTC Race Center

O nome, os logótipos e os restantes elementos da identidade GTC Race Center pertencem aos respetivos titulares. A licença do nosso código não concede direitos sobre essas marcas ou recursos gráficos. A aplicação é um projeto da comunidade, sem afiliação oficial declarada.

Fonte dos elementos gráficos: https://esports.gtcracecenter.com/

## Classificação e aplicações externas

Os dados da classificação são consultados na API da GTC e não fazem parte do código licenciado neste repositório. Não são incluídas classificações pessoais nem configurações de acesso nos pacotes de distribuição.

rFactor 2 e Steam são instalados separadamente e mantêm as suas próprias condições. A distribuição inclui TinyPedal 2.52.0 (GPL-3.0-or-later) e Crew Chief 4.19.5.8 (MIT). A biblioteca Python do TinyPedal contém alterações do Companion; os fontes modificados constam de `SOURCE.zip/assets/tinypedal-live`. Mantêm-se as licenças e avisos dos projetos e das suas dependências. Os pacotes de voz do Crew Chief não são incluídos. A licença MIT do código do Crew Chief não altera as condições próprias dos seus sons ou de componentes de terceiros.

As páginas nativas TinyPedal e Crew Chief utilizam os formatos de configuração destes projetos. O esquema de referência TinyPedal foi derivado das definições oficiais da versão 2.52.0, Copyright (C) 2022-2026 TinyPedal developers, sob GPL-3.0-or-later. A cópia editável do esquema está em `assets/engines/tiny-defaults.json`, incluída no código-fonte correspondente. A licença está em `TINYPEDAL-LICENSE.txt` no pacote e em `assets/engines/TINYPEDAL-LICENSE.txt` no código-fonte.

O esquema de referência do Crew Chief foi derivado de `CrewChiefV4/Properties/Settings.settings`, Copyright (c) 2019-2024 Britton IT Ltd, sob MIT. Está em `assets/engines/crew-defaults.json`. O aviso de copyright e a licença MIT são preservados em `CREWCHIEF-LICENSE.txt` no pacote e em `assets/engines/CREWCHIEF-LICENSE.txt` no código-fonte. As revisões e ligações dos dois projetos estão em `assets/engines/PROVENANCE.json`.

O leitor de telemetria implementa em C# o formato de memória partilhada rFactor2SharedMemoryMapPlugin 3.7. As definições de campos consultadas estão identificadas no código (`TonyWhitley/pyRfactor2SharedMemory`, `rF2data.py`). O pacote inclui o plugin 3.7.15.1, sem alterações, com licença GPLv3, README original e proveniência em `components/rF2SharedMemoryMap`. As fontes correspondentes da revisão `48aa12dbb68849923870acd8e68044c46c3d83eb` acompanham o pacote em `third-party-source/rF2SharedMemoryMap-3.7.15.1-source.zip`. A instalação é opcional e preserva os plugins e as preferências existentes. Projeto oficial: https://github.com/TheIronWolfModding/rF2SharedMemoryMapPlugin

## Fontes e bibliotecas dos motores

`third-party-source/README.md` identifica as versões e as fontes correspondentes. O ZIP TinyPedal inclui o código e os submódulos nas revisões fixadas. Qt, PySide2 e shiboken mantêm LGPLv3 e as bibliotecas partilhadas originais substituíveis; não é imposto qualquer bloqueio à sua substituição nem à depuração de versões modificadas. Os avisos PSF, BSD, MIT e GPL dos restantes componentes permanecem nas pastas originais. O código e os scripts do Companion estão em `SOURCE.zip`. Os restantes plugins distribuídos nos diretórios dos motores mantêm a instalação própria dos respetivos projetos.
