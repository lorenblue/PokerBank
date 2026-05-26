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

<section class="mb-6">
	<h1 class="mt-1 text-4xl leading-none font-bold tracking-normal sm:text-6xl">My Summary</h1>
</section>

<section class="grid gap-4 lg:grid-cols-[minmax(0,2fr)_minmax(18rem,1fr)]">
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<h2 class="mb-4 text-base font-bold">My balance</h2>
		<div class="flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-4">
			<div>
				<p class="text-sm text-slate-500">{balanceLabel(balance.balanceAmount)}</p>
				<p class="mt-1 text-sm text-slate-500">
					Games {money(balance.gameNetAmount)} · Payments {money(balance.paymentNetAmount)}
				</p>
			</div>
			<strong
				class={`text-right text-3xl font-extrabold ${Number(balance.balanceAmount) > 0 ? 'text-emerald-700' : Number(balance.balanceAmount) < 0 ? 'text-red-700' : ''}`}
			>
				{money(balance.balanceAmount)}
			</strong>
		</div>
	</div>

	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<div class="mb-3 flex items-center justify-between">
			<h2 class="text-base font-bold">My games</h2>
			<a href="/games" class="text-sm font-bold text-emerald-900 hover:text-emerald-950">View all</a>
		</div>

		{#if games.length === 0}
			<p class="text-sm text-slate-500">No games yet.</p>
		{:else}
			<div class="grid gap-3">
				{#each games.slice(0, 4) as game}
					<a href={`/games/${game.id}`} class="block rounded-lg border border-slate-100 p-3 hover:bg-slate-50">
						<div class="flex items-center justify-between gap-3">
							<strong>{formatDateTime(game.playedAtUtc)}</strong>
							<span
								class={`font-bold ${Number(game.myNetAmount) > 0 ? 'text-emerald-700' : Number(game.myNetAmount) < 0 ? 'text-red-700' : ''}`}
							>
								{money(game.myNetAmount)}
							</span>
						</div>
						<p class="mt-1 text-sm text-slate-500">
							{game.status} · {game.playerCount} {Number(game.playerCount) === 1 ? 'player' : 'players'}
						</p>
					</a>
				{/each}
			</div>
		{/if}
	</div>

	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs lg:col-span-2">
		<div class="mb-3 flex items-center justify-between">
			<h2 class="text-base font-bold">My payments</h2>
			<a href="/payments" class="text-sm font-bold text-emerald-900 hover:text-emerald-950">View all</a>
		</div>

		{#if payments.length === 0}
			<p class="text-sm text-slate-500">No payments recorded yet.</p>
		{:else}
			<div class="grid gap-3 sm:grid-cols-2">
				{#each payments.slice(0, 4) as payment}
					<div class="flex items-center justify-between gap-3 rounded-lg border border-slate-100 p-3">
						<div>
							<p class="text-sm font-bold">{paymentLabel(payment.direction)}</p>
							<p class="mt-1 text-xs text-slate-500">
								{methodLabel(payment.method)} · {formatDateTime(payment.recordedAtUtc)}
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
</section>
