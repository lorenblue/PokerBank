<script lang="ts">
	import Modal from '$lib/components/Modal.svelte';
	import RecordPaymentModal from '$lib/components/RecordPaymentModal.svelte';
	import { formatDateTime } from '$lib/format';
	import type { Balance, SendBalanceUpdatesResponse } from '$lib/api/client';
	import type { PageData } from './$types';

	type FormData = {
		error?: string;
		paymentRecorded?: boolean;
		balanceUpdates?: SendBalanceUpdatesResponse;
	};

	let { data, form }: { data: PageData; form: FormData | null } = $props();

	let balanceToSettle = $state<Balance | null>(null);
	let isSendUpdatesOpen = $state(false);

	const managerData = $derived.by(() => {
		if (data.view !== 'manager') {
			throw new Error('Admin dashboard requires manager data.');
		}

		return data;
	});

	const games = $derived(managerData.games);
	const balances = $derived(managerData.balances);
	const players = $derived(managerData.players);
	const payments = $derived(managerData.payments);
	const openGame = $derived(games.find((game) => game.status === 'Open'));
	const activePlayers = $derived(players.filter((player) => player.isActive));
	const unsettledBalances = $derived(
		balances.filter((balance) => balance.isActive && Number(balance.balanceAmount) !== 0)
	);
	const playerNames = $derived(new Map(players.map((player) => [player.id, player.name] as const)));

	function money(value: number | string) {
		const amount = Number(value);
		const sign = amount > 0 ? '+' : amount < 0 ? '-' : '';

		return `${sign}$${Math.abs(amount).toFixed(2)}`;
	}

	function unsignedMoney(value: number | string) {
		return `$${Math.abs(Number(value)).toFixed(2)}`;
	}

	function balanceLabel(value: number | string) {
		const amount = Number(value);

		if (amount > 0) return 'should receive';
		if (amount < 0) return 'owes';

		return 'settled';
	}

	function amountClass(value: number | string) {
		const amount = Number(value);

		if (amount > 0) return 'amount-positive';
		if (amount < 0) return 'amount-negative';

		return 'amount-neutral';
	}

	function paymentLabel(direction: string) {
		if (direction === 'MadeByPlayer') return 'Player made payment';
		if (direction === 'ReceivedByPlayer') return 'Player received payment';

		return direction;
	}

	function methodLabel(method: string) {
		if (method === 'ETransfer') return 'e-transfer';
		if (method === 'Cash') return 'cash';

		return method;
	}

	function settlementDirection(balanceAmount: number | string) {
		return Number(balanceAmount) < 0 ? 'MadeByPlayer' : 'ReceivedByPlayer';
	}

	function settlementAmount(balanceAmount: number | string) {
		return Math.abs(Number(balanceAmount)).toFixed(2);
	}

	function closeSettleBalance() {
		balanceToSettle = null;
	}
</script>

<svelte:head>
	<title>Dashboard | PokerBank</title>
</svelte:head>

<section class="page-header">
	<div>
		<h1 class="page-title">Dashboard</h1>
	</div>

	<div class="page-actions">
		{#if openGame}
			<a href={`/games/${openGame.id}`} class="btn btn-primary">Continue open game</a>
		{:else}
			<form method="POST" action="?/createGame">
				<button type="submit" class="btn btn-primary">New game</button>
			</form>
		{/if}
	</div>
</section>

{#if form?.error}
	<p class="alert alert-error">{form.error}</p>
{:else if form?.paymentRecorded}
	<p class="alert alert-success">Payment recorded.</p>
{:else if form?.balanceUpdates}
	<div class="alert alert-success">
		<p>Sent {form.balanceUpdates.sent} balance update emails.</p>
		{#if form.balanceUpdates.skipped.length > 0}
			<ul class="mt-2 grid gap-1 text-sm">
				{#each form.balanceUpdates.skipped as skipped}
					<li>{skipped.playerName}: {skipped.reason}</li>
				{/each}
			</ul>
		{/if}
	</div>
{/if}

<section class="stat-grid">
	<div class="stat-card">
		<span class="stat-label">Open game</span>
		<strong class="stat-value">{openGame ? 'Active' : 'None'}</strong>
		<p class="stat-detail">{openGame ? formatDateTime(openGame.createdAtUtc) : 'Ready when the next game starts'}</p>
	</div>
	<div class="stat-card">
		<span class="stat-label">Active players</span>
		<strong class="stat-value">{activePlayers.length}</strong>
		<p class="stat-detail">Available for games and payments</p>
	</div>
	<div class="stat-card">
		<span class="stat-label">Unsettled</span>
		<strong class="stat-value">{unsettledBalances.length}</strong>
		<p class="stat-detail">Players with non-zero balances</p>
	</div>
</section>

<section class="grid-main">
	<div class="card card-pad">
		<div class="section-head">
			<div>
				<h2 class="section-title">Current balances</h2>
				<p class="row-meta">Positive means the player should receive money.</p>
			</div>
			<div class="row-actions">
				<button type="button" class="btn btn-secondary" onclick={() => (isSendUpdatesOpen = true)}>
					Send updates
				</button>
				<a href="/payments" class="btn btn-ghost">Record payment</a>
			</div>
		</div>

		{#if balances.length === 0}
			<p class="empty-state">No players yet.</p>
		{:else}
			<div class="table-wrap">
				<table class="data-table">
					<thead>
						<tr>
							<th>Player</th>
							<th>State</th>
							<th class="text-right">Game net</th>
							<th class="text-right">Payments</th>
							<th class="text-right">Balance</th>
							<th class="text-right">Action</th>
						</tr>
					</thead>
					<tbody>
						{#each balances as balance}
							<tr class={balance.isActive ? '' : 'opacity-55'}>
								<td>
									<a href={`/players/${balance.playerId}`} class="row-title">{balance.playerName}</a>
								</td>
								<td><span class="badge">{balanceLabel(balance.balanceAmount)}</span></td>
								<td class={`text-right amount ${amountClass(balance.gameNetAmount)}`}>
									{money(balance.gameNetAmount)}
								</td>
								<td class={`text-right amount ${amountClass(balance.paymentNetAmount)}`}>
									{money(balance.paymentNetAmount)}
								</td>
								<td class={`text-right amount ${amountClass(balance.balanceAmount)}`}>
									{money(balance.balanceAmount)}
								</td>
								<td class="text-right">
									{#if balance.isActive && Number(balance.balanceAmount) !== 0}
										<button
											type="button"
											class="btn btn-ghost"
											onclick={() => (balanceToSettle = balance)}
										>
											Settle
										</button>
									{/if}
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			</div>
		{/if}
	</div>

	<div class="data-list">
		<div class="card card-pad">
			<div class="section-head">
				<h2 class="section-title">Open game</h2>
				<a href="/games" class="section-link">All games</a>
			</div>

			{#if openGame}
				<a href={`/games/${openGame.id}`} class="data-row">
					<div>
						<strong class="row-title">{formatDateTime(openGame.createdAtUtc)}</strong>
						<p class="row-meta">In progress</p>
					</div>
					<span class="badge badge-open">Open</span>
				</a>
			{:else}
				<p class="empty-state">No open game.</p>
			{/if}
		</div>

		<div class="card card-pad">
			<div class="section-head">
				<h2 class="section-title">Recent payments</h2>
				<a href="/payments" class="section-link">View all</a>
			</div>

			{#if payments.length === 0}
				<p class="empty-state">No payments recorded yet.</p>
			{:else}
				<div class="data-list">
					{#each payments.slice(0, 4) as payment}
						<div class="data-row">
							<div>
								<a href={`/players/${payment.playerId}`} class="row-title">
									{playerNames.get(payment.playerId) ?? payment.playerId}
								</a>
								<p class="row-meta">{paymentLabel(payment.direction)} · {methodLabel(payment.method)}</p>
							</div>
							<strong
								class={`amount ${payment.direction === 'ReceivedByPlayer' ? 'amount-positive' : 'amount-negative'}`}
							>
								{payment.direction === 'ReceivedByPlayer' ? '+' : '-'}{unsignedMoney(payment.amount)}
							</strong>
						</div>
					{/each}
				</div>
			{/if}
		</div>
	</div>
</section>

{#if balanceToSettle}
	<RecordPaymentModal
		players={activePlayers}
		selectedPlayerId={balanceToSettle.playerId}
		selectedDirection={settlementDirection(balanceToSettle.balanceAmount)}
		amount={settlementAmount(balanceToSettle.balanceAmount)}
		onClose={closeSettleBalance}
	/>
{/if}

{#if isSendUpdatesOpen}
	<Modal
		title="Send balance updates"
		subtitle="Emails will be sent to active players with email addresses."
		onClose={() => (isSendUpdatesOpen = false)}
	>
		<form method="POST" action="?/sendBalanceUpdates" class="form-grid">
			<p class="empty-state">
				Players without email addresses will be skipped. In development, emails are logged by the API
				instead of being sent through a real provider.
			</p>

			<div class="form-actions">
				<button type="button" class="btn btn-secondary" onclick={() => (isSendUpdatesOpen = false)}>
					Cancel
				</button>
				<button type="submit" class="btn btn-primary">Send updates</button>
			</div>
		</form>
	</Modal>
{/if}
