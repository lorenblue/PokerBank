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
		tone === 'positive' ? 'amount-positive' : tone === 'negative' ? 'amount-negative' : 'amount-neutral'
	);
</script>

<div class="stat-card">
	<span class="stat-label">{label}</span>
	<strong class={`stat-value ${valueClass}`}>{value}</strong>
	{#if detail}
		<p class="stat-detail">{detail}</p>
	{/if}
</div>
