'use client'

import Link from 'next/link'
import { ChevronRight } from 'lucide-react'

const WORDMARK = 'CASCADIA'.split('')

export default function LandingPage() {
  return (
    <main className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[#0a1628] px-6 py-16 text-white sm:px-8">
      <div className="pointer-events-none absolute inset-0">
        <div className="absolute inset-x-0 top-0 h-40 bg-[linear-gradient(180deg,rgba(55,200,221,0.18),transparent)]" />
        <div className="absolute bottom-10 left-0 right-0 h-px bg-[linear-gradient(90deg,transparent,rgba(123,219,234,0.45),transparent)]" />
      </div>

      <section className="relative z-10 flex w-full max-w-4xl flex-col items-center gap-8 text-center">
        <div className="flex flex-col items-center gap-5 sm:gap-7">
          <div className="relative inline-flex flex-col items-center gap-5">
            <svg
              viewBox="0 0 220 220"
              width="220"
              height="220"
              fill="none"
              aria-hidden="true"
              className="h-[172px] w-[172px] drop-shadow-[0_12px_42px_rgba(63,208,209,0.18)] sm:h-[202px] sm:w-[202px]"
            >
              <circle cx="110" cy="110" r="10" fill="#72D8D5" className="animate-core-breathe" />
              <circle
                cx="110"
                cy="110"
                r="48"
                stroke="#72D8D5"
                strokeWidth="3"
                opacity="0.95"
              />
              <g className="animate-ripple-spin">
                <circle
                  cx="110"
                  cy="110"
                  r="84"
                  stroke="#72D8D5"
                  strokeWidth="3.5"
                  strokeLinecap="round"
                  strokeDasharray="190 96 66 176"
                  transform="rotate(12 110 110)"
                  opacity="0.95"
                />
              </g>
              <g className="animate-ripple-spin-reverse">
                <circle
                  cx="110"
                  cy="110"
                  r="104"
                  stroke="#91A0B7"
                  strokeWidth="3"
                  strokeLinecap="round"
                  strokeDasharray="18 16 188 84 24 324"
                  transform="rotate(-38 110 110)"
                  opacity="0.72"
                />
              </g>
            </svg>
            <h1 className="font-mono text-5xl font-bold tracking-[0.12em] text-white sm:text-7xl lg:text-8xl">
              <span className="wordmark-wave" aria-label="CASCADIA">
                {WORDMARK.map((letter, index) => (
                  <span
                    key={`${letter}-${index}`}
                    className="wordmark-wave-letter"
                    aria-hidden="true"
                  >
                    {letter}
                  </span>
                ))}
              </span>
            </h1>
          </div>
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
