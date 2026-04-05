window.taskTrackerAuth = {
    getTokens: function () {
        return {
            accessToken: localStorage.getItem("tasktracker.accessToken") || "",
            refreshToken: localStorage.getItem("tasktracker.refreshToken") || ""
        };
    },
    setTokens: function (accessToken, refreshToken) {
        localStorage.setItem("tasktracker.accessToken", accessToken || "");
        localStorage.setItem("tasktracker.refreshToken", refreshToken || "");
    },
    clearTokens: function () {
        localStorage.removeItem("tasktracker.accessToken");
        localStorage.removeItem("tasktracker.refreshToken");
    }
};