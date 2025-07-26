# Contributing to LianLiProfileWatcher

Merci de votre intérêt ! Voici comment contribuer :

## Workflow de branches

- **main** : version stable
- **feature/*** : nouvelles fonctionnalités
- **fix/*** : corrections de bugs
- **chore/*** : tâches diverses (ci, docs, refactoring)

1. Fork du repo et clone (**`git clone …`**).
2. Créez une branche :  

   ```bash
      git checkout -b feature/nom-de-ma-fonctionnalite
   ```

3. Code selon les guidelines (voir Style de code).
4. Testez localement (**`dotnet test`**).
5. Committez avec Conventional Commits :

   ```scss
   feat(worker): add debounce for WinEvent hook
   ```

6. Poussez et ouvrez une Pull Request vers **`main`**.
7. Attendez le pipeline CI, corrigez si besoin, puis mergez.

## Style de code

- C# 10, .NET 9.0
- Pas de tabs, 4 espaces
- Noms PascalCase pour classes, méthodes ; camelCase pour variables privées
- Usings groupés et triés
- Respecter les commentaires XML pour les APIs publiques

## Tests

- Couverture minimale : **80%**
- Placer les tests dans **`tests/LianLiProfileWatcher.Tests`**
