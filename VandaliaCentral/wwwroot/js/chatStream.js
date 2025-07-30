window.streamChat = (url, messages, dotNetHelper) => {
    const eventSource = new EventSourcePolyfill(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ messages })
    });

    eventSource.onmessage = (e) => {
        const data = JSON.parse(e.data);
        if (data.token) {
            dotNetHelper.invokeMethodAsync("AppendToken", data.token);
        }
        if (data.done) {
            eventSource.close();
            dotNetHelper.invokeMethodAsync("FinishStreaming");
        }
    };

    eventSource.onerror = (e) => {
        console.error("Stream error", e);
        eventSource.close();
    };
};
