<script lang="ts">
	import AdminDashboardPage from './AdminDashboardPage.svelte';
	import MemberSummaryPage from './MemberSummaryPage.svelte';
	import type { SendBalanceUpdatesResponse } from '$lib/api/client';
	import type { PageData } from './$types';

	type FormData = {
		error?: string;
		paymentRecorded?: boolean;
		balanceUpdates?: SendBalanceUpdatesResponse;
	};

	let {
		data,
		form
	}: { data: PageData; form: FormData | null } = $props();
</script>

{#if data.view === 'manager'}
	<AdminDashboardPage {data} {form} />
{:else}
	<MemberSummaryPage {data} />
{/if}
