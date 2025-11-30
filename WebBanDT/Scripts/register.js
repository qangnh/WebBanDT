document.addEventListener("DOMContentLoaded", function () {

    var form = document.getElementById("registerForm");
    if (!form) return;

    var phoneInput = document.getElementById("Phone");
    var emailInput = document.getElementById("Email");

    // Xóa lỗi cũ
    function clearErrors() {
        document.querySelectorAll(".input-error").forEach(e => e.remove());
        document.querySelectorAll(".error-field").forEach(i => i.classList.remove("error-field"));
    }

    // Tạo lỗi dưới input
    function showError(input, message) {
        input.classList.add("error-field");

        var err = document.createElement("div");
        err.className = "input-error";
        err.textContent = message;
        input.parentNode.appendChild(err);
    }

    // Chỉ nhập số, max 10 ký tự
    if (phoneInput) {
        phoneInput.addEventListener("input", function () {
            this.value = this.value.replace(/\D/g, "");  // chỉ số
            if (this.value.length > 10) this.value = this.value.slice(0, 10);
        });
    }

    form.addEventListener("submit", function (e) {
        clearErrors();
        let isValid = true;

        let phone = phoneInput.value.trim();
        let email = emailInput.value.trim();

        // RULE: SDT phải 0 + 9 số
        let phoneRegex = /^0\d{9}$/;
        if (!phoneRegex.test(phone)) {
            showError(phoneInput, "SDT tối đa 10 số ");
            isValid = false;
        }

        // RULE: Email phải đúng dạng "ten@gmail.com"
        // - chữ thường hoặc số
        // - không dấu
        // - tối đa 30 ký tự
        let emailRegex = /^[a-z0-9]{1,30}@gmail\.com$/;
        if (!emailRegex.test(email)) {
            showError(emailInput, "Email không hợp lệ");
            isValid = false;
        }

        if (!isValid) {
            e.preventDefault();
            let firstError = document.querySelector(".error-field");
            if (firstError) firstError.scrollIntoView({ behavior: "smooth", block: "center" });
        }
    });
});
