// FullCalendar Interop for Blazor - Multi-Instance Support
window.fullCalendarInterop = {
    calendars: {},  // Multiple calendars by elementId
    dotNetHelpers: {},  // Store dotNetHelper per calendar
    eventChangeCache: {},
    isBlazorConnected: true,  // Track Blazor circuit state

    // Check if Blazor circuit is connected
    checkBlazorConnection: function() {
        // First check browser's native online status
        if (!navigator.onLine) {
            console.log('🔌 Browser reports offline');
            this.isBlazorConnected = false;
            return false;
        }
        
        // Check connectivity manager if available (more accurate)
        if (window.connectivityManager) {
            const managerOnline = window.connectivityManager.isOnline;
            if (!managerOnline) {
                console.log('🔌 Connectivity manager reports offline');
                this.isBlazorConnected = false;
                return false;
            }
        }
        
        // Check if Blazor is available and has an active circuit
        if (!window.Blazor) {
            this.isBlazorConnected = false;
            return false;
        }
        
        // All checks passed - we're online and Blazor should be connected
        // If isBlazorConnected was previously false, restore it to true
        if (!this.isBlazorConnected && navigator.onLine) {
            console.log('🔌 Connection restored - re-enabling Blazor calls');
            this.isBlazorConnected = true;
        }
        
        return this.isBlazorConnected;
    },

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
                
                // Check if we're offline
                if (!this.checkBlazorConnection()) {
                    console.log('⚠️ Offline mode - showing cached event info');
                    const title = info.event.title;
                    const start = info.event.start;
                    const end = info.event.end;
                    const desc = info.event.extendedProps?.description || 'No description';
                    const location = info.event.extendedProps?.location || 'No location';
                    alert(`${title}\n\n${desc}\n\nLocation: ${location}\nStarts: ${start ? start.toLocaleString() : 'N/A'}\nEnds: ${end ? end.toLocaleString() : 'N/A'}\n\n⚠️ You're offline - full details and editing available when online`);
                    return;
                }
                
                // Try to call Blazor with timeout
                const clickPromise = this.invokeDotNet(elementId, 'OnEventClick', eventId);
                const timeoutPromise = new Promise((_, reject) => 
                    setTimeout(() => reject(new Error('Click timeout - offline')), 500)
                );
                
                Promise.race([clickPromise, timeoutPromise])
                    .catch(err => {
                        console.log('⚠️ Event click failed - showing cached info');
                        this.isBlazorConnected = false;
                        const title = info.event.title;
                        const start = info.event.start;
                        const end = info.event.end;
                        const desc = info.event.extendedProps?.description || 'No description';
                        const location = info.event.extendedProps?.location || 'No location';
                        alert(`${title}\n\n${desc}\n\nLocation: ${location}\nStarts: ${start ? start.toLocaleString() : 'N/A'}\nEnds: ${end ? end.toLocaleString() : 'N/A'}\n\n⚠️ You're offline - full details and editing available when online`);
                    });
            },
            
            select: (info) => {
                // Check if we're offline
                if (!this.checkBlazorConnection()) {
                    console.log('⚠️ Offline mode - date selection disabled');
                    alert('⚠️ You\'re offline\n\nCreating new events is only available when online.\n\nYou can still drag/resize existing events - they will sync when you\'re back online.');
                    return;
                }
                
                // Try to call Blazor with timeout
                const selectPromise = this.invokeDotNet(elementId, 'OnDateSelect', info.startStr, info.endStr, info.allDay);
                const timeoutPromise = new Promise((_, reject) => 
                    setTimeout(() => reject(new Error('Select timeout - offline')), 500)
                );
                
                Promise.race([selectPromise, timeoutPromise])
                    .catch(err => {
                        console.log('⚠️ Date select failed - offline mode');
                        this.isBlazorConnected = false;
                        alert('⚠️ You\'re offline\n\nCreating new events is only available when online.\n\nYou can still drag/resize existing events - they will sync when you\'re back online.');
                    });
            },
            
            dateClick: (info) => {
                // Check if we're offline
                if (!this.checkBlazorConnection()) {
                    console.log('⚠️ Offline mode - date click disabled');
                    alert('⚠️ You\'re offline\n\nCreating new events is only available when online.\n\nYou can still drag/resize existing events - they will sync when you\'re back online.');
                    return;
                }
                
                // Try to call Blazor with timeout
                const clickPromise = this.invokeDotNet(elementId, 'OnDateClick', info.dateStr);
                const timeoutPromise = new Promise((_, reject) => 
                    setTimeout(() => reject(new Error('Date click timeout - offline')), 500)
                );
                
                Promise.race([clickPromise, timeoutPromise])
                    .catch(err => {
                        console.log('⚠️ Date click failed - offline mode');
                        this.isBlazorConnected = false;
                        alert('⚠️ You\'re offline\n\nCreating new events is only available when online.\n\nYou can still drag/resize existing events - they will sync when you\'re back online.');
                    });
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
                
                console.log(`🎯 Event ${eventId} dropped - checking connection...`);
                
                // Check if Blazor circuit is connected
                const isConnected = this.checkBlazorConnection();
                console.log(`� Blazor circuit connected: ${isConnected}`);
                
                if (!isConnected) {
                    // Circuit disconnected - go straight to offline mode
                    console.log('⚠️ Circuit disconnected - using offline mode directly');
                    this.handleOfflineEventDrop(info, eventId, elementId);
                    return;
                }
                
                // Try Blazor save with timeout protection
                console.log(`📤 Attempting Blazor save...`);
                const savePromise = this.invokeDotNet(elementId, 'OnEventDrop', eventId, info.event.startStr, info.event.endStr, info.event.allDay);
                const timeoutPromise = new Promise((_, reject) => 
                    setTimeout(() => reject(new Error('Blazor call timeout - likely offline')), 500)
                );
                
                Promise.race([savePromise, timeoutPromise])
                    .then(() => {
                        console.log(`✅ Event ${eventId} saved via Blazor successfully`);
                        info.el.style.opacity = '1';
                        info.el.classList.remove('event-saving');
                        info.el.classList.add('event-saved');
                        setTimeout(() => info.el.classList.remove('event-saved'), 1000);
                    })
                    .catch(async (err) => {
                        console.log(`❌ Blazor save failed - switching to offline mode`);
                        this.isBlazorConnected = false; // Mark circuit as disconnected
                        this.handleOfflineEventDrop(info, eventId, elementId);
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
                    .catch(async (err) => {
                        console.log(`❌ Blazor resize failed for event ${eventId}`);
                        console.log(`🔍 Error:`, err);
                        
                        // Check if error is due to connection loss
                        const errorMessage = err?.message || String(err) || '';
                        const isOfflineError = errorMessage.includes('not in the \'Connected\' State') ||
                            errorMessage.includes('Cannot send data') ||
                            errorMessage.includes('connection') ||
                            errorMessage.includes('disconnect');
                        
                        if (isOfflineError) {
                            console.log('⚠️ Offline mode - saving event resize locally');
                            
                            // Save directly to IndexedDB
                            try {
                                await window.indexedDBManager.updateEventDates(
                                    eventId, 
                                    info.event.startStr, 
                                    info.event.endStr, 
                                    info.event.allDay
                                );
                                
                                // Queue the operation for sync with COMPLETE event data
                                const token = localStorage.getItem('auth_token');
                                if (token && window.indexedDBManager) {
                                    // Get full event data from IndexedDB
                                    const fullEvent = await window.indexedDBManager.getEvent(eventId);
                                    
                                    if (fullEvent) {
                                        // Convert date strings to ISO 8601 datetime format for API
                                        const startDate = info.event.start ? info.event.start.toISOString() : info.event.startStr;
                                        const endDate = info.event.end ? info.event.end.toISOString() : info.event.endStr;
                                        
                                        console.log('📅 Queueing resize sync with dates:', { startDate, endDate, isAllDay: info.event.allDay });
                                        
                                        await window.indexedDBManager.savePendingOperation({
                                            type: 'PUT',
                                            endpoint: `/api/events/${eventId}`,
                                            data: {
                                                title: fullEvent.title,
                                                description: fullEvent.description || '',
                                                startDate: startDate,
                                                endDate: endDate,
                                                location: fullEvent.location || '',
                                                isAllDay: info.event.allDay,
                                                color: fullEvent.color || '#3788d8',
                                                categoryId: fullEvent.categoryId || null,
                                                status: fullEvent.status || 'Pending',
                                                eventType: fullEvent.eventType || 'Other',
                                                isPublic: fullEvent.isPublic || false
                                            },
                                            token: token,
                                            timestamp: new Date().toISOString()
                                        });
                                    }
                                }
                                
                                info.el.style.opacity = '1';
                                info.el.classList.remove('event-saving');
                                info.el.classList.add('event-saved');
                                info.el.title = 'Saved offline - will sync when online';
                                setTimeout(() => info.el.classList.remove('event-saved'), 1000);
                                
                                console.log('✅ Event resize saved offline, will sync when connection restored');
                            } catch (offlineErr) {
                                console.error('Failed to save offline:', offlineErr);
                                info.el.style.opacity = '1';
                                info.el.classList.remove('event-saving');
                                this.revertEvent(elementId, eventId);
                            }
                        } else {
                            console.error('Error resizing event:', err);
                            info.el.style.opacity = '1';
                            info.el.classList.remove('event-saving');
                            this.revertEvent(elementId, eventId);
                        }
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
                // Log connection errors gracefully - but let offline operations through
                if (err.message && err.message.includes('not in the \'Connected\' State')) {
                    console.warn(`[${elementId}] ⚠️ ${methodName} failed - Connection error (will retry via sync)`);
                } else {
                    console.error(`[${elementId}] Error calling ${methodName}:`, err);
                }
                throw err;
            });
    },

    // Handle offline event drop (pure JavaScript, no Blazor needed)
    handleOfflineEventDrop: async function(info, eventId, elementId) {
        console.log('💾 Saving event change offline...');
        
        try {
            // Update event dates in IndexedDB
            await window.indexedDBManager.updateEventDates(
                eventId, 
                info.event.startStr, 
                info.event.endStr, 
                info.event.allDay
            );
            
            // Queue the operation for sync with COMPLETE event data
            const token = localStorage.getItem('auth_token');
            if (token && window.indexedDBManager) {
                // Get full event data from IndexedDB
                const fullEvent = await window.indexedDBManager.getEvent(eventId);
                
                if (fullEvent) {
                    // Convert date strings to ISO 8601 datetime format for API
                    const startDate = info.event.start ? info.event.start.toISOString() : info.event.startStr;
                    const endDate = info.event.end ? info.event.end.toISOString() : info.event.endStr;
                    
                    console.log('📅 Queueing sync operation:', { 
                        eventId, 
                        title: fullEvent.title,
                        startDate, 
                        endDate, 
                        isAllDay: info.event.allDay 
                    });
                    
                    await window.indexedDBManager.savePendingOperation({
                        type: 'PUT',
                        endpoint: `/api/events/${eventId}`,
                        data: {
                            title: fullEvent.title,
                            description: fullEvent.description || '',
                            startDate: startDate,
                            endDate: endDate,
                            location: fullEvent.location || '',
                            isAllDay: info.event.allDay,
                            color: fullEvent.color || '#3788d8',
                            categoryId: fullEvent.categoryId || null,
                            status: fullEvent.status || 'Pending',
                            eventType: fullEvent.eventType || 'Other',
                            isPublic: fullEvent.isPublic || false
                        },
                        token: token,
                        timestamp: new Date().toISOString()
                    });
                }
            }
            
            // Update UI
            info.el.style.opacity = '1';
            info.el.classList.remove('event-saving');
            info.el.classList.add('event-saved');
            info.el.title = 'Saved offline - will sync when online';
            setTimeout(() => info.el.classList.remove('event-saved'), 1000);
            
            console.log('✅ Event saved offline successfully, queued for sync');
        } catch (offlineErr) {
            console.error('❌ Failed to save offline:', offlineErr);
            info.el.style.opacity = '1';
            info.el.classList.remove('event-saving');
            this.revertEvent(elementId, eventId);
        }
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
    
    console.log('🎨 updateEventInCalendar received:', {
        id: eventData.id,
        title: eventData.title,
        color: eventData.color,
        eventType: eventData.eventType,
        fullEventData: eventData
    });
    
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
        console.log(`Event updated in calendar '${elementId || 'default'}':`, eventData.id, 'with color:', eventData.color);
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
