# TCP Chat - Projet Socket C#

Ce dépôt contient deux implémentations distinctes de tchat en C# basées sur des sockets TCP :

- `ClientWeb` / `ServerWeb` : tchat en temps réel sans authentification
- `AuthClient` / `AuthServer` : tchat non temps réel structuré, avec authentification et stockage en base de données

## Structure du projet

### ClientWeb / ServerWeb

Tchat simple, en temps réel, sans authentification.

- Communication directe entre clients et serveur via sockets TCP.
- Chaque message est envoyé immédiatement à tous les autres clients connectés.
- Pas de format de message structuré (texte brut).
- Convient pour des démonstrations ou expérimentations réseau bas niveau.

### AuthClient / AuthServer

Tchat structuré, non temps réel, basé sur l'échange de données JSON via sockets TCP.

- Communication via paquets JSON avec champ `route` + `data`.
- Gestion de l'authentification (`login`, `register`) avec persistance des utilisateurs.
- Support des canaux (channels) pour organiser les conversations.
- Historique des messages enregistré dans une base MariaDB.
- Désérialisation dynamique des paquets selon la route (`AddRoute<T>`).
- Système extensible avec typage fort et routage générique.

## Technologies utilisées

- .NET 9
- System.Net.Sockets pour la communication TCP
- System.Text.Json pour la sérialisation / désérialisation JSON
- Pomelo.EntityFrameworkCore.MySql pour l'accès à MariaDB via Entity Framework Core
