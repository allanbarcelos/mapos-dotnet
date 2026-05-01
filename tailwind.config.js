/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Pages/**/*.cshtml",
    "./wwwroot/js/**/*.js"
  ],
  theme: {
    extend: {
      colors: {
        // Valores lidos de CSS custom properties → permitem troca de tema sem rebuild
        primary: {
          50:  'var(--color-primary-50)',
          100: 'var(--color-primary-100)',
          200: '#bae6fd',
          300: '#7dd3fc',
          400: '#38bdf8',
          500: '#0ea5e9',
          600: 'var(--color-primary-600)',
          700: 'var(--color-primary-700)',
          800: '#075985',
          900: '#0c4a6e',
        }
      }
    }
  },
  plugins: []
}
