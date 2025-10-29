// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// NSFW Checkbox functionality for search page
function initNsfwCheckbox() {
    var nsfwDropdown = $('#NsfwSetting');
    var nsfwCheckbox = $('#showNsfwCheckbox');
    var searchForm = nsfwCheckbox.closest('form');
    
    if (nsfwCheckbox.length === 0) {
        return; // Not on search page
    }
    
    var initialCheckboxState = nsfwCheckbox.is(':checked');
    
    // Load NSFW checkbox state from localStorage on page load
    var savedNsfwState = localStorage.getItem('showNsfwCheckbox');
    if (savedNsfwState !== null) {
        var shouldBeChecked = savedNsfwState === 'true';
        nsfwCheckbox.prop('checked', shouldBeChecked);
        
        // If the saved state differs from the server state, auto-submit to sync
        if (shouldBeChecked !== initialCheckboxState) {
            searchForm.submit();
        }
    }
    
    // Auto-check NSFW checkbox when NSFW dropdown is selected
    nsfwDropdown.on('change', function() {
        var selectedValue = $(this).val();
        // If NSFW only (value 2) is selected, automatically check the NSFW checkbox
        if (selectedValue == '2' && !nsfwCheckbox.is(':checked')) {
            nsfwCheckbox.prop('checked', true);
            localStorage.setItem('showNsfwCheckbox', 'true');
        }
    });
    
    // When NSFW checkbox is clicked, save state and auto-submit form
    nsfwCheckbox.on('change', function() {
        var isChecked = $(this).is(':checked');
        localStorage.setItem('showNsfwCheckbox', isChecked.toString());
        searchForm.submit();
    });
}

// Initialize when document is ready
$(document).ready(function() {
    initNsfwCheckbox();
});
