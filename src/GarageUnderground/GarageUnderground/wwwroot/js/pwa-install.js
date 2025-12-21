// PWA Install functionality
(function () {
    let deferredPrompt = null;
    let isInstalled = false;
    let dotNetHelper = null;

    // Check if app is already installed
    function checkInstalled() {
        // Check display-mode for standalone (installed PWA)
        if (window.matchMedia('(display-mode: standalone)').matches) {
            isInstalled = true;
            return true;
        }
        // Check for iOS standalone mode
        if (window.navigator.standalone === true) {
            isInstalled = true;
            return true;
        }
        return false;
    }

    // Listen for the beforeinstallprompt event
    window.addEventListener('beforeinstallprompt', (e) => {
        // Prevent the mini-infobar from appearing on mobile
        e.preventDefault();
        // Store the event for later use
        deferredPrompt = e;
        console.log('PWA: beforeinstallprompt event captured');
        // Notify Blazor that install is available
        notifyInstallStateChanged();
    });

    // Listen for app installed event
    window.addEventListener('appinstalled', () => {
        console.log('PWA: App installed');
        deferredPrompt = null;
        isInstalled = true;
        notifyInstallStateChanged();
    });

    // Listen for display-mode changes
    window.matchMedia('(display-mode: standalone)').addEventListener('change', (e) => {
        isInstalled = e.matches;
        notifyInstallStateChanged();
    });

    // Notify Blazor component of state changes
    function notifyInstallStateChanged() {
        if (dotNetHelper) {
            try {
                dotNetHelper.invokeMethodAsync('OnPwaInstallStateChanged', canInstall(), isInstalled);
            } catch (e) {
                console.error('PWA: Error notifying Blazor', e);
            }
        }
    }

    // Check if install prompt is available
    function canInstall() {
        return deferredPrompt !== null && !isInstalled;
    }

    // Trigger the install prompt
    async function promptInstall() {
        if (!deferredPrompt) {
            console.log('PWA: No deferred prompt available');
            return false;
        }

        // Show the install prompt
        deferredPrompt.prompt();

        // Wait for the user response
        const { outcome } = await deferredPrompt.userChoice;
        console.log('PWA: User choice', outcome);

        // Clear the deferred prompt
        deferredPrompt = null;

        return outcome === 'accepted';
    }

    // Expose functions to Blazor
    window.pwaInstall = {
        canInstall: function () {
            return canInstall();
        },
        isInstalled: function () {
            return checkInstalled() || isInstalled;
        },
        promptInstall: function () {
            return promptInstall();
        },
        setDotNetHelper: function (helper) {
            dotNetHelper = helper;
            console.log('PWA: DotNet helper registered');
            // Immediately notify current state
            notifyInstallStateChanged();
        },
        disposeDotNetHelper: function () {
            dotNetHelper = null;
        }
    };

    // Initial check
    checkInstalled();
    console.log('PWA: Install script initialized, isInstalled:', isInstalled, 'canInstall:', canInstall());
})();
