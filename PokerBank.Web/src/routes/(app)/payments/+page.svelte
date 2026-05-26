<script lang="ts">
	import AdminPaymentsPage from './AdminPaymentsPage.svelte';
	import MemberPaymentsPage from './MemberPaymentsPage.svelte';
	import type { PageData } from './$types';

	let {
		data,
		form
	}: { data: PageData; form: { deleted?: boolean; error?: string; success?: boolean } | null } = $props();
</script>

{#if data.isManager}
	<AdminPaymentsPage {data} {form} />
{:else}
	<MemberPaymentsPage {data} />
{/if}
