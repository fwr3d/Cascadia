import Link from 'next/link'
import { ChevronRight } from 'lucide-react'

function WaveLogo() {
  return (
    <svg viewBox="0 0 120 120" width="130" height="130" fill="none">
      <defs>
        <clipPath id="circle-clip">
          <circle cx="60" cy="60" r="54" />
        </clipPath>
      </defs>
      <circle cx="60" cy="60" r="54" fill="#0d2244" />
      <circle cx="60" cy="60" r="54" stroke="#37C8DD" strokeWidth="1.5" strokeOpacity="0.3" />
      <g clipPath="url(#circle-clip)">
        <path
          d="M 0,32 C 15,21 25,21 40,32 C 55,43 65,43 80,32 C 95,21 105,21 120,32"
          stroke="#37C8DD"
          strokeWidth="3"
          strokeLinecap="round"
        />
        <path
          d="M 0,60 C 15,49 25,49 40,60 C 55,71 65,71 80,60 C 95,49 105,49 120,60"
          stroke="#37C8DD"
          strokeWidth="3"
          strokeLinecap="round"
          strokeOpacity="0.55"
        />
        <path
          d="M 0,88 C 15,77 25,77 40,88 C 55,99 65,99 80,88 C 95,77 105,77 120,88"
          stroke="#37C8DD"
          strokeWidth="3"
          strokeLinecap="round"
          strokeOpacity="0.22"
        />
      </g>
    </svg>
  )
}

export default function LandingPage() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-8 bg-[#0a1628] px-8 text-white">
      <div className="flex items-center gap-7">
        <WaveLogo />
        <h1 className="font-mono text-8xl font-bold tracking-[0.15em]">CASCADIA</h1>
      </div>
      <p className="font-mono text-sm tracking-widest text-[#37C8DD]">
        REAL-TIME PACIFIC TSUNAMI SIMULATOR
      </p>
      <Link
        href="/sim"
        className="group flex items-center gap-2 rounded-lg bg-[#37C8DD] px-8 py-3 font-mono text-sm font-bold text-[#0a1628] transition-all hover:bg-[#55d4e6] active:scale-[0.98]"
      >
        Launch Simulator
        <ChevronRight size={16} className="transition-transform group-hover:translate-x-0.5" />
      </Link>
    </main>
  )
}
