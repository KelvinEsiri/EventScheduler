# Testing Guide - Date Validation Fix

## Server Status
✅ API Server: http://localhost:5006
✅ Web Server: http://localhost:5292

## Test Case: End Date Before Start Date

### Test Steps

1. **Navigate to Calendar View**
   - Go to http://localhost:5292/calendar-view
   - Login if not already authenticated

2. **Try to Create an Event with Invalid Dates**
   - Click "New Event" button
   - Fill in the form:
     - **Title**: Test Event
     - **Start Date**: October 20, 2025, 10:00 AM
     - **End Date**: October 19, 2025, 10:00 AM (earlier than start)
   - Click "Save"

3. **Expected Result**
   - ✅ You should see a **toast notification** with the error: 
     > "End date cannot be before start date"
   - ✅ The modal should remain open so you can correct the dates
   - ✅ No 500 error should occur
   - ✅ The error should be clear and actionable

4. **Test from Calendar List**
   - Go to http://localhost:5292/calendar-list
   - Click "New Event" button
   - Fill in the same invalid dates
   - Click "Save"

5. **Expected Result**
   - ✅ You should see an **alert dialog** with the error:
     > "End date cannot be before start date"
   - ✅ No 500 error should occur

6. **Test Update Event**
   - Edit an existing event
   - Change the end date to be earlier than start date
   - Try to save

7. **Expected Result**
   - ✅ Same validation error should appear
   - ✅ Event should not be updated

### Success Criteria
- [x] Clear, specific error message displayed to user
- [x] HTTP 400 Bad Request (not 500 Internal Server Error)
- [x] Error logged at WARNING level (not ERROR)
- [x] User can correct the error and retry
- [x] Works in both Calendar View and Calendar List

### Additional Tests

#### Test Valid Dates
1. Create an event with:
   - Start Date: October 20, 2025, 10:00 AM
   - End Date: October 20, 2025, 12:00 PM
2. Click "Save"
3. Expected: Event created successfully

#### Test Same Start and End Date
1. Create an event with:
   - Start Date: October 20, 2025, 10:00 AM
   - End Date: October 20, 2025, 10:00 AM (same time)
2. Click "Save"
3. Expected: Event created successfully (same time is valid)

## API Testing (Optional)

You can also test the API directly using curl or Postman:

### Request
```bash
POST http://localhost:5006/api/events
Authorization: Bearer <your-token>
Content-Type: application/json

{
  "title": "Test Event",
  "startDate": "2025-10-20T10:00:00",
  "endDate": "2025-10-19T10:00:00",
  "isAllDay": false,
  "eventType": "Meeting"
}
```

### Expected Response
```json
Status: 400 Bad Request
Body: {
  "error": "End date cannot be before start date"
}
```

## Log Verification

Check the API logs for:
```
[WRN] Validation error creating event
EventScheduler.Application.Services.EventService: End date cannot be before start date
```

Should NOT see:
```
[ERR] Error creating event (for validation errors)
```

## Fixes Applied
1. ✅ EventsController catches InvalidOperationException separately
2. ✅ ApiService extracts error messages from API responses
3. ✅ CalendarView displays validation errors via toast
4. ✅ CalendarList displays validation errors via alert
5. ✅ Proper HTTP status codes (400 vs 500)
6. ✅ Proper log levels (Warning vs Error)

## Date Fixed
October 16, 2025
