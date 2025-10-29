document.addEventListener('DOMContentLoaded', () => {
	$('#delete-form').on('submit', (event) => {
		event.preventDefault();
		$('#confirm-delete-modal').modal('show');
	})

	const copyNaiJsonButton = $('#get-nai-json');
	copyNaiJsonButton.on('click', async (_event) => {
		try {
			const promptId = copyNaiJsonButton.data("id");
			if (promptId) {
				const response = await fetch(`/${promptId}/nai-scenario`);
				const json = await response.text();
				await navigator.clipboard.writeText(json);
				const currentText = copyNaiJsonButton.text();
				copyNaiJsonButton.text("Copied!");
				setTimeout(() => {
					copyNaiJsonButton.text(currentText);
				}, 1000)
			}
		} catch(err){
			alert(`There was an error copying to clipboard: ${err}`);
		}
	})

	// Sub-scenario expand/collapse functionality
	$('.sub-scenario-toggle').on('click', function() {
		const button = $(this);
		const targetId = button.data('target');
		const content = $('#' + targetId);
		const caret = button.find('.sub-scenario-caret');
		const isExpanded = button.attr('aria-expanded') === 'true';

		if (isExpanded) {
			// Collapse
			content.slideUp(200);
			caret.removeClass('expanded');
			button.attr('aria-expanded', 'false');
		} else {
			// Expand
			content.slideDown(200);
			caret.addClass('expanded');
			button.attr('aria-expanded', 'true');
		}
	});
});
