# Lembrete de Atualização de Jogos

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](https://opensource.org/licenses/AGPL-3.0)

---

Uma ferramenta para registar e acompanhar o progresso de jogos cuja atualização demora bastante tempo. Originalmente criada para **Boom Beach**.

## Funcionalidades

- 🕒 Acompanhar tarefas de atualização em várias contas
- ⏰ Ao contrário de calendário/alarme, a contagem decrescente é sincronizada com o jogo, evitando calcular o tempo manualmente de cada vez
- 🔔 Mostrar notificação do sistema quando a atualização for concluída
- ♻️ Tarefas recorrentes: diárias / semanais / mensais / anuais / personalizadas; hora de término opcional (pré-definição: nenhuma); suporta regras de salto
- 🌐 Suporte para 27 idiomas

## Requisitos do Sistema

- [Windows 10](https://www.microsoft.com/en-ca/software-download/windows10) ou superior
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) ou superior

> Não sei se outras versões funcionarão :<

## Instalação

1. Descarregue a versão mais recente da página [Releases](https://github.com/YuanXiQWQ/Game-Upgrade-Reminder/releases)
2. Extraia para qualquer pasta
3. Execute `Game Upgrade Reminder.exe`

## Guia de Utilização

### Adicionar tarefa de atualização

1. Selecione a conta na parte superior da interface
2. Selecione ou crie um nome de tarefa (pode ser deixado em branco)
3. Configure o tempo necessário: hora de início, dias, horas, minutos (se não for definida, a hora de início padrão será a hora atual do sistema)
4. Clique no botão "Adicionar" para criar a tarefa

### Gerir tarefas

- As tarefas cujo prazo chegou serão destacadas; clique em "Concluir" para marcá-las como concluídas
- As tarefas podem ser removidas da lista, e a remoção pode ser desfeita em até três segundos

## Perguntas Frequentes

### Não recebo notificações do sistema

- Desative o **Assistente de Concentração (Focus Assist)** ou adicione `Game Upgrade Reminder.exe` à lista de prioridade. Se a regra automática estiver definida como "Apenas alarmes", altere para "Apenas notificações prioritárias".
- Fora isso, não sei

### Outros problemas estranhos

- Provavelmente é um bug, basta ignorar
- Pode ser reportado na página Issues, mas provavelmente não saberei como corrigir

## Licença

Este projeto está licenciado sob a [GNU Affero General Public License v3.0](../LICENSE).