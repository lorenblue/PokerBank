<script lang="ts">
	import { formatDateTime, formatGameEntryDateTime } from '$lib/format';
	import type { PageData } from './$types';

	let { data, form }: { data: PageData; form: { error?: string; success?: boolean } | null } = $props();

	let isAddBuyInOpen = $state(false);
	let isAddCashOutOpen = $state(false);
	let isDeleteGameOpen = $state(false);
	let isCloseGameOpen = $state(false);
	let entryToDelete = $state<PageData['game']['entries'][number] | null>(null);

	const playerNames = $derived(
		new Map(data.players.map((player) => [player.id, player.name] as const))
	);
	const canCloseGame = $derived(Number(data.game.remainingCashOutAmount) === 0);

	function money(value: number | string) {
		return `$${Number(value).toFixed(2)}`;
	}

	function signedMoney(value: number | string) {
		const amount = Number(value);
		const sign = amount > 0 ? '+' : amount < 0 ? '-' : '';

		return `${sign}$${Math.abs(amount).toFixed(2)}`;
	}

	function entryLabel(type: string) {
		if (type === 'BuyIn') return 'Buy-in';
		if (type === 'CashOut') return 'Cash-out';

		return type;
	}

	function closeDeleteEntry() {
		entryToDelete = null;
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
		<p class="mt-2 text-slate-500">{formatDateTime(data.game.createdAtUtc)}</p>
	</div>

	{#if data.game.status === 'Open'}
		<div class="flex flex-wrap justify-end gap-3">
			<button
				type="button"
				disabled={data.players.length === 0}
				class="rounded-md border border-slate-300 px-4 py-3 font-bold text-slate-700 hover:bg-slate-50 disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-400"
				onclick={() => (isAddBuyInOpen = true)}
			>
				Add buy-in
			</button>
			<button
				type="button"
				disabled={data.players.length === 0}
				class="rounded-md bg-emerald-900 px-4 py-3 font-bold text-white hover:bg-emerald-950 disabled:cursor-not-allowed disabled:bg-slate-300 disabled:text-slate-500"
				onclick={() => (isAddCashOutOpen = true)}
			>
				Add cash-out
			</button>
			<button
				type="button"
				class="rounded-md bg-red-700 px-4 py-3 font-bold text-white hover:bg-red-800"
				onclick={() => (isDeleteGameOpen = true)}
			>
				Delete game
			</button>

			<button
				type="button"
				disabled={!canCloseGame}
				title={canCloseGame ? 'Close game' : 'Buy-ins must equal cash-outs before closing.'}
				class="rounded-md bg-emerald-900 px-4 py-3 font-bold text-white hover:bg-emerald-950 disabled:cursor-not-allowed disabled:bg-slate-300 disabled:text-slate-500"
				onclick={() => (isCloseGameOpen = true)}
			>
				Close game
			</button>
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

<section class="mt-4 grid gap-4 lg:grid-cols-2">
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<h2 class="mb-4 text-base font-bold">Entries</h2>

		{#if data.game.entries.length === 0}
			<p class="text-sm text-slate-500">No entries yet.</p>
		{:else}
			<div class="grid gap-3">
				{#each data.game.entries as entry}
					<div class="flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-3">
						<div class="min-w-0">
							<strong>{playerNames.get(entry.playerId) ?? entry.playerId}</strong>
							<span class="mt-1 block text-sm text-slate-500">
								{entryLabel(entry.type)} · {formatGameEntryDateTime(
									entry.recordedAtUtc,
									data.game.createdAtUtc
								)}
							</span>
						</div>
						<div class="flex shrink-0 items-center gap-3">
							<span
								class={`font-bold ${entry.type === 'CashOut' ? 'text-emerald-700' : 'text-slate-950'}`}
							>
								{money(entry.amount)}
							</span>
							{#if data.game.status === 'Open'}
								<button
									type="button"
									class="rounded-md px-2 py-1 text-xs font-bold text-red-700 hover:bg-red-50"
									onclick={() => (entryToDelete = entry)}
								>
									Delete
								</button>
							{/if}
						</div>
					</div>
				{/each}
			</div>
		{/if}
	</div>

	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<h2 class="mb-4 text-base font-bold">Player totals</h2>

		{#if data.game.playerTotals.length === 0}
			<p class="text-sm text-slate-500">Player totals appear after entries are added.</p>
		{:else}
			<div class="grid gap-3">
				{#each data.game.playerTotals as total}
					<div class="flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-3">
						<div class="min-w-0">
							<strong>{total.playerName}</strong>
							<span class="mt-1 block text-sm text-slate-500">
								Buy-ins {money(total.buyInAmount)} · Cash-outs {money(total.cashOutAmount)}
							</span>
						</div>
						<span
							class={`shrink-0 font-bold ${Number(total.netAmount) > 0 ? 'text-emerald-700' : Number(total.netAmount) < 0 ? 'text-red-700' : ''}`}
						>
							{signedMoney(total.netAmount)}
						</span>
					</div>
				{/each}
			</div>
		{/if}
	</div>
</section>

{#if isAddBuyInOpen}
	<div class="fixed inset-0 z-50 grid place-items-center bg-slate-950/35 p-4">
		<div class="w-full max-w-md rounded-lg bg-white p-5 shadow-xl">
			<div class="mb-4 flex items-start justify-between gap-4">
				<h2 class="text-lg font-bold text-slate-950">Add buy-in</h2>
				<button
					type="button"
					class="rounded-md px-2 py-1 text-sm font-bold text-slate-500 hover:bg-slate-100 hover:text-slate-900"
					aria-label="Close"
					onclick={() => (isAddBuyInOpen = false)}
				>
					Close
				</button>
			</div>

			<form method="POST" action="?/addBuyIn" class="grid gap-4">
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

				<div class="flex justify-end gap-2">
					<button
						type="button"
						class="rounded-md px-4 py-2 text-sm font-bold text-slate-700 hover:bg-slate-50"
						onclick={() => (isAddBuyInOpen = false)}
					>
						Cancel
					</button>
					<button
						type="submit"
						class="rounded-md bg-emerald-900 px-4 py-2 text-sm font-bold text-white hover:bg-emerald-950"
					>
						Add buy-in
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}

{#if isAddCashOutOpen}
	<div class="fixed inset-0 z-50 grid place-items-center bg-slate-950/35 p-4">
		<div class="w-full max-w-md rounded-lg bg-white p-5 shadow-xl">
			<div class="mb-4 flex items-start justify-between gap-4">
				<h2 class="text-lg font-bold text-slate-950">Add cash-out</h2>
				<button
					type="button"
					class="rounded-md px-2 py-1 text-sm font-bold text-slate-500 hover:bg-slate-100 hover:text-slate-900"
					aria-label="Close"
					onclick={() => (isAddCashOutOpen = false)}
				>
					Close
				</button>
			</div>

			<form method="POST" action="?/addCashOut" class="grid gap-4">
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

				<div class="flex justify-end gap-2">
					<button
						type="button"
						class="rounded-md px-4 py-2 text-sm font-bold text-slate-700 hover:bg-slate-50"
						onclick={() => (isAddCashOutOpen = false)}
					>
						Cancel
					</button>
					<button
						type="submit"
						class="rounded-md bg-emerald-900 px-4 py-2 text-sm font-bold text-white hover:bg-emerald-950"
					>
						Add cash-out
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}

{#if entryToDelete}
	<div class="fixed inset-0 z-50 grid place-items-center bg-slate-950/35 p-4">
		<div class="w-full max-w-md rounded-lg bg-white p-5 shadow-xl">
			<h2 class="text-lg font-bold text-slate-950">Delete entry?</h2>
			<p class="mt-2 text-sm leading-6 text-slate-600">
				This removes the {entryLabel(entryToDelete.type).toLowerCase()} from the open game. This
				is only intended for entries recorded by mistake.
			</p>

			<div class="mt-4 rounded-lg border border-slate-100 bg-slate-50 p-3 text-sm">
				<p class="font-bold text-slate-950">
					{playerNames.get(entryToDelete.playerId) ?? entryToDelete.playerId}
				</p>
				<p class="mt-1 text-slate-600">
					{entryLabel(entryToDelete.type)} · {money(entryToDelete.amount)}
				</p>
			</div>

			<form method="POST" action="?/deleteEntry" class="mt-5 flex justify-end gap-2">
				<input type="hidden" name="entryId" value={entryToDelete.id} />
				<button
					type="button"
					class="rounded-md px-4 py-2 text-sm font-bold text-slate-700 hover:bg-slate-50"
					onclick={closeDeleteEntry}
				>
					Cancel
				</button>
				<button
					type="submit"
					class="rounded-md bg-red-700 px-4 py-2 text-sm font-bold text-white hover:bg-red-800"
				>
					Delete entry
				</button>
			</form>
		</div>
	</div>
{/if}

{#if isDeleteGameOpen}
	<div class="fixed inset-0 z-50 grid place-items-center bg-slate-950/35 p-4">
		<div class="w-full max-w-md rounded-lg bg-white p-5 shadow-xl">
			<h2 class="text-lg font-bold text-slate-950">Delete game?</h2>
			<p class="mt-2 text-sm leading-6 text-slate-600">
				This removes the open game and its entries. This is only intended for games created by mistake.
			</p>

			<form method="POST" action="?/deleteGame" class="mt-5 flex justify-end gap-2">
				<button
					type="button"
					class="rounded-md px-4 py-2 text-sm font-bold text-slate-700 hover:bg-slate-50"
					onclick={() => (isDeleteGameOpen = false)}
				>
					Cancel
				</button>
				<button
					type="submit"
					class="rounded-md bg-red-700 px-4 py-2 text-sm font-bold text-white hover:bg-red-800"
				>
					Delete game
				</button>
			</form>
		</div>
	</div>
{/if}

{#if isCloseGameOpen}
	<div class="fixed inset-0 z-50 grid place-items-center bg-slate-950/35 p-4">
		<div class="w-full max-w-md rounded-lg bg-white p-5 shadow-xl">
			<h2 class="text-lg font-bold text-slate-950">Close game?</h2>
			<p class="mt-2 text-sm leading-6 text-slate-600">
				Closed games cannot be changed. Make sure every buy-in and cash-out has been recorded.
			</p>

			<form method="POST" action="?/closeGame" class="mt-5 flex justify-end gap-2">
				<button
					type="button"
					class="rounded-md px-4 py-2 text-sm font-bold text-slate-700 hover:bg-slate-50"
					onclick={() => (isCloseGameOpen = false)}
				>
					Cancel
				</button>
				<button
					type="submit"
					class="rounded-md bg-emerald-900 px-4 py-2 text-sm font-bold text-white hover:bg-emerald-950"
				>
					Close game
				</button>
			</form>
		</div>
	</div>
{/if}
