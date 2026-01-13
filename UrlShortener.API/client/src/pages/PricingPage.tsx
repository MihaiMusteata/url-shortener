import { useMemo, useState } from "react";

export type PlanDto = {
    id: string;
    name: string;
    priceMonthly: number;
    maxLinksPerMonth: number;
    customAliasEnabled: boolean;
    qrEnabled: boolean;
};

const mockPlans: PlanDto[] = [
    {
        id: "c3c2f3a0-0f2b-4c4d-9a4c-18f5a0d9a101",
        name: "Starter",
        priceMonthly: 0,
        maxLinksPerMonth: 100,
        customAliasEnabled: false,
        qrEnabled: true,
    },
    {
        id: "b1f2c8a1-4d1c-4c1b-8b77-7d2c4f0e8a22",
        name: "Pro",
        priceMonthly: 9.99,
        maxLinksPerMonth: 5000,
        customAliasEnabled: true,
        qrEnabled: true,
    },
    {
        id: "a7d91c33-9d4c-4f11-bc8d-1c1f9fdc5b33",
        name: "Business",
        priceMonthly: 24.99,
        maxLinksPerMonth: 25000,
        customAliasEnabled: true,
        qrEnabled: true,
    },
];

function formatPriceMonthly(value: number) {
    if (value === 0) return "Free";
    return new Intl.NumberFormat(undefined, {
        style: "currency",
        currency: "USD",
        maximumFractionDigits: value % 1 === 0 ? 0 : 2,
    }).format(value);
}

function clsx(...parts: Array<string | boolean | undefined | null>) {
    return parts.filter(Boolean).join(" ");
}

export default function PricingPage() {
    const [selectedPlanId, setSelectedPlanId] = useState<string>(mockPlans[0]?.id ?? "");
    const [query, setQuery] = useState("");

    const plans = useMemo(() => {
        return [...mockPlans].sort((a, b) => {
            if (a.priceMonthly !== b.priceMonthly) return a.priceMonthly - b.priceMonthly;
            return a.name.localeCompare(b.name);
        });
    }, []);

    const filteredPlans = useMemo(() => {
        const q = query.trim().toLowerCase();
        if (!q) return plans;
        return plans.filter((p) => {
            const hay = `${p.name} ${p.priceMonthly} ${p.maxLinksPerMonth}`.toLowerCase();
            return hay.includes(q);
        });
    }, [plans, query]);

    const selectedPlan = useMemo(
        () => plans.find((p) => p.id === selectedPlanId) ?? plans[0],
        [plans, selectedPlanId]
    );

    const handleContinue = () => {
        alert(`Selected: ${selectedPlan.name} (${formatPriceMonthly(selectedPlan.priceMonthly)} / month)`);
    };

    return (
        <div className="min-h-screen bg-[#070A12] text-white">
            {/* subtle background */}
            <div className="pointer-events-none fixed inset-0 overflow-hidden">
                {/* center bloom scales better on ultrawide */}
                <div
                    className="absolute left-1/2 top-[-220px] h-[520px] w-[720px] -translate-x-1/2 rounded-full bg-gradient-to-r from-indigo-500/20 via-fuchsia-500/20 to-cyan-400/20 blur-3xl sm:h-[620px] sm:w-[920px] 2xl:h-[720px] 2xl:w-[1100px]" />
                <div
                    className="absolute bottom-[-220px] right-[-220px] h-[420px] w-[420px] rounded-full bg-gradient-to-r from-cyan-400/10 to-indigo-500/10 blur-3xl sm:h-[520px] sm:w-[520px]" />
            </div>

            {/* page container:
          - max width grows on large screens (2xl/3xl)
          - side padding grows too
          - vertical padding adapts */}
            <div
                className="relative mx-auto w-full max-w-5xl px-4 py-10 sm:px-6 sm:py-14 lg:max-w-6xl lg:px-8 2xl:max-w-7xl 2xl:px-10 3xl:max-w-[92rem] 3xl:px-12">
                {/* header */}
                <header className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                    <div className="flex items-center gap-3">
                        <div
                            className="h-10 w-10 rounded-2xl bg-white/10 ring-1 ring-white/15 backdrop-blur flex items-center justify-center">
                            <span className="text-sm font-semibold">US</span>
                        </div>
                        <div className="leading-tight">
                            <div className="text-sm font-semibold tracking-wide">UrlShortener</div>
                            <div className="text-xs text-white/60">Pricing</div>
                        </div>
                    </div>

                    <div className="flex items-center gap-2">
                        <button
                            className="w-full sm:w-auto rounded-2xl px-4 py-2 text-sm font-semibold bg-white text-[#070A12] hover:bg-white/90">
                            Sign in
                        </button>
                    </div>
                </header>

                {/* hero */}
                <section className="mt-10 sm:mt-12">
                    <div className="max-w-2xl">
                        <h1 className="text-3xl font-semibold tracking-tight sm:text-5xl">
                            Choose the plan that fits you.
                        </h1>
                        <p className="mt-4 text-base text-white/70">
                            Simple monthly pricing. Pick a plan and start shortening links in minutes.
                        </p>
                    </div>

                    {/* summary becomes 2-column on wide screens */}
                    <div className="mt-8 rounded-3xl bg-white/5 p-5 ring-1 ring-white/10 backdrop-blur sm:p-6">
                        <div className="grid gap-4 sm:grid-cols-[1fr_auto] sm:items-center">
                            <div className="min-w-0">
                                <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Selected
                                </div>
                                <div className="mt-1 text-lg font-semibold truncate">{selectedPlan.name}</div>
                                <div className="mt-1 text-sm text-white/65">
                                    Up to <span
                                    className="font-semibold text-white">{selectedPlan.maxLinksPerMonth}</span> links/month
                                    •{" "}
                                    {selectedPlan.customAliasEnabled ? "Custom alias" : "No custom alias"} •{" "}
                                    {selectedPlan.qrEnabled ? "QR enabled" : "No QR"}
                                </div>
                            </div>

                            <div className="flex flex-col items-start gap-3 sm:items-end">
                                <div className="text-left sm:text-right">
                                    <div
                                        className="text-xs font-semibold tracking-wider text-white/60 uppercase">Monthly
                                    </div>
                                    <div className="mt-1 text-2xl font-semibold">
                                        {formatPriceMonthly(selectedPlan.priceMonthly)}
                                        {selectedPlan.priceMonthly !== 0 && (
                                            <span className="text-sm text-white/60 font-semibold"> / mo</span>
                                        )}
                                    </div>
                                </div>

                                <button
                                    type="button"
                                    onClick={handleContinue}
                                    className="w-full sm:w-auto rounded-2xl bg-white px-5 py-3 text-sm font-semibold text-[#070A12] hover:bg-white/90"
                                >
                                    Continue
                                </button>
                            </div>
                        </div>
                    </div>
                </section>

                {/* list */}
                <section className="mt-10">
                    <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
                        <div className="max-w-2xl">
                            <h2 className="text-xl font-semibold">Plans</h2>
                            <p className="mt-1 text-sm text-white/65">
                                Scroll-friendly list for many plans.
                            </p>
                        </div>

                        <div className="relative w-full sm:w-[22rem] lg:w-[26rem]">
                            <input
                                value={query}
                                onChange={(e) => setQuery(e.target.value)}
                                placeholder="Search plan..."
                                className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-2.5 text-sm text-white placeholder:text-white/40 shadow-inner outline-none focus:bg-white/10 focus:ring-2 focus:ring-white/40"
                            />
                            <div
                                className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-white/40">
                                ⌕
                            </div>
                        </div>
                    </div>

                    {/* on very large screens, center the list and cap line length */}
                    <div className="mt-5 space-y-3">
                        {filteredPlans.length === 0 ? (
                            <div className="rounded-3xl bg-white/5 p-6 ring-1 ring-white/10">
                                <div className="text-sm font-semibold">No plans found</div>
                                <div className="mt-1 text-sm text-white/65">Try a different search.</div>
                            </div>
                        ) : (
                            filteredPlans.map((p) => {
                                const isSelected = p.id === selectedPlanId;

                                return (
                                    <button
                                        key={p.id}
                                        type="button"
                                        onClick={() => setSelectedPlanId(p.id)}
                                        className={clsx(
                                            "w-full text-left rounded-3xl p-5 ring-1 transition outline-none",
                                            "bg-white/5 ring-white/10 hover:bg-white/10",
                                            isSelected && "ring-2 ring-white/50"
                                        )}
                                    >
                                        {/* grid keeps it stable on ultrawide and on mobile */}
                                        <div className="grid gap-4 sm:grid-cols-[1fr_auto] sm:items-start">
                                            <div className="min-w-0">
                                                <div className="flex items-center gap-3">
                                                    <div
                                                        className={clsx(
                                                            "h-10 w-10 rounded-2xl ring-1 flex items-center justify-center shrink-0",
                                                            isSelected
                                                                ? "bg-white text-[#070A12] ring-white/20"
                                                                : "bg-white/5 text-white ring-white/10"
                                                        )}
                                                    >
                                                        {isSelected ? "✓" : "→"}
                                                    </div>

                                                    <div className="min-w-0">
                                                        <div className="text-lg font-semibold truncate">{p.name}</div>
                                                        <div className="mt-1 text-sm text-white/65">
                                                            Up to <span
                                                            className="font-semibold text-white">{p.maxLinksPerMonth}</span> links/month
                                                        </div>
                                                    </div>
                                                </div>

                                                <div className="mt-4 flex flex-wrap gap-2 text-sm text-white/75">
                          <span
                              className={clsx(
                                  "inline-flex items-center gap-2 rounded-2xl px-3 py-2 ring-1",
                                  p.customAliasEnabled
                                      ? "bg-emerald-400/10 ring-emerald-400/20"
                                      : "bg-white/5 ring-white/10"
                              )}
                          >
                            <span
                                className={clsx(
                                    "h-5 w-5 rounded-lg flex items-center justify-center ring-1",
                                    p.customAliasEnabled
                                        ? "bg-emerald-400/15 ring-emerald-400/20 text-emerald-300"
                                        : "bg-white/5 ring-white/10 text-white/60"
                                )}
                            >
                              {p.customAliasEnabled ? "✓" : "–"}
                            </span>
                            Custom alias
                          </span>

                                                    <span
                                                        className={clsx(
                                                            "inline-flex items-center gap-2 rounded-2xl px-3 py-2 ring-1",
                                                            p.qrEnabled ? "bg-emerald-400/10 ring-emerald-400/20" : "bg-white/5 ring-white/10"
                                                        )}
                                                    >
                            <span
                                className={clsx(
                                    "h-5 w-5 rounded-lg flex items-center justify-center ring-1",
                                    p.qrEnabled
                                        ? "bg-emerald-400/15 ring-emerald-400/20 text-emerald-300"
                                        : "bg-white/5 ring-white/10 text-white/60"
                                )}
                            >
                              {p.qrEnabled ? "✓" : "–"}
                            </span>
                            QR codes
                          </span>
                                                </div>
                                            </div>

                                            {/* right column: stays compact, wraps on small */}
                                            <div className="shrink-0">
                                                <div className="text-left sm:text-right">
                                                    <div
                                                        className="text-xs font-semibold tracking-wider text-white/60 uppercase">Monthly
                                                    </div>
                                                    <div className="mt-1 text-2xl font-semibold">
                                                        {formatPriceMonthly(p.priceMonthly)}
                                                        {p.priceMonthly !== 0 && (
                                                            <span
                                                                className="text-sm text-white/60 font-semibold"> / mo</span>
                                                        )}
                                                    </div>
                                                </div>

                                                <div className="mt-3 flex sm:justify-end">
                          <span
                              className={clsx(
                                  "inline-flex items-center rounded-full px-3 py-1 text-xs font-semibold ring-1",
                                  isSelected
                                      ? "bg-white text-[#070A12] ring-white/20"
                                      : "bg-white/5 text-white/80 ring-white/10"
                              )}
                          >
                            {isSelected ? "Selected" : "Select"}
                          </span>
                                                </div>
                                            </div>
                                        </div>
                                    </button>
                                );
                            })
                        )}
                    </div>
                </section>

                <footer className="mt-14 border-t border-white/10 pt-8">
                    <div className="text-sm text-white/60">© 2026 UrlShortener</div>
                </footer>
            </div>
        </div>
    );
}
