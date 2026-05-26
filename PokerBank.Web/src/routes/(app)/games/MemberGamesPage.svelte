<script lang="ts">
	import { formatDateTime } from '$lib/format';
	import type { PageData } from './$types';

	type MemberGame = Extract<PageData['games'][number], { playedAtUtc: string }>;

	let { data }: { data: PageData } = $props();

	const games = $derived(data.games.filter((game): game is MemberGame => 'playedAtUtc' in game));
	const openGames = $derived(games.filter((game) => game.status === 'Open'));
	const closedGames = $derived(games.filter((game) => game.status !== 'Open'));

	function money(value: number | string) {
		const amount = Number(value);
		const sign = amount > 0 ? '+' : amount < 0 ? '-' : '';

		return `${sign}$${Math.abs(amount).toFixed(2)}`;
	}
</script>

<svelte:head>
	<title>My Games | PokerBank</title>
</svelte:head>

<section class="mb-6">
	<h1 class="mt-1 text-4xl leading-none font-bold tracking-normal sm:text-6xl">My Games</h1>
</section>

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
						<div>
							<span class="text-sm text-slate-500">{formatDateTime(game.playedAtUtc)}</span>
							<p class="mt-1 text-xs text-slate-500">
								{game.playerCount} {Number(game.playerCount) === 1 ? 'player' : 'players'}
							</p>
						</div>
						<strong
							class={`text-right ${Number(game.myNetAmount) > 0 ? 'text-emerald-700' : Number(game.myNetAmount) < 0 ? 'text-red-700' : ''}`}
						>
							{money(game.myNetAmount)}
						</strong>
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
						<div>
							<span class="text-sm text-slate-500">{formatDateTime(game.playedAtUtc)}</span>
							<p class="mt-1 text-xs text-slate-500">
								{game.playerCount} {Number(game.playerCount) === 1 ? 'player' : 'players'}
							</p>
						</div>
						<strong
							class={`text-right ${Number(game.myNetAmount) > 0 ? 'text-emerald-700' : Number(game.myNetAmount) < 0 ? 'text-red-700' : ''}`}
						>
							{money(game.myNetAmount)}
						</strong>
					</a>
				{/each}
			</div>
		{/if}
	</div>
</section>
