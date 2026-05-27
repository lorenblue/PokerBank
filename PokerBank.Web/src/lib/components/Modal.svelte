<script lang="ts">
	import type { Snippet } from 'svelte';

	let {
		title,
		subtitle,
		onClose,
		children
	}: {
		title: string;
		subtitle?: string;
		onClose: () => void;
		children: Snippet;
	} = $props();
</script>

<div class="modal-backdrop">
	<div class="modal-panel" role="dialog" aria-modal="true" aria-label={title}>
		<div class="modal-header">
			<div>
				<h2 class="modal-title">{title}</h2>
				{#if subtitle}
					<p class="modal-subtitle">{subtitle}</p>
				{/if}
			</div>
			<button type="button" class="btn btn-ghost" aria-label="Close" onclick={onClose}>
				Close
			</button>
		</div>

		<div class="modal-body">
			{@render children()}
		</div>
	</div>
</div>
