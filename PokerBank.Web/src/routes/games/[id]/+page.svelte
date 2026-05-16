<script lang="ts">
	import type { PageData } from './$types';

	let { data, form }: { data: PageData; form: { error?: string; success?: boolean } | null } = $props();

	const playerNames = $derived(
		new Map(data.players.map((player) => [player.id, player.name] as const))
	);

	function money(value: number | string) {
		return `$${Number(value).toFixed(2)}`;
	}

	function signedMoney(value: number | string) {
		const amount = Number(value);
		const sign = amount > 0 ? '+' : amount < 0 ? '-' : '';

		return `${sign}$${Math.abs(amount).toFixed(2)}`;
	}
</script>

<svelte:head>
	<title>Game | PokerBank</title>
</svelte:head>

<a href="/" class="back-link">Back to balances</a>

<section class="page-header">
	<div>
		<p class="eyebrow">Game</p>
		<h1>{data.game.status}</h1>
		<p>{new Date(data.game.createdAtUtc).toLocaleString()}</p>
	</div>

	{#if data.game.status === 'Open'}
		<form method="POST" action="?/closeGame">
			<button type="submit">Close game</button>
		</form>
	{/if}
</section>

{#if form?.error}
	<p class="error">{form.error}</p>
{/if}

<section class="summary-grid">
	<div class="metric">
		<span>Buy-ins</span>
		<strong>{money(data.game.totalBuyInAmount)}</strong>
	</div>
	<div class="metric">
		<span>Cash-outs</span>
		<strong>{money(data.game.totalCashOutAmount)}</strong>
	</div>
	<div class="metric">
		<span>Remaining</span>
		<strong>{money(data.game.remainingCashOutAmount)}</strong>
	</div>
</section>

{#if data.game.status === 'Open'}
	<section class="grid">
		<div class="panel">
			<h2>Add buy-in</h2>
			<form method="POST" action="?/addBuyIn" class="entry-form">
				<label>
					Player
					<select name="playerId" required>
						<option value="">Choose player</option>
						{#each data.players as player}
							<option value={player.id}>{player.name}</option>
						{/each}
					</select>
				</label>

				<label>
					Amount
					<input name="amount" type="number" min="0.01" step="0.01" required />
				</label>

				<button type="submit">Add buy-in</button>
			</form>
		</div>

		<div class="panel">
			<h2>Add cash-out</h2>
			<form method="POST" action="?/addCashOut" class="entry-form">
				<label>
					Player
					<select name="playerId" required>
						<option value="">Choose player</option>
						{#each data.players as player}
							<option value={player.id}>{player.name}</option>
						{/each}
					</select>
				</label>

				<label>
					Amount
					<input name="amount" type="number" min="0.01" step="0.01" required />
				</label>

				<button type="submit">Add cash-out</button>
			</form>
		</div>
	</section>
{/if}

<section class="grid">
	<div class="panel">
		<h2>Entries</h2>

		{#if data.game.entries.length === 0}
			<p class="empty">No entries yet.</p>
		{:else}
			<div class="entry-list">
				{#each data.game.entries as entry}
					<div class="entry-row">
						<div>
							<strong>{playerNames.get(entry.playerId) ?? entry.playerId}</strong>
							<span>{entry.type}</span>
						</div>
						<span>{money(entry.amount)}</span>
					</div>
				{/each}
			</div>
		{/if}
	</div>

	<div class="panel">
		<h2>Results</h2>

		{#if data.results.length === 0}
			<p class="empty">Results appear after the game is closed.</p>
		{:else}
			<div class="entry-list">
				{#each data.results as result}
					<div class="entry-row">
						<div>
							<strong>{result.playerName}</strong>
							<span>{money(result.buyInAmount)} in / {money(result.cashOutAmount)} out</span>
						</div>
						<span class:positive={Number(result.netAmount) > 0} class:negative={Number(result.netAmount) < 0}>
							{signedMoney(result.netAmount)}
						</span>
					</div>
				{/each}
			</div>
		{/if}
	</div>
</section>
