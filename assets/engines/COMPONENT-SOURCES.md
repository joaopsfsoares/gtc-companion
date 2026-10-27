# Fontes dos componentes incluídos

Os motores são distribuídos sem alterações aos binários. Os avisos originais permanecem em `components`. Este ficheiro indica onde obter o código correspondente das bibliotecas e os respetivos scripts de compilação. As bibliotecas partilhadas podem ser substituídas por versões modificadas compatíveis.

- TinyPedal **2.52.0**, GPL-3.0-or-later: `TinyPedal-2.52.0-source.zip`, neste diretório. Inclui o projeto e os submódulos pyACC, pyLMU e pyRfactor2SharedMemory nas revisões fixadas pelo projeto. Origem: https://github.com/TinyPedal/TinyPedal/tree/v2.52.0 . Revisão: `bf43bd5db0cd15f57672e5c75696ced0df020781`.
- Qt **5.15.2**, LGPLv3 e avisos de terceiros mantidos: código-fonte completo, incluindo o sistema de compilação, em https://download.qt.io/archive/qt/5.15/5.15.2/single/qt-everywhere-src-5.15.2.tar.xz . As DLL estão em `components/TinyPedal/lib/PySide2`.
- PySide2 / shiboken2 **5.15.2.1**, LGPLv3: https://download.qt.io/official_releases/QtForPython/pyside2/PySide2-5.15.2.1-src/pyside-setup-opensource-src-5.15.2.1.tar.gz . Documentação de compilação incluída no arquivo. Aviso em `components/TinyPedal/docs/licenses/LICENSE.PySide2.txt`.
- Python **3.8.10**, PSF: https://www.python.org/ftp/python/3.8.10/Python-3.8.10.tar.xz . Licença preservada em `LICENSE.Python3.txt` na mesma pasta de avisos.
- psutil **6.1.1**, BSD-3-Clause: https://github.com/giampaolo/psutil/tree/release-6.1.1 . Licença em `LICENSE.psutil.txt`.
- Crew Chief **4.19.5.8**, código MIT: https://gitlab.com/mr_belowski/CrewChiefV4 . A licença e o aviso originais estão em `CREWCHIEF-LICENSE.txt` e no diretório do motor. Os pacotes de voz são descarregados separadamente do serviço oficial; a licença do código não concede direitos adicionais sobre esses sons.
- rF2SharedMemoryMapPlugin, GPLv3, distribuído nos diretórios de plugins dos motores: https://github.com/TheIronWolfModding/rF2SharedMemoryMapPlugin . O código e as versões publicadas estão no projeto original; licença preservada em `components/TinyPedal/docs/licenses/LICENSE.rF2SharedMemoryMapPlugin.txt`. O Companion não instala esses plugins no jogo.

O código original do Companion, os esquemas editáveis, os recursos e os scripts de compilação estão em `SOURCE.zip` (GPL-3.0-only). `prepare-components.ps1` fixa as versões e verifica os SHA256 dos arquivos oficiais antes de preparar os motores. Estes URLs externos são indicados para acesso às fontes das bibliotecas originais, sem custo adicional; os restantes ficheiros-fonte referidos localmente acompanham este pacote.
