/**
 * Hangfire Dashboard 扩展脚本
 * 在周期性任务列表页面添加暂停/恢复按钮
 */

(function () {
    'use strict';

    // 等待页面加载完成
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    function init() {
        // 只在周期性任务页面执行
        if (window.location.pathname.includes('/hangfire/recurring')) {
            addPauseResumeButtons();
            
            // 监听页面变化（Hangfire Dashboard 使用 AJAX 加载内容）
            observePageChanges();
        }
    }

    /**
     * 添加暂停/恢复按钮
     */
    function addPauseResumeButtons() {
        // 查找周期性任务表格
        const table = document.querySelector('#recurring-jobs table');
        if (!table) {
            return;
        }

        // 查找表头，添加"操作"列
        const thead = table.querySelector('thead tr');
        if (thead && !thead.querySelector('th:last-child')?.textContent.includes('操作')) {
            const actionHeader = document.createElement('th');
            actionHeader.textContent = '操作';
            actionHeader.style.width = '150px';
            thead.appendChild(actionHeader);
        }

        // 为每一行添加按钮
        const tbody = table.querySelector('tbody');
        if (!tbody) {
            return;
        }

        const rows = tbody.querySelectorAll('tr');
        rows.forEach(row => {
            // 检查是否已经添加了按钮
            if (row.querySelector('.pause-resume-btn')) {
                return;
            }

            const jobIdCell = row.querySelector('td:first-child');
            if (!jobIdCell) {
                return;
            }

            const jobId = jobIdCell.textContent.trim();
            if (!jobId) {
                return;
            }

            // 检查任务状态（通过 NextExecution 列判断）
            const nextExecutionCell = Array.from(row.querySelectorAll('td')).find(
                cell => cell.textContent.includes('Next execution') || 
                       cell.textContent.match(/\d{4}-\d{2}-\d{2}/)
            );

            const isPaused = !nextExecutionCell || 
                            nextExecutionCell.textContent.trim() === '' ||
                            nextExecutionCell.textContent.includes('N/A');

            // 创建操作单元格
            const actionCell = document.createElement('td');
            actionCell.className = 'text-center';

            // 创建按钮
            const button = document.createElement('button');
            button.className = 'btn btn-sm pause-resume-btn';
            button.style.margin = '0 2px';
            
            if (isPaused) {
                button.className += ' btn-success';
                button.innerHTML = '<i class="fa fa-play"></i> 恢复';
                button.onclick = () => resumeJob(jobId, button);
            } else {
                button.className += ' btn-warning';
                button.innerHTML = '<i class="fa fa-pause"></i> 暂停';
                button.onclick = () => pauseJob(jobId, button);
            }

            actionCell.appendChild(button);
            row.appendChild(actionCell);
        });
    }

    /**
     * 暂停任务
     */
    function pauseJob(jobId, button) {
        if (!confirm(`确定要暂停任务 "${jobId}" 吗？`)) {
            return;
        }

        button.disabled = true;
        button.innerHTML = '<i class="fa fa-spinner fa-spin"></i> 暂停中...';

        fetch(`/api/hangfire/recurring/pause/${encodeURIComponent(jobId)}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            }
        })
        .then(response => {
            if (!response.ok) {
                return response.json().then(data => {
                    throw new Error(data.message || '暂停失败');
                });
            }
            return response.json();
        })
        .then(data => {
            showMessage('success', data.message || '任务已暂停');
            // 刷新页面
            setTimeout(() => {
                window.location.reload();
            }, 1000);
        })
        .catch(error => {
            showMessage('error', error.message || '暂停任务失败');
            button.disabled = false;
            button.innerHTML = '<i class="fa fa-pause"></i> 暂停';
        });
    }

    /**
     * 恢复任务
     */
    function resumeJob(jobId, button) {
        if (!confirm(`确定要恢复任务 "${jobId}" 吗？`)) {
            return;
        }

        button.disabled = true;
        button.innerHTML = '<i class="fa fa-spinner fa-spin"></i> 恢复中...';

        fetch(`/api/hangfire/recurring/resume/${encodeURIComponent(jobId)}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': getAntiForgeryToken()
            }
        })
        .then(response => {
            if (!response.ok) {
                return response.json().then(data => {
                    throw new Error(data.message || '恢复失败');
                });
            }
            return response.json();
        })
        .then(data => {
            showMessage('success', data.message || '任务已恢复');
            // 刷新页面
            setTimeout(() => {
                window.location.reload();
            }, 1000);
        })
        .catch(error => {
            showMessage('error', error.message || '恢复任务失败');
            button.disabled = false;
            button.innerHTML = '<i class="fa fa-play"></i> 恢复';
        });
    }

    /**
     * 获取防伪令牌
     */
    function getAntiForgeryToken() {
        const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenElement ? tokenElement.value : '';
    }

    /**
     * 显示消息
     */
    function showMessage(type, message) {
        // 使用 Hangfire Dashboard 的消息显示方式，或者简单的 alert
        if (window.alert) {
            alert(message);
        } else {
            console.log(`[${type.toUpperCase()}] ${message}`);
        }
    }

    /**
     * 监听页面变化（Hangfire Dashboard 使用 AJAX）
     */
    function observePageChanges() {
        // 使用 MutationObserver 监听 DOM 变化
        const observer = new MutationObserver(() => {
            // 延迟执行，确保 DOM 已更新
            setTimeout(() => {
                addPauseResumeButtons();
            }, 500);
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }
})();

