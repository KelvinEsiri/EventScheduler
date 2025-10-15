// Authentication helper functions
window.authHelper = {
    setAuthData: function (username, email, userId, token) {
        localStorage.setItem('auth_username', username);
        localStorage.setItem('auth_email', email);
        localStorage.setItem('auth_userId', userId);
        localStorage.setItem('auth_token', token);
    },
    
    getAuthData: function () {
        return {
            username: localStorage.getItem('auth_username'),
            email: localStorage.getItem('auth_email'),
            userId: localStorage.getItem('auth_userId'),
            token: localStorage.getItem('auth_token')
        };
    },
    
    clearAuthData: function () {
        localStorage.removeItem('auth_username');
        localStorage.removeItem('auth_email');
        localStorage.removeItem('auth_userId');
        localStorage.removeItem('auth_token');
    },
    
    navigateToCalendar: function () {
        window.location.href = '/calendar-view';
    }
};
