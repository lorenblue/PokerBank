<script lang="ts">
	import type { PageData } from './$types';

	let { data, form }: { data: PageData; form: { error?: string; success?: boolean } | null } = $props();

	const activePlayers = $derived(data.players.filter((player) => player.isActive));
	const playerNames = $derived(new Map(data.players.map((player) => [player.id, player.name] as const)));

	function unsignedMoney(value: number | string) {
		return `$${Math.abs(Number(value)).toFixed(2)}`;
	}

	function paymentLabel(type: string) {
		if (type === 'PlayerPaysBank') return 'Player paid me';
		if (type === 'BankPaysPlayer') return 'I paid player';

		return type;
	}
</script>

<svelte:head>
	<title>Payments | PokerBank</title>
</svelte:head>

<section class="mb-6">
	<h1 class="mt-1 text-4xl leading-none font-bold tracking-normal sm:text-6xl">Payments</h1>
</section>

{#if form?.error}
	<p class="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-700">{form.error}</p>
{:else if form?.success}
	<p class="mb-4 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-emerald-800">
		Payment recorded.
	</p>
{/if}

<section class="grid gap-4 lg:grid-cols-[minmax(18rem,24rem)_1fr]">
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<h2 class="mb-4 text-base font-bold">Record payment</h2>

		{#if activePlayers.length === 0}
			<p class="text-sm text-slate-500">Add an active player before recording payments.</p>
		{:else}
			<form method="POST" action="?/createPayment" class="grid gap-3">
				<label class="grid gap-1 text-sm font-bold text-slate-700">
					Player
					<select name="playerId" required class="rounded-md border border-slate-300 px-3 py-2">
						<option value="">Choose a player</option>
						{#each activePlayers as player}
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

				<label class="grid gap-1 text-sm font-bold text-slate-700">
					Direction
					<select name="type" required class="rounded-md border border-slate-300 px-3 py-2">
						<option value="">Choose direction</option>
						<option value="PlayerPaysBank">Player paid me</option>
						<option value="BankPaysPlayer">I paid player</option>
					</select>
				</label>

				<button
					type="submit"
					class="rounded-md bg-emerald-900 px-4 py-3 font-bold text-white hover:bg-emerald-950"
				>
					Record payment
				</button>
			</form>
		{/if}
	</div>

	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<h2 class="mb-4 text-base font-bold">Payment history</h2>

		{#if data.payments.length === 0}
			<p class="text-sm text-slate-500">No payments recorded yet.</p>
		{:else}
			<div class="grid gap-3">
				{#each data.payments as payment}
					<article class="flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-3">
						<div>
							<h3 class="text-sm font-bold">{playerNames.get(payment.playerId) ?? payment.playerId}</h3>
							<p class="mt-1 text-xs text-slate-500">
								{paymentLabel(payment.type)} · {new Date(payment.recordedAtUtc).toLocaleString()}
							</p>
						</div>
						<strong
							class={`text-right font-bold ${payment.type === 'BankPaysPlayer' ? 'text-emerald-700' : 'text-red-700'}`}
						>
							{payment.type === 'BankPaysPlayer' ? '+' : '-'}{unsignedMoney(payment.amount)}
						</strong>
					</article>
				{/each}
			</div>
		{/if}
	</div>
</section>
