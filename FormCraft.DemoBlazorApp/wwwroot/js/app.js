// Hide loading screen when Blazor is ready
window.addEventListener('DOMContentLoaded', () => {
    // Blazor will automatically replace the loading content when ready
    // but we can add a smooth transition
    const checkBlazorReady = setInterval(() => {
        const loadingElement = document.getElementById('app-loading');
        if (loadingElement && document.querySelector('.mud-layout')) {
            // Blazor has loaded, fade out loading screen
            loadingElement.style.transition = 'opacity 0.3s ease-out';
            loadingElement.style.opacity = '0';
            setTimeout(() => {
                loadingElement.style.display = 'none';
            }, 300);
            clearInterval(checkBlazorReady);
        }
    }, 100);
});