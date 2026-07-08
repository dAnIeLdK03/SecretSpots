# Workflow

Проектът е соло, но минава през същата дисциплина като екипна разработка — `main` е защитен (GitHub ruleset), директен `git push` към него се отхвърля. Всяка промяна влиза само през Pull Request.

## Цикъл на една задача

1. **Избери/създай issue** в [Issues](https://github.com/dAnIeLdK03/SecretSpots/issues) — всяка смислена задача (нов feature slice, поправка, рефакторинг) си има issue, преди да почне работа по нея.
2. **Клонирай branch от `main`**, именуван по темата на issue-то:
   ```bash
   git checkout main
   git pull
   git checkout -b feature/auth-register-login
   ```
   Именуване: `feature/<кратко-описание>` за нова функционалност, `fix/<кратко-описание>` за бъгфикс, `chore/<кратко-описание>` за инфраструктура/конфигурация.
3. **Commit-вай** с кратки, ясни съобщения в imperative mood (`Add`, `Fix`, `Refactor`, не `Added`/`Fixed`). Reference-ни issue номера, когато е relevant.
4. **Push-ни branch-а**:
   ```bash
   git push -u origin feature/auth-register-login
   ```
5. **Отвори Pull Request** към `main` (`gh pr create` или през GitHub). В описанието добави `Closes #<issue-номер>`, за да се затвори issue-то автоматично при merge.
6. **Прегледай diff-а** сам през GitHub-ския PR изглед (или помоли Claude за `/code-review` на branch-а) — точно това е моментът за "одобрение" преди да влезе в `main`.
7. **Merge** (squash препоръчително, за чиста история на `main`) и изтрий branch-а.

## Защо няма required review count

GitHub изисква поне 1 одобрение от **друг** акаунт, за да работи "Require approvals" — self-approval на собствен PR не се брои. Repото е соло, затова ruleset-ът изисква PR (блокира директен push), но не изисква формално approval — прегледът става от теб самия през PR diff-а, преди да натиснеш "Merge".

## Тестове преди PR

```bash
cd backend
dotnet build
dotnet test
```

И двете трябва да минават чисто, преди да отвориш PR.
