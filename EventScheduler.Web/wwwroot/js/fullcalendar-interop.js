// FullCalendar Interop for Blazor - Optimized
window.fullCalendarInterop = {
    calendar: null,
    dotNetHelper: null,
    eventChangeCache: {},

    initialize: function (elementId, dotNetHelper, events, editable) {
        console.log('Initializing FullCalendar with', events.length, 'events');
        
        // Validation
        if (typeof FullCalendar === 'undefined') {
            console.error('FullCalendar library not loaded!');
            return false;
        }
        
        const calendarEl = document.getElementById(elementId);
        if (!calendarEl) {
            console.error('Calendar element not found:', elementId);
            return false;
        }
        
        this.dotNetHelper = dotNetHelper;

        // Destroy existing calendar if any
        if (this.calendar) {
            this.calendar.destroy();
            this.calendar = null;
        }

        // Optimized calendar configuration
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
            height: 650,
            contentHeight: 'auto',
            editable: editable,
            selectable: true,
            selectMirror: true,
            selectMinDistance: 5,
            dayMaxEvents: true,
            dayMaxEventRows: 4,
            fixedWeekCount: false,
            moreLinkClick: 'popover',
            moreLinkContent: function(args) {
                return '+' + args.num + ' more';
            },
            weekends: true,
            events: events,
            eventDisplay: 'block',
            displayEventTime: false,
            displayEventEnd: false,
            eventTimeFormat: {
                hour: 'numeric',
                minute: '2-digit',
                meridiem: 'short'
            },
            views: {
                dayGridMonth: { displayEventTime: false },
                timeGridWeek: { displayEventTime: true },
                timeGridDay: { displayEventTime: true }
            },
            
            // Optimized event handlers with error handling
            eventClick: (info) => {
                info.jsEvent.preventDefault();
                const eventId = parseInt(info.event.id);
                this.invokeDotNet('OnEventClick', eventId);
            },
            
            select: (info) => {
                this.invokeDotNet('OnDateSelect', info.startStr, info.endStr, info.allDay);
            },
            
            dateClick: (info) => {
                this.invokeDotNet('OnDateClick', info.dateStr);
            },
            
            eventDrop: (info) => {
                const eventId = parseInt(info.event.id);
                this.cacheEventState(eventId, info.oldEvent);
                this.invokeDotNet('OnEventDrop', eventId, info.event.startStr, info.event.endStr, info.event.allDay);
            },
            
            eventResize: (info) => {
                const eventId = parseInt(info.event.id);
                this.cacheEventState(eventId, info.oldEvent);
                this.invokeDotNet('OnEventResize', eventId, info.event.startStr, info.event.endStr);
            },

            eventDidMount: (info) => {
                if (info.event.extendedProps.description) {
                    info.el.title = info.event.extendedProps.description;
                }
            }
        });

        this.calendar.render();
        console.log('FullCalendar initialized successfully');
        return true;
    },

    // Helper methods for optimized operations
    invokeDotNet: function(methodName, ...args) {
        if (!this.dotNetHelper) {
            console.error('DotNet helper not available');
            return;
        }
        this.dotNetHelper.invokeMethodAsync(methodName, ...args)
            .catch(err => console.error(`Error calling ${methodName}:`, err));
    },

    cacheEventState: function(eventId, oldEvent) {
        this.eventChangeCache[eventId] = {
            start: oldEvent.start,
            end: oldEvent.end,
            allDay: oldEvent.allDay
        };
    },

    updateEvents: function (events) {
        if (!this.calendar) return;
        this.calendar.removeAllEvents();
        this.calendar.addEventSource(events);
    },

    addEvent: function (event) {
        if (this.calendar) {
            this.calendar.addEvent(event);
        }
    },

    removeEvent: function (eventId) {
        if (!this.calendar) return;
        const event = this.calendar.getEventById(eventId);
        if (event) event.remove();
    },

    updateEvent: function (eventId, updates) {
        if (!this.calendar) return;
        const event = this.calendar.getEventById(eventId);
        if (!event) return;
        
        event.setProp('title', updates.title);
        event.setStart(updates.start);
        event.setEnd(updates.end);
        event.setAllDay(updates.allDay);
        event.setExtendedProp('description', updates.description);
        event.setExtendedProp('location', updates.location);
        event.setProp('backgroundColor', updates.color);
    },

    revertEvent: function(eventId) {
        const cached = this.eventChangeCache[eventId];
        if (cached && this.calendar) {
            const event = this.calendar.getEventById(eventId.toString());
            if (event) {
                event.setStart(cached.start);
                event.setEnd(cached.end);
                event.setAllDay(cached.allDay);
            }
        }
        delete this.eventChangeCache[eventId];
    },

    destroy: function () {
        if (this.calendar) {
            this.calendar.destroy();
            this.calendar = null;
        }
        this.dotNetHelper = null;
        this.eventChangeCache = {};
    },

    refetch: function () {
        if (this.calendar) {
            this.calendar.refetchEvents();
        }
    }
};

// Optimized SignalR Real-time Update Functions
window.addEventToCalendar = function(eventData) {
    const interop = window.fullCalendarInterop;
    if (!interop.calendar) return;
    
    const calendarEvent = {
        id: eventData.id.toString(),
        title: eventData.title,
        start: eventData.startDate,
        end: eventData.endDate,
        allDay: eventData.isAllDay,
        backgroundColor: eventData.color || '#3788d8',
        borderColor: eventData.color || '#3788d8',
        extendedProps: {
            description: eventData.description,
            location: eventData.location,
            eventType: eventData.eventType,
            isPublic: eventData.isPublic
        }
    };
    
    interop.calendar.addEvent(calendarEvent);
    console.log('Event added:', eventData.id);
};

window.updateEventInCalendar = function(eventData) {
    const interop = window.fullCalendarInterop;
    if (!interop.calendar) return;
    
    const event = interop.calendar.getEventById(eventData.id.toString());
    if (event) {
        event.setProp('title', eventData.title);
        event.setStart(eventData.startDate);
        event.setEnd(eventData.endDate);
        event.setAllDay(eventData.isAllDay);
        event.setProp('backgroundColor', eventData.color || '#3788d8');
        event.setProp('borderColor', eventData.color || '#3788d8');
        event.setExtendedProp('description', eventData.description);
        event.setExtendedProp('location', eventData.location);
        event.setExtendedProp('eventType', eventData.eventType);
        event.setExtendedProp('isPublic', eventData.isPublic);
        console.log('Event updated:', eventData.id);
    } else {
        console.warn('Event not found, adding instead:', eventData.id);
        window.addEventToCalendar(eventData);
    }
};

window.removeEventFromCalendar = function(eventId) {
    const interop = window.fullCalendarInterop;
    if (!interop.calendar) return;
    
    const event = interop.calendar.getEventById(eventId.toString());
    if (event) {
        event.remove();
        console.log('Event removed:', eventId);
    }
};
