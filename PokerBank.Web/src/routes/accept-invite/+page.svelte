<script lang="ts">
	import type { ActionData, PageData } from './$types';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	const token = $derived(form?.token ?? data.token);
</script>

<svelte:head>
	<title>Accept invite | PokerBank</title>
</svelte:head>

<section class="mx-auto grid min-h-screen max-w-md content-center px-4 py-12">
	<div class="mb-6">
		<a href="/login" class="brand">
			<span class="brand-mark">PB</span>
			<span>PokerBank</span>
		</a>
	</div>

	<div class="card card-pad">
		<h1 class="page-title">Accept invite</h1>

		{#if form?.error}
			<p class="alert alert-error mt-4">{form.error}</p>
		{/if}

		{#if !token}
			<p class="alert alert-error mt-4">Invitation token is missing.</p>
		{:else}
			<form method="POST" class="form-grid mt-6">
				<input name="token" type="hidden" value={token} />

				<label class="field">
					Password
					<input name="password" type="password" autocomplete="new-password" required minlength="8" />
				</label>

				<label class="field">
					Confirm password
					<input
						name="confirmPassword"
						type="password"
						autocomplete="new-password"
						required
						minlength="8"
					/>
				</label>

				<button type="submit" class="btn btn-primary">Create account</button>
			</form>
		{/if}
	</div>
</section>
