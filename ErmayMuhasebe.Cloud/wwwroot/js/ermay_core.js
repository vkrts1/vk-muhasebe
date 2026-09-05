// ERMAY CLOUD CORE JAVASCRIPT - V10.20
// Optimized for Blazor WASM 9.0 + SkiaSharp + MudBlazor

// 1. Emergency UI Management (Loading Screen Fix)
(function () {
    window.addEventListener('load', () => {
        // If app doesn't start in 8 seconds, force show the app container
        setTimeout(() => {
            const appDiv = document.getElementById('app');
            if (appDiv && appDiv.innerText.includes('SİSTEM BAŞLATILIYOR')) {
                console.warn("ERMAY: App bootstrap taking too long, checking for fatal crashes...");
            }
        }, 8000);
    });
})();

// 2. Desktop Behavior & Shortcuts
window.initDesktopBehavior = (dotnetHelper) => {
    window.addEventListener('keydown', (e) => {
        const isFKey = ['F1', 'F3', 'F10'].includes(e.key);
        const isNewShortcut = e.ctrlKey && e.key === 'n';

        if (isFKey || isNewShortcut) {
            e.preventDefault();
            dotnetHelper.invokeMethodAsync('HandleGlobalShortcut', isNewShortcut ? 'New' : e.key);
            return;
        }

        if (e.key === 'Escape') {
            if (window._escBlockerWasVisible) {
                window._escBlockerWasVisible = false;
                return;
            }
            dotnetHelper.invokeMethodAsync('HandleGlobalShortcut', 'Esc');
        }
    }, false);

    window.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            const isTrulyVisible = (el) => {
                if (!el || el.offsetParent === null) return false;
                const style = window.getComputedStyle(el);
                return style.display !== 'none' && style.visibility !== 'hidden' && style.opacity !== '0';
            };
            const selectors = '.mud-dialog, .mud-overlay, .mud-popover-open:not(.mud-tooltip-root), .mud-drawer-open';
            const blockers = document.querySelectorAll(selectors);
            window._escBlockerWasVisible = [...blockers].some(isTrulyVisible);
        }
    }, true);

    window.addEventListener('contextmenu', (e) => {
        const row = e.target.closest('.desktop-row');
        if (row) {
            e.preventDefault();
            dotnetHelper.invokeMethodAsync('HandleRightClick', {
                x: e.clientX,
                y: e.clientY,
                itemId: row.getAttribute('data-id')
            });
        }
    });

    // History Management
    window.historyManagement = {
        registerBackListener: function (helper) {
            window.onpopstate = function () {
                helper.invokeMethodAsync('HandleGlobalShortcut', 'Esc');
            };
        }
    };
};

// 3. File & PDF Download Helpers
window.downloadFile = (fileName, contentType, content) => {
    try {
        const blob = new Blob([content], { type: contentType });
        const url = URL.createObjectURL(blob);
        const anchorElement = document.createElement('a');
        anchorElement.href = url;
        anchorElement.download = fileName ?? 'belge.json';
        anchorElement.click();
        anchorElement.remove();
        URL.revokeObjectURL(url);
    } catch (e) {
        console.error("File download failed:", e);
    }
}

window.openPdf = (content) => {
    const blob = new Blob([content], { type: 'application/pdf' });
    const url = URL.createObjectURL(blob);
    const win = window.open(url, '_blank');
    if (!win) window.location.href = url;
}

// 4. Verification Helpers
window.jsExists = function (name) {
    try {
        return !!(window[name]);
    } catch (e) { return false; }
};

// 5. Context Helpers
window.getLocalStorage = (key) => {
    try {
        return localStorage.getItem(key);
    } catch (e) { return null; }
};

window.setResponsiveMode = (mode) => {
    const appDiv = document.getElementById('app');
    if (mode === "desktop") {
        localStorage.setItem("app_responsive_mode", "desktop");
        document.body.classList.remove("mobile-mode");
        document.documentElement.classList.remove("mobile-mode");
        // Re-apply scale
        let savedScale = localStorage.getItem("app_scale") || 1.0;
        window.applyAppScaling(savedScale, false);
    } else {
        localStorage.setItem("app_responsive_mode", "mobile");
        document.body.classList.add("mobile-mode");
        document.documentElement.classList.add("mobile-mode");
        
        // Reset browser zoom
        document.documentElement.style.zoom = 1;
        document.body.style.zoom = 1;

        // Reset #app forced styles for native mobile responsiveness
        if (appDiv) {
            appDiv.style.transform = "none";
            appDiv.style.width = "100%";
            appDiv.style.height = "auto";
            appDiv.style.position = "static";
            appDiv.style.overflow = "visible";
            
            // Sync internal layout components
            const layouts = appDiv.querySelectorAll('.mud-layout');
            layouts.forEach(l => {
                l.style.height = 'auto';
                l.style.width = '100%';
                l.style.maxWidth = '100%';
                l.style.overflow = 'visible';
            });
        }
    }
    // Trigger resize to let charts redraw
    window.dispatchEvent(new Event('resize'));
};

window.toggleResponsiveMode = () => {
    const isMobile = document.body.classList.contains("mobile-mode");
    window.setResponsiveMode(isMobile ? "desktop" : "mobile");
};

// Continuous persistence check to prevent Blazor / MudBlazor from overwriting classes
function syncMobileModeClasses() {
    const mode = localStorage.getItem("app_responsive_mode");
    if (mode === "mobile") {
        if (document.body && !document.body.classList.contains("mobile-mode")) {
            document.body.classList.add("mobile-mode");
        }
        if (document.documentElement && !document.documentElement.classList.contains("mobile-mode")) {
            document.documentElement.classList.add("mobile-mode");
        }
    }
}

// Aggressive Mobile Detection & Force Setting
const checkIsMobileDevice = () => {
    return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) || window.innerWidth < 960;
};

// Initialize responsive mode on load
let responsiveMode = localStorage.getItem("app_responsive_mode");
if (checkIsMobileDevice()) {
    // Mobile always defaults to mobile mode on load to prevent desktop scaling lock
    responsiveMode = "mobile";
    localStorage.setItem("app_responsive_mode", "mobile");
} else if (!responsiveMode) {
    responsiveMode = "desktop";
    localStorage.setItem("app_responsive_mode", "desktop");
}

syncMobileModeClasses();

// Setup MutationObserver to prevent class loss
if (typeof MutationObserver !== 'undefined') {
    const classObserver = new MutationObserver(() => {
        syncMobileModeClasses();
    });
    
    const startObserver = () => {
        if (document.body) {
            syncMobileModeClasses();
            classObserver.observe(document.body, { attributes: true, attributeFilter: ['class'] });
            classObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });
        } else {
            setTimeout(startObserver, 50);
        }
    };
    
    if (document.readyState === 'loading') {
        window.addEventListener('DOMContentLoaded', startObserver);
    } else {
        startObserver();
    }
}

// 6. Global Scale & Visual Management
// 6. Global Scale & Visual Management
window.applyAppScaling = (scaleArg, autoFit) => {
    const isAutoStored = localStorage.getItem("auto_scale_enabled");
    const isAuto = (typeof autoFit === "boolean") ? autoFit : (isAutoStored !== "false" && isAutoStored !== null);

    if (localStorage.getItem("app_responsive_mode") === "mobile") {
        document.documentElement.style.zoom = "1";
        document.body.style.zoom = "1";
        const appDiv = document.getElementById('app');
        if (appDiv) {
            appDiv.style.transform = "none";
            appDiv.style.width = "100%";
            appDiv.style.height = "auto";
            appDiv.style.position = "static";
        }
        return "1.00";
    }

    try {
        const root = document.documentElement;
        const targetWidth = window.innerWidth;
        const targetHeight = window.innerHeight;

        const refWidth = 1920; 
        const refHeight = 940; 
        
        const widthScale = targetWidth / refWidth;
        const heightScale = targetHeight / refHeight;

        let scale = 1.0;

        if (isAuto) {
            scale = Math.min(widthScale, heightScale);
            // Limit downscaling/upscaling for auto mode (staying within reasonable limits)
            scale = Math.max(0.65, Math.min(scale, 1.30));
        } else {
            scale = parseFloat(scaleArg);
            if (isNaN(scale) || !isFinite(scale) || scale < 0.2) {
                scale = parseFloat(localStorage.getItem("app_scale")) || 1.0;
            }
            scale = Math.max(0.4, Math.min(scale, 2.5));
        }

        scale = Math.round(scale * 100) / 100;

        // Apply scale
        root.style.setProperty('--app-scale', scale);
        localStorage.setItem("app_scale", scale);
        localStorage.setItem("auto_scale_enabled", isAuto ? "true" : "false");

        const appDiv = document.getElementById('app');
        if (appDiv) {
            appDiv.style.transform = `scale(${scale})`;
            appDiv.style.transformOrigin = 'top left';
            
            const safeWidth = root.clientWidth;
            const safeHeight = root.clientHeight;
            
            appDiv.style.width = (safeWidth / scale) + "px";
            appDiv.style.height = (safeHeight / scale) + "px";
            
            appDiv.style.position = 'fixed'; 
            appDiv.style.top = '0';
            appDiv.style.left = '0';
            appDiv.style.overflow = 'hidden';
            
            // Sync internal layout components
            const layouts = appDiv.querySelectorAll('.mud-layout');
            if (layouts.length > 0) {
                layouts.forEach(l => {
                    l.style.height = '100%';
                    l.style.width = '100%';
                    l.style.maxWidth = '100%';
                    l.style.overflow = 'hidden';
                });
            }
        }

        console.log(`[ERMAY SCALE] Mode: ${isAuto ? "Auto" : "Manual"}, Value: ${scale}, Ref: ${refWidth}x${refHeight}`);
        return scale.toFixed(2);
    } catch (e) {
        console.error("[ERMAY SCALE] Fatal Error:", e);
        return "1.00";
    }
};



// Continuous Responsive Fitting (Immediate Parity)
(function() {
    window.addEventListener('resize', () => {
        if (localStorage.getItem("app_responsive_mode") === "mobile") return;
        
        const isAutoStored = localStorage.getItem("auto_scale_enabled");
        const isAuto = isAutoStored !== "false" && isAutoStored !== null;
        
        if (isAuto) {
            window.applyAppScaling(null, true);
        } else {
            // In manual mode, we still need to re-apply to update viewport unit calculations (vw/vh)
            const savedScale = localStorage.getItem("app_scale") || 1.0;
            window.applyAppScaling(savedScale, false);
        }
    });

    // Initial Fit
    const fitOnStart = () => {
        if (localStorage.getItem("app_responsive_mode") !== "mobile") {
            const isAutoStored = localStorage.getItem("auto_scale_enabled");
            const isAuto = isAutoStored !== "false" && isAutoStored !== null;
            const savedScale = localStorage.getItem("app_scale") || 1.0;
            
            window.applyAppScaling(isAuto ? null : savedScale, isAuto);
        }
    };

    if (document.readyState === 'complete') fitOnStart();
    else window.addEventListener('load', fitOnStart);

    // NAVIGATION OBSERVER: Re-apply scaling when content changes (useful for Blazor navigation)
    let lastUrl = location.href;
    const observer = new MutationObserver(() => {
        const url = location.href;
        if (url !== lastUrl) {
            lastUrl = url;
            const isAuto = localStorage.getItem("auto_scale_enabled") !== "false";
            const scale = localStorage.getItem("app_scale") || 1.0;
            window.applyAppScaling(isAuto ? null : scale, isAuto);
        }
    });
    
    const initObserver = () => {
        const app = document.getElementById('app');
        if (app) {
            observer.observe(app, { subtree: true, childList: true });
        } else {
            setTimeout(initObserver, 100);
        }
    };
    initObserver();
})();

// 7. Global Error Mitigation
window.onerror = function (msg, url, lineNo, columnNo, error) {
    console.error("ERMAY CORE LOG:", msg, url, lineNo);
    return false;
};

// 8. Interaction Helpers
window.clickElement = (id) => {
    const el = document.getElementById(id);
    if (el) el.click();
};

// 9. Theme Management
window.applyAppTheme = (themeName) => {
    document.body.classList.remove("theme-standard", "theme-ide");
    if (themeName === "ide") {
        document.body.classList.add("theme-ide");
    } else {
        document.body.classList.add("theme-standard");
    }
    localStorage.setItem("app_theme", themeName || "standard");
};

// Initialize theme on load
(function() {
    const savedTheme = localStorage.getItem("app_theme") || "standard";
    window.applyAppTheme(savedTheme);
})();
