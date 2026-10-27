# Acesso a servidores

O código e o pacote distribuível não contêm dados de acesso de servidores reais. Na primeira configuração, o utilizador introduz o link obtido no site da competição ou configura os dados a que tem acesso.

Importar um link não autentica a pessoa na GTC, não verifica inscrições e não comprova autorização. A aplicação apenas guarda os dados localmente e transmite o pedido de ligação ao serviço local do rFactor 2. Compete à organização e ao servidor controlar quem pode entrar. Uma palavra-passe partilhada pode ser reenviada por qualquer pessoa que a conheça; esconder o link na interface de um site não resolve essa limitação.

Não incluir no Git configurações, links reais de entrada, palavras-passe, histórico pessoal ou ficheiros da instalação do jogo. A pasta `dist` deve ser gerada pelo script, sem copiar a pasta de dados do utilizador. O `.gitignore` exclui os ficheiros conhecidos da aplicação.

Os links de importação aceites apontam apenas para `http://localhost:5397/rest/multiplayer/join` ou a variante `127.0.0.1`; a aplicação constrói o pedido local a partir dos parâmetros. Não abre o endereço colado como uma página nem utiliza esse URL para comunicar com serviços externos.

Se uma palavra-passe real tiver sido publicada, deve ser alterada pela organização. Apagar o ficheiro ou torná-lo privado depois da publicação não garante a eliminação de cópias.
