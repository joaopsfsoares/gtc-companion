# GTC Companion — versão de teste 0.7.4

Criado por **João Soares** · GPLv3 · Windows

**[Descarregar a aplicação completa](https://github.com/joaopsfsoares/gtc-companion/releases/tag/v0.7.4)** — na secção Assets escolhe `GTC-Companion-0.7.4.zip`. Os arquivos Source code contêm apenas código-fonte.

Extrai o ZIP para uma pasta e abre **GTC-Companion.exe**. Na primeira abertura, confirma o nome de piloto, a pasta do rFactor 2 e as aplicações que queres abrir. Na página **Servidor**, abre o site GTC, inicia sessão e copia o link de entrada disponibilizado para a tua competição. Cola-o no campo **Link de entrada** e carrega em **Guardar** ou **Iniciar sessão**.

A distribuição não contém endereço, porta ou palavra-passe de nenhum servidor. Os dados que introduzires ficam na configuração local do teu utilizador. Não partilhes a pasta de dados nem o link de entrada. A app não verifica inscrições: a autorização para entrar tem de ser garantida pela GTC e pelo servidor. Um link partilhável, por si só, não prova que uma pessoa está inscrita.

Nas aberturas seguintes, a sessão arranca automaticamente após quatro segundos. Seleciona uma página ou carrega em **Cancelar arranque** para configurar. Podes desligar o arranque automático na página **Sessão**.

## TinyPedal e Crew Chief no Companion

As páginas **TinyPedal** e **Crew Chief** têm controlos próprios, com o aspeto do Companion. São editores das configurações dos motores originais, sem alojar as janelas das aplicações.

- **TinyPedal → Widgets:** ativar/desativar widgets, pesquisar e abrir as suas definições. Em **Definições** escolhe-se o grupo para editar posição, opacidade, escala, cores, opções de apresentação, unidades, módulos e configuração global, consoante os campos disponíveis no perfil instalado.
- **Crew Chief:** selecionar o perfil e a categoria para editar o nome usado pelo engenheiro, vozes, volume, saídas de áudio, microfone, reconhecimento de voz, spotter, combustível, mensagens e preferências avançadas. Os volumes usam percentagens e as saídas de áudio mostram os dispositivos ativos do Windows. Selecionar outro perfil do Crew Chief prepara a sua ativação ao guardar.
- **Guardar alterações:** quando o motor está fechado, aplica apenas os campos alterados. Quando está aberto, guarda uma fila local e aplica-a depois de fechar o motor, ou antes do próximo arranque pelo Companion. Estas alterações não são aplicadas em direto nesta versão.
- **Iniciar motor:** abre o motor selecionado em segundo plano. No arranque de uma sessão, os motores novos também passam para segundo plano; as aplicações que já estavam abertas mantêm o seu estado.

Antes de escrever é guardada uma cópia do ficheiro em `backups/engines` na pasta de dados do Companion. Os campos desconhecidos e as outras preferências são preservados. Se a aplicação original alterar entretanto o mesmo campo, a fila fica suspensa e é apresentada uma mensagem para recarregar e rever. **Recarregar** volta a ler as preferências; não descarta alterações por guardar.

O ZIP inclui os motores TinyPedal 2.52.0 e Crew Chief 4.19.5.8 na pasta `components`; extrai o pacote completo. Não é necessário configurar os caminhos destas aplicações. São criados perfis iniciais quando não existem, e as preferências de uma instalação anterior são migradas localmente. O seletor do TinyPedal escolhe o perfil a editar; o motor original continua a escolher o perfil a carregar segundo as suas regras. Os pacotes de voz são obtidos separadamente pelo botão **Instalar vozes**, com o motor Crew Chief parado. As listas de nomes e vozes mostram apenas os pacotes encontrados no disco. As duas páginas usam duas colunas, pesquisa, paginação e cache dos ficheiros de preferências. Esta versão recria a configuração no Companion; não é uma reprodução completa de todas as ferramentas das duas aplicações.

Os esquemas de referência foram obtidos do TinyPedal 2.52.0 e do Crew Chief, com origem registada em `assets/engines/PROVENANCE.json`. O TinyPedal mantém a GPLv3 ou posterior e o Crew Chief a MIT; os avisos estão em **TINYPEDAL-LICENSE.txt**, **CREWCHIEF-LICENSE.txt** e **THIRD-PARTY-NOTICES.md**.

## Configurar vários campeonatos

No topo da janela, escolhe o campeonato ou usa **+ Adicionar**. Indica o nome e, se quiseres, a página do campeonato e a ligação da classificação GTC (`https://esports.gtcracecenter.com/livetiming/123`). Depois, na página **Servidor**, cola o link de entrada correspondente. O botão **…** permite editar o nome e as ligações do campeonato selecionado.

Cada campeonato guarda o seu acesso, classificação, recordes e histórico. O piloto, as aplicações auxiliares, a posição/transparência e os quatro atalhos do overlay são comuns. A configuração anterior é migrada automaticamente, preservando o Porsche já configurado.

Para eliminar um campeonato, seleciona-o no topo, abre **…** e carrega em **Eliminar**. É pedida confirmação. O campeonato e o seu link saem da lista; o histórico local e a sessão em curso são mantidos. Podes eliminar o último campeonato e voltar a usar **+ Adicionar** quando quiseres.

Selecionar outro campeonato prepara o próximo arranque; não muda uma sessão em curso. Se o jogo estiver ligado a outro servidor, sai para o menu principal antes de iniciar o campeonato escolhido. Sem ligação de classificação, o jogo e as aplicações podem arrancar, mas o overlay GTC fica desativado.

Os dados ficam em `competitions/<identificador>/timing-<tabela>`, e as preferências comuns do overlay em `overlay/settings.json`. O histórico é recolhido para todos os campeonatos com classificação enquanto o Companion estiver aberto. Sem o Companion, apenas o overlay ativo continua a recolher.

## Arranque

Na página **Aplicações**, o link **Repositório ↗** junto ao Crew Chief e ao TinyPedal abre o projeto oficial no navegador, mesmo quando o arranque dessa aplicação está desativado:

- [Crew Chief — GitLab](https://gitlab.com/mr_belowski/CrewChiefV4)
- [TinyPedal — GitHub](https://github.com/TinyPedal/TinyPedal)

- Abre o rFactor 2 pelo Steam, aguarda pelo menu e envia a entrada no servidor configurado.
- Abre o Crew Chief e o TinyPedal apenas se estiverem selecionados. Se já estiverem abertos, reutiliza-os.
- Abre o overlay com o piloto e a tabela GTC escolhidos. O acesso configura-se apenas pelo link na página **Servidor**. O identificador da tabela está em **Overlay → Estado**.
- Se o jogo já estiver ligado a um servidor, pede que regresses ao menu antes de mudar.
- Alguns estados do rFactor apresentam o ecrã da sessão depois de receber o pedido: nesse ecrã, escolhe **Drive → Join Server**. A aplicação distingue pedido enviado de ligação confirmada.
- A opção de modo sem bordas só altera o ficheiro de vídeo antes de abrir o jogo. Guarda uma cópia de segurança. Se o jogo já estiver aberto em ecrã inteiro exclusivo, muda para sem bordas no jogo para veres os overlays.

O Crew Chief e o TinyPedal são opcionais no arranque e os seus motores estão incluídos. As páginas TinyPedal e Crew Chief permitem editar as preferências desses motores. As opções de áudio só mudam quando forem alteradas e guardadas nesta interface. O perfil do volante, os controlos, o carro e o conteúdo instalado no jogo são mantidos. Para o servidor são necessários os carros e as pistas correspondentes.

## Overlay

O overlay está incluído no executável e é extraído automaticamente para a pasta de dados. A tabela pública da GTC funciona sem o plugin de telemetria. A linha de volta atual precisa de um plugin rFactor2SharedMemoryMap compatível, com os nomes legacy de memória partilhada ativados. Se não estiver disponível, aparece **Sem telemetria**.

A coluna **Voltas** mostra o total de voltas acumuladas publicado pela GTC para cada piloto, nas vistas compacta e completa. Se a fonte não fornecer esse número, aparece **—**. O número da volta atual do jogador continua na linha de telemetria.

| Atalho | Ação |
|---|---|
| Ctrl + Alt + S | Definições do overlay |
| Ctrl + Alt + G | Alternar vista compacta/completa |
| Ctrl + Alt + O | Mover/bloquear |
| Ctrl + Alt + H | Mostrar/ocultar |

Na página **Overlay** do Companion escolhem-se transparência, posição, número de pilotos próximos e variação de posições. As alterações são guardadas automaticamente e aplicadas ao overlay aberto. O atalho Ctrl+Alt+S abre esta mesma página. Verde indica melhoria, vermelho perda e roxo recordes/melhores setores.

## Voltas, diferencial e presença

Em **Overlay → Classificação**, escolhe **Recorde pessoal** (predefinição) ou **Volta anterior** como referência do diferencial. As linhas da volta atual e anterior respeitam essa escolha. O recorde usa os setores da mesma volta; não compara com uma soma teórica de setores de voltas diferentes.

**Mostrar volta anterior** conserva a última volta concluída numa linha própria. **Mostrar volta ideal** acrescenta a soma dos melhores setores pessoais registados, por piloto, pista e classe, mesmo que pertençam a voltas diferentes. Só apresenta o tempo ideal quando tem os três setores; os setores disponíveis aparecem a roxo. Os recordes locais mantêm-se entre sessões. Voltas observadas como inválidas não melhoram estes registos. A classificação pode fornecer o recorde e os setores da respetiva volta, mas não contém necessariamente todos os melhores setores pessoais históricos.

Os pontos junto dos nomes usam apenas a telemetria local atual: verde em pista, laranja nas boxes e cinzento no paddock. Sem dados locais, sem correspondência inequívoca ou noutra pista, não aparece ponto. Não é um indicador global de quem está online na GTC.

A posição personalizada fica guardada ao terminar o arrasto, incluindo ao fechar o overlay. Alternar entre vista compacta e completa conserva a posição preferida, ajustando os limites visíveis quando necessário.

**Ideal observada de cada piloto** acrescenta uma coluna opcional. Recolhe setores da leaderboard, das voltas válidas do histórico da sessão GTC e da telemetria local. O endpoint de leitura `/livetiming/snapshot/{id}` só é consultado quando uma vista de volta ideal está ativada, no máximo uma vez a cada 15 segundos. São usados apenas nomes, classe, pista e tempos; a app não guarda os restantes dados da resposta. São excluídas voltas com invalidade ou passagem pelas boxes e registos incompletos/inconsistentes. Não usa os campos acumulados de S2 como um setor individual.

Os registos mantêm-se entre sessões. Uma ideal observada pode não incluir recordes históricos nunca publicados ou vistos pela aplicação. A ausência de um piloto no snapshot atual não significa ausência de histórico local; sem os três setores registados mostra **—**. Uma falha neste endpoint mantém a classificação e os melhores setores já guardados.

## Histórico de posições

A app recolhe uma classificação por minuto enquanto estiver aberta, incluindo quando o jogo está fechado. Se fechares a janela da app mas mantiveres o overlay aberto, o overlay continua a recolher enquanto estiver ativo. São guardados oito dias, separando a tabela, a ronda e a pista.

Em **Overlay → Classificação**, ativa os lugares ganhos/perdidos e escolhe **Início da sessão**, **Últimas X horas** (1 a 168) ou **Ontem às 00:00**, na hora local do computador.

A referência por período usa o último registo anterior ao instante escolhido, com uma tolerância máxima de 15 minutos. A hora exata da referência está na página **Sessão** das definições do overlay. Sem registos suficientes, ou para um piloto sem posição nessa referência, aparece **—**. Não recupera o passado nem recolhe enquanto a app e o overlay estiverem fechados. Outros utilizadores terão o seu próprio histórico; esta versão não tem um serviço central.

## Dados e requisitos

Windows 10/11 com .NET Framework 4.8, Steam e rFactor 2 instalados. Não precisa de Node, Python, Codex ou do launcher RFactor de quem criou a aplicação.

Por defeito, as preferências, os tempos e o histórico ficam em **%LOCALAPPDATA%\GTC-Companion**. O ZIP contém a aplicação, este guia, os avisos de licença e o código-fonte em SOURCE.zip; cada pessoa configura o seu piloto e a pasta do rFactor 2. Inclui também `components` e `third-party-source`, com os motores, avisos e fontes correspondentes. Para fazer cópia de segurança, copia a pasta de dados. Fechar o launcher não fecha o jogo nem as aplicações auxiliares.

Aplicação de apoio à comunidade, sem afiliação oficial declarada. Logótipo, cores e tipografia Ubuntu provenientes do [site da GTC Race Center](https://esports.gtcracecenter.com/). A fonte está incluída, sem instalação no Windows, e mantém a licença Ubuntu Font Licence 1.0 (ver **FONT-LICENCE.txt**). Dados da classificação pública GTC.

## Estado do arranque e link guardado

O progresso mostra separadamente Crew Chief, TinyPedal, rFactor 2, servidor e overlay. Uma aplicação só fica pronta depois de o processo abrir; o jogo só fica pronto depois de responder pelo menu. Pedido de ligação enviado e ligação confirmada são estados distintos. Se o jogo precisar de confirmação, a etapa do servidor indica **Confirmar no jogo**; em caso de falha, o botão permite tentar novamente.

Depois de configurares o acesso, a página Servidor mostra **Link guardado** e **Substituir**. O link não fica permanentemente visível no campo. Podes cancelar uma substituição e manter o acesso anterior. Esta indicação confirma que há um link guardado; não verifica a inscrição na competição nem se o servidor está online.

A janela ajusta a altura à página escolhida. As definições do overlay estão todas no Companion, em Aparência, Classificação e Estado. **Mostrar overlay** permite pré-visualizar sem abrir o jogo; **Mover overlay** permite ajustar a posição, terminando com Ctrl+Alt+O ou com o botão indicado no overlay.
## Atalhos personalizáveis

Em **Overlay → Atalhos**, seleciona uma ação e escolhe a tecla e os modificadores Ctrl, Alt e Shift. Podes desativar uma combinação ou usar **Repor atalhos originais**. A alteração aplica-se ao overlay aberto, sem reiniciar o jogo. Os valores da tabela anterior são os atalhos originais; os menus e as indicações no ecrã acompanham as escolhas atuais.

A app recusa combinações repetidas e verifica os atalhos globais reservados no Windows. Se a nova configuração não puder ser ativada, mantém os atalhos anteriores. Esta verificação não lê os comandos internos dos jogos.

## Deteção do rFactor

O estado distingue o processo do jogo aberto do menu pronto para entrar. Se o jogo já confirmar ligação a um servidor, o Companion mantém a sessão sem enviar outro pedido e sem exigir um registo de arranque anterior. Para mudar de servidor, sai primeiro para o menu. A deteção é consultada a cada meio segundo; os pedidos de leitura têm um limite de dois segundos. O primeiro arranque espera três segundos de menu estável, e um jogo já aberto espera meio segundo. O pedido de entrada mantém o seu próprio limite de tempo e só fica confirmado quando o jogo indica a ligação ao servidor.


## Licença e código-fonte

O código original do GTC Companion e do overlay GTC Timing está licenciado sob a **GNU General Public License, versão 3** (`GPL-3.0-only`). Copyright (C) 2026 GTC Companion contributors.

Podes utilizar, modificar e redistribuir o programa nos termos dessa licença. A aplicação é fornecida sem garantia. O texto integral está em **LICENSE**. As fontes, os logótipos e os recursos de terceiros mantêm as respetivas condições, descritas em **THIRD-PARTY-NOTICES.md**.

O pacote da aplicação inclui **SOURCE.zip**, com o código-fonte correspondente, os recursos e os scripts necessários à compilação. Extrai-o para uma pasta e executa `powershell -File .\build.ps1` no Windows com o .NET Framework instalado. `-Test` executa também os testes. A assinatura digital é opcional para compilar uma versão modificada; a sua configuração está em **SIGNING.md**. Não é necessário usar as chaves ou o certificado do editor para compilar ou executar a tua versão.


## Abrir o rFactor sem entrar num servidor

O botão **Abrir sem servidor** abre o rFactor 2 pelo Steam e aguarda pelo menu. Abre também o Crew Chief e o TinyPedal, se estiverem ativados. Não envia pedidos de entrada em servidores nem abre o overlay GTC neste modo. Se o jogo já estiver aberto, mantém a sessão atual.

Este arranque não exige um campeonato, nome de piloto ou link de servidor configurados. O botão **Entrar no servidor** mantém o arranque direto para o campeonato selecionado. Na linha de comandos, `--launch-menu` corresponde a Abrir sem servidor, e `--launch` corresponde ao arranque para o servidor.

## Versão instalada

No fundo da navegação, **Sobre · v0.7.4** abre a página com a versão do executável, a indicação de versão de teste, as versões dos motores incluídos, os repositórios e os avisos de licença. **Copiar informação da versão** copia apenas estes números e nomes, sem caminhos, nomes de piloto ou acessos a servidores. A versão é lida dos metadados do executável.


A versão 0.7.3 identifica João Soares na página Sobre e aplica a paleta escura à moldura e às áreas de deslocamento da aplicação.

As linhas Volta atual, Volta anterior e Volta ideal usam a mesma altura das linhas da classificação, sem texto de estado ou segunda linha. O número da volta aparece na coluna Voltas; o diferencial total e as cores dos setores mantêm a comparação selecionada.

Na secção Motores incluídos, cada linha com o nome e a versão é um link para o repositório oficial do motor.
