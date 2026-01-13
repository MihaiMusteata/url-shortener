import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { api } from "../lib/api.ts";

export type PlanDto = {
    id: string;
    name: string;
    priceMonthly: number;
    maxLinksPerMonth: number;
    customAliasEnabled: boolean;
    qrEnabled: boolean;
};

type CurrentPlanDto = {
    planId: string | null;
    planName: string | null;
    priceMonthly: number | null;
    maxLinksPerMonth: number | null;
};

function clsx(...parts: Array<string | boolean | undefined | null>) {
    return parts.filter(Boolean).join(" ");
}

function formatPriceMonthly(value: number) {
    if (value === 0) return "Free";
    return new Intl.NumberFormat(undefined, {
        style: "currency",
        currency: "USD",
        maximumFractionDigits: value % 1 === 0 ? 0 : 2,
    }).format(value);
}

function planRank(p: PlanDto) {
    return p.priceMonthly * 1_000_000 + p.maxLinksPerMonth;
}

export default function PricingPage() {
    const nav = useNavigate();
    const { isAuthenticated } = useAuth();

    const [plans, setPlans] = useState<PlanDto[]>([]);
    const [current, setCurrent] = useState<CurrentPlanDto | null>(null);

    const [query, setQuery] = useState("");
    const [selectedPlanId, setSelectedPlanId] = useState<string>("");

    const [loadingPlans, setLoadingPlans] = useState(true);
    const [loadingCurrent, setLoadingCurrent] = useState(false);
    const [busyAction, setBusyAction] = useState(false);
    const [error, setError] = useState<string | null>(null);

    async function loadPlans() {
        setLoadingPlans(true);
        setError(null);
        try {
            const res = await api.get<PlanDto[]>("/api/plans");
            setPlans(res.data ?? []);
        } catch (e: any) {
            setError(e?.response?.data ?? "Failed to load plans.");
            setPlans([]);
        } finally {
            setLoadingPlans(false);
        }
    }

    async function loadCurrentPlanIfAuth() {
        if (!isAuthenticated) {
            setCurrent(null);
            return;
        }

        setLoadingCurrent(true);
        setError(null);
        try {
            const res = await api.get<CurrentPlanDto>("/api/subscriptions/me/current");
            setCurrent(res.data ?? null);
        } catch (e: any) {
            // dacă token invalid => API poate da 401
            setError(e?.response?.data ?? "Failed to load current plan.");
            setCurrent(null);
        } finally {
            setLoadingCurrent(false);
        }
    }

    useEffect(() => {
        void loadPlans();
    }, []);

    useEffect(() => {
        void loadCurrentPlanIfAuth();
    }, [isAuthenticated]);

    const sortedPlans = useMemo(() => {
        return [...plans].sort((a, b) => planRank(a) - planRank(b));
    }, [plans]);

    const currentPlan = useMemo(() => {
        if (!current?.planId) return null;
        return sortedPlans.find((p) => p.id === current.planId) ?? null;
    }, [current, sortedPlans]);

    const currentRank = currentPlan ? planRank(currentPlan) : -Infinity;

    const filteredPlans = useMemo(() => {
        const q = query.toLowerCase();
        if (!q) return sortedPlans;
        return sortedPlans.filter((p) => `${p.name} ${p.priceMonthly} ${p.maxLinksPerMonth}`.toLowerCase().includes(q));
    }, [sortedPlans, query]);

    // default select: first upgrade over current, else current, else cheapest
    useEffect(() => {
        if (!sortedPlans.length) return;

        if (currentPlan) {
            const firstUpgrade = sortedPlans.find((p) => planRank(p) > currentRank);
            setSelectedPlanId(firstUpgrade?.id ?? currentPlan.id);
            return;
        }

        setSelectedPlanId(sortedPlans[0].id);
    }, [sortedPlans, currentPlan, currentRank]);

    const selectedPlan = useMemo(() => {
        return sortedPlans.find((p) => p.id === selectedPlanId) ?? sortedPlans[0] ?? null;
    }, [sortedPlans, selectedPlanId]);

    function isDowngrade(p: PlanDto) {
        if (!currentPlan) return false;
        return planRank(p) < currentRank;
    }

    function isCurrent(p: PlanDto) {
        if (!currentPlan) return false;
        return p.id === currentPlan.id;
    }

    function isUpgrade(p: PlanDto) {
        if (!currentPlan) return true;
        return planRank(p) > currentRank;
    }

    async function handleContinue() {
        if (!selectedPlan) return;

        setBusyAction(true);
        setError(null);

        try {
            if (!isAuthenticated) {
                // user neautentificat => du-l la login și revine după
                nav("/login", { state: { from: "/pricing" } });
                return;
            }

            if (!currentPlan) {
                // subscribe
                await api.post("/api/subscriptions/subscribe", { planId: selectedPlan.id });
            } else {
                // upgrade only
                if (!isUpgrade(selectedPlan)) {
                    setError("You can only upgrade to higher plans.");
                    return;
                }
                await api.post("/api/subscriptions/upgrade", { newPlanId: selectedPlan.id });
            }

            // refresh current plan after action
            await loadCurrentPlanIfAuth();

            // opțional: du-te la profile după upgrade
            nav("/profile");
        } catch (e: any) {
            setError(e?.response?.data ?? "Action failed.");
        } finally {
            setBusyAction(false);
        }
    }

    return (
        <div className="relative">
            {/* hero */}
            <section>
                <div className="max-w-2xl">
                    <h1 className="text-3xl font-semibold tracking-tight sm:text-5xl">Choose the plan that fits you.</h1>
                    <p className="mt-4 text-base text-white/70">Simple monthly pricing. Pick a plan and start shortening links.</p>

                    {loadingPlans ? (
                        <div className="mt-5 rounded-2xl bg-white/5 p-4 ring-1 ring-white/10 text-sm text-white/70">
                            Loading plans…
                        </div>
                    ) : currentPlan ? (
                        <div className="mt-5 rounded-2xl bg-white/5 p-4 ring-1 ring-white/10 text-sm text-white/70">
                            Current plan: <span className="font-semibold text-white">{currentPlan.name}</span>. Lower plans are disabled.
                        </div>
                    ) : isAuthenticated ? (
                        <div className="mt-5 rounded-2xl bg-white/5 p-4 ring-1 ring-white/10 text-sm text-white/70">
                            No active subscription. Select a plan to subscribe.
                        </div>
                    ) : (
                        <div className="mt-5 rounded-2xl bg-white/5 p-4 ring-1 ring-white/10 text-sm text-white/70">
                            Sign in to subscribe or upgrade.
                        </div>
                    )}

                    {loadingCurrent && (
                        <div className="mt-3 text-sm text-white/50">Loading your current plan…</div>
                    )}
                </div>

                {/* summary */}
                <div className="mt-8 rounded-3xl bg-white/5 p-5 ring-1 ring-white/10 backdrop-blur sm:p-6">
                    <div className="grid gap-4 sm:grid-cols-[1fr_auto] sm:items-center">
                        <div className="min-w-0">
                            <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Selected</div>
                            <div className="mt-1 text-lg font-semibold truncate">{selectedPlan?.name ?? "—"}</div>
                            <div className="mt-1 text-sm text-white/65">
                                Up to <span className="font-semibold text-white">{selectedPlan?.maxLinksPerMonth ?? 0}</span> links/month •{" "}
                                {selectedPlan?.customAliasEnabled ? "Custom alias" : "No custom alias"} •{" "}
                                {selectedPlan?.qrEnabled ? "QR enabled" : "No QR"}
                            </div>
                        </div>

                        <div className="flex flex-col items-start gap-3 sm:items-end">
                            <div className="text-left sm:text-right">
                                <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Monthly</div>
                                <div className="mt-1 text-2xl font-semibold">
                                    {formatPriceMonthly(selectedPlan?.priceMonthly ?? 0)}
                                    {(selectedPlan?.priceMonthly ?? 0) !== 0 && (
                                        <span className="text-sm text-white/60 font-semibold"> / mo</span>
                                    )}
                                </div>
                            </div>

                            <button
                                type="button"
                                onClick={handleContinue}
                                disabled={
                                    busyAction ||
                                    loadingPlans ||
                                    !selectedPlan ||
                                    (isAuthenticated && !!currentPlan && !isUpgrade(selectedPlan))
                                }
                                className={clsx(
                                    "w-full sm:w-auto rounded-2xl px-5 py-3 text-sm font-semibold transition",
                                    busyAction || loadingPlans
                                        ? "bg-white/10 text-white/50 ring-1 ring-white/10 cursor-not-allowed"
                                        : isAuthenticated && !!currentPlan && selectedPlan && !isUpgrade(selectedPlan)
                                            ? "bg-white/10 text-white/50 ring-1 ring-white/10 cursor-not-allowed"
                                            : "bg-white text-[#070A12] hover:bg-white/90"
                                )}
                            >
                                {busyAction ? "Processing..." : currentPlan ? "Upgrade" : "Continue"}
                            </button>
                        </div>
                    </div>

                    {error && (
                        <div className="mt-4 rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-100">
                            {error}
                        </div>
                    )}
                </div>
            </section>

            {/* list */}
            <section className="mt-10">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
                    <div className="max-w-2xl">
                        <h2 className="text-xl font-semibold">Plans</h2>
                        <p className="mt-1 text-sm text-white/65">Scroll-friendly list for many plans.</p>
                    </div>

                    <div className="relative w-full sm:w-[22rem] lg:w-[26rem]">
                        <input
                            value={query}
                            onChange={(e) => setQuery(e.target.value)}
                            placeholder="Search plan..."
                            className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-2.5 text-sm text-white placeholder:text-white/40 shadow-inner outline-none focus:bg-white/10 focus:ring-2 focus:ring-white/40"
                        />
                        <div className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-white/40">⌕</div>
                    </div>
                </div>

                <div className="mt-5 space-y-3">
                    {loadingPlans ? (
                        <div className="rounded-3xl bg-white/5 p-6 ring-1 ring-white/10 text-sm text-white/70">Loading…</div>
                    ) : filteredPlans.length === 0 ? (
                        <div className="rounded-3xl bg-white/5 p-6 ring-1 ring-white/10">
                            <div className="text-sm font-semibold">No plans found</div>
                            <div className="mt-1 text-sm text-white/65">Try a different search.</div>
                        </div>
                    ) : (
                        filteredPlans.map((p) => {
                            const downgrade = isAuthenticated && isDowngrade(p);
                            const currentP = isAuthenticated && isCurrent(p);
                            const selected = p.id === selectedPlanId;

                            return (
                                <button
                                    key={p.id}
                                    type="button"
                                    disabled={downgrade || currentP}
                                    onClick={() => setSelectedPlanId(p.id)}
                                    className={clsx(
                                        "w-full text-left rounded-3xl p-5 ring-1 transition outline-none",
                                        "bg-white/5 ring-white/10",
                                        downgrade || currentP ? "opacity-50 cursor-not-allowed" : "hover:bg-white/10",
                                        selected && !downgrade && !currentP && "ring-2 ring-white/50"
                                    )}
                                    title={downgrade ? "You can only upgrade to higher plans." : currentP ? "This is your current plan." : ""}
                                >
                                    <div className="grid gap-4 sm:grid-cols-[1fr_auto] sm:items-start">
                                        <div className="min-w-0">
                                            <div className="flex items-center gap-3">
                                                <div
                                                    className={clsx(
                                                        "h-10 w-10 rounded-2xl ring-1 flex items-center justify-center shrink-0",
                                                        currentP
                                                            ? "bg-white text-[#070A12] ring-white/20"
                                                            : selected
                                                                ? "bg-white text-[#070A12] ring-white/20"
                                                                : "bg-white/5 text-white ring-white/10"
                                                    )}
                                                >
                                                    {currentP ? "★" : selected ? "✓" : "→"}
                                                </div>

                                                <div className="min-w-0">
                                                    <div className="flex items-center gap-2">
                                                        <div className="text-lg font-semibold truncate">{p.name}</div>
                                                        {currentP && (
                                                            <span className="rounded-full bg-white/10 px-3 py-1 text-xs font-semibold text-white/80 ring-1 ring-white/10">
                                Current
                              </span>
                                                        )}
                                                        {downgrade && (
                                                            <span className="rounded-full bg-white/10 px-3 py-1 text-xs font-semibold text-white/70 ring-1 ring-white/10">
                                Not available
                              </span>
                                                        )}
                                                    </div>

                                                    <div className="mt-1 text-sm text-white/65">
                                                        Up to <span className="font-semibold text-white">{p.maxLinksPerMonth}</span> links/month
                                                    </div>
                                                </div>
                                            </div>

                                            <div className="mt-4 flex flex-wrap gap-2 text-sm text-white/75">
                        <span
                            className={clsx(
                                "inline-flex items-center gap-2 rounded-2xl px-3 py-2 ring-1",
                                p.customAliasEnabled ? "bg-emerald-400/10 ring-emerald-400/20" : "bg-white/5 ring-white/10"
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

                                        <div className="shrink-0">
                                            <div className="text-left sm:text-right">
                                                <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Monthly</div>
                                                <div className="mt-1 text-2xl font-semibold">
                                                    {formatPriceMonthly(p.priceMonthly)}
                                                    {p.priceMonthly !== 0 && <span className="text-sm text-white/60 font-semibold"> / mo</span>}
                                                </div>
                                            </div>

                                            <div className="mt-3 flex sm:justify-end">
                        <span
                            className={clsx(
                                "inline-flex items-center rounded-full px-3 py-1 text-xs font-semibold ring-1",
                                currentP
                                    ? "bg-white text-[#070A12] ring-white/20"
                                    : downgrade
                                        ? "bg-white/5 text-white/60 ring-white/10"
                                        : selected
                                            ? "bg-white text-[#070A12] ring-white/20"
                                            : "bg-white/5 text-white/80 ring-white/10"
                            )}
                        >
                          {currentP ? "Current" : downgrade ? "Disabled" : selected ? "Selected" : "Select"}
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
        </div>
    );
}
