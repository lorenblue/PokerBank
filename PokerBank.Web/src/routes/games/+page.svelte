<script lang="ts">
	import type { PageData } from './$types';

	let { data, form }: { data: PageData; form: { error?: string } | null } = $props();

	const openGames = $derived(data.games.filter((game) => game.status === 'Open'));
	const closedGames = $derived(data.games.filter((game) => game.status !== 'Open'));
	const openGame = $derived(openGames[0]);
</script>

<svelte:head>
	<title>Games | PokerBank</title>
</svelte:head>

<section class="mb-6 flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
	<div>
		<h1 class="mt-1 text-4xl leading-none font-bold tracking-normal sm:text-6xl">Games</h1>
	</div>

	{#if openGame}
		<a
			href={`/games/${openGame.id}`}
			class="inline-flex rounded-md bg-emerald-900 px-4 py-3 font-bold text-white hover:bg-emerald-950"
		>
			Continue open game
		</a>
	{:else}
		<form method="POST" action="?/createGame">
			<button
				type="submit"
				class="rounded-md bg-emerald-900 px-4 py-3 font-bold text-white hover:bg-emerald-950"
			>
				New game
			</button>
		</form>
	{/if}
</section>

{#if form?.error}
	<p class="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-700">{form.error}</p>
{/if}

<section class="grid gap-4 lg:grid-cols-2">
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<h2 class="mb-4 text-base font-bold">Open</h2>

		{#if openGames.length === 0}
			<p class="text-sm text-slate-500">No open games.</p>
		{:else}
			<div class="grid gap-3">
				{#each openGames as game}
					<a
						href={`/games/${game.id}`}
						class="flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-3 hover:bg-slate-50"
					>
						<span class="text-sm text-slate-500">{new Date(game.createdAtUtc).toLocaleString()}</span>
						<strong>{game.status}</strong>
					</a>
				{/each}
			</div>
		{/if}
	</div>

	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<h2 class="mb-4 text-base font-bold">Closed</h2>

		{#if closedGames.length === 0}
			<p class="text-sm text-slate-500">No closed games.</p>
		{:else}
			<div class="grid gap-3">
				{#each closedGames as game}
					<a
						href={`/games/${game.id}`}
						class="flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-3 hover:bg-slate-50"
					>
						<span class="text-sm text-slate-500">{new Date(game.createdAtUtc).toLocaleString()}</span>
						<strong>{game.status}</strong>
					</a>
				{/each}
			</div>
		{/if}
	</div>
</section>
