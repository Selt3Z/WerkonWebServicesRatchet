window.ratchetInfiniteScroll = {
    observers: {},

    observe: function (element, dotNetRef, observerId) {
        this.unobserve(observerId);

        if (!element) {
            return;
        }

        const observer = new IntersectionObserver(entries => {
            for (const entry of entries) {
                if (entry.isIntersecting) {
                    dotNetRef.invokeMethodAsync('OnSentinelVisible');
                }
            }
        }, { rootMargin: '300px' });

        observer.observe(element);
        this.observers[observerId] = observer;
    },

    unobserve: function (observerId) {
        const observer = this.observers[observerId];

        if (observer) {
            observer.disconnect();
            delete this.observers[observerId];
        }
    }
};
