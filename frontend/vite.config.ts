import { existsSync, readFileSync } from 'node:fs'
import { fileURLToPath, URL } from 'node:url'
import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vitest/config'

// Адреса сервисов при разработке. Браузер ходит только на адрес Vite, а он пересылает запросы дальше,
// поэтому CORS не нужен. В продакшене то же самое делает nginx.
const apiUrl = process.env.API_URL ?? 'http://localhost:5160'
const realtimeUrl = process.env.REALTIME_URL ?? 'http://localhost:5170'

// HTTPS для dev-сервера включается, если рядом лежит сертификат (см. docs/frontend.md). Без него работает обычный http.
const certFile = 'certs/dev.pem'
const keyFile = 'certs/dev.key'
const https = existsSync(certFile) && existsSync(keyFile) ? { cert: readFileSync(certFile), key: readFileSync(keyFile) } : undefined

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  server: {
    port: 5173,
    https,
    proxy: {
      '/api': { target: apiUrl, changeOrigin: true, secure: false },
      '/hubs': { target: realtimeUrl, changeOrigin: true, ws: true, secure: false },
    },
  },
  test: {
    environment: 'node',
  },
})
