// 密码可见性切换功能
document.addEventListener('DOMContentLoaded', function () {
    const passwordInput = document.querySelector('input[type="password"]');
    const passwordButton = document.getElementById('PasswordVisibilityButton');
    
    if (passwordButton && passwordInput) {
        passwordButton.addEventListener('click', function () {
            const type = passwordInput.getAttribute('type') === 'password' ? 'text' : 'password';
            passwordInput.setAttribute('type', type);
            
            // 切换图标
            const icon = passwordButton.querySelector('i');
            if (icon) {
                if (type === 'password') {
                    icon.className = 'fa fa-eye';
                } else {
                    icon.className = 'fa fa-eye-slash';
                }
            }
        });
    }
});

