# Rappel de mise à niveau de jeu

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](https://opensource.org/licenses/AGPL-3.0)

---

Un outil pour enregistrer et suivre la progression des jeux dont la mise à niveau prend beaucoup de temps. Initialement créé pour **Boom Beach**.

## Fonctionnalités

- 🕒 Suivi des tâches de mise à niveau sur plusieurs comptes
- ⏰ Contrairement aux calendriers/alarme, le compte à rebours est synchronisé avec le jeu, évitant ainsi de calculer le temps manuellement à chaque fois
- 🔔 Notification système lorsque la mise à niveau est terminée
- ♻️ Tâches récurrentes : quotidiennes / hebdomadaires / mensuelles / annuelles / personnalisées ; heure de fin optionnelle (par défaut : aucune) ; prise en charge des règles de saut
- 🌐 Prend en charge 27 langues

## Configuration requise

- [Windows 10](https://www.microsoft.com/en-ca/software-download/windows10) ou version ultérieure
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) ou version ultérieure

> Je ne sais pas si d'autres versions fonctionneront :<

## Installation

1. Téléchargez la dernière version depuis la page [Releases](https://github.com/YuanXiQWQ/Game-Upgrade-Reminder/releases)
2. Extrayez dans n’importe quel dossier
3. Exécutez `Game Upgrade Reminder.exe`

## Mode d’emploi

### Ajouter une tâche de mise à niveau

1. Sélectionnez le compte en haut de l’interface
2. Sélectionnez ou créez un nom de tâche (facultatif)
3. Définissez la durée de la mise à niveau : heure de début, jours, heures, minutes (si aucune heure de début n’est définie, l’heure actuelle du système sera utilisée par défaut)
4. Cliquez sur le bouton « Ajouter » pour créer la tâche

### Gérer les tâches

- Les tâches arrivées à échéance seront mises en surbrillance ; cliquez sur « Terminer » pour les marquer comme terminées
- Les tâches peuvent être supprimées de la liste, et la suppression peut être annulée dans les trois secondes

## FAQ

### Ne pas recevoir de notifications système

- Désactivez **Assistant de concentration (Focus Assist)** ou ajoutez `Game Upgrade Reminder.exe` à la liste des priorités. Si une règle automatique de l’assistant de concentration est définie sur « Alarmes uniquement », changez-la en « Priorité uniquement ».
- Sinon, je ne sais pas

### Autres problèmes étranges

- Probablement un bug, ignorez-le simplement
- Vous pouvez le signaler sur la page Issues, mais il est probable que je ne sache pas comment le corriger

## Licence

Ce projet est sous licence [GNU Affero General Public License v3.0](../LICENSE).