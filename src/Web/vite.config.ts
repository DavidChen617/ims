import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/api/organization': {
        target: 'http://localhost:5032',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/organization/, ''),
      },
      '/api/ordering': {
        target: 'http://localhost:5116',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/ordering/, ''),
      },
      '/api/inventory': {
        target: 'http://localhost:5205',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api\/inventory/, ''),
      },
    },
  },
})
