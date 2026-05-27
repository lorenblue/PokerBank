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
	<form method="POST" {action} class="form-grid">
		{#if hint}
			<p class="empty-state">{hint}</p>
		{/if}

		<label class="field">
			Player
			<select name="playerId" required bind:value={playerId}>
				<option value="">Choose player</option>
				{#each players as player}
					<option value={player.id}>{player.name}</option>
				{/each}
			</select>
		</label>

		<label class="field">
			Amount
			<input
				name="amount"
				type="number"
				min="0.01"
				step="0.01"
				required
				bind:value={amount}
			/>
			<div class="quick-amounts">
				{#each quickAmounts as quickAmount}
					<button
						type="button"
						class="quick-chip"
						onclick={() => (amount = quickAmount.toFixed(2))}
					>
						${quickAmount}
					</button>
				{/each}
			</div>
		</label>

		<div class="form-actions">
			<button type="button" class="btn btn-secondary" onclick={onClose}>
				Cancel
			</button>
			<button type="submit" class="btn btn-primary">
				{submitLabel}
			</button>
		</div>
	</form>
</Modal>
