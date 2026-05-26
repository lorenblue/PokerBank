<script lang="ts">
	import { formatDateTime } from '$lib/format';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

	const payments = $derived(data.payments ?? []);

	function unsignedMoney(value: number | string) {
		return `$${Math.abs(Number(value)).toFixed(2)}`;
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
	<title>My Payments | PokerBank</title>
</svelte:head>

<section class="mb-6">
	<h1 class="mt-1 text-4xl leading-none font-bold tracking-normal sm:text-6xl">My Payments</h1>
</section>

<section>
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<h2 class="mb-4 text-base font-bold">My payment history</h2>

		{#if payments.length === 0}
			<p class="text-sm text-slate-500">No payments recorded yet.</p>
		{:else}
			<div class="grid gap-3">
				{#each payments as payment}
					<article class="flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-3">
						<div>
							<p class="text-sm font-bold">You</p>
							<p class="mt-1 text-xs text-slate-500">
								{paymentLabel(payment.direction)} · {methodLabel(payment.method)} · {formatDateTime(
									payment.recordedAtUtc
								)}
							</p>
						</div>
						<strong
							class={`text-right font-bold ${payment.direction === 'ReceivedByPlayer' ? 'text-emerald-700' : 'text-red-700'}`}
						>
							{payment.direction === 'ReceivedByPlayer' ? '+' : '-'}{unsignedMoney(payment.amount)}
						</strong>
					</article>
				{/each}
			</div>
		{/if}
	</div>
</section>
