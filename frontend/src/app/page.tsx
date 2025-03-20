"use client"
import GameBoard from "./gameboard";
const ws = new WebSocket('ws://localhost:5041/ws');

ws.onopen = (event) => {
  ws.send("Here's some text that the server is urgently awaiting!");
};

ws.onmessage = (message : MessageEvent) => {
  console.log(`Received message from server: ${message}`);
};

ws.onclose = () => {
  console.log('Disconnected from server');
};

ws.onerror = (error) => {
  console.error("WebSocket error:", error);
};

export default function Home() {
  return (
    <div className="game-area">
      <GameBoard/> 
    </div>
  );
}
