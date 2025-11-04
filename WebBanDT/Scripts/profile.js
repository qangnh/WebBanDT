(function () {
    'use strict';

    // Khởi tạo khi DOM đã sẵn sàng
    document.addEventListener('DOMContentLoaded', function () {
        initAutoHideAlerts();
        initFormValidation();
        initDateFilter();
    });

    // Tự động ẩn thông báo sau 5 giây
    function initAutoHideAlerts() {
        var alerts = document.querySelectorAll('.alert');
        alerts.forEach(function (alert) {
            setTimeout(function () {
                alert.style.transition = 'opacity 0.5s';
                alert.style.opacity = '0';
                setTimeout(function () {
                    alert.remove();
                }, 500);
            }, 5000);
        });
    }

    // Validation form đổi mật khẩu
    function initFormValidation() {
        var passwordForms = document.querySelectorAll('form[action*="ChangePassword"]');

        passwordForms.forEach(function (form) {
            form.addEventListener('submit', function (e) {
                var oldPassword = form.querySelector('input[name="OldPassword"]');
                var newPassword = form.querySelector('input[name="NewPassword"]');

                if (oldPassword && newPassword) {
                    if (oldPassword.value.length < 6) {
                        e.preventDefault();
                        alert('Mật khẩu phải có ít nhất 6 ký tự');
                        return false;
                    }

                    if (newPassword.value.length < 6) {
                        e.preventDefault();
                        alert('Mật khẩu mới phải có ít nhất 6 ký tự');
                        return false;
                    }

                    if (oldPassword.value === newPassword.value) {
                        e.preventDefault();
                        alert('Mật khẩu mới phải khác mật khẩu cũ');
                        return false;
                    }
                }
            });
        });
    }

    // Filter theo ngày
    function initDateFilter() {
        var dateInputs = document.querySelectorAll('input[type="date"]');

        dateInputs.forEach(function (input) {
            // Set max date to today
            var today = new Date().toISOString().split('T')[0];
            input.setAttribute('max', today);
        });
    }

    // Export functions nếu cần dùng ở nơi khác
    window.ProfileJS = {
        showAlert: function (message, type) {
            var alertDiv = document.createElement('div');
            alertDiv.className = 'alert alert-' + type;
            alertDiv.textContent = message;

            var wrap = document.querySelector('.wrap');
            if (wrap) {
                wrap.insertBefore(alertDiv, wrap.firstChild);

                setTimeout(function () {
                    alertDiv.style.transition = 'opacity 0.5s';
                    alertDiv.style.opacity = '0';
                    setTimeout(function () {
                        alertDiv.remove();
                    }, 500);
                }, 5000);
            }
        },

        confirmDelete: function (message) {
            return confirm(message || 'Bạn có chắc chắn muốn xóa?');
        }
    };

})();


/* ============================================
   BONUS: Animation CSS (Thêm vào cuối profile.css)
   ============================================ */

/* Fade in animation */
@keyframes fadeIn {
    from {
        opacity: 0;
        transform: translateY(10px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

.panel {
    animation: fadeIn 0.3s ease - out;
}

/* Loading state */
.btn.loading {
    position: relative;
    color: transparent;
    pointer - events: none;
}

.btn.loading::after {
    content: "";
    position: absolute;
    width: 16px;
    height: 16px;
    top: 50 %;
    left: 50 %;
    margin - left: -8px;
    margin - top: -8px;
    border: 2px solid #ffffff;
    border - radius: 50 %;
    border - top - color: transparent;
    animation: spinner 0.6s linear infinite;
}

@keyframes spinner {
    to {
        transform: rotate(360deg);
    }
}

/* Smooth transitions */
.kpi - card,
.panel,
.btn,
.pill {
    transition: all 0.2s ease;
}

/* Hover effects */
.panel:hover {
    box - shadow: 0 12px 35px rgba(0, 0, 0, .08);
}

.table tbody tr:hover {
    background: #f0f0f0;
}

/* Focus states */
.ipt: focus,
.btn: focus,
.pill:focus {
    outline: 2px solid var(--brand - 100);
    outline - offset: 2px;
}


/* ============================================
   BONUS: JavaScript utilities (Thêm vào profile.js)
   ============================================ */

// Thêm vào cuối file profile.js:

// Format currency
window.ProfileJS.formatCurrency = function (amount) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
};

// Format date
window.ProfileJS.formatDate = function (dateString) {
    var date = new Date(dateString);
    var day = ('0' + date.getDate()).slice(-2);
    var month = ('0' + (date.getMonth() + 1)).slice(-2);
    var year = date.getFullYear();
    return day + '/' + month + '/' + year;
};

// Confirm before action
window.ProfileJS.confirmAction = function (message, callback) {
    if (confirm(message)) {
        if (typeof callback === 'function') {
            callback();
        }
    }
};

// Show loading button
window.ProfileJS.setButtonLoading = function (button, loading) {
    if (loading) {
        button.classList.add('loading');
        button.disabled = true;
    } else {
        button.classList.remove('loading');
        button.disabled = false;
    }
};

// Copy to clipboard
window.ProfileJS.copyToClipboard = function (text) {
    if (navigator.clipboard) {
        navigator.clipboard.writeText(text).then(function () {
            window.ProfileJS.showAlert('Đã sao chép!', 'success');
        });
    } else {
        // Fallback for older browsers
        var textarea = document.createElement('textarea');
        textarea.value = text;
        document.body.appendChild(textarea);
        textarea.select();
        document.execCommand('copy');
        document.body.removeChild(textarea);
        window.ProfileJS.showAlert('Đã sao chép!', 'success');
    }
};