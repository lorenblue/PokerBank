<script lang="ts">
	import type { PageData } from './$types';

	let { data, form }: { data: PageData; form: { error?: string } | null } = $props();

	const openGames = $derived(data.games.filter((game) => game.status === 'Open'));
	const closedGames = $derived(data.games.filter((game) => game.status !== 'Open'));
	const openGame = $derived(openGames[0]);

	function money(value: number | string) {
		const amount = Number(value);
		const sign = amount > 0 ? '+' : amount < 0 ? '-' : '';

		return `${sign}$${Math.abs(amount).toFixed(2)}`;
	}

	function balanceLabel(value: number | string) {
		const amount = Number(value);

		if (amount > 0) return 'should receive';
		if (amount < 0) return 'owes';

		return 'settled';
	}
</script>

<svelte:head>
	<title>PokerBank</title>
</svelte:head>

<section class="page-header">
	<div>
		<p class="eyebrow">PokerBank</p>
		<h1>Dashboard</h1>
	</div>

	{#if openGame}
		<a href={`/games/${openGame.id}`} class="button-link">Continue open game</a>
	{:else}
		<form method="POST" action="?/createGame">
			<button type="submit">New game</button>
		</form>
	{/if}
</section>

{#if form?.error}
	<p class="error">{form.error}</p>
{/if}

<section class="grid">
	<div class="panel balances-panel">
		<div class="panel-header">
			<h2>Current balances</h2>
		</div>

		{#if data.balances.length === 0}
			<p class="empty">No players yet.</p>
		{:else}
			<div class="balance-list">
				{#each data.balances as balance}
					<article class="balance-row" class:inactive={!balance.isActive}>
						<div>
							<h3>{balance.playerName}</h3>
							<p>{balanceLabel(balance.balanceAmount)}</p>
						</div>
						<strong class:positive={Number(balance.balanceAmount) > 0} class:negative={Number(balance.balanceAmount) < 0}>
							{money(balance.balanceAmount)}
						</strong>
					</article>
				{/each}
			</div>
		{/if}
	</div>

	<div class="panel">
		<div class="panel-header">
			<h2>Games</h2>
		</div>

		<div class="game-section">
			<h3>Open</h3>
			{#if openGames.length === 0}
				<p class="empty">No open games.</p>
			{:else}
				<div class="game-list">
					{#each openGames as game}
						<a href={`/games/${game.id}`} class="game-row">
							<span>{new Date(game.createdAtUtc).toLocaleString()}</span>
							<strong>{game.status}</strong>
						</a>
					{/each}
				</div>
			{/if}
		</div>

		<div class="game-section">
			<h3>Closed</h3>
			{#if closedGames.length === 0}
				<p class="empty">No closed games.</p>
			{:else}
				<div class="game-list">
					{#each closedGames as game}
						<a href={`/games/${game.id}`} class="game-row">
							<span>{new Date(game.createdAtUtc).toLocaleString()}</span>
							<strong>{game.status}</strong>
						</a>
					{/each}
				</div>
			{/if}
		</div>
	</div>
</section>
