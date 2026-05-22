<script lang="ts">
	import Modal from './Modal.svelte';

	type PlayerOption = {
		id: string;
		name: string;
	};

	const quickAmounts = [20, 40, 60, 100];

	let {
		title,
		action,
		players,
		submitLabel,
		onClose,
		selectedPlayerId = '',
		hint
	}: {
		title: string;
		action: string;
		players: PlayerOption[];
		submitLabel: string;
		onClose: () => void;
		selectedPlayerId?: string;
		hint?: string;
	} = $props();

	let playerId = $state('');
	let amount = $state('');

	$effect(() => {
		playerId = selectedPlayerId;
	});
</script>

<Modal {title} {onClose}>
	<form method="POST" {action} class="grid gap-4">
		{#if hint}
			<p class="rounded-lg border border-slate-100 bg-slate-50 px-3 py-2 text-sm text-slate-500">
				{hint}
			</p>
		{/if}

		<label class="grid gap-1 text-sm font-bold text-slate-700">
			Player
			<select
				name="playerId"
				required
				bind:value={playerId}
				class="rounded-md border border-slate-300 px-3 py-2"
			>
				<option value="">Choose player</option>
				{#each players as player}
					<option value={player.id}>{player.name}</option>
				{/each}
			</select>
		</label>

		<label class="grid gap-2 text-sm font-bold text-slate-700">
			Amount
			<input
				name="amount"
				type="number"
				min="0.01"
				step="0.01"
				required
				bind:value={amount}
				class="rounded-md border border-slate-300 px-3 py-2"
			/>
			<div class="flex flex-wrap gap-2">
				{#each quickAmounts as quickAmount}
					<button
						type="button"
						class="rounded-md border border-slate-200 px-3 py-1 text-xs font-bold text-slate-700 hover:bg-slate-50"
						onclick={() => (amount = quickAmount.toFixed(2))}
					>
						${quickAmount}
					</button>
				{/each}
			</div>
		</label>

		<div class="flex justify-end gap-2">
			<button
				type="button"
				class="rounded-md px-4 py-2 text-sm font-bold text-slate-700 hover:bg-slate-50"
				onclick={onClose}
			>
				Cancel
			</button>
			<button
				type="submit"
				class="rounded-md bg-emerald-900 px-4 py-2 text-sm font-bold text-white hover:bg-emerald-950"
			>
				{submitLabel}
			</button>
		</div>
	</form>
</Modal>
