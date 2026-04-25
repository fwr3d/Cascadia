'use client'

import Link from 'next/link'
import { ChevronRight, AlertTriangle } from 'lucide-react'

const WORDMARK = 'CASCADIA'.split('')

const STATS = [
  { value: '1,000 km', label: 'fault length' },
  { value: 'M9.2+', label: 'max magnitude' },
  { value: '~10M', label: 'people at risk' },
]

export default function LandingPage() {
  return (
    <main className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[#0a1628] px-6 py-16 text-white sm:px-8">
      <div className="pointer-events-none absolute inset-0">
        <div className="absolute inset-x-0 top-0 h-32 bg-[linear-gradient(180deg,rgba(55,200,221,0.12),transparent)]" />
        <div className="absolute inset-x-0 bottom-0 h-64 bg-[linear-gradient(0deg,rgba(226,75,74,0.05),transparent)]" />
        <div className="absolute inset-x-0 bottom-12 h-px bg-[linear-gradient(90deg,transparent,rgba(123,219,234,0.28),transparent)]" />
      </div>

      <section className="relative z-10 flex w-full max-w-3xl flex-col items-center gap-7 text-center sm:gap-8">
        <svg
          viewBox="0 0 220 220"
          width="220"
          height="220"
          fill="none"
          aria-hidden="true"
          className="h-[172px] w-[172px] drop-shadow-[0_12px_42px_rgba(63,208,209,0.18)] sm:h-[202px] sm:w-[202px]"
        >
          <circle cx="110" cy="110" r="48" fill="none" stroke="#37C8DD" strokeWidth="1.5" className="animate-wave-ring animate-wave-ring-delay-1" />
          <circle cx="110" cy="110" r="48" fill="none" stroke="#37C8DD" strokeWidth="1.5" className="animate-wave-ring animate-wave-ring-delay-2" />
          <circle cx="110" cy="110" r="48" fill="none" stroke="#37C8DD" strokeWidth="1.5" className="animate-wave-ring animate-wave-ring-delay-3" />
          <circle cx="110" cy="110" r="10" fill="#72D8D5" className="animate-core-breathe" />
          <circle cx="110" cy="110" r="48" stroke="#72D8D5" strokeWidth="3" opacity="0.95" />
          <g className="animate-ripple-spin">
            <circle
              cx="110" cy="110" r="84"
              stroke="#72D8D5" strokeWidth="3.5" strokeLinecap="round"
              strokeDasharray="190 96 66 176" transform="rotate(12 110 110)" opacity="0.95"
            />
          </g>
          <g className="animate-ripple-spin-reverse">
            <circle
              cx="110" cy="110" r="104"
              stroke="#91A0B7" strokeWidth="3" strokeLinecap="round"
              strokeDasharray="18 16 188 84 24 324" transform="rotate(-38 110 110)" opacity="0.72"
            />
          </g>
        </svg>

        <div className="flex flex-col items-center gap-3">
          <h1 className="font-mono text-5xl font-bold tracking-[0.12em] text-white sm:text-7xl lg:text-8xl">
            {WORDMARK.join('')}
          </h1>
          <p className="font-mono text-xs tracking-[0.2em] text-[#37C8DD] sm:text-sm">
            PACIFIC TSUNAMI SIMULATION SYSTEM
          </p>
        </div>

        <p className="max-w-xl text-sm leading-relaxed text-slate-400 sm:text-base">
          The Cascadia Subduction Zone stretches 1,000 km off the Pacific Coast.
          When it ruptures — and it will — the resulting tsunami will reach coastal communities
          in under 15 minutes. <span className="text-slate-200">This simulator models exactly that.</span>
        </p>

        <div className="flex items-center gap-8 border-y border-white/[0.06] py-5">
          {STATS.map((s) => (
            <div key={s.label} className="flex flex-col items-center gap-1">
              <span className="font-mono text-xl font-bold text-[#37C8DD] sm:text-2xl">{s.value}</span>
              <span className="font-mono text-[10px] tracking-widest text-slate-500 uppercase">{s.label}</span>
            </div>
          ))}
        </div>

        <div className="flex flex-col items-center gap-3 sm:flex-row">
          <Link
            href="/sim"
            className="group flex items-center gap-2 rounded-lg bg-[#37C8DD] px-8 py-3 font-mono text-sm font-bold text-[#0a1628] transition-all hover:bg-[#55d4e6] active:scale-[0.98]"
          >
            Launch Simulator
            <ChevronRight size={16} className="transition-transform group-hover:translate-x-0.5" />
          </Link>
        </div>

        <div className="flex items-center gap-2 rounded-lg border border-[#E24B4A]/20 bg-[#E24B4A]/5 px-4 py-2">
          <AlertTriangle size={12} className="shrink-0 text-[#E24B4A]" />
          <span className="font-mono text-[10px] tracking-wide text-[#E24B4A]/80">
            USGS estimates a 37% probability of M8.0+ Cascadia rupture within 50 years
          </span>
        </div>
      </section>
    </main>
  )
}
