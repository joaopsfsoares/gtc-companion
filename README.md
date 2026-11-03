# GTC Companion

Aplicação Windows para preparar sessões de rFactor 2 e acompanhar as classificações da GTC Race Center.

Criado por **João Soares** · GPLv3 · Windows 10/11

**[Descarregar a aplicação](https://github.com/joaopsfsoares/gtc-companion/releases)** — abre a versão mais recente e escolhe `GTC-Companion-<versão>.zip`. Os ficheiros `Source code` destinam-se ao desenvolvimento.

## Instalação

1. Extrai o ZIP completo para uma pasta onde possas escrever e abre **GTC-Companion.exe**.
2. Em **Sessão**, confirma o nome do piloto, a pasta do rFactor 2 e o arranque automático.
3. Em **Sessão → Instalação → Instalar componentes…**, instala o plugin de telemetria e os pacotes de áudio do Crew Chief.
4. Nas páginas **TinyPedal** e **Crew Chief**, escolhe se queres abrir cada motor com a sessão. Os motores estão incluídos no ZIP; não precisam de instalação separada nem de configuração dos seus caminhos.
5. O botão **Crew Chief → Instalar vozes** abre o mesmo assistente para verificar ou completar a instalação.

Requisitos: Steam, rFactor 2 e .NET Framework 4.8. A app não exige Python ou Node instalados. Os carros e as pistas devem estar instalados no jogo.

### Telemetria do rFactor 2

O ZIP inclui o plugin **rF2 Shared Memory Map 3.7.15.1**, com licença e fontes correspondentes. O assistente copia e ativa o plugin automaticamente: não é necessário procurar uma DLL nem abrir páginas de download. O rFactor tem de estar fechado para alterar a instalação do plugin.

No mesmo assistente, **Crew Chief · vozes, nomes e personalizações** obtém os pacotes do serviço oficial. São verificadas as versões instaladas e descarregados apenas os pacotes necessários. Os downloads concluídos ficam em cache; se cancelares ou houver uma falha, volta a carregar em **Instalar / verificar**. A instalação dos ficheiros de áudio exige o motor Crew Chief parado. A escolha de voz e nome continua nas definições do Crew Chief e não é substituída pelo instalador.

Quando faltam componentes, a abertura normal do Companion apresenta este assistente antes do arranque automático da sessão. Podes escolher os componentes ou fechar o assistente e configurar a aplicação. **Instalar componentes…** fica sempre disponível em Sessão.

A instalação do plugin preserva as outras preferências e guarda cópias dos ficheiros substituídos em `UserData/player/CompanionPluginBackups`. Configuração inválida ou outro plugin de memória partilhada potencialmente ativo bloqueiam a operação. Não são instalados plugins concorrentes. Depois de instalar, abre o jogo e entra em pista para confirmar a telemetria.

Se a DLL não aparecer no jogo, verifica o Visual C++ 2013 Runtime em `Support/Runtimes`, conforme o [guia oficial do TinyPedal](https://github.com/TinyPedal/TinyPedal#setup-for-rfactor-2). A origem, a versão e os hashes do plugin estão em `assets/plugins/RF2-PROVENANCE.json`.

A classificação GTC funciona sem este plugin. Voltas em direto, presença dos carros e widgets dependem da telemetria local compatível.

## Campeonatos e arranque

No topo da janela, escolhe um campeonato ou usa **+ Adicionar**. Cada campeonato tem nome, página, classificação GTC e acesso ao servidor próprios. O menu **…** permite editar ou eliminar o campeonato. A seleção prepara o próximo arranque e preserva a sessão em curso.

Na página **Servidor**, abre o site GTC, inicia sessão e copia o link de entrada disponibilizado para a tua competição. Cola-o em **Link de entrada** e guarda. Não são incluídos servidores nem palavras-passe no pacote; cada utilizador configura os seus acessos.

Em **Servidor → Replays automáticos**, podes ativar a monitorização da sessão GTC. Quando outro piloto estiver em pista, o Companion tenta abrir o rFactor como espetador, desde que o jogo esteja fechado. A comparação usa a tua melhor volta GTC da mesma pista e classe; são consideradas voltas completas e válidas observadas após a ligação. O Companion guarda a sessão completa como replay, com o nome e o tempo do piloto, conservando a melhor gravação de cada piloto, pista e classe. O ficheiro é arquivado depois de fechares o rFactor e de o jogo terminar de o escrever. Mantém o Companion aberto; se o fechares entretanto, a verificação pendente é retomada quando voltares a abri-lo. O rFactor precisa de ter gravação de replays ativada e o servidor de permitir espetadores.

Se o jogo já estiver aberto, a monitorização aguarda para não interromper a sessão em curso. Se não for possível identificar um único replay da sessão ou confirmar piloto e pista, os ficheiros são mantidos e a app apresenta o motivo. A volta específica não é recortada do replay completo.

- **Entrar no servidor** abre as aplicações selecionadas e o rFactor, aguarda pelo menu e pede a ligação ao servidor configurado. Distingue pedido enviado de ligação confirmada.
- **Abrir sem servidor** permite treinar sem configurar campeonato, piloto ou acesso. Abre as aplicações selecionadas e o jogo; este modo não abre o overlay GTC.
- Se o jogo ou os motores já estiverem abertos, são reutilizados. Uma ligação existente é preservada. Para mudar de servidor, regressa primeiro ao menu do jogo.
- O arranque automático começa após quatro segundos e pode ser cancelado ou desativado em **Sessão**.

Usa modo de janela sem bordas para os overlays aparecerem sobre o jogo. Fechar o Companion não fecha o jogo nem os motores.

## TinyPedal e Crew Chief

As páginas próprias do Companion editam as configurações dos motores originais:

- **TinyPedal:** widgets, posição, opacidade, escala, cores, unidades e módulos. A secção **Pneus / travões** reúne os controlos mais usados. Pesquisa, grupos e paginação ajudam a encontrar as outras opções. Numa instalação nova, o perfil predefinido inclui o mapa, o radar, o desgaste e as temperaturas dos pneus e dos travões; perfis existentes são preservados.
- **Crew Chief:** perfis, nomes e vozes instalados, volumes, dispositivos de áudio, microfone, reconhecimento de voz, spotter e mensagens.
- **Guardar alterações:** no TinyPedal, as alterações válidas são guardadas automaticamente e o perfil ativo é atualizado com o motor aberto. Outros perfis e definições globais ficam pendentes até o motor fechar. No Crew Chief, as alterações ficam pendentes enquanto o motor estiver aberto. Conflitos com alterações externas exigem recarregar e rever; ao aplicar alterações com os motores fechados, são guardadas cópias em `backups/engines`.

Inclui TinyPedal **2.52.0** e Crew Chief **4.19.5.8**. A biblioteca Python do TinyPedal contém uma integração para aplicar alterações ao perfil ativo enquanto o motor está aberto; os respetivos fontes estão em `assets/tinypedal-live`. Os pacotes de áudio são descarregados pelo assistente de instalação. Algumas ferramentas continuam a pertencer às aplicações originais.

Ao arrancar pelo Companion, a janela principal do Crew Chief fica em segundo plano enquanto o motor continua ativo. A janela de carregamento e a janela principal são tratadas separadamente; outras janelas do motor continuam disponíveis para funções que ainda pertençam à aplicação original.

## Overlay de classificação

O overlay apresenta posições, nomes, voltas acumuladas, diferenças e setores da classificação GTC. As definições estão em **Overlay**: aparência, classificação, estado e atalhos.

- Vistas completa e compacta, com número configurável de pilotos acima e abaixo.
- Posição e transparência guardadas entre utilizações.
- Volta atual, volta anterior opcional e volta ideal opcional, com o mesmo tamanho das linhas dos pilotos.
- Diferencial comparado com **Recorde pessoal** (predefinição) ou **Volta anterior**. A referência da classificação GTC tem prioridade sobre tempos locais, mesmo que um registo local seja mais rápido. Sem referência GTC correspondente, usa o recorde local. Os setores pertencem à mesma volta.
- Ideal pessoal calculada pela soma dos melhores setores válidos disponíveis na GTC. Setores ausentes podem usar os registos locais como apoio. A coluna opcional **Ideal observada de cada piloto** segue a mesma regra e pode não representar todo o histórico.
- Pontos de presença com prioridade para o estado explícito da sessão GTC: verde em pista, laranja nas boxes e cinzento no paddock. Dados remotos com mais de 30 s, flags ausentes ou sem correspondência inequívoca recorrem à telemetria local fresca; sem fonte válida, não aparece ponto.
- Lugares ganhos/perdidos desde o início da sessão, nas últimas X horas ou desde ontem às 00:00.

Verde indica melhoria, vermelho perda e roxo recordes/melhores setores. A volta ideal é identificada a roxo. Dados indisponíveis aparecem como **—**.

| Atalho inicial | Ação |
|---|---|
| Ctrl + Alt + S | Definições do overlay |
| Ctrl + Alt + G | Alternar vista compacta/completa |
| Ctrl + Alt + O | Mover/bloquear |
| Ctrl + Alt + H | Mostrar/ocultar |

As combinações podem ser alteradas em **Overlay → Atalhos**. As alterações aplicam-se ao overlay aberto. Combinações repetidas ou indisponíveis são recusadas.

### Dados e histórico

O histórico de posições é recolhido uma vez por minuto enquanto o Companion ou o overlay estiverem abertos e é conservado durante oito dias, separado por campeonato, ronda e pista. Não recupera o passado. Uma comparação por período usa o último registo anterior ao instante escolhido, com tolerância de 15 minutos; sem dados suficientes mostra **—**.

As ideais dão prioridade aos setores de voltas válidas da classificação e da sessão GTC, guardados numa cache separada dos recordes locais. O overlay consulta `/livetiming/snapshot/{id}` com uma vista ideal ou o estado dos pilotos ativo, no máximo uma vez a cada 15 segundos. A análise de pilotos usa a mesma fonte quando solicitada ou com recolha em segundo plano ativa. São guardados apenas os dados desportivos necessários. Uma falha desta fonte mantém os dados GTC guardados e permite o apoio local. Pistas, classes, pilotos, rondas e campeonatos não são intercambiáveis.

A classificação é também recolhida pelo Companion sem exigir acesso ao servidor nem o overlay aberto. As duas respostas já usadas pela aplicação continuam a ser as únicas fontes GTC; não são consultadas novas rotas. Os dados guardados evitam novas consultas a cada interação. Preferências, instalação dos motores, telemetria da volta em curso e estimativas de saídas continuam a usar as fontes locais necessárias.

As preferências e o histórico ficam, por defeito, em `%LOCALAPPDATA%\GTC-Companion`. Cada utilizador mantém o seu próprio histórico. Não partilhes a pasta de dados nem o link de entrada. A app não verifica inscrições; a organização e o servidor controlam a autorização de acesso.

## Treino de ritmo

Em **Overlay → Stint**, escolhe a duração em **voltas** ou **minutos**, o **ritmo alvo** (`m:ss,mmm`) e a **janela de consistência** em segundos para cada lado do alvo. A sugestão opcional usa a melhor volta disponível, com prioridade à GTC: alvo 0,5% acima do recorde e janela ±0,6% do recorde, arredondada a centésimos, com mínimo de 0,10 s. São estimativas ajustáveis; confirma a referência, a pista e as condições. **Iniciar stint** prepara a contagem na passagem seguinte pela meta; a volta de saída fica de fora. O treino funciona em singleplayer ou num servidor, sem precisar de classificação GTC.

Em **Overlay → Aparência**, o mesmo controlo de transparência, os botões Mostrar e Mover e os atalhos de vista, visibilidade e movimento atuam nos overlays de classificação e de stint. A posição de cada um pode ser ajustada separadamente; ao terminar o movimento a posição escolhida é preservada.

Em **Como configurar**, a calculadora usa **melhor volta válida × (1 + margem / 100)**. A referência vem primeiro da classificação GTC e, na ausência dela, das voltas válidas disponíveis da sessão GTC; só depois usa o jogo e os recordes locais. A origem aparece junto da referência e pode ser editada. A margem de 0,5–1,0% é uma estimativa inicial ajustável, não uma regra universal. Usa uma referência representativa e confirma o alvo com 10–15 voltas nas condições de combustível, pneus e pista da corrida. O botão **Aplicar ao próximo stint** preenche o alvo, sem alterar um stint em curso. A ajuda explica a configuração, a passagem inicial pela meta e a leitura do resultado.

O HUD de treino e o overlay de classificação têm apresentações separadas e **recordes locais partilhados**. Enquanto o Companion estiver aberto, as voltas completas e válidas observadas alimentam os recordes de tempo e setores, mesmo sem stint ativo ou com o HUD oculto. Voltas inválidas, parciais, em boxes ou com falhas de observação não melhoram os recordes. Os recordes existentes são importados sem apagar os ficheiros anteriores. A sincronização entre processos preserva as melhorias de ambos e mantém separados os setores da volta recorde e os melhores setores individuais.

O registo comum fica em `records/personal-records.json` na pasta de dados, por pista, classe e piloto. A classificação **online** da GTC continua a mostrar apenas as voltas registadas pelo servidor; uma volta de singleplayer pode melhorar o recorde local, mas não é enviada para a GTC.

Durante o stint, o HUD mostra voltas completas, voltas/tempo restantes, média, diferencial para o alvo, última volta, voltas que cumprem a janela, saídas estimadas e nota de 0 a 100. Média e diferencial usam todas as voltas completas, incluindo inválidas. Verde significa dentro do alvo ± tolerância; verde carregado significa abaixo do limite rápido e também conta como cumprimento da janela; vermelho significa acima do limite lento. As configurações de duração e ritmo ficam fixas durante o stint.

A nota soma **70 pontos de ritmo** e **30 de limpeza**. O ritmo recebe 70 se a média for igual ou inferior ao alvo + tolerância, sem penalizar tempos mais rápidos. Acima desse limite, perde pontos linearmente até zero quando a diferença adicional chega a 5% do alvo. A limpeza perde 10 pontos por cada saída por 10 voltas, até ao máximo de 30 pontos. Por exemplo, 15 voltas no ritmo com uma saída dão 93/100; com três saídas, 80/100. O resultado é arredondado ao inteiro mais próximo. É uma avaliação do treino, não um rating GTC; saídas estimadas não equivalem a penalizações oficiais. A nota é provisória durante o treino, parcial num objetivo interrompido ou terminado manualmente e estimada se houve falhas de telemetria. Sem voltas completas não há nota. Os históricos são avaliados com as mesmas regras, sem alterar os tempos e saídas guardados.

Nos treinos por minutos, o contador chega a zero e aguarda pela passagem seguinte na meta, contando apenas voltas completas. Uma ida às boxes, mudança de carro/pista/sessão ou salto de voltas interrompe o stint, preservando o resumo. Uma distância negativa transitória na meta não termina o treino. Uma perda do estado de condução coloca a recolha em pausa e permite retomar na mesma sessão, sem anunciar o objetivo como concluído. Na ausência de telemetria, o progresso fica suspenso. Voltas com dados incompletos são assinaladas. **Objetivo concluído**, **Stint interrompido** e **Terminado manualmente** têm indicações distintas.

O gráfico permite escolher **Volta completa**, **Setor 1**, **Setor 2** ou **Setor 3**. Na volta completa, a linha laranja representa o alvo e as linhas tracejadas delimitam a janela: verde identifica tempos dentro do alvo ± tolerância; verde carregado identifica tempos abaixo do limite rápido, também aceites; vermelho identifica tempos acima do limite lento. Nos setores, a referência é o setor correspondente da melhor volta válida do stint: verde significa igual ou mais rápido e vermelho mais lento. O alvo e a tolerância da volta não são divididos pelos três setores. A legenda e a nota aparecem no gráfico e no PNG; cinzentos com cruz são inválidos ou incompletos e o círculo laranja assinala uma saída estimada. Passa o rato sobre um ponto para os detalhes. **Guardar gráfico** exporta uma imagem PNG. O seletor permite rever os últimos 30 stints; os registos locais ficam em `training/stints` na pasta de dados.

Os novos treinos guardam os setores de voltas completas com tempos confirmados pela telemetria. Históricos anteriores podem não ter setores; os campos ausentes aparecem como **—** e não são estimados. A média principal do treino continua a usar todas as voltas completas, incluindo inválidas.

**Mover HUD** permite arrastá-lo; um duplo clique volta a bloqueá-lo. A posição e a opacidade ficam guardadas. O modo bloqueado deixa passar os cliques para o jogo.

### Pilotos e stints

**Overlay → Stint → Pilotos e stints** apresenta gráficos de outros pilotos. Escolhe o **piloto**, a **sessão GTC**, **todos os stints** ou um stint específico e **Volta completa / Setor 1 / Setor 2 / Setor 3**. Os tempos e setores da GTC prevalecem. Voltas locais em falta são usadas como complemento apenas quando piloto, pista, classe, número de volta, tempo e hora permitem identificar uma única sessão correspondente. Uma cruz indica invalidade, dados incompletos ou possível invalidade; consulta o ponto para distinguir. Ausência de tempo na GTC não prova que uma volta seja inválida. No gráfico do teu stint, sem sessão GTC inequivocamente associada, os dados locais mantêm a contagem, mas não são apresentados como gráfico GTC. Dados de pneus disponíveis aparecem discretamente no gráfico; zeros ou campos ausentes não são apresentados como desgaste confirmado. **Mostrar referência** apresenta o alvo e a janela do treino na volta completa; nos setores, apresenta o setor correspondente da melhor volta limpa da seleção. A seleção não altera os teus stints nem os recordes partilhados.

- As linhas **PIT** assinalam entradas nas boxes observadas. A saída observada define o início do próximo stint. Marcas **~** indicam um limite aproximado, obtido de uma volta marcada com boxes ou de um aumento do contador de paragens entre duas leituras. Sem saída observada, a passagem continua em aberto.
- A **média limpa** usa apenas as voltas disponíveis, válidas, completas e sem boxes. **Incluir outras voltas** mostra também as de boxes, inválidas e incompletas, assinaladas com uma cruz. Estas voltas não entram na média limpa.
- Os números no eixo são as voltas reais da sessão. Voltas ou setores em falta ficam sem pontos e **não são interpretados como paragens**. Passa o rato pelos pontos e marcas PIT para consultar os detalhes. Clica num ponto ou usa o seletor de volta para consultar o tempo total, S1, S2, S3 e as diferenças por setor para a melhor volta limpa da seleção. O círculo branco assinala a volta selecionada. **Guardar gráfico** exporta PNG com a legenda.

A telemetria local acompanha os carros da sessão como fonte de apoio enquanto o Companion está aberto, mesmo sem stint de treino ativo. **Atualizar GTC** consulta apenas o snapshot desportivo do campeonato selecionado, com intervalo mínimo de 15 segundos. **Recolher GTC em segundo plano** permite continuar a consulta com esta janela fechada, enquanto o Companion estiver aberto; a opção vem ativa em novas configurações e pode ser desligada. Preferências anteriormente guardadas são respeitadas.

O snapshot pode conter apenas alguns pilotos e parte das suas voltas. A app mostra a cobertura disponível e mantém os dados anteriores quando a fonte falha. Não recupera todas as sessões passadas nem inventa tempos ou paragens em falta. As sessões e campeonatos ficam separados; uma nova abertura da app inicia uma nova janela de observação local. Os registos desportivos ficam em `training/drivers` na pasta de dados deste PC e não são incluídos no ZIP distribuído.

As **saídas de pista são estimadas**, não são as infrações oficiais do servidor. É contado um evento quando o centro do carro permanece mais de 1 m além do limite reportado pela telemetria durante 0,6 s. Só é contado outro evento depois de regressar ao interior durante 0,6 s. Boxes e menus não contam. O traçado e falhas de telemetria podem afetar esta estimativa.

## Versão e atualizações

A página **Sobre** apresenta a versão instalada, os créditos, as licenças e as versões dos motores como links para os projetos oficiais. **Ver atualizações** abre as versões publicadas no GitHub, incluindo as de teste. Não instala atualizações automaticamente.

**Copiar informação da versão** copia os números da aplicação e dos motores para ajudar no diagnóstico, sem incluir configurações pessoais ou acessos.

As versões de teste ainda precisam de validação num PC sem configurações anteriores. Os binários atuais não têm assinatura digital. O ficheiro `.sha256` de cada versão permite verificar a integridade do ZIP.

## Licença e desenvolvimento

O código original do Companion e do overlay está sob **GNU GPL versão 3** (`GPL-3.0-only`). O texto integral está em [LICENSE](LICENSE). O programa é fornecido sem garantia. TinyPedal, Crew Chief e as suas dependências mantêm as respetivas licenças, descritas em [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

O pacote inclui `SOURCE.zip`, os motores em `components` e as fontes/avisos correspondentes em `third-party-source`. Para compilar no Windows com .NET Framework:

```powershell
powershell -File .\build.ps1 -Test
```

O script obtém os componentes nas versões fixadas quando necessário. A assinatura digital opcional está documentada em [SIGNING.md](SIGNING.md). As regras de publicação sem dados privados estão em [SECURITY.md](SECURITY.md).

Projeto da comunidade, sem afiliação oficial declarada. A identidade GTC pertence aos respetivos titulares. A tipografia Ubuntu mantém a Ubuntu Font Licence 1.0. rFactor 2 e Steam são instalados separadamente e mantêm as suas próprias condições.

A telemetria das voltas do próprio piloto é guardada localmente em CSV em `%LOCALAPPDATA%\GTC-Companion\telemetry\laps` enquanto o Companion e o rFactor 2 estão abertos. Inclui posição e comandos brutos e filtrados; valores dos pedais são frações de 0 a 1, direção de -1 a 1. Uma gravação iniciada a meio da volta é assinalada como parcial. Estes dados pessoais não são enviados para o repositório.

Durante spectate, o Companion identifica o carro da câmara através do canal Graphics do plugin e grava as voltas observadas em `telemetry\laps\observados`. O canal é ativado temporariamente se necessário; a identificação e a gravação param quando a câmara muda ou os dados deixam de ser fiáveis.
