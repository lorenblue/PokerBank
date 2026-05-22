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

<div class="fixed inset-0 z-50 grid place-items-center bg-slate-950/35 p-4">
	<div
		class="w-full max-w-md rounded-lg bg-white p-5 shadow-xl"
		role="dialog"
		aria-modal="true"
		aria-label={title}
	>
		<div class="mb-4 flex items-start justify-between gap-4">
			<div>
				<h2 class="text-lg font-bold text-slate-950">{title}</h2>
				{#if subtitle}
					<p class="mt-1 text-sm text-slate-500">{subtitle}</p>
				{/if}
			</div>
			<button
				type="button"
				class="rounded-md px-2 py-1 text-sm font-bold text-slate-500 hover:bg-slate-100 hover:text-slate-900"
				aria-label="Close"
				onclick={onClose}
			>
				Close
			</button>
		</div>

		{@render children()}
	</div>
</div>
