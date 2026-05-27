<script lang="ts">
	import { formatDateTime } from '$lib/format';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

	const memberData = $derived.by(() => {
		if (data.view !== 'member') {
			throw new Error('Member summary requires member data.');
		}

		return data;
	});

	const balance = $derived(memberData.balance);
	const games = $derived(memberData.games);
	const payments = $derived(memberData.payments);

	function money(value: number | string) {
		const amount = Number(value);
		const sign = amount > 0 ? '+' : amount < 0 ? '-' : '';

		return `${sign}$${Math.abs(amount).toFixed(2)}`;
	}

	function unsignedMoney(value: number | string) {
		return `$${Math.abs(Number(value)).toFixed(2)}`;
	}

	function amountClass(value: number | string) {
		const amount = Number(value);

		if (amount > 0) return 'amount-positive';
		if (amount < 0) return 'amount-negative';

		return 'amount-neutral';
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
</script>

<svelte:head>
	<title>My Summary | PokerBank</title>
</svelte:head>

<section class="page-header">
	<div>
		<h1 class="page-title">My Summary</h1>
	</div>
</section>

<section class="grid-main">
	<div class="card card-pad">
		<div class="section-head">
			<h2 class="section-title">My balance</h2>
			<span class="badge">{balanceLabel(balance.balanceAmount)}</span>
		</div>

		<strong class={`stat-value ${amountClass(balance.balanceAmount)}`}>{money(balance.balanceAmount)}</strong>
		<p class="stat-detail">
			Games {money(balance.gameNetAmount)} · Payments {money(balance.paymentNetAmount)}
		</p>
	</div>

	<div class="card card-pad">
		<div class="section-head">
			<h2 class="section-title">My games</h2>
			<a href="/games" class="section-link">View all</a>
		</div>

		{#if games.length === 0}
			<p class="empty-state">No games yet.</p>
		{:else}
			<div class="data-list">
				{#each games.slice(0, 4) as game}
					<a href={`/games/${game.id}`} class="data-row">
						<div>
							<strong class="row-title">{formatDateTime(game.playedAtUtc)}</strong>
							<p class="row-meta">
								{game.status} · {game.playerCount} {Number(game.playerCount) === 1 ? 'player' : 'players'}
							</p>
						</div>
						<strong class={`amount ${amountClass(game.myNetAmount)}`}>{money(game.myNetAmount)}</strong>
					</a>
				{/each}
			</div>
		{/if}
	</div>

	<div class="card card-pad lg:col-span-2">
		<div class="section-head">
			<h2 class="section-title">My payments</h2>
			<a href="/payments" class="section-link">View all</a>
		</div>

		{#if payments.length === 0}
			<p class="empty-state">No payments recorded yet.</p>
		{:else}
			<div class="data-list">
				{#each payments.slice(0, 4) as payment}
					<div class="data-row">
						<div>
							<strong class="row-title">{paymentLabel(payment.direction)}</strong>
							<p class="row-meta">{methodLabel(payment.method)} · {formatDateTime(payment.recordedAtUtc)}</p>
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
</section>
