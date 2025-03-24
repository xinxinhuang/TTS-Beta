document.addEventListener('DOMContentLoaded', function() {
    // Get the calendar element
    const calendarEl = document.getElementById('teesheet-calendar');
    
    if (!calendarEl) return;

    // Initialize the calendar
    const calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'timeGridWeek',
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,timeGridDay'
        },
        slotMinTime: '07:00:00',
        slotMaxTime: '19:00:00',
        slotDuration: '00:08:00',
        allDaySlot: false,
        height: 'auto',
        expandRows: true,
        navLinks: true,
        selectable: true,
        selectMirror: true,
        dayMaxEvents: true,
        weekNumbers: true,
        nowIndicator: true,
        businessHours: {
            daysOfWeek: [0, 1, 2, 3, 4, 5, 6], // Sunday - Saturday
            startTime: '07:00',
            endTime: '19:00',
        },
        // Event handling
        select: function(info) {
            // When a time slot is selected
            if (document.getElementById('event-modal')) {
                // Set form values
                document.getElementById('event-date').value = info.startStr.split('T')[0];
                document.getElementById('event-start-time').value = info.startStr.split('T')[1].substring(0, 5);
                document.getElementById('event-end-time').value = info.endStr.split('T')[1].substring(0, 5);
                
                // Show the modal
                const eventModal = new bootstrap.Modal(document.getElementById('event-modal'));
                eventModal.show();
            }
            calendar.unselect();
        },
        eventClick: function(info) {
            // When an event is clicked
            if (info.event.extendedProps.isReservation) {
                // Show reservation details
                if (document.getElementById('reservation-details-modal')) {
                    document.getElementById('reservation-title').textContent = info.event.title;
                    document.getElementById('reservation-time').textContent = 
                        info.event.start.toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'});
                    document.getElementById('reservation-players').textContent = 
                        info.event.extendedProps.players || 'N/A';
                    document.getElementById('reservation-carts').textContent = 
                        info.event.extendedProps.carts || 'N/A';
                    
                    // Show the modal
                    const reservationModal = new bootstrap.Modal(document.getElementById('reservation-details-modal'));
                    reservationModal.show();
                }
            } else if (info.event.extendedProps.isEvent) {
                // Show event details
                if (document.getElementById('event-details-modal')) {
                    document.getElementById('event-details-title').textContent = info.event.title;
                    document.getElementById('event-details-time').textContent = 
                        info.event.start.toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'}) + ' - ' +
                        info.event.end.toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'});
                    
                    // Set the delete button data attribute
                    const deleteButton = document.getElementById('delete-event-button');
                    if (deleteButton) {
                        deleteButton.setAttribute('data-event-id', info.event.id);
                    }
                    
                    // Show the modal
                    const eventDetailsModal = new bootstrap.Modal(document.getElementById('event-details-modal'));
                    eventDetailsModal.show();
                }
            }
        },
        // Load events from the server
        events: function(info, successCallback, failureCallback) {
            // Get the start and end dates from the calendar
            const start = info.startStr;
            const end = info.endStr;
            
            // Make an AJAX request to get the events
            fetch(`/api/TeeSheet/GetEvents?start=${start}&end=${end}`)
                .then(response => response.json())
                .then(data => {
                    successCallback(data);
                })
                .catch(error => {
                    console.error('Error fetching events:', error);
                    failureCallback(error);
                });
        }
    });

    calendar.render();

    // Handle event form submission
    const eventForm = document.getElementById('event-form');
    if (eventForm) {
        eventForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            // Get form data
            const eventData = {
                eventDate: document.getElementById('event-date').value,
                eventName: document.getElementById('event-name').value,
                eventStartTime: document.getElementById('event-start-time').value,
                eventEndTime: document.getElementById('event-end-time').value
            };
            
            // Submit the form using fetch
            fetch('/TeeSheet/Manage?handler=AddEvent', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                },
                body: JSON.stringify(eventData)
            })
            .then(response => {
                if (response.ok) {
                    // Close the modal
                    const eventModal = bootstrap.Modal.getInstance(document.getElementById('event-modal'));
                    eventModal.hide();
                    
                    // Refresh the calendar
                    calendar.refetchEvents();
                    
                    // Show success message
                    showAlert('Event added successfully!', 'success');
                } else {
                    // Show error message
                    showAlert('Failed to add event. Please try again.', 'danger');
                }
            })
            .catch(error => {
                console.error('Error adding event:', error);
                showAlert('An error occurred. Please try again.', 'danger');
            });
        });
    }

    // Handle event deletion
    const deleteEventButton = document.getElementById('delete-event-button');
    if (deleteEventButton) {
        deleteEventButton.addEventListener('click', function() {
            const eventId = this.getAttribute('data-event-id');
            
            // Submit the delete request
            fetch(`/TeeSheet/Manage?handler=DeleteEvent&id=${eventId}`, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                }
            })
            .then(response => {
                if (response.ok) {
                    // Close the modal
                    const eventDetailsModal = bootstrap.Modal.getInstance(document.getElementById('event-details-modal'));
                    eventDetailsModal.hide();
                    
                    // Refresh the calendar
                    calendar.refetchEvents();
                    
                    // Show success message
                    showAlert('Event deleted successfully!', 'success');
                } else {
                    // Show error message
                    showAlert('Failed to delete event. Please try again.', 'danger');
                }
            })
            .catch(error => {
                console.error('Error deleting event:', error);
                showAlert('An error occurred. Please try again.', 'danger');
            });
        });
    }

    // Helper function to show alerts
    function showAlert(message, type) {
        const alertContainer = document.getElementById('alert-container');
        if (alertContainer) {
            const alert = document.createElement('div');
            alert.className = `alert alert-${type} alert-dismissible fade show`;
            alert.role = 'alert';
            alert.innerHTML = `
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            `;
            alertContainer.appendChild(alert);
            
            // Auto-dismiss after 5 seconds
            setTimeout(() => {
                alert.classList.remove('show');
                setTimeout(() => {
                    alertContainer.removeChild(alert);
                }, 150);
            }, 5000);
        }
    }
});
