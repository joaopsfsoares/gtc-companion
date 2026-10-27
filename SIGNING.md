# Preparação de assinatura e publicação

A assinatura pública ainda não foi efetuada. Não há certificado de assinatura disponível nesta instalação nem conta de fornecedor configurada no projeto.

O processo de compilação aceita assinatura Authenticode através de um certificado com chave privada disponível no repositório `CurrentUser/My` do Windows e do SignTool do Windows SDK. Primeiro valida os pré-requisitos, depois assina o overlay antes de o incorporar no Companion e finalmente assina o Companion. Cada assinatura é verificada, incluindo carimbo temporal RFC3161. Os executáveis não podem ser modificados depois de assinados.

```powershell
.\build.ps1 -Test -SigningThumbprint <impressao-digital-do-certificado> -TimestampUrl <https-do-fornecedor-rfc3161> -SignToolPath <caminho-do-signtool.exe>
```

Sem esses parâmetros, o processo continua a gerar uma compilação de teste sem assinatura e indica esse estado. O ficheiro `.zip.sha256` permite conferir a integridade do ZIP; não substitui a assinatura do editor. Não guardar certificados privados, chaves, credenciais ou palavras-passe no repositório.

A licença GPLv3 foi escolhida. Pode ser considerada uma candidatura à SignPath Foundation para assinatura de projetos de código aberto. A aceitação exige análise do projeto, código e compilação verificáveis e aprovação de cada assinatura. Esta candidatura e a integração do serviço ainda não foram efetuadas. O script de certificado local acima não configura a SignPath.

A licença do código original é GPLv3 (`GPL-3.0-only`). A integração GitHub ainda precisa de ser ligada pelo titular da conta antes de criar o repositório remoto e a primeira release. Não foi publicada nenhuma versão.

Referências:

- [SignTool, Microsoft](https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool)
- [Elegibilidade do Azure Artifact Signing](https://learn.microsoft.com/en-gb/azure/artifact-signing/quickstart)
- [Condições SignPath Foundation](https://signpath.org/terms.html)
