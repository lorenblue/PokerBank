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

	function amountClass(value: number | string) {
		const amount = Number(value);

		if (amount > 0) return 'amount-positive';
		if (amount < 0) return 'amount-negative';

		return 'amount-neutral';
	}
</script>

<svelte:head>
	<title>My Games | PokerBank</title>
</svelte:head>

<section class="page-header">
	<div>
		<h1 class="page-title">My Games</h1>
	</div>
</section>

<section class="grid-two">
	<div class="card card-pad">
		<div class="section-head">
			<h2 class="section-title">Open</h2>
			<span class="badge badge-open">{openGames.length}</span>
		</div>

		{#if openGames.length === 0}
			<p class="empty-state">No open games.</p>
		{:else}
			<div class="data-list">
				{#each openGames as game}
					<a href={`/games/${game.id}`} class="data-row">
						<div>
							<strong class="row-title">{formatDateTime(game.playedAtUtc)}</strong>
							<p class="row-meta">
								{game.playerCount} {Number(game.playerCount) === 1 ? 'player' : 'players'}
							</p>
						</div>
						<strong class={`amount ${amountClass(game.myNetAmount)}`}>{money(game.myNetAmount)}</strong>
					</a>
				{/each}
			</div>
		{/if}
	</div>

	<div class="card card-pad">
		<div class="section-head">
			<h2 class="section-title">Closed</h2>
			<span class="badge badge-closed">{closedGames.length}</span>
		</div>

		{#if closedGames.length === 0}
			<p class="empty-state">No closed games.</p>
		{:else}
			<div class="data-list">
				{#each closedGames as game}
					<a href={`/games/${game.id}`} class="data-row">
						<div>
							<strong class="row-title">{formatDateTime(game.playedAtUtc)}</strong>
							<p class="row-meta">
								{game.playerCount} {Number(game.playerCount) === 1 ? 'player' : 'players'}
							</p>
						</div>
						<strong class={`amount ${amountClass(game.myNetAmount)}`}>{money(game.myNetAmount)}</strong>
					</a>
				{/each}
			</div>
		{/if}
	</div>
</section>
