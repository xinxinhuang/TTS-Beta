/**
 * TeeTime Calendar Logic
 * Handles the FullCalendar integration for booking tee times
 * - Initializes the FullCalendar
 * - Fetches booked dates from the server
 * - Disables dates that are fully booked 
 * - Shows dates with limited availability in yellow
 */

document.addEventListener('DOMContentLoaded', function() {
    initializeCalendar();
});

/**
 * Initializes the FullCalendar for date selection
 */
function initializeCalendar() {
    const calendarEl = document.getElementById('calendar');
    const dateInput = document.getElementById('SelectedDate');
    const checkTimesButton = document.getElementById('check-times-btn');
    const form = document.getElementById('date-selection-form');
    
    if (!calendarEl) return;
    
    // Get min and max dates from the hidden input
    const minDate = dateInput.getAttribute('min');
    const maxDate = dateInput.getAttribute('max');
    
    // Set default date if not already set
    if (!dateInput.value) {
        dateInput.value = new Date().toISOString().slice(0, 10);
    }
    
    // Create calendar
    const calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        selectable: true,
        validRange: {
            start: minDate || new Date(),
            end: maxDate || new Date(new Date().setDate(new Date().getDate() + 14))
        },
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: ''
        },
        dateClick: function(info) {
            const clickedDate = info.dateStr;
            const selectedDateElement = document.querySelector('.date-selected');
            
            // Don't allow selection of past dates or fully booked dates
            if (new Date(clickedDate) < new Date(new Date().setHours(0,0,0,0)) || 
                info.dayEl.classList.contains('date-fully-booked')) {
                return;
            }
            
            // Remove previous selection
            if (selectedDateElement) {
                selectedDateElement.classList.remove('date-selected');
            }
            
            // Update the selection
            info.dayEl.classList.add('date-selected');
            dateInput.value = clickedDate;
            
            // Enable the submit button
            checkTimesButton.disabled = false;
        },
        datesSet: function(dateInfo) {
            fetchDateAvailability()
                .then(dateAvailability => {
                    applyDateAvailabilityStyling(calendar, dateAvailability);
                });
        }
    });
    
    calendar.render();
    
    // Fetch initial date availability
    fetchDateAvailability()
        .then(dateAvailability => {
            applyDateAvailabilityStyling(calendar, dateAvailability);
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
 * Applies styling to dates in the calendar based on availability
 * @param {FullCalendar.Calendar} calendar - The FullCalendar instance
 * @param {Object} dateAvailability - Object containing date availability information
 */
function applyDateAvailabilityStyling(calendar, dateAvailability) {
    // Get all date cells
    const dateElements = document.querySelectorAll('.fc-daygrid-day');
    
    // Reset all styling first
    dateElements.forEach(el => {
        el.classList.remove('date-available', 'date-limited-availability', 'date-fully-booked');
        // Remove any existing tooltips
        const tooltip = el.querySelector('.date-tooltip');
        if (tooltip) {
            tooltip.remove();
        }
    });
    
    // Apply styling based on availability
    dateElements.forEach(el => {
        const date = el.getAttribute('data-date');
        
        // Past dates
        if (new Date(date) < new Date(new Date().setHours(0,0,0,0))) {
            el.classList.add('fc-day-past');
            return;
        }
        
        // Apply availability status
        if (dateAvailability.fullyBooked.includes(date)) {
            el.classList.add('date-fully-booked');
            addTooltip(el, 'Fully Booked', 'danger', dateAvailability.dateSummaries[date]);
        } else if (dateAvailability.limitedAvailability.includes(date)) {
            el.classList.add('date-limited-availability');
            addTooltip(el, 'Limited Availability', 'warning', dateAvailability.dateSummaries[date]);
        } else {
            el.classList.add('date-available');
            addTooltip(el, 'Available', 'success', dateAvailability.dateSummaries[date]);
        }
    });
    
    // If there's a selected date, highlight it
    const selectedDate = document.getElementById('SelectedDate').value;
    if (selectedDate) {
        const selectedDateElement = document.querySelector(`[data-date="${selectedDate}"]`);
        if (selectedDateElement) {
            selectedDateElement.classList.add('date-selected');
        }
    }
}

/**
 * Adds a tooltip to a date cell
 * @param {HTMLElement} element - The date cell element
 * @param {string} status - The availability status text
 * @param {string} type - The tooltip type (success, warning, danger)
 * @param {Object} data - The date summary data
 */
function addTooltip(element, status, type, data) {
    if (!data) return;
    
    // Get the content area where we'll add the tooltip
    const contentEl = element.querySelector('.fc-daygrid-day-top');
    if (!contentEl) return;
    
    // Create tooltip element
    const tooltip = document.createElement('div');
    tooltip.className = `date-tooltip tooltip-${type}`;
    tooltip.innerHTML = `
        <strong>${status}</strong>
        <div class="tooltip-details">
            <span>${data.availableSlots} of ${data.totalSlots} tee times available</span>
            <div class="progress">
                <div class="progress-bar bg-${type}" style="width: ${data.percentAvailable}%">${data.percentAvailable}%</div>
            </div>
        </div>
    `;
    
    // Position the tooltip
    element.addEventListener('mouseenter', function() {
        tooltip.style.display = 'block';
    });
    
    element.addEventListener('mouseleave', function() {
        tooltip.style.display = 'none';
    });
    
    // Initially hidden
    tooltip.style.display = 'none';
    
    // Add to the DOM
    element.appendChild(tooltip);
}
