const body = document.querySelector("body"),
    modeToggle = body.querySelector(".mode-toggle"),
    sidebar = body.querySelector("nav"),
    sidebarToggle = body.querySelector(".sidebar-toggle");
const logo = document.getElementById("logo");

// Check and apply the saved mode on page load
let getMode = localStorage.getItem("mode");
if (getMode && getMode === "dark") {
    body.classList.add("dark");
    logo.src = "/assets/images/logo-dark.png"; // Set the dark mode logo
} else {
    logo.src = "/assets/images/logo.png"; // Set the light mode logo
}

// Check and apply the saved sidebar status on page load
let getStatus = localStorage.getItem("status");
if (getStatus && getStatus === "close") {
    sidebar.classList.add("close");
}

// Toggle dark mode
modeToggle.addEventListener("click", () => {
    body.classList.toggle("dark");
    if (body.classList.contains("dark")) {
        logo.src = "/assets/images/logo-dark.png"; // Set the dark mode logo
        localStorage.setItem("mode", "dark");
    } else {
        logo.src = "/assets/images/logo.png"; // Set the light mode logo
        localStorage.setItem("mode", "light");
    }
});

// Toggle sidebar status
sidebarToggle.addEventListener("click", () => {
    sidebar.classList.toggle("close");
    if (sidebar.classList.contains("close")) {
        localStorage.setItem("status", "close");
    } else {
        localStorage.setItem("status", "open");
    }
});
