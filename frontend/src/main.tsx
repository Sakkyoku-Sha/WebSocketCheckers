import React from 'react';
import ReactDOM from 'react-dom/client';
import Home from './home';
import "./globals.css"
import {connectWebSocket} from "@/WebSocket/WebSocketConnect";

connectWebSocket("ws://localhost:5050/ws");

ReactDOM.createRoot(document.getElementById('root')!).render(
    <React.StrictMode>
        <Home />
    </React.StrictMode>
);