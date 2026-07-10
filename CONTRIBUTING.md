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

## Локални тайни (никога не се commit-ват)

`appsettings.Development.json` вече не съдържа `Jwt:Secret` и `ConnectionStrings:Postgres` — GitGuardian хвана и двете като hardcoded secrets в PR #7 (dev-only стойности, но repото е public, значи щяха да останат публично видими завинаги в git историята). Вместо това:

**Backend API** — през [`dotnet user-secrets`](https://learn.microsoft.com/aspnet/core/security/app-secrets) (съхранява се извън repo-то, per-machine):
```bash
cd backend/src/SecretSpots.Api
dotnet user-secrets set "Jwt:Secret" "<произволен base64 низ, поне 32 байта>"
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=secretspots;Username=secretspots;Password=secretspots_local_dev"
dotnet user-secrets set "R2:AccessKeyId" "<от Cloudflare R2 API token>"
dotnet user-secrets set "R2:SecretAccessKey" "<от Cloudflare R2 API token>"
```

### Настройка на Cloudflare R2 bucket (еднократно, за Photos slice-а)

1. Cloudflare акаунт → **R2** → Create bucket (произволно име, напр. `secretspots-photos`).
2. Bucket → Settings → Public access → включва се (стига default r2.dev subdomain-а за сега) → копира се публичния base URL.
3. R2 → Manage API tokens → create token с Object Read & Write права, ограничен до bucket-а → копират се Access Key ID + Secret Access Key (виж командите по-горе).
4. Account ID се вижда в R2 overview страницата на dashboard-а.
5. `AccountId`, `BucketName`, `PublicBaseUrl` (не са тайни) отиват в `appsettings.Development.json` под `"R2"` секцията.

**Тестовия проект** — през environment variable (тестовете хващат реален Postgres, не InMemory — виж `TestDbContextFactory.cs`):
```bash
export SECRETSPOTS_TEST_CONNECTION_STRING="Host=localhost;Port=5432;Database=secretspots_test;Username=secretspots;Password=secretspots_local_dev"
```

И двете стойности по-горе съвпадат с credentials-ите в `docker-compose.yml` — само локални, не продукционни тайни.

## Тестове преди PR

```bash
cd backend
dotnet build
export SECRETSPOTS_TEST_CONNECTION_STRING="Host=localhost;Port=5432;Database=secretspots_test;Username=secretspots;Password=secretspots_local_dev"
dotnet test
```

И двете трябва да минават чисто, преди да отвориш PR.
