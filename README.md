# PokerBank

PokerBank helps small poker groups play without cash on hand. It records buy-ins, cash-outs, and real-money settlement payments so everyone can see what they owe or are owed after each game.

The app is built around a feature-oriented ASP.NET Core API, a small domain model, EF Core with PostgreSQL, integration tests, OpenAPI/Scalar, Docker Compose, and OpenTelemetry through the Aspire dashboard.

## Features

- Create, delete, close, and view poker games
- Record game buy-ins and cash-outs
- Track player-level game results
- Track settlement payments made by or received by players
- View balances across game results and payments
- Manage players, including optional contact email addresses
- Sign in with group-scoped manager/member access
- Send balance update emails to players

## Tech Stack

- .NET 10
- ASP.NET Core minimal APIs
- EF Core 10
- PostgreSQL 18
- xUnit and Testcontainers
- Scalar for API testing
- OpenTelemetry with the Aspire dashboard
- SvelteKit, Tailwind CSS, and `openapi-fetch`

## Architecture Notes

The API is organized by feature slices under `PokerBank.Api/Features`. Each slice owns its endpoint mapping, request/response types, and handler logic.

The domain project contains the core business objects:

- `PokerGame`
- `GameEntry`
- `Payment`
- `Player`
- `Money`

The domain handles local rules such as valid money amounts, game close rules, player email validation, and payment direction. Query-heavy screens such as balances and game results are projected with EF Core so the database does the aggregation work.

Authentication uses ASP.NET Core Identity. Access is scoped to a poker group, with group roles used to separate manager workflows from member-facing views.

## Run Locally

Start the full local stack:

```sh
docker compose up --build
```

Useful URLs:

- Web app: `http://localhost:5173`
- API: `http://localhost:5186`
- OpenAPI JSON: `http://localhost:5186/openapi/v1.json`
- Scalar UI: `http://localhost:5186/scalar/v1`
- Aspire dashboard: `http://localhost:18888`

PostgreSQL is exposed locally for database tools:

```txt
Host: localhost
Port: 54329
Database: pokerbank
Username: pokerbank
Password: pokerbank
```

EF Core migrations are applied by the API on startup.

### Development Login

Docker Compose configures a default owner account for local development:

```txt
Email: admin@pokerbank.local
Password: PokerBank123!
```

Override `AUTHENTICATION__ADMINEMAIL` and `AUTHENTICATION__ADMINPASSWORD` in `.env` if you want different local credentials. The API only seeds an owner account when both values are configured.

### Email

Balance update emails use `LoggingEmailSender` by default, so local sends are written to API logs instead of actually being sent.

To test SMTP through Docker Compose, copy `.env.example` to `.env` at the repository root and fill in the SMTP values:

```sh
cp .env.example .env
```

```txt
EMAIL__SMTP__ENABLED=true
EMAIL__SMTP__HOST=smtp.example.com
EMAIL__SMTP__PORT=587
EMAIL__SMTP__USERNAME=...
EMAIL__SMTP__PASSWORD=...
EMAIL__SMTP__FROMEMAIL=pokerbank@example.com
EMAIL__SMTP__FROMNAME=PokerBank
EMAIL__SMTP__SECURESOCKETOPTIONS=StartTls
```

The API requires `Host` and `FromEmail` when SMTP is enabled. `UserName` and `Password` must either both be configured or both be omitted.

## Development

Run backend build and tests:

```sh
dotnet build PokerBank.slnx
dotnet test PokerBank.slnx
```

Run frontend checks:

```sh
cd PokerBank.Web
npm ci
npm run check
npm run build
```

Regenerate frontend API types while the API is running:

```sh
cd PokerBank.Web
npm run generate:api
```
