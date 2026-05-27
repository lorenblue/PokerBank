<script lang="ts">
	import GameEntryModal from '$lib/components/GameEntryModal.svelte';
	import Modal from '$lib/components/Modal.svelte';
	import StatCard from '$lib/components/StatCard.svelte';
	import { formatDateTime, formatGameEntryDateTime } from '$lib/format';
	import type { PageData } from './$types';

	let { data, form }: { data: PageData; form: { error?: string; success?: boolean } | null } = $props();

	let isAddBuyInOpen = $state(false);
	let isAddCashOutOpen = $state(false);
	let isDeleteGameOpen = $state(false);
	let isCloseGameOpen = $state(false);
	let selectedEntryPlayerId = $state('');
	let entryToEdit = $state<PageData['game']['entries'][number] | null>(null);
	let entryToDelete = $state<PageData['game']['entries'][number] | null>(null);

	const isManager = $derived(data.isManager);
	const playerNames = $derived(
		new Map([
			...data.game.playerTotals.map((total) => [total.playerId, total.playerName] as const),
			...data.players.map((player) => [player.id, player.name] as const)
		])
	);
	const cashOutPlayers = $derived(
		data.game.playerTotals
			.filter((total) => Number(total.buyInAmount) > 0)
			.map((total) => ({ id: total.playerId, name: total.playerName }))
	);
	const canCloseGame = $derived(data.game.entries.length > 0 && Number(data.game.remainingCashOutAmount) === 0);
	const isOpen = $derived(data.game.status === 'Open');

	function money(value: number | string) {
		return `$${Number(value).toFixed(2)}`;
	}

	function signedMoney(value: number | string) {
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

	function entryLabel(type: string) {
		if (type === 'BuyIn') return 'Buy-in';
		if (type === 'CashOut') return 'Cash-out';

		return type;
	}

	function closeDeleteEntry() {
		entryToDelete = null;
	}

	function closeEditEntry() {
		entryToEdit = null;
	}

	function openAddBuyIn(playerId = '') {
		selectedEntryPlayerId = playerId;
		isAddBuyInOpen = true;
	}

	function openAddCashOut(playerId = '') {
		selectedEntryPlayerId = playerId;
		isAddCashOutOpen = true;
	}
</script>

<svelte:head>
	<title>Game | PokerBank</title>
</svelte:head>

<a href={isManager ? '/' : '/games'} class="back-link">
	{isManager ? 'Back to dashboard' : 'Back to my games'}
</a>

<section class="page-header">
	<div>
		<h1 class="page-title">{data.game.status}</h1>
		<p class="page-subtitle">{formatDateTime(data.game.createdAtUtc)}</p>
	</div>

	{#if isManager && isOpen}
		<div class="page-actions">
			<button type="button" disabled={data.players.length === 0} class="btn btn-secondary" onclick={() => openAddBuyIn()}>
				Add buy-in
			</button>
			<button
				type="button"
				disabled={cashOutPlayers.length === 0}
				title={cashOutPlayers.length === 0 ? 'A player needs a buy-in before cashing out.' : 'Add cash-out'}
				class="btn btn-primary"
				onclick={() => openAddCashOut()}
			>
				Add cash-out
			</button>
			<button type="button" class="btn btn-secondary" onclick={() => (isCloseGameOpen = true)} disabled={!canCloseGame}>
				Close game
			</button>
			<button type="button" class="btn btn-subtle-danger" onclick={() => (isDeleteGameOpen = true)}>
				Delete
			</button>
		</div>
	{/if}
</section>

{#if form?.error}
	<p class="alert alert-error">{form.error}</p>
{/if}

<section class="stat-grid">
	<StatCard label="Buy-ins" value={money(data.game.totalBuyInAmount)} />
	<StatCard label="Cash-outs" value={money(data.game.totalCashOutAmount)} />
	<StatCard
		label="Remaining"
		value={money(data.game.remainingCashOutAmount)}
		detail={canCloseGame ? 'Ready to close' : 'Buy-ins must equal cash-outs'}
		tone={canCloseGame ? 'positive' : 'negative'}
	/>
</section>

<section class="grid-main">
	<div class="card card-pad">
		<div class="section-head">
			<div>
				<h2 class="section-title">Player totals</h2>
				<p class="row-meta">Current position for each player in this game.</p>
			</div>
		</div>

		{#if data.game.playerTotals.length === 0}
			<p class="empty-state">Player totals appear after entries are added.</p>
		{:else}
			<div class="table-wrap">
				<table class="data-table">
					<thead>
						<tr>
							<th>Player</th>
							<th class="text-right">Buy-ins</th>
							<th class="text-right">Cash-outs</th>
							<th class="text-right">Net</th>
							{#if isManager && isOpen}
								<th class="text-right">Action</th>
							{/if}
						</tr>
					</thead>
					<tbody>
						{#each data.game.playerTotals as total}
							<tr>
								<td>
									{#if isManager}
										<a href={`/players/${total.playerId}`} class="row-title">{total.playerName}</a>
									{:else}
										<strong class="row-title">{total.playerName}</strong>
									{/if}
								</td>
								<td class="text-right amount">{money(total.buyInAmount)}</td>
								<td class="text-right amount">{money(total.cashOutAmount)}</td>
								<td class={`text-right amount ${amountClass(total.netAmount)}`}>{signedMoney(total.netAmount)}</td>
								{#if isManager && isOpen}
									<td>
										<div class="row-actions">
											<button type="button" class="btn btn-ghost" onclick={() => openAddBuyIn(total.playerId)}>
												Buy-in
											</button>
											<button type="button" class="btn btn-ghost" onclick={() => openAddCashOut(total.playerId)}>
												Cash-out
											</button>
										</div>
									</td>
								{/if}
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		{/if}
	</div>

	<div class="card card-pad">
		<div class="section-head">
			<div>
				<h2 class="section-title">Entry history</h2>
				<p class="row-meta">Chronological ledger for this game.</p>
			</div>
		</div>

		{#if data.game.entries.length === 0}
			<p class="empty-state">No entries yet.</p>
		{:else}
			<div class="data-list">
				{#each data.game.entries as entry}
					<div class="data-row">
						<div>
							{#if isManager}
								<a href={`/players/${entry.playerId}`} class="row-title">
									{playerNames.get(entry.playerId) ?? entry.playerId}
								</a>
							{:else}
								<strong class="row-title">{playerNames.get(entry.playerId) ?? entry.playerId}</strong>
							{/if}
							<p class="row-meta">
								{entryLabel(entry.type)} · {formatGameEntryDateTime(entry.recordedAtUtc, data.game.createdAtUtc)}
							</p>
						</div>
						<div class="row-actions">
							<strong class={`amount ${entry.type === 'CashOut' ? 'amount-positive' : 'amount-neutral'}`}>
								{money(entry.amount)}
							</strong>
							{#if isManager && isOpen}
								<button type="button" class="btn btn-ghost" onclick={() => (entryToEdit = entry)}>
									Edit
								</button>
								<button type="button" class="btn btn-subtle-danger" onclick={() => (entryToDelete = entry)}>
									Delete
								</button>
							{/if}
						</div>
					</div>
				{/each}
			</div>
		{/if}
	</div>
</section>

{#if isManager && isAddBuyInOpen}
	<GameEntryModal
		title="Add buy-in"
		action="?/addBuyIn"
		players={data.players}
		selectedPlayerId={selectedEntryPlayerId}
		submitLabel="Add buy-in"
		onClose={() => (isAddBuyInOpen = false)}
	/>
{/if}

{#if isManager && isAddCashOutOpen}
	<GameEntryModal
		title="Add cash-out"
		action="?/addCashOut"
		players={cashOutPlayers}
		selectedPlayerId={selectedEntryPlayerId}
		submitLabel="Add cash-out"
		hint="Only players with a buy-in can cash out."
		onClose={() => (isAddCashOutOpen = false)}
	/>
{/if}

{#if isManager && entryToEdit}
	<Modal title={`Edit ${entryLabel(entryToEdit.type).toLowerCase()}`} onClose={closeEditEntry}>
		<form method="POST" action="?/updateEntry" class="form-grid">
			<input type="hidden" name="entryId" value={entryToEdit.id} />

			<div class="empty-state">
				<strong>{playerNames.get(entryToEdit.playerId) ?? entryToEdit.playerId}</strong>
				<p>
					{entryLabel(entryToEdit.type)} · {formatGameEntryDateTime(entryToEdit.recordedAtUtc, data.game.createdAtUtc)}
				</p>
			</div>

			<label class="field">
				Amount
				<input
					name="amount"
					type="number"
					min="0.01"
					step="0.01"
					required
					value={Number(entryToEdit.amount).toFixed(2)}
				/>
			</label>

			<div class="form-actions">
				<button type="button" class="btn btn-secondary" onclick={closeEditEntry}>Cancel</button>
				<button type="submit" class="btn btn-primary">Save entry</button>
			</div>
		</form>
	</Modal>
{/if}

{#if isManager && entryToDelete}
	<Modal title="Delete entry?" onClose={closeDeleteEntry}>
		<p class="page-subtitle">
			This removes the {entryLabel(entryToDelete.type).toLowerCase()} from the open game. This is only intended
			for entries recorded by mistake.
		</p>

		<div class="empty-state mt-4">
			<strong>{playerNames.get(entryToDelete.playerId) ?? entryToDelete.playerId}</strong>
			<p>{entryLabel(entryToDelete.type)} · {money(entryToDelete.amount)}</p>
		</div>

		<form method="POST" action="?/deleteEntry" class="form-grid mt-5">
			<input type="hidden" name="entryId" value={entryToDelete.id} />
			<div class="form-actions">
				<button type="button" class="btn btn-secondary" onclick={closeDeleteEntry}>Cancel</button>
				<button type="submit" class="btn btn-danger">Delete entry</button>
			</div>
		</form>
	</Modal>
{/if}

{#if isManager && isDeleteGameOpen}
	<Modal title="Delete game?" onClose={() => (isDeleteGameOpen = false)}>
		<p class="page-subtitle">
			This removes the open game and its entries. This is only intended for games created by mistake.
		</p>

		<form method="POST" action="?/deleteGame" class="form-grid mt-5">
			<div class="form-actions">
				<button type="button" class="btn btn-secondary" onclick={() => (isDeleteGameOpen = false)}>
					Cancel
				</button>
				<button type="submit" class="btn btn-danger">Delete game</button>
			</div>
		</form>
	</Modal>
{/if}

{#if isManager && isCloseGameOpen}
	<Modal title="Close game?" onClose={() => (isCloseGameOpen = false)}>
		<p class="page-subtitle">
			Closed games cannot be changed. Make sure every buy-in and cash-out has been recorded.
		</p>

		<form method="POST" action="?/closeGame" class="form-grid mt-5">
			<div class="form-actions">
				<button type="button" class="btn btn-secondary" onclick={() => (isCloseGameOpen = false)}>
					Cancel
				</button>
				<button type="submit" class="btn btn-primary">Close game</button>
			</div>
		</form>
	</Modal>
{/if}
