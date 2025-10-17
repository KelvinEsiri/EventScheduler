// FullCalendar Interop for Blazor - Multi-Instance Support
window.fullCalendarInterop = {
    calendars: {},  // Multiple calendars by elementId
    dotNetHelpers: {},  // Store dotNetHelper per calendar
    eventChangeCache: {},

    initialize: function (elementId, dotNetHelper, events, editable) {
        console.log(`[${elementId}] Initializing FullCalendar with ${events.length} events`);
        
        // Validation
        if (typeof FullCalendar === 'undefined') {
            console.error('FullCalendar library not loaded!');
            return false;
        }
        
        const calendarEl = document.getElementById(elementId);
        if (!calendarEl) {
            console.error(`Calendar element not found: ${elementId}`);
            return false;
        }
        
        this.dotNetHelpers[elementId] = dotNetHelper;

        // Destroy existing calendar for this element if any
        if (this.calendars[elementId]) {
            console.log(`[${elementId}] Destroying existing calendar`);
            this.calendars[elementId].destroy();
            delete this.calendars[elementId];
        }

        // Optimized calendar configuration
        this.calendars[elementId] = new FullCalendar.Calendar(calendarEl, {
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
            
            // Event handlers with elementId context
            eventClick: (info) => {
                info.jsEvent.preventDefault();
                const eventId = parseInt(info.event.id);
                this.invokeDotNet(elementId, 'OnEventClick', eventId);
            },
            
            select: (info) => {
                this.invokeDotNet(elementId, 'OnDateSelect', info.startStr, info.endStr, info.allDay);
            },
            
            dateClick: (info) => {
                this.invokeDotNet(elementId, 'OnDateClick', info.dateStr);
            },
            
            eventDrop: (info) => {
                const eventId = parseInt(info.event.id);
                this.cacheEventState(eventId, info.oldEvent);
                
                if (!info.event.startStr || !info.event.endStr) {
                    console.error('Invalid date strings:', info.event.startStr, info.event.endStr);
                    this.revertEvent(elementId, eventId);
                    return;
                }
                
                info.el.style.opacity = '0.6';
                info.el.classList.add('event-saving');
                
                this.invokeDotNet(elementId, 'OnEventDrop', eventId, info.event.startStr, info.event.endStr, info.event.allDay)
                    .then(() => {
                        info.el.style.opacity = '1';
                        info.el.classList.remove('event-saving');
                        info.el.classList.add('event-saved');
                        setTimeout(() => info.el.classList.remove('event-saved'), 1000);
                    })
                    .catch((err) => {
                        console.error('Error saving event:', err);
                        info.el.style.opacity = '1';
                        info.el.classList.remove('event-saving');
                        this.revertEvent(elementId, eventId);
                    });
            },
            
            eventResize: (info) => {
                const eventId = parseInt(info.event.id);
                this.cacheEventState(eventId, info.oldEvent);
                
                if (!info.event.startStr || !info.event.endStr) {
                    console.error('Invalid date strings for resize:', info.event.startStr, info.event.endStr);
                    this.revertEvent(elementId, eventId);
                    return;
                }
                
                info.el.style.opacity = '0.6';
                info.el.classList.add('event-saving');
                
                this.invokeDotNet(elementId, 'OnEventResize', eventId, info.event.startStr, info.event.endStr)
                    .then(() => {
                        info.el.style.opacity = '1';
                        info.el.classList.remove('event-saving');
                        info.el.classList.add('event-saved');
                        setTimeout(() => info.el.classList.remove('event-saved'), 1000);
                    })
                    .catch((err) => {
                        console.error('Error resizing event:', err);
                        info.el.style.opacity = '1';
                        info.el.classList.remove('event-saving');
                        this.revertEvent(elementId, eventId);
                    });
            },

            eventDidMount: (info) => {
                if (info.event.extendedProps.description) {
                    info.el.title = info.event.extendedProps.description;
                }
            }
        });

        this.calendars[elementId].render();
        
        // Force calendar to recalculate size after render
        setTimeout(() => {
            if (this.calendars[elementId]) {
                this.calendars[elementId].updateSize();
                console.log(`[${elementId}] Calendar size updated`);
            }
        }, 100);
        
        console.log(`[${elementId}] FullCalendar initialized successfully`);
        return true;
    },

    // Helper method to invoke .NET methods
    invokeDotNet: function(elementId, methodName, ...args) {
        const helper = this.dotNetHelpers[elementId];
        if (!helper) {
            console.error(`[${elementId}] DotNet helper not available`);
            return Promise.reject('DotNet helper not available');
        }
        return helper.invokeMethodAsync(methodName, ...args)
            .catch(err => {
                console.error(`[${elementId}] Error calling ${methodName}:`, err);
                throw err;
            });
    },

    cacheEventState: function(eventId, oldEvent) {
        this.eventChangeCache[eventId] = {
            start: oldEvent.start,
            end: oldEvent.end,
            allDay: oldEvent.allDay
        };
    },

    // Get calendar by ID (defaults to first calendar if not specified)
    getCalendar: function(elementId) {
        if (elementId && this.calendars[elementId]) {
            return this.calendars[elementId];
        }
        // Fallback to first calendar for backward compatibility
        const calendarIds = Object.keys(this.calendars);
        return calendarIds.length > 0 ? this.calendars[calendarIds[0]] : null;
    },

    updateEvents: function (events, elementId = null) {
        const calendar = elementId ? this.calendars[elementId] : this.getCalendar();
        if (!calendar) {
            console.warn('No calendar available for updateEvents');
            return;
        }
        calendar.removeAllEvents();
        calendar.addEventSource(events);
    },

    addEvent: function (event, elementId = null) {
        const calendar = elementId ? this.calendars[elementId] : this.getCalendar();
        if (calendar) {
            calendar.addEvent(event);
        }
    },

    removeEvent: function (eventId, elementId = null) {
        const calendar = elementId ? this.calendars[elementId] : this.getCalendar();
        if (!calendar) return;
        const event = calendar.getEventById(eventId);
        if (event) event.remove();
    },

    updateEvent: function (eventId, updates, elementId = null) {
        const calendar = elementId ? this.calendars[elementId] : this.getCalendar();
        if (!calendar) return;
        const event = calendar.getEventById(eventId);
        if (!event) return;
        
        event.setProp('title', updates.title);
        event.setStart(updates.start);
        event.setEnd(updates.end);
        event.setAllDay(updates.allDay);
        event.setExtendedProp('description', updates.description);
        event.setExtendedProp('location', updates.location);
        event.setProp('backgroundColor', updates.color);
    },

    revertEvent: function(elementId, eventId) {
        const cached = this.eventChangeCache[eventId];
        const calendar = this.calendars[elementId];
        if (cached && calendar) {
            const event = calendar.getEventById(eventId.toString());
            if (event) {
                event.setStart(cached.start);
                event.setEnd(cached.end);
                event.setAllDay(cached.allDay);
            }
        }
        delete this.eventChangeCache[eventId];
    },

    destroy: function (elementId = null) {
        if (elementId) {
            // Destroy specific calendar
            if (this.calendars[elementId]) {
                this.calendars[elementId].destroy();
                delete this.calendars[elementId];
            }
            if (this.dotNetHelpers[elementId]) {
                delete this.dotNetHelpers[elementId];
            }
            console.log(`[${elementId}] Calendar destroyed`);
        } else {
            // Destroy all calendars (backward compatibility)
            Object.keys(this.calendars).forEach(id => {
                this.calendars[id].destroy();
            });
            this.calendars = {};
            this.dotNetHelpers = {};
            this.eventChangeCache = {};
            console.log('All calendars destroyed');
        }
    },

    refetch: function (elementId = null) {
        const calendar = elementId ? this.calendars[elementId] : this.getCalendar();
        if (calendar) {
            calendar.refetchEvents();
        }
    },

    // Force calendar to recalculate its size
    updateSize: function (elementId = null) {
        const calendar = elementId ? this.calendars[elementId] : this.getCalendar();
        if (calendar) {
            calendar.updateSize();
            console.log(`[${elementId || 'default'}] Calendar size updated`);
        }
    }
};

// Real-time Update Functions - with element ID support
window.addEventToCalendar = function(eventData, elementId = null) {
    const interop = window.fullCalendarInterop;
    const calendar = elementId ? interop.calendars[elementId] : interop.getCalendar();
    if (!calendar) return;
    
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
    
    calendar.addEvent(calendarEvent);
    console.log(`Event added to calendar '${elementId || 'default'}':`, eventData.id);
};

window.updateEventInCalendar = function(eventData, elementId = null) {
    const interop = window.fullCalendarInterop;
    const calendar = elementId ? interop.calendars[elementId] : interop.getCalendar();
    if (!calendar) return;
    
    const event = calendar.getEventById(eventData.id.toString());
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
        console.log(`Event updated in calendar '${elementId || 'default'}':`, eventData.id);
    } else {
        console.warn('Event not found, adding instead:', eventData.id);
        window.addEventToCalendar(eventData, elementId);
    }
};

window.removeEventFromCalendar = function(eventId, elementId = null) {
    const interop = window.fullCalendarInterop;
    const calendar = elementId ? interop.calendars[elementId] : interop.getCalendar();
    if (!calendar) return;
    
    const event = calendar.getEventById(eventId.toString());
    if (event) {
        event.remove();
        console.log(`Event removed from calendar '${elementId || 'default'}':`, eventId);
    }
};
