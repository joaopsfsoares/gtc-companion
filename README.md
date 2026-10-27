# GTC Companion

Aplicação Windows para preparar sessões de rFactor 2 e acompanhar as classificações da GTC Race Center.

Criado por **João Soares** · GPLv3 · Windows 10/11

**[Descarregar a aplicação](https://github.com/joaopsfsoares/gtc-companion/releases)** — abre a versão mais recente e escolhe `GTC-Companion-<versão>.zip`. Os ficheiros `Source code` destinam-se ao desenvolvimento.

## Instalação

1. Extrai o ZIP completo para uma pasta onde possas escrever e abre **GTC-Companion.exe**.
2. Em **Sessão**, confirma o nome do piloto, a pasta do rFactor 2 e o arranque automático.
3. Para os widgets e as voltas em direto, usa **Sessão → Telemetria → Configurar telemetria…**.
4. Em **Aplicações**, escolhe se queres abrir o TinyPedal e o Crew Chief. Os motores estão incluídos no ZIP; não precisam de instalação separada nem de configuração dos seus caminhos.
5. Em **Crew Chief → Instalar vozes**, descarrega o áudio com o motor parado.

Requisitos: Steam, rFactor 2 e .NET Framework 4.8. A app não exige Python ou Node instalados. Os carros e as pistas devem estar instalados no jogo.

### Telemetria do rFactor 2

A instalação assistida abre o [projeto oficial rF2 Shared Memory Map](https://github.com/TheIronWolfModding/rF2SharedMemoryMapPlugin#download). Descarrega e extrai o pacote, fecha o jogo e seleciona `rFactor2SharedMemoryMapPlugin64.dll` no assistente. **Instalar e ativar** copia a DLL para `Bin64/Plugins` e ativa a entrada em `UserData/player/CustomPluginVariables.JSON`. As restantes preferências são preservadas. Os ficheiros substituídos ficam em `UserData/player/CompanionPluginBackups`.

A DLL é obtida pelo utilizador na origem oficial; o Companion não a descarrega nem instala sem essa seleção. A instalação verifica o formato Windows de 64 bits, mas essa verificação não certifica a origem do ficheiro. Com o jogo aberto, configuração inválida ou outro plugin de memória partilhada ativo/por configurar, a operação é bloqueada. Não instala um segundo plugin concorrente. Versões com outro nome, como `RF2.3.8.SharedMemoryMapPlugin.dll`, exigem verificar a compatibilidade no projeto do plugin antes de mudar de versão.

Depois abre o rFactor e entra em pista. Também podes ativar o plugin manualmente em **Settings → Gameplay → Plugins**, reiniciando o jogo depois da alteração. Se a DLL não aparecer, verifica o Visual C++ 2013 Runtime em `Support/Runtimes` do jogo, conforme o guia oficial do TinyPedal.

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

Inclui TinyPedal **2.52.0** e Crew Chief **4.19.5.8**, sem alterações aos executáveis. As vozes são descarregadas separadamente. Estas páginas cobrem as configurações disponíveis nos perfis; algumas ferramentas continuam a pertencer às aplicações originais.

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

As ideais usam setores de voltas válidas da classificação, da sessão GTC e da telemetria local. A leitura de `/livetiming/snapshot/{id}` ocorre apenas com uma vista ideal ativa, no máximo uma vez a cada 15 segundos. São guardados apenas os dados desportivos necessários. Uma falha desta fonte mantém a classificação e os setores já observados.

As preferências e o histórico ficam, por defeito, em `%LOCALAPPDATA%\GTC-Companion`. Cada utilizador mantém o seu próprio histórico. Não partilhes a pasta de dados nem o link de entrada. A app não verifica inscrições; a organização e o servidor controlam a autorização de acesso.

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