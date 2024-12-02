const passwordField = document.getElementById("password");
const togglePassword = document.querySelector(".password-toggle-icon i");

togglePassword.addEventListener("click", function () {
    if (passwordField.type === "password") {
        passwordField.type = "text";
        togglePassword.classList.remove("fa-eye-slash");
        togglePassword.classList.add("fa-eye");
    } else {
        passwordField.type = "password";
        togglePassword.classList.remove("fa-eye");
        togglePassword.classList.add("fa-eye-slash");
        
    }
});

document.addEventListener("DOMContentLoaded", function () {
    var successAlert = document.getElementById('successAlert');
    var errorAlert = document.getElementById('errorAlert');

    if (successAlert) {
        setTimeout(() => {
            successAlert.classList.remove('show');
            successAlert.style.opacity = '0';
        }, 5000);
    }

    if (errorAlert) {
        setTimeout(() => {
            errorAlert.classList.remove('show');
            errorAlert.style.opacity = '0';
        }, 5000);
    }
});
