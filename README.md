# SecretSpots

Локален гид за скрити съкровища — мобилно и уеб приложение за споделяне и откриване на тайни, красиви или нестандартни места (водопади, гледки, скрити кафенета, изоставени сгради), които не се намират лесно в Google Maps или стандартните пътеводители.

## MVP обхват

Разработката е разделена на фази. Първата (текущата) фаза покрива само основния цикъл:

1. **Auth** — регистрация/логин, JWT (access + refresh), BCrypt.
2. **Spots** — създаване на място (снимка, GPS, категория, описание) + гео-търсене наблизо.
3. **CheckIns** — check-in със снимка, сървърна проверка на разстоянието до мястото, награда с кристали.

Rewards/Businesses (оферти, промотирани обяви) и Chat/Notifications (SignalR) са съзнателно оставени за следваща фаза — изискват двустранен пазар (бизнеси) и real-time инфраструктура, които нямат смисъл преди да е валидиран основният цикъл.

## Технологичен стек

**Backend**
- ASP.NET Core 8, Minimal APIs, C# 12
- EF Core 8 + Npgsql, PostgreSQL 16 + PostGIS (NetTopologySuite за геометрия)
- Собствен минимален mediator (виж `SecretSpots.Features/Common/Mediator`) — **не** MediatR, за да няма зависимост от библиотека с несигурно лицензионно бъдеще
- FluentValidation, включена в mediator pipeline-а
- Ръчни `ToDto()` мапъри — **не** AutoMapper, същата причина
- JWT Bearer (access + refresh) + BCrypt

**Уеб (по-късно)**: Next.js + TypeScript, Tailwind, MapLibre GL JS, Zustand.
**Мобилно (по-късно)**: React Native (Expo), `@maplibre/maplibre-react-native`.

Пълният контекст зад тези избори (включително защо не MediatR/AutoMapper/Mapbox) е документиран в историята на разговорите с Claude по проекта.

## Архитектура: Vertical Slice

Бизнес логиката е организирана около features, не около технически слоеве. Всяка операция (команда/заявка) живее в един файл — Command/Query + Validator + Handler заедно — в папката на своя feature.

```
backend/
├── SecretSpots.sln
├── src/
│   ├── SecretSpots.Domain/      # чисти entities + value objects, нулеви зависимости
│   ├── SecretSpots.Features/    # вертикалните срезове: Auth, Spots, CheckIns, Rewards, Businesses
│   │   └── Common/              # Mediator, IAppDbContext/AppDbContext, JWT service
│   └── SecretSpots.Api/         # Program.cs, endpoint mapping, appsettings
└── tests/
    └── SecretSpots.Features.Tests/
```

Референции вървят в една посока: `Api → Features → Domain`. Domain никога не reference-ва Features или Api.

Няма repository слой над EF Core — features-ите ползват `IAppDbContext` директно.

## Стартиране локално

Изисквания: .NET 8 SDK, Docker + Docker Compose.

```bash
# 1. Вдигни PostgreSQL + PostGIS
docker compose up -d

# 2. Build + тестове
cd backend
dotnet build
dotnet test

# 3. Стартирай API-то
dotnet run --project src/SecretSpots.Api

# 4. Провери
curl http://localhost:5080/health   # {"status":"ok"}
```

Connection string-ът за локална разработка е в `backend/src/SecretSpots.Api/appsettings.Development.json` и съвпада с credentials-ите в `docker-compose.yml` (само за локална употреба, не са продукционни тайни).

## Как се работи по проекта

Виж [CONTRIBUTING.md](./CONTRIBUTING.md) за git workflow-а (issue → branch → PR → merge). `main` е защитен — директен push е забранен, всичко минава през Pull Request.
