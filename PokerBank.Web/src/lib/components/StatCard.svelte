<script lang="ts">
	let {
		label,
		value,
		detail,
		tone = 'neutral'
	}: {
		label: string;
		value: string;
		detail?: string;
		tone?: 'neutral' | 'positive' | 'negative';
	} = $props();

	const valueClass = $derived(
		tone === 'positive' ? 'text-emerald-700' : tone === 'negative' ? 'text-red-700' : ''
	);
</script>

<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
	<span class="mb-1 block text-sm text-slate-500">{label}</span>
	<strong class={`text-2xl ${valueClass}`}>{value}</strong>
	{#if detail}
		<p class="mt-1 text-sm text-slate-500">{detail}</p>
	{/if}
</div>
