/// <reference types="vitest" />
import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

const devProxyTarget = process.env.VITE_DEV_PROXY_TARGET || 'http://localhost:5000'

function getManualChunk(id: string) {
  if (!id.includes('node_modules')) {
    return undefined
  }

  const packageGroups: Array<[string, string[]]> = [
    ['vendor-react', ['react', 'react-dom', 'react-router-dom']],
    ['vendor-redux', ['@reduxjs/toolkit', 'react-redux']],
    ['vendor-signalr', ['@microsoft/signalr']],
    ['vendor-radix', ['@radix-ui', '@floating-ui']],
    ['vendor-icons', ['lucide-react', '@heroicons']],
    ['vendor-forms', ['react-hook-form', '@hookform', 'zod']],
    ['vendor-ui', ['sonner', 'next-themes']],
    ['vendor-utils', ['class-variance-authority', 'clsx', 'tailwind-merge', 'dayjs', 'uuid']],
  ]

  for (const [chunkName, packages] of packageGroups) {
    if (packages.some((pkg) => id.includes(`/node_modules/${pkg}/`))) {
      return chunkName
    }
  }

  return undefined
}

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    allowedHosts: ['ecommerce.berkansozer.com', '10.2.2.103', 'localhost'],
    proxy: {
      '/api': {
        target: devProxyTarget,
        changeOrigin: true,
        secure: false,
      },
      '/hubs': {
        target: devProxyTarget,
        changeOrigin: true,
        secure: false,
        ws: true,
      },
    },
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: getManualChunk,
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './vitest.setup.ts',
    css: true,
  },
})
