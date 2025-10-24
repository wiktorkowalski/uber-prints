import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'
import { fileURLToPath } from 'url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5203',
        changeOrigin: true,
        secure: false,
      },
      '/stream': {
        target: 'http://localhost:5203',
        changeOrigin: true,
        secure: false,
      },
    },
  },
  build: {
    outDir: '../UberPrints.Server/wwwroot',
    emptyOutDir: true,
    sourcemap: true,
  },
})
