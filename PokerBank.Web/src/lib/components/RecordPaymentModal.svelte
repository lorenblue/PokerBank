<script lang="ts">
	import Modal from './Modal.svelte';

	type PlayerOption = {
		id: string;
		name: string;
	};

	let {
		onClose,
		playerName,
		players = [],
		selectedPlayerId = '',
		selectedDirection = '',
		amount = ''
	}: {
		onClose: () => void;
		playerName?: string;
		players?: PlayerOption[];
		selectedPlayerId?: string;
		selectedDirection?: 'MadeByPlayer' | 'ReceivedByPlayer' | '';
		amount?: string;
	} = $props();
</script>

<Modal title="Record payment" subtitle={playerName} {onClose}>
	<form method="POST" action="?/createPayment" class="form-grid">
		{#if players.length > 0}
			<label class="field">
				Player
				<select name="playerId" required value={selectedPlayerId}>
					<option value="">Choose a player</option>
					{#each players as player}
						<option value={player.id}>{player.name}</option>
					{/each}
				</select>
			</label>
		{/if}

		<label class="field">
			Amount
			<input
				name="amount"
				type="number"
				min="0.01"
				step="0.01"
				required
				value={amount}
			/>
		</label>

		<label class="field">
			Direction
			<select name="direction" required value={selectedDirection}>
				<option value="">Choose direction</option>
				<option value="MadeByPlayer">Player made payment</option>
				<option value="ReceivedByPlayer">Player received payment</option>
			</select>
		</label>

		<label class="field">
			Method
			<select name="method" required>
				<option value="">Choose method</option>
				<option value="ETransfer">e-transfer</option>
				<option value="Cash">cash</option>
			</select>
		</label>

		<div class="form-actions">
			<button type="button" class="btn btn-secondary" onclick={onClose}>
				Cancel
			</button>
			<button type="submit" class="btn btn-primary">
				Record payment
			</button>
		</div>
	</form>
</Modal>
