﻿import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
    plugins: [react()],
    resolve: {
        alias: {
            '@': path.resolve(__dirname, 'src'), // Set '@' to point to the 'src' directory
        },
    },
    server: {
        port: 3000,
    },
});