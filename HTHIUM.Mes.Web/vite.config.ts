import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  build: {
    outDir: '../src/HTHIUM.Mes.Api/wwwroot',
    emptyOutDir: true,
  },
  server: {
    host: '127.0.0.1',
    port: 5173,
    proxy: {
      '/api': 'http://127.0.0.1:5180',
      '/health': 'http://127.0.0.1:5180',
      '/hubs': {
        target: 'http://127.0.0.1:5180',
        ws: true,
      },
    },
  },
})
