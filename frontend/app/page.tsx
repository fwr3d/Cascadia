import Link from 'next/link'
import { ChevronRight } from 'lucide-react'

function WaveLogo() {
  return (
    <svg viewBox="0 0 160 120" width="160" height="120" fill="none" aria-hidden="true">
      <defs>
        <linearGradient id="wave-stroke" x1="16" y1="24" x2="140" y2="100" gradientUnits="userSpaceOnUse">
          <stop stopColor="#9BE7F2" />
          <stop offset="0.45" stopColor="#37C8DD" />
          <stop offset="1" stopColor="#1D6FA3" />
        </linearGradient>
        <linearGradient id="wave-fill" x1="28" y1="30" x2="124" y2="92" gradientUnits="userSpaceOnUse">
          <stop stopColor="#37C8DD" stopOpacity="0.26" />
          <stop offset="1" stopColor="#37C8DD" stopOpacity="0.03" />
        </linearGradient>
        <filter id="wave-glow" x="-20%" y="-20%" width="140%" height="140%">
          <feGaussianBlur stdDeviation="4" result="blur" />
          <feMerge>
            <feMergeNode in="blur" />
            <feMergeNode in="SourceGraphic" />
          </feMerge>
        </filter>
      </defs>

      <path
        d="M18 70C31 58 41 52 56 54C72 56 80 73 93 74C109 76 118 58 133 43L142 34"
        stroke="#37C8DD"
        strokeOpacity="0.18"
        strokeWidth="18"
        strokeLinecap="round"
        filter="url(#wave-glow)"
      />

      <path
        d="M25 79C38 65 49 58 63 60C76 61 84 75 96 77C111 79 122 66 136 49L121 89C100 99 74 100 49 93C38 89 31 85 25 79Z"
        fill="url(#wave-fill)"
      />

      <path
        d="M18 70C31 58 41 52 56 54C72 56 80 73 93 74C109 76 118 58 133 43L142 34"
        stroke="url(#wave-stroke)"
        strokeWidth="7"
        strokeLinecap="round"
        strokeLinejoin="round"
      />

      <path
        d="M31 84C43 75 53 72 64 73C75 74 83 84 95 85C108 87 119 79 132 67"
        stroke="#7FDBEA"
        strokeOpacity="0.7"
        strokeWidth="4"
        strokeLinecap="round"
      />

      <path
        d="M44 94C54 88 63 86 73 87C84 88 92 94 103 95C113 96 121 93 129 88"
        stroke="#37C8DD"
        strokeOpacity="0.35"
        strokeWidth="3"
        strokeLinecap="round"
      />

      <path
        d="M118 34C123 30 127 29 131 31C134 33 134 38 132 43"
        stroke="#D9FAFF"
        strokeWidth="4"
        strokeLinecap="round"
      />

      <g opacity="0.55">
        <path
          d="M22 25H69"
          stroke="#37C8DD"
          strokeOpacity="0.2"
          strokeWidth="2"
          strokeLinecap="round"
        />
        <path
          d="M22 25H44"
          stroke="#D9FAFF"
          strokeWidth="2"
          strokeLinecap="round"
        />
      </g>
    </svg>
  )
}

export default function LandingPage() {
  return (
    <main className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[#0a1628] px-6 py-16 text-white sm:px-8">
      <div className="pointer-events-none absolute inset-0">
        <div className="absolute inset-x-0 top-0 h-40 bg-[linear-gradient(180deg,rgba(55,200,221,0.18),transparent)]" />
        <div className="absolute left-1/2 top-16 h-56 w-56 -translate-x-1/2 rounded-full bg-[#37C8DD]/10 blur" />
        <div className="absolute bottom-10 left-0 right-0 h-px bg-[linear-gradient(90deg,transparent,rgba(123,219,234,0.45),transparent)]" />
      </div>

      <section className="relative z-10 flex w-full max-w-4xl flex-col items-center gap-8 text-center">
        <div className="flex flex-col items-center gap-5 sm:gap-7">
          <WaveLogo />
          <h1 className="font-mono text-5xl font-bold tracking-[0.15em] sm:text-7xl lg:text-8xl">
            CASCADIA
          </h1>
        </div>

        <p className="max-w-2xl font-mono text-xs tracking-[0.2em] text-[#37C8DD] sm:text-sm">
          REAL-TIME PACIFIC TSUNAMI SIMULATOR
        </p>

        <Link
          href="/sim"
          className="group flex items-center gap-2 rounded-lg bg-[#37C8DD] px-8 py-3 font-mono text-sm font-bold text-[#0a1628] transition-all hover:bg-[#55d4e6] active:scale-[0.98]"
        >
          Launch Simulator
          <ChevronRight size={16} className="transition-transform group-hover:translate-x-0.5" />
        </Link>
      </section>
    </main>
  )
}
