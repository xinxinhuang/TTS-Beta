/**
 * TeeTime Date Picker Logic
 * Handles the date selection for booking tee times
 * - Fetches booked dates from the server
 * - Disables dates that are fully booked
 * - Grays out dates with limited availability
 */

document.addEventListener('DOMContentLoaded', function() {
    const dateInput = document.getElementById('SelectedDate');
    
    if (dateInput) {
        // Initialize our custom date picker handling
        initializeDateDisabling(dateInput);
    }
});

/**
 * Initializes date disabling functionality
 * @param {HTMLInputElement} dateInput - The date input element
 */
function initializeDateDisabling(dateInput) {
    // Fetch date availability data
    fetchDateAvailability()
        .then(dateAvailability => {
            // Store the dates to be used when date input is clicked
            window.teeTimeDateAvailability = dateAvailability;
            
            // Add event listener for the date input
            dateInput.addEventListener('click', function(e) {
                // This triggers before the browser's date picker opens
                applyDateAvailabilityStyling(dateAvailability);
            });
            
            // Add change event listener to validate selected dates
            dateInput.addEventListener('change', function(e) {
                validateSelectedDate(dateInput, dateAvailability);
            });
            
            // Check if date is already selected and validate it
            if (dateInput.value) {
                validateSelectedDate(dateInput, dateAvailability);
            }
        })
        .catch(error => {
            console.error('Error loading date availability:', error);
        });
}

/**
 * Fetches date availability from the server
 * @returns {Promise<Object>} - Promise resolving to date availability data
 */
function fetchDateAvailability() {
    return fetch('/api/teetime/availability')
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        });
}

/**
 * Applies styling to dates based on availability
 * @param {Object} dateAvailability - Object containing date availability information
 */
function applyDateAvailabilityStyling(dateAvailability) {
    // Since we can't modify the browser's native date picker display directly,
    // we'll add a custom calendar as an alternative approach
    
    // Create a hidden input field to store available dates information as JSON
    let availabilityDataElement = document.getElementById('date-availability-data');
    
    if (!availabilityDataElement) {
        availabilityDataElement = document.createElement('input');
        availabilityDataElement.type = 'hidden';
        availabilityDataElement.id = 'date-availability-data';
        document.querySelector('form').appendChild(availabilityDataElement);
    }
    
    // Store the availability data for server-side validation
    availabilityDataElement.value = JSON.stringify(dateAvailability);
    
    // Add a warning message about unavailable dates
    if (dateAvailability.fullyBooked.length > 0) {
        const warningMsg = document.createElement('div');
        warningMsg.className = 'text-danger small mt-1';
        warningMsg.id = 'date-picker-warning';
        warningMsg.innerHTML = `<i class="bi bi-exclamation-triangle"></i> Some dates are unavailable due to full booking.`;
        
        // Add after date input if warning doesn't already exist
        if (!document.getElementById('date-picker-warning')) {
            const dateInput = document.getElementById('SelectedDate');
            dateInput.parentNode.insertBefore(warningMsg, dateInput.nextSibling);
        }
    }
}

/**
 * Validates a selected date against availability
 * @param {HTMLInputElement} dateInput - The date input element
 * @param {Object} dateAvailability - Object containing date availability information
 */
function validateSelectedDate(dateInput, dateAvailability) {
    const selectedDate = dateInput.value;
    
    if (dateAvailability.fullyBooked.includes(selectedDate)) {
        // Show error message if date is fully booked
        showDateErrorMessage(dateInput, 'This date is fully booked. Please select another date.');
        dateInput.value = ''; // Clear the selection
        dateInput.classList.add('is-invalid');
    } 
    else if (dateAvailability.limitedAvailability.includes(selectedDate)) {
        // Show warning for limited availability
        showDateWarningMessage(dateInput, 'Limited tee times available for this date.');
        dateInput.classList.add('border-warning');
    }
    else {
        // Date is available, clear any warnings
        dateInput.classList.remove('is-invalid');
        dateInput.classList.remove('border-warning');
        clearDateMessages();
    }
}

/**
 * Displays an error message for the date input
 * @param {HTMLInputElement} dateInput - The date input element
 * @param {string} message - The error message to display
 */
function showDateErrorMessage(dateInput, message) {
    // Find or create error message element
    let errorElement = document.getElementById('date-error-message');
    
    if (!errorElement) {
        errorElement = document.createElement('div');
        errorElement.id = 'date-error-message';
        errorElement.className = 'text-danger mt-2';
        dateInput.parentNode.appendChild(errorElement);
    }
    
    errorElement.textContent = message;
}

/**
 * Displays a warning message for the date input
 * @param {HTMLInputElement} dateInput - The date input element
 * @param {string} message - The warning message to display
 */
function showDateWarningMessage(dateInput, message) {
    // Find or create warning message element
    let warningElement = document.getElementById('date-warning-message');
    
    if (!warningElement) {
        warningElement = document.createElement('div');
        warningElement.id = 'date-warning-message';
        warningElement.className = 'text-warning mt-2';
        dateInput.parentNode.appendChild(warningElement);
    }
    
    warningElement.textContent = message;
}

/**
 * Clears all date-related messages
 */
function clearDateMessages() {
    const errorElement = document.getElementById('date-error-message');
    if (errorElement) {
        errorElement.remove();
    }
    
    const warningElement = document.getElementById('date-warning-message');
    if (warningElement) {
        warningElement.remove();
    }
}
