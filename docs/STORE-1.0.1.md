# Organizador de Janelas: correção do MSIX 1.0.1.0

Preparado em 15/10/2026. Sem envio ou publicação pelo Codex.

## Causa e alteração

O MSIX 1.0.0.0 só contém o EXE 0.3.2.0. O release desse EXE também
possui cinco DLLs nativas WPF, omitidas do pacote. O Windows registrou
`DllNotFoundException` ao tentar abrir o app no teste de 15/10, às 14:40.

A configuração agora recebe a pasta completa da publicação. O build
confere as cinco DLLs e a versão do executável antes de empacotar.
O wrapper usa versão Store 1.0.1 sem mudar a versão 0.3.2 do site.
Não há alteração em regras, interface, modo silencioso ou limpeza do EXE.

O que mudou: “Corrige a abertura do Organizador de Janelas na edição
da Microsoft Store, incluindo as bibliotecas nativas necessárias.”

## Validação realizada

- Build .NET 10 e MakeAppx concluídos.
- Manifesto: `Firawynix.Firaw-OrganizadordeJanelas`, Publisher
  `CN=1FDE3668-C222-4506-AFE6-E2E425EAECD8`, x64, versão 1.0.1.0,
  AppId `FirawWindowManager` e executável existente.
- Cinco testes de DLL ausente rejeitados antes de MakeAppx.
- EXE antigo com versão divergente rejeitado.
- Hashes do EXE e das cinco DLLs dentro do ZIP iguais ao publish.
- Sem assinatura local, instalação ou abertura pelo Codex.

Artefato local:
`C:\Users\Hugo\firaw-window-manager-msix-fix\packaging\windows\msix\artifacts\Firaw-Organizador-de-Janelas-1.0.1.0-x64-store.msix`.

Tamanho: 78.301.460 bytes. SHA-256:
`36c11626da9d78ff80e4d26615080f695b0f9a11cde6a647b56fc76aef55daf0`.
Um novo build pode produzir outro hash; conferir novamente antes do envio.

## Próximo teste do Hugo

Para conferir a inicialização do publish sem substituir o app instalado,
execute num PowerShell normal:

```powershell
& 'C:\Users\Hugo\firaw-window-manager-msix-fix\release-store-1.0.1\FirawynixWindowManager.exe'
```

Confira a tela inicial sem criar ou aplicar regras. Saia pelo menu do
ícone na bandeja: fechar a janela principal mantém o app rodando.
Se houver aviso de bloqueio, envie a mensagem e pare; não desative
proteções nem adicione certificados. Esse teste não comprova a ativação
do MSIX corrigido com identidade de pacote.

Depois, testar o publish no contexto de identidade Store ou em ambiente
de teste autorizado. Não instalar o MSIX sem assinatura com bypass.
Hugo confere se 1.0.1.0 é maior que o último envio no Partner Center,
faz o merge e envia o MSIX sem assinatura local, conforme
`G:\firawos\workspace\docs\guias\PUBLICAR-VERSOES.md`.

Após aprovação e teste do pacote assinado pela Microsoft, obter o novo
arquivo intacto e preparar inventário/hash novo no Center. Não ativar o
MSIX 1.0.0.0 defeituoso, nem alterar bytes ou assinatura desse arquivo.
O canal EXE do site permanece como está.

## Diário da FirawOS

15/10: 23 apps abriram no roteiro manual; SnapCopyText teve atraso.
Organizador Store 1.0.0.0 encerrou por DLL WPF ausente no pacote.
Preparado Store 1.0.1.0 com payload completo e sete verificações aprovadas.
Sem abertura, instalação, assinatura privada ou publicação pelo Codex.
Falta confirmação de execução e ensaio do MSIX corrigido.
