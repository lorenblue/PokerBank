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

<section class="mb-6 flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
	<h1 class="mt-1 text-4xl leading-none font-bold tracking-normal sm:text-6xl">Dashboard</h1>

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
{:else if form?.paymentRecorded}
	<p class="mb-4 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-emerald-800">
		Payment recorded.
	</p>
{:else if form?.balanceUpdates}
	<div class="mb-4 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-emerald-800">
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

<section class="grid gap-4 lg:grid-cols-[minmax(0,2fr)_minmax(18rem,1fr)]">
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<div class="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
			<h2 class="text-base font-bold">Current balances</h2>
			<div class="flex flex-wrap gap-2">
				<button
					type="button"
					class="rounded-md px-3 py-2 text-sm font-bold text-emerald-900 hover:bg-emerald-50"
					onclick={() => (isSendUpdatesOpen = true)}
				>
					Send updates
				</button>
				<a href="/payments" class="rounded-md px-3 py-2 text-sm font-bold text-emerald-900 hover:bg-emerald-50">
					Record payment
				</a>
			</div>
		</div>

		{#if balances.length === 0}
			<p class="text-sm text-slate-500">No players yet.</p>
		{:else}
			<div class="grid gap-3">
				{#each balances as balance}
					<div
						class={`flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-4 hover:bg-slate-50 ${balance.isActive ? '' : 'opacity-55'}`}
					>
						<div>
							<a href={`/players/${balance.playerId}`} class="font-bold hover:text-emerald-900">
								{balance.playerName}
							</a>
							<p class="mt-1 text-sm text-slate-500">{balanceLabel(balance.balanceAmount)}</p>
						</div>
						<div class="flex shrink-0 items-center gap-3">
							{#if balance.isActive && Number(balance.balanceAmount) !== 0}
								<button
									type="button"
									class="rounded-md px-3 py-2 text-sm font-bold text-emerald-900 hover:bg-emerald-50"
									onclick={() => (balanceToSettle = balance)}
								>
									Settle
								</button>
							{/if}
							<strong
								class={`text-right text-2xl font-extrabold ${Number(balance.balanceAmount) > 0 ? 'text-emerald-700' : Number(balance.balanceAmount) < 0 ? 'text-red-700' : ''}`}
							>
								{money(balance.balanceAmount)}
							</strong>
						</div>
					</div>
				{/each}
			</div>
		{/if}
	</div>

	<div class="grid gap-4">
		<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
			<h2 class="mb-3 text-base font-bold">Open game</h2>

			{#if openGame}
				<a href={`/games/${openGame.id}`} class="block rounded-lg border border-slate-100 p-3 hover:bg-slate-50">
					<strong>{formatDateTime(openGame.createdAtUtc)}</strong>
					<p class="mt-1 text-sm text-slate-500">In progress</p>
				</a>
			{:else}
				<p class="text-sm text-slate-500">No open game.</p>
			{/if}
		</div>

		<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
			<div class="mb-3 flex items-center justify-between">
				<h2 class="text-base font-bold">Recent payments</h2>
				<a href="/payments" class="text-sm font-bold text-emerald-900 hover:text-emerald-950">View all</a>
			</div>

			{#if payments.length === 0}
				<p class="text-sm text-slate-500">No payments recorded yet.</p>
			{:else}
				<div class="grid gap-3">
					{#each payments.slice(0, 3) as payment}
						<div class="flex items-center justify-between gap-3 rounded-lg border border-slate-100 p-3">
							<div>
								<a href={`/players/${payment.playerId}`} class="text-sm font-bold hover:text-emerald-900">
									{playerNames.get(payment.playerId) ?? payment.playerId}
								</a>
								<p class="mt-1 text-xs text-slate-500">
									{paymentLabel(payment.direction)} · {methodLabel(payment.method)}
								</p>
							</div>
							<strong
								class={`text-right font-bold ${payment.direction === 'ReceivedByPlayer' ? 'text-emerald-700' : 'text-red-700'}`}
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
		<form method="POST" action="?/sendBalanceUpdates" class="grid gap-4">
			<p class="text-sm leading-6 text-slate-600">
				Players without email addresses will be skipped. In development, emails are logged by the API
				instead of being sent through a real provider.
			</p>

			<div class="flex justify-end gap-2">
				<button
					type="button"
					class="rounded-md px-4 py-2 text-sm font-bold text-slate-700 hover:bg-slate-50"
					onclick={() => (isSendUpdatesOpen = false)}
				>
					Cancel
				</button>
				<button
					type="submit"
					class="rounded-md bg-emerald-900 px-4 py-2 text-sm font-bold text-white hover:bg-emerald-950"
				>
					Send updates
				</button>
			</div>
		</form>
	</Modal>
{/if}
