<script lang="ts">
	import Modal from './Modal.svelte';

	type PlayerOption = {
		id: string;
		name: string;
	};

	let {
		onClose,
		playerName,
		players = []
	}: {
		onClose: () => void;
		playerName?: string;
		players?: PlayerOption[];
	} = $props();
</script>

<Modal title="Record payment" subtitle={playerName} {onClose}>
	<form method="POST" action="?/createPayment" class="grid gap-4">
		{#if players.length > 0}
			<label class="grid gap-1 text-sm font-bold text-slate-700">
				Player
				<select name="playerId" required class="rounded-md border border-slate-300 px-3 py-2">
					<option value="">Choose a player</option>
					{#each players as player}
						<option value={player.id}>{player.name}</option>
					{/each}
				</select>
			</label>
		{/if}

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
			<select name="direction" required class="rounded-md border border-slate-300 px-3 py-2">
				<option value="">Choose direction</option>
				<option value="MadeByPlayer">Player made payment</option>
				<option value="ReceivedByPlayer">Player received payment</option>
			</select>
		</label>

		<label class="grid gap-1 text-sm font-bold text-slate-700">
			Method
			<select name="method" required class="rounded-md border border-slate-300 px-3 py-2">
				<option value="">Choose method</option>
				<option value="ETransfer">e-transfer</option>
				<option value="Cash">cash</option>
			</select>
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
				Record payment
			</button>
		</div>
	</form>
</Modal>
