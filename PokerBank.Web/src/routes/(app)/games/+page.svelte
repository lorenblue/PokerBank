<script lang="ts">
	import AdminGamesPage from './AdminGamesPage.svelte';
	import MemberGamesPage from './MemberGamesPage.svelte';
	import type { PageData } from './$types';

	let { data, form }: { data: PageData; form: { error?: string } | null } = $props();
</script>

{#if data.isManager}
	<AdminGamesPage {data} {form} />
{:else}
	<MemberGamesPage {data} />
{/if}
