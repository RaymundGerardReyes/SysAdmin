/** @type {import('tailwindcss').Config} */
// Rule: N-10 Design tokens in tailwind.config.js only
module.exports = {
  content: ['./src/**/*.{js,jsx,ts,tsx}'],
  presets: [require('nativewind/preset')],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        // PART 1: Neutral Foundation (88% target visual weight)
        canvas: {
          white: '#ffffff',
          50: '#fafafa',   // Light primary canvas
          100: '#f4f4f5',  // Light subtle surface
          200: '#e4e4e7',  // Light border
          300: '#d4d4d8',  // Disabled control border
          400: '#a1a1aa',  // Dark secondary text
          500: '#71717a',  // Light secondary text (4.6:1 contrast on #FFFFFF)
          700: '#3f3f46',  // Dark border
          800: '#27272a',  // Dark secondary surface
          900: '#18181b',  // Light primary text / Dark elevated surface
          950: '#09090b',  // Dark primary canvas
        },
        // PART 2: Interactive Indigo Accent (12% target visual weight)
        accent: {
          50: '#eef2ff',   // Light soft accent surface
          100: '#e0e7ff',  // Light accent border
          400: '#818cf8',  // Dark interactive accent
          500: '#6366f1',  // Focus ring indicator
          600: '#4f46e5',  // Light primary interactive accent
          700: '#4338ca',  // Light pressed accent
          900: '#312e81',  // Dark accent border
          950: '#1e1b4b',  // Dark soft accent surface
        },
        // Semantic Status Tokens (Always paired with icons + labels)
        status: {
          successBg: '#f0fdf4',
          successBorder: '#bbf7d0',
          successText: '#15803d',
          warningBg: '#fffbeb',
          warningBorder: '#fde68a',
          warningText: '#b45309',
          dangerBg: '#fef2f2',
          dangerBorder: '#fecaca',
          dangerText: '#b91c1c',
          infoBg: '#f0f9ff',
          infoBorder: '#bae6fd',
          infoText: '#0369a1',
        },
        // Backward-compatible aliases
        primary: {
          50: '#eef2ff',
          500: '#4f46e5',
          700: '#4338ca',
        },
        surface: '#ffffff',
        background: '#fafafa',
      },
      spacing: {
        1: '4px',
        2: '8px',
        3: '12px',
        4: '16px',
        6: '24px',
        8: '32px',
      },
      borderRadius: {
        sm: '8px',
        md: '10px',
        lg: '12px',
        xl: '16px',
      },
    },
  },
  plugins: [],
};
