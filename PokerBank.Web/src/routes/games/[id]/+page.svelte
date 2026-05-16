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

<a href="/" class="mb-4 inline-block font-bold text-emerald-900">Back to balances</a>

<section class="mb-6 flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
	<div>
		<p class="text-xs font-extrabold tracking-wider text-emerald-900 uppercase">Game</p>
		<h1 class="mt-1 text-4xl leading-none font-bold tracking-normal sm:text-6xl">{data.game.status}</h1>
		<p class="mt-2 text-slate-500">{new Date(data.game.createdAtUtc).toLocaleString()}</p>
	</div>

	{#if data.game.status === 'Open'}
		<div class="flex flex-wrap justify-end gap-3">
			<form method="POST" action="?/deleteGame">
				<button type="submit" class="rounded-md bg-red-700 px-4 py-3 font-bold text-white hover:bg-red-800">
					Delete game
				</button>
			</form>

			<form method="POST" action="?/closeGame">
				<button
					type="submit"
					class="rounded-md bg-emerald-900 px-4 py-3 font-bold text-white hover:bg-emerald-950"
				>
					Close game
				</button>
			</form>
		</div>
	{/if}
</section>

{#if form?.error}
	<p class="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-700">{form.error}</p>
{/if}

<section class="my-4 grid gap-4 md:grid-cols-3">
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<span class="mb-1 block text-sm text-slate-500">Buy-ins</span>
		<strong class="text-2xl">{money(data.game.totalBuyInAmount)}</strong>
	</div>
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<span class="mb-1 block text-sm text-slate-500">Cash-outs</span>
		<strong class="text-2xl">{money(data.game.totalCashOutAmount)}</strong>
	</div>
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<span class="mb-1 block text-sm text-slate-500">Remaining</span>
		<strong class="text-2xl">{money(data.game.remainingCashOutAmount)}</strong>
	</div>
</section>

{#if data.game.status === 'Open'}
	<section class="mt-4 grid gap-4 lg:grid-cols-2">
		<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
			<h2 class="mb-4 text-base font-bold">Add buy-in</h2>
			<form method="POST" action="?/addBuyIn" class="grid gap-3">
				<label class="grid gap-1 text-sm font-bold text-slate-700">
					Player
					<select name="playerId" required class="rounded-md border border-slate-300 px-3 py-2">
						<option value="">Choose player</option>
						{#each data.players as player}
							<option value={player.id}>{player.name}</option>
						{/each}
					</select>
				</label>

				<label class="grid gap-1 text-sm font-bold text-slate-700">
					Amount
					<input
						name="amount"
						type="number"
						min="0.01"
						step="0.01"
						required
						class="rounded-md border border-slate-300 px-3 py-2"
					/>
				</label>

				<button type="submit" class="rounded-md bg-emerald-900 px-4 py-3 font-bold text-white hover:bg-emerald-950">
					Add buy-in
				</button>
			</form>
		</div>

		<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
			<h2 class="mb-4 text-base font-bold">Add cash-out</h2>
			<form method="POST" action="?/addCashOut" class="grid gap-3">
				<label class="grid gap-1 text-sm font-bold text-slate-700">
					Player
					<select name="playerId" required class="rounded-md border border-slate-300 px-3 py-2">
						<option value="">Choose player</option>
						{#each data.players as player}
							<option value={player.id}>{player.name}</option>
						{/each}
					</select>
				</label>

				<label class="grid gap-1 text-sm font-bold text-slate-700">
					Amount
					<input
						name="amount"
						type="number"
						min="0.01"
						step="0.01"
						required
						class="rounded-md border border-slate-300 px-3 py-2"
					/>
				</label>

				<button type="submit" class="rounded-md bg-emerald-900 px-4 py-3 font-bold text-white hover:bg-emerald-950">
					Add cash-out
				</button>
			</form>
		</div>
	</section>
{/if}

<section class="mt-4 grid gap-4 lg:grid-cols-2">
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<h2 class="mb-4 text-base font-bold">Entries</h2>

		{#if data.game.entries.length === 0}
			<p class="text-sm text-slate-500">No entries yet.</p>
		{:else}
			<div class="grid gap-3">
				{#each data.game.entries as entry}
					<div class="flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-3">
						<div>
							<strong>{playerNames.get(entry.playerId) ?? entry.playerId}</strong>
							<span class="mt-1 block text-sm text-slate-500">{entry.type}</span>
						</div>
						<span>{money(entry.amount)}</span>
					</div>
				{/each}
			</div>
		{/if}
	</div>

	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<h2 class="mb-4 text-base font-bold">Results</h2>

		{#if data.results.length === 0}
			<p class="text-sm text-slate-500">Results appear after the game is closed.</p>
		{:else}
			<div class="grid gap-3">
				{#each data.results as result}
					<div class="flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-3">
						<div>
							<strong>{result.playerName}</strong>
							<span class="mt-1 block text-sm text-slate-500">
								{money(result.buyInAmount)} in / {money(result.cashOutAmount)} out
							</span>
						</div>
						<span
							class={`font-bold ${Number(result.netAmount) > 0 ? 'text-emerald-700' : Number(result.netAmount) < 0 ? 'text-red-700' : ''}`}
						>
							{signedMoney(result.netAmount)}
						</span>
					</div>
				{/each}
			</div>
		{/if}
	</div>
</section>
