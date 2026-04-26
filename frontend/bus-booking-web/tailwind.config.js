/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{html,ts}"],
  theme: {
    extend: {
      colors: {
        ink: {
          DEFAULT: '#131212',
          80: '#272524',
          60: '#4a4744',
          40: '#7a746d',
          20: '#b9b2a6'
        },
        paper: {
          DEFAULT: '#F4EFE6',
          100: '#FBF7EE',
          200: '#EDE4D3',
          300: '#DCCFB5',
          400: '#C4B491'
        },
        ochre: {
          DEFAULT: '#D4A24C',
          light: '#E7C27B',
          dark: '#A8791F'
        },
        oxblood: {
          DEFAULT: '#8B2E2A',
          light: '#B14843',
          dark: '#5A1A17'
        },
        moss: {
          DEFAULT: '#3E5B3B',
          light: '#6F8C63',
          dark: '#243823'
        },
        indigo2: {
          DEFAULT: '#213A5C'
        }
      },
      fontFamily: {
        display: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
        mono: ['"JetBrains Mono"', 'ui-monospace', 'SFMono-Regular', 'monospace']
      },
      boxShadow: {
        ticket: '0 1px 0 #e0d6c2, 0 12px 24px -18px rgba(19,18,18,0.35)',
        stamp: 'inset 0 0 0 2px currentColor',
        crisp: '4px 4px 0 0 #131212',
        softcrisp: '3px 3px 0 0 rgba(19,18,18,0.9)'
      },
      letterSpacing: {
        ticket: '0.18em',
        tight2: '-0.04em'
      },
      keyframes: {
        flip: {
          '0%': { transform: 'rotateX(0deg)' },
          '50%': { transform: 'rotateX(-90deg)' },
          '100%': { transform: 'rotateX(0deg)' }
        },
        marquee: {
          '0%': { transform: 'translateX(0)' },
          '100%': { transform: 'translateX(-50%)' }
        },
        stamp: {
          '0%': { transform: 'rotate(-18deg) scale(3)', opacity: '0' },
          '60%': { transform: 'rotate(-9deg) scale(1.05)', opacity: '1' },
          '100%': { transform: 'rotate(-8deg) scale(1)', opacity: '1' }
        },
        riseIn: {
          '0%': { transform: 'translateY(14px)', opacity: '0' },
          '100%': { transform: 'translateY(0)', opacity: '1' }
        }
      },
      animation: {
        flip: 'flip 400ms ease-in-out',
        marquee: 'marquee 40s linear infinite',
        stamp: 'stamp 600ms cubic-bezier(.2,.8,.2,1) forwards',
        riseIn: 'riseIn 500ms cubic-bezier(.2,.8,.2,1) both'
      }
    }
  },
  plugins: [],
  corePlugins: {
    preflight: false
  }
};
