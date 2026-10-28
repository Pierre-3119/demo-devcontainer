# API .NET 10 — SQL Server & Redis (devcontainer)

Ouvrir dans GitHub Codespaces (ou VS Code « Reopen in Container »). Au premier démarrage, le `postCreateCommand` restaure `dotnet-ef` et applique les migrations EF Core sur SQL Server.

## Lancer l'API

```bash
dotnet run --project src/Api
```

Puis ouvrir le port **5080** (onglet *Ports*) : la racine redirige vers Scalar (`/scalar`).

## Services

| Service   | Hôte (depuis le conteneur) | Persistance (volume)  |
|-----------|----------------------------|-----------------------|
| SQL Server| `sqlserver,1433` (sa / voir `.devcontainer/.env`) | `sqlserver-data` |
| Redis     | `redis:6379`               | `redis-data` (AOF)    |

- Extension SQL Server : la connexion `devcontainer (sqlserver)` est préconfigurée.
- Extension Redis : ajouter une connexion vers l'hôte `redis`, port `6379`.

## Cache

`GET /products/{id}` passe par Redis (cache-aside, 5 min). L'en-tête `X-Cache` vaut `MISS` puis `HIT` ; `PUT` et `DELETE` invalident l'entrée.

## Nouvelle migration

```bash
dotnet ef migrations add <Nom> --project src/Api
dotnet ef database update --project src/Api
```
