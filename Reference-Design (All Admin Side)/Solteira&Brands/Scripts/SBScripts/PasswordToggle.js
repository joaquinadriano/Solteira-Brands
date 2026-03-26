// Single implementation for password visibility toggle on all auth pages.
// One icon only: we replace the button contents so no duplicate glyphs can appear.
document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".toggle-password").forEach(function (button) {
        button.addEventListener("click", function () {
            var wrapper = this.closest(".password-wrapper");
            var input = wrapper ? wrapper.querySelector(".password-input") : null;
            if (!input) return;
            if (input.type === "password") {
                input.type = "text";
                button.innerHTML = "<i class=\"bi bi-eye\"></i>";
            } else {
                input.type = "password";
                button.innerHTML = "<i class=\"bi bi-eye-slash\"></i>";
            }
        });
    });
});
