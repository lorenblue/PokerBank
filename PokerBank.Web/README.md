# PokerBank.Web

SvelteKit frontend for PokerBank.

## Local Development

Start the API stack first:

```sh
docker compose up -d --build
```

Then run the web app:

```sh
npm run dev
```

The frontend defaults to `http://localhost:5186` for the API. Override it with:

```sh
POKERBANK_API_BASE_URL=http://localhost:5186 npm run dev
```

## API Types

The generated OpenAPI types live in `src/lib/api/schema.ts`.

Regenerate them while the API is running:

```sh
npm run generate:api
```

## Checks

```sh
npm run check
npm run build
```
