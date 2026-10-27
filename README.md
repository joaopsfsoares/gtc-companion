# GTC Companion

Aplicação Windows para preparar sessões de rFactor 2 e acompanhar as classificações da GTC Race Center.

Criado por **João Soares** · GPLv3 · Windows 10/11

**[Descarregar a aplicação](https://github.com/joaopsfsoares/gtc-companion/releases)** — abre a versão mais recente e escolhe `GTC-Companion-<versão>.zip`. Os ficheiros `Source code` destinam-se ao desenvolvimento.

## Instalação

1. Extrai o ZIP completo para uma pasta onde possas escrever e abre **GTC-Companion.exe**.
2. Em **Sessão**, confirma o nome do piloto, a pasta do rFactor 2 e o arranque automático.
3. Em **Sessão → Instalação → Instalar componentes…**, instala o plugin de telemetria e os pacotes de áudio do Crew Chief.
4. Em **Aplicações**, escolhe se queres abrir o TinyPedal e o Crew Chief. Os motores estão incluídos no ZIP; não precisam de instalação separada nem de configuração dos seus caminhos.
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

- **Entrar no servidor** abre as aplicações selecionadas e o rFactor, aguarda pelo menu e pede a ligação ao servidor configurado. Distingue pedido enviado de ligação confirmada.
- **Abrir sem servidor** permite treinar sem configurar campeonato, piloto ou acesso. Abre as aplicações selecionadas e o jogo; este modo não abre o overlay GTC.
- Se o jogo ou os motores já estiverem abertos, são reutilizados. Uma ligação existente é preservada. Para mudar de servidor, regressa primeiro ao menu do jogo.
- O arranque automático começa após quatro segundos e pode ser cancelado ou desativado em **Sessão**.

Usa modo de janela sem bordas para os overlays aparecerem sobre o jogo. Fechar o Companion não fecha o jogo nem os motores.

## TinyPedal e Crew Chief

As páginas próprias do Companion editam as configurações dos motores originais:

- **TinyPedal:** widgets, posição, opacidade, escala, cores, unidades e módulos. Pesquisa, grupos e paginação ajudam a encontrar as opções.
- **Crew Chief:** perfis, nomes e vozes instalados, volumes, dispositivos de áudio, microfone, reconhecimento de voz, spotter e mensagens.
- **Guardar alterações:** aplica apenas os campos alterados. Com o motor aberto, coloca-os numa fila local para aplicar depois de fechar ou no próximo arranque. Os ficheiros originais são guardados em `backups/engines`. Conflitos com alterações externas exigem recarregar e rever.

Inclui TinyPedal **2.52.0** e Crew Chief **4.19.5.8**, sem alterações aos executáveis. Os pacotes de áudio são descarregados pelo assistente de instalação. Estas páginas cobrem as configurações disponíveis nos perfis; algumas ferramentas continuam a pertencer às aplicações originais.

## Overlay de classificação

O overlay apresenta posições, nomes, voltas acumuladas, diferenças e setores da classificação GTC. As definições estão em **Overlay**: aparência, classificação, estado e atalhos.

- Vistas completa e compacta, com número configurável de pilotos acima e abaixo.
- Posição e transparência guardadas entre utilizações.
- Volta atual, volta anterior opcional e volta ideal opcional, com o mesmo tamanho das linhas dos pilotos.
- Diferencial comparado com **Recorde pessoal** (predefinição) ou **Volta anterior**. O recorde compara os setores da mesma volta.
- Ideal pessoal calculada pela soma dos melhores setores válidos observados. A coluna opcional **Ideal observada de cada piloto** segue a mesma regra e pode não representar todo o histórico.
- Pontos de presença obtidos pela telemetria local: verde em pista, laranja nas boxes e cinzento no paddock. Sem correspondência inequívoca, não aparece ponto.
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

As ideais usam setores de voltas válidas da classificação, da sessão GTC e da telemetria local. O overlay consulta `/livetiming/snapshot/{id}` apenas com uma vista ideal ativa, no máximo uma vez a cada 15 segundos. A análise de pilotos usa a mesma fonte quando solicitada ou com recolha em segundo plano ativa. São guardados apenas os dados desportivos necessários. Uma falha desta fonte mantém a classificação e os dados já observados.

As preferências e o histórico ficam, por defeito, em `%LOCALAPPDATA%\GTC-Companion`. Cada utilizador mantém o seu próprio histórico. Não partilhes a pasta de dados nem o link de entrada. A app não verifica inscrições; a organização e o servidor controlam a autorização de acesso.

## Treino de ritmo

Na página **Treino**, escolhe a duração em **voltas** ou **minutos**, o **ritmo alvo** (`m:ss,mmm`) e a **janela de consistência** em segundos para cada lado do alvo. **Iniciar stint** prepara a contagem na passagem seguinte pela meta; a volta de saída fica de fora. O treino funciona em singleplayer ou num servidor, sem precisar de classificação GTC.

Em **Como configurar**, a calculadora usa **melhor volta válida × (1 + margem / 100)**. A margem de 0,5–1,0% é uma estimativa inicial ajustável, não uma regra universal. Usa uma referência representativa e confirma o alvo com 10–15 voltas nas condições de combustível, pneus e pista da corrida. O botão **Aplicar ao próximo stint** preenche o alvo, sem alterar um stint em curso. A ajuda explica a configuração, a passagem inicial pela meta e a leitura do resultado.

O HUD de treino e o overlay de classificação têm apresentações separadas e **recordes locais partilhados**. Enquanto o Companion estiver aberto, as voltas completas e válidas observadas alimentam os recordes de tempo e setores, mesmo sem stint ativo ou com o HUD oculto. Voltas inválidas, parciais, em boxes ou com falhas de observação não melhoram os recordes. Os recordes existentes são importados sem apagar os ficheiros anteriores. A sincronização entre processos preserva as melhorias de ambos e mantém separados os setores da volta recorde e os melhores setores individuais.

O registo comum fica em `records/personal-records.json` na pasta de dados, por pista, classe e piloto. A classificação **online** da GTC continua a mostrar apenas as voltas registadas pelo servidor; uma volta de singleplayer pode melhorar o recorde local, mas não é enviada para a GTC.

Durante o stint, o HUD mostra voltas completas, voltas/tempo restantes, média, diferencial para o alvo, última volta, voltas dentro da janela e saídas estimadas. Média e diferencial usam todas as voltas completas, incluindo inválidas. Verde significa mais rápido que o alvo e vermelho mais lento. As configurações de duração e ritmo ficam fixas durante o stint.

Nos treinos por minutos, o contador chega a zero e aguarda pela passagem seguinte na meta, contando apenas voltas completas. Uma ida às boxes, mudança de carro/pista/sessão ou salto de voltas interrompe o stint, preservando o resumo. A pausa do jogo não acrescenta tempo; na ausência de telemetria, o progresso fica suspenso. Voltas com dados incompletos são assinaladas.

O gráfico apresenta um ponto por volta, a linha do alvo e os limites superior/inferior da janela. Uma cruz assinala invalidade ou dados incompletos; o círculo laranja indica uma volta com saída estimada. Passa o rato sobre um ponto para os detalhes. **Guardar gráfico** exporta uma imagem PNG. O seletor permite rever os últimos 30 stints; os registos locais ficam em `training/stints` na pasta de dados.

**Mover HUD** permite arrastá-lo; um duplo clique volta a bloqueá-lo. A posição e a opacidade ficam guardadas. O modo bloqueado deixa passar os cliques para o jogo.

### Pilotos e stints

**Treino → Pilotos e stints** apresenta gráficos de outros pilotos. Escolhe o **piloto**, a **sessão/origem** e **todos os stints** ou um stint específico. **Mostrar alvo do treino** usa a mesma referência e janela do teu treino, para comparar o ritmo. A seleção não altera os teus stints nem os recordes partilhados.

- As linhas **PIT** assinalam entradas nas boxes observadas. A saída observada define o início do próximo stint. Marcas **~** indicam um limite aproximado, obtido de uma volta marcada com boxes ou de um aumento do contador de paragens entre duas leituras. Sem saída observada, a passagem continua em aberto.
- A **média limpa** usa apenas as voltas disponíveis, válidas, completas e sem boxes. **Incluir outras voltas** mostra também as de boxes, inválidas e incompletas, assinaladas com uma cruz. Estas voltas não entram na média limpa.
- Os números no eixo são as voltas reais da sessão. Voltas em falta ficam sem pontos e **não são interpretadas como paragens**. Passa o rato pelos pontos e marcas PIT para consultar os detalhes. **Guardar gráfico** exporta PNG.

A telemetria local acompanha os carros da sessão enquanto o Companion está aberto, mesmo sem stint de treino ativo. **Atualizar GTC** consulta apenas o snapshot desportivo do campeonato selecionado, com intervalo mínimo de 15 segundos. **Recolher GTC em segundo plano** permite continuar a consulta com esta janela fechada, enquanto o Companion estiver aberto; a opção vem desativada na distribuição.

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
