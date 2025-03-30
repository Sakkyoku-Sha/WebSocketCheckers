import { createContext, useEffect, useState } from 'react';

const WebSocketContext = createContext<WebSocket | null>(null);

export const ServerContext = ({ children }: { children: React.ReactNode }) => {
    const [webSocket, setWebSocket] = useState<WebSocket | null>(null);

    useEffect(() => {
        
    }, []);

    return (
        <WebSocketContext.Provider value={webSocket}>
            {children}
        </WebSocketContext.Provider>
    );
};