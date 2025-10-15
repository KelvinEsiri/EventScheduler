// FullCalendar Interop for Blazor
window.fullCalendarInterop = {
    calendar: null,
    dotNetHelper: null,

    initialize: function (elementId, dotNetHelper, events, editable) {
        console.log('Initializing FullCalendar...', { elementId, eventCount: events.length });
        
        // Check if FullCalendar is loaded
        if (typeof FullCalendar === 'undefined') {
            console.error('FullCalendar library not loaded!');
            return false;
        }
        
        this.dotNetHelper = dotNetHelper;
        const calendarEl = document.getElementById(elementId);
        
        if (!calendarEl) {
            console.error('Calendar element not found:', elementId);
            return false;
        }
        
        console.log('Calendar element found:', calendarEl);

        // Destroy existing calendar if any
        if (this.calendar) {
            this.calendar.destroy();
        }

        this.calendar = new FullCalendar.Calendar(calendarEl, {
            initialView: 'dayGridMonth',
            headerToolbar: {
                left: 'prev,next today',
                center: 'title',
                right: 'dayGridMonth,timeGridWeek,timeGridDay,listWeek'
            },
            buttonText: {
                today: 'Today',
                month: 'Month',
                week: 'Week',
                day: 'Day',
                list: 'List'
            },
            height: 'auto',
            editable: editable,
            selectable: true,
            selectMirror: true,
            dayMaxEvents: true,
            weekends: true,
            events: events,
            
            // Event click - edit existing event
            eventClick: function(info) {
                info.jsEvent.preventDefault();
                dotNetHelper.invokeMethodAsync('OnEventClick', parseInt(info.event.id));
            },
            
            // Date select - create new event
            select: function(info) {
                dotNetHelper.invokeMethodAsync('OnDateSelect', 
                    info.startStr, 
                    info.endStr, 
                    info.allDay
                );
            },
            
            // Event drag and drop
            eventDrop: function(info) {
                dotNetHelper.invokeMethodAsync('OnEventDrop',
                    parseInt(info.event.id),
                    info.event.startStr,
                    info.event.endStr,
                    info.event.allDay
                );
            },
            
            // Event resize
            eventResize: function(info) {
                dotNetHelper.invokeMethodAsync('OnEventResize',
                    parseInt(info.event.id),
                    info.event.startStr,
                    info.event.endStr
                );
            },

            // Custom event styling
            eventDidMount: function(info) {
                // Add tooltip
                if (info.event.extendedProps.description) {
                    info.el.title = info.event.extendedProps.description;
                }
            }
        });

        this.calendar.render();
        console.log('FullCalendar rendered successfully!');
        return true;
    },

    updateEvents: function (events) {
        if (this.calendar) {
            this.calendar.removeAllEvents();
            this.calendar.addEventSource(events);
        }
    },

    addEvent: function (event) {
        if (this.calendar) {
            this.calendar.addEvent(event);
        }
    },

    removeEvent: function (eventId) {
        if (this.calendar) {
            const event = this.calendar.getEventById(eventId);
            if (event) {
                event.remove();
            }
        }
    },

    updateEvent: function (eventId, updates) {
        if (this.calendar) {
            const event = this.calendar.getEventById(eventId);
            if (event) {
                event.setProp('title', updates.title);
                event.setStart(updates.start);
                event.setEnd(updates.end);
                event.setAllDay(updates.allDay);
                event.setExtendedProp('description', updates.description);
                event.setExtendedProp('location', updates.location);
                event.setProp('backgroundColor', updates.color);
            }
        }
    },

    destroy: function () {
        if (this.calendar) {
            this.calendar.destroy();
            this.calendar = null;
        }
    },

    refetch: function () {
        if (this.calendar) {
            this.calendar.refetchEvents();
        }
    }
};
