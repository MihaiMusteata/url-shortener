import { useMemo, useState } from "react";

export type PlanDto = {
    id: string;
    name: string;
    priceMonthly: number;
    maxLinksPerMonth: number;
    customAliasEnabled: boolean;
    qrEnabled: boolean;
};

export type ShortLinkCreateRequest = {
    url: string;
    customAlias?: string;
    enableQr: boolean;
};

type ShortLinkCreateResponseMock = {
    shortUrl: string;
    alias: string;
    qrUrl?: string;
};

const mockUserPlan: PlanDto = {
    id: "b1f2c8a1-4d1c-4c1b-8b77-7d2c4f0e8a22",
    name: "Pro",
    priceMonthly: 9.99,
    maxLinksPerMonth: 5000,
    customAliasEnabled: true,
    qrEnabled: true,
};

function clsx(...parts: Array<string | boolean | undefined | null>) {
    return parts.filter(Boolean).join(" ");
}

function isValidUrl(value: string) {
    try {
        const u = new URL(value.trim());
        return u.protocol === "http:" || u.protocol === "https:";
    } catch {
        return false;
    }
}

function normalizeUrl(value: string) {
    const v = value.trim();
    if (!v) return v;
    if (/^https?:\/\//i.test(v)) return v;
    return `https://${v}`;
}

function randomAlias() {
    const chars = "abcdefghijkmnpqrstuvwxyz23456789";
    let s = "";
    for (let i = 0; i < 7; i++) s += chars[Math.floor(Math.random() * chars.length)];
    return s;
}

/**
 * Hero page for client app (URL shortener + optional QR)
 * - No advanced settings (kept aligned with service capabilities)
 * - Request model matches ShortLinkCreateRequest
 */
export default function HeroShortenerPage() {
    const plan = mockUserPlan;

    const [url, setUrl] = useState("");
    const [customAlias, setCustomAlias] = useState("");
    const [enableQr, setEnableQr] = useState(true);

    const [busy, setBusy] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [result, setResult] = useState<ShortLinkCreateResponseMock | null>(null);

    const urlNormalized = useMemo(() => normalizeUrl(url), [url]);
    const canUseAlias = plan.customAliasEnabled;
    const canUseQr = plan.qrEnabled;

    const request: ShortLinkCreateRequest = useMemo(
        () => ({
            url: urlNormalized,
            customAlias: canUseAlias && customAlias.trim() ? customAlias.trim() : undefined,
            enableQr: canUseQr ? enableQr : false,
        }),
        [urlNormalized, canUseAlias, customAlias, canUseQr, enableQr]
    );

    async function onGenerate() {
        setError(null);
        setResult(null);

        if (!request.url || !isValidUrl(request.url)) {
            setError("Please enter a valid URL (http/https).");
            return;
        }

        if (request.customAlias && !/^[a-zA-Z0-9-_]{3,32}$/.test(request.customAlias)) {
            setError("Custom alias must be 3–32 chars (letters, numbers, - or _).");
            return;
        }

        setBusy(true);
        try {
            await new Promise((r) => setTimeout(r, 350)); // mock latency

            const alias = request.customAlias ?? randomAlias();
            const shortUrl = `https://sho.rt/${alias}`;
            const qrUrl = request.enableQr
                ? `https://api.qrserver.com/v1/create-qr-code/?size=220x220&data=${encodeURIComponent(shortUrl)}`
                : undefined;

            setResult({ shortUrl, alias, qrUrl });
        } catch {
            setError("Something went wrong. Please try again.");
        } finally {
            setBusy(false);
        }
    }

    async function copy(text: string) {
        try {
            await navigator.clipboard.writeText(text);
        } catch {
            // ignore
        }
    }

    return (
        <div className="min-h-screen w-full bg-[#070A12] text-white">
            {/* background */}
            <div className="pointer-events-none fixed inset-0 overflow-hidden">
                <div className="absolute left-1/2 top-[-240px] h-[520px] w-[720px] -translate-x-1/2 rounded-full bg-gradient-to-r from-indigo-500/20 via-fuchsia-500/20 to-cyan-400/20 blur-3xl sm:h-[620px] sm:w-[920px] 2xl:h-[720px] 2xl:w-[1100px]" />
                <div className="absolute bottom-[-240px] right-[-240px] h-[420px] w-[420px] rounded-full bg-gradient-to-r from-cyan-400/10 to-indigo-500/10 blur-3xl sm:h-[520px] sm:w-[520px]" />
                <div className="absolute top-[35%] left-[-220px] h-[360px] w-[360px] rounded-full bg-gradient-to-r from-fuchsia-500/10 to-indigo-500/10 blur-3xl" />
            </div>

            <div className="relative mx-auto w-full max-w-5xl px-4 py-10 sm:px-6 sm:py-14 lg:max-w-6xl lg:px-8 2xl:max-w-7xl 2xl:px-10">
                {/* header */}
                <header className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                    <div className="flex items-center gap-3">
                        <div className="h-10 w-10 rounded-2xl bg-white/10 ring-1 ring-white/15 backdrop-blur flex items-center justify-center">
                            <span className="text-sm font-semibold">US</span>
                        </div>
                        <div className="leading-tight">
                            <div className="text-sm font-semibold tracking-wide">UrlShortener</div>
                            <div className="text-xs text-white/60">Short links + QR codes</div>
                        </div>
                    </div>

                    <div className="flex items-center gap-2">
                        <div className="rounded-2xl bg-white/5 px-3 py-2 ring-1 ring-white/10">
                            <div className="text-[11px] font-semibold uppercase tracking-wider text-white/50">Plan</div>
                            <div className="text-sm font-semibold text-white">{plan.name}</div>
                        </div>

                        <button className="w-full sm:w-auto rounded-2xl px-4 py-2 text-sm font-semibold bg-white text-[#070A12] hover:bg-white/90">
                            Sign in
                        </button>
                    </div>
                </header>

                {/* hero */}
                <section className="mt-10 sm:mt-12">
                    <div className="max-w-2xl">
                        <div className="inline-flex items-center gap-2 rounded-full bg-white/5 px-4 py-2 ring-1 ring-white/10">
                            <span className="h-2 w-2 rounded-full bg-emerald-400" />
                            <span className="text-xs font-semibold tracking-wide text-white/80">
                Create short links instantly
              </span>
                        </div>

                        <h1 className="mt-6 text-3xl font-semibold tracking-tight sm:text-5xl">
                            Paste a URL. Get a short link and QR code.
                        </h1>

                        <p className="mt-4 text-base text-white/70">
                            Choose an alias (if your plan allows) and optionally generate a QR code.
                        </p>
                    </div>

                    {/* main card */}
                    <div className="mt-8 rounded-3xl bg-white/5 p-5 ring-1 ring-white/10 backdrop-blur sm:p-6">
                        {/* URL bar */}
                        <div className="grid gap-3 sm:grid-cols-[1fr_auto] sm:items-start">
                            <div>
                                <label className="text-xs font-semibold tracking-wider text-white/60 uppercase">
                                    Destination URL
                                </label>

                                <div className="mt-2 flex items-center gap-2 rounded-2xl border border-white/10 bg-white/5 px-3 py-2.5 shadow-inner focus-within:ring-2 focus-within:ring-white/30">
                                    <span className="text-white/40 text-sm">🔗</span>
                                    <input
                                        value={url}
                                        onChange={(e) => setUrl(e.target.value)}
                                        placeholder="https://example.com/long-url"
                                        className="w-full bg-transparent text-sm text-white placeholder:text-white/35 outline-none"
                                        inputMode="url"
                                    />
                                </div>

                                <div className="mt-2 text-xs text-white/50">
                                    Tip: you can paste without https:// — we’ll add it automatically.
                                </div>
                            </div>

                            <button
                                type="button"
                                onClick={onGenerate}
                                disabled={busy}
                                className={clsx(
                                    "mt-6 sm:mt-7 w-full sm:w-auto rounded-2xl px-5 py-3 text-sm font-semibold",
                                    busy ? "bg-white/60 text-[#070A12]" : "bg-white text-[#070A12] hover:bg-white/90"
                                )}
                            >
                                {busy ? "Generating..." : "Generate"}
                            </button>
                        </div>

                        {/* options (service-aligned) */}
                        <div className="mt-6 grid gap-3 lg:grid-cols-2">
                            <div className="rounded-3xl bg-white/5 p-4 ring-1 ring-white/10">
                                <div className="flex items-start justify-between gap-4">
                                    <div>
                                        <div className="text-sm font-semibold">Custom alias</div>
                                        <div className="mt-1 text-sm text-white/65">
                                            {canUseAlias ? "Choose a readable short code" : "Not available on your plan"}
                                        </div>
                                    </div>
                                    <span
                                        className={clsx(
                                            "rounded-full px-3 py-1 text-xs font-semibold ring-1",
                                            canUseAlias
                                                ? "bg-emerald-400/10 ring-emerald-400/20 text-emerald-300"
                                                : "bg-white/5 ring-white/10 text-white/70"
                                        )}
                                    >
                    {canUseAlias ? "Available" : "Locked"}
                  </span>
                                </div>

                                <div className="mt-3 flex items-center gap-2 rounded-2xl border border-white/10 bg-white/5 px-3 py-2.5 shadow-inner focus-within:ring-2 focus-within:ring-white/30">
                                    <span className="text-white/40 text-sm">sho.rt/</span>
                                    <input
                                        value={customAlias}
                                        onChange={(e) => setCustomAlias(e.target.value)}
                                        placeholder={canUseAlias ? "my-link" : "Upgrade to unlock"}
                                        disabled={!canUseAlias}
                                        className={clsx(
                                            "w-full bg-transparent text-sm outline-none",
                                            canUseAlias
                                                ? "text-white placeholder:text-white/35"
                                                : "text-white/40 placeholder:text-white/25 cursor-not-allowed"
                                        )}
                                    />
                                </div>

                                <div className="mt-2 text-xs text-white/50">
                                    Letters, numbers, <span className="font-semibold text-white/70">-</span> and{" "}
                                    <span className="font-semibold text-white/70">_</span>, 3–32 chars.
                                </div>
                            </div>

                            <ToggleCard
                                title="QR code"
                                subtitle={canUseQr ? "Generate a QR code for this short link" : "Not available on your plan"}
                                checked={canUseQr ? enableQr : false}
                                disabled={!canUseQr}
                                onChange={setEnableQr}
                            />
                        </div>

                        {/* error */}
                        {error && (
                            <div className="mt-6 rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-100">
                                {error}
                            </div>
                        )}

                        {/* result */}
                        {result && (
                            <div className="mt-6 grid gap-4 rounded-3xl bg-white/5 p-5 ring-1 ring-white/10">
                                <div className="grid gap-3 lg:grid-cols-[1fr_auto] lg:items-start">
                                    <div className="min-w-0">
                                        <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">
                                            Your short link
                                        </div>

                                        <div className="mt-2 flex flex-col gap-2 sm:flex-row sm:items-center">
                                            <div className="min-w-0 flex-1 rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-white shadow-inner">
                                                <div className="truncate">{result.shortUrl}</div>
                                            </div>

                                            <button
                                                type="button"
                                                onClick={() => copy(result.shortUrl)}
                                                className="rounded-2xl bg-white px-4 py-3 text-sm font-semibold text-[#070A12] hover:bg-white/90"
                                            >
                                                Copy
                                            </button>
                                        </div>

                                        <div className="mt-3 flex flex-wrap gap-2 text-xs text-white/60">
                      <span className="rounded-full bg-white/5 px-3 py-1 ring-1 ring-white/10">
                        Alias: <span className="font-semibold text-white/80">{result.alias}</span>
                      </span>
                                            <span className="rounded-full bg-white/5 px-3 py-1 ring-1 ring-white/10">
                        QR:{" "}
                                                <span className="font-semibold text-white/80">
                          {request.enableQr ? "Enabled" : "Disabled"}
                        </span>
                      </span>
                                        </div>
                                    </div>

                                    <div className="lg:pl-2">
                                        {result.qrUrl ? (
                                            <div className="rounded-3xl bg-white/5 p-4 ring-1 ring-white/10">
                                                <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">QR Code</div>
                                                <div className="mt-3 grid place-items-center rounded-2xl bg-white p-3">
                                                    <img src={result.qrUrl} alt="QR code" className="h-44 w-44" />
                                                </div>
                                                <button
                                                    type="button"
                                                    onClick={() => copy(result.qrUrl!)}
                                                    className="mt-3 w-full rounded-2xl bg-white px-4 py-2.5 text-sm font-semibold text-[#070A12] hover:bg-white/90"
                                                >
                                                    Copy QR URL
                                                </button>
                                            </div>
                                        ) : (
                                            <div className="rounded-3xl bg-white/5 p-4 ring-1 ring-white/10">
                                                <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">QR Code</div>
                                                <div className="mt-2 text-sm text-white/65">QR generation is disabled for this link.</div>
                                            </div>
                                        )}
                                    </div>
                                </div>

                                <details className="rounded-2xl bg-white/5 p-4 ring-1 ring-white/10">
                                    <summary className="cursor-pointer text-sm font-semibold text-white/85">
                                        View request payload (mock)
                                    </summary>
                                    <pre className="mt-3 overflow-auto text-xs text-white/70">
{JSON.stringify(request, null, 2)}
                  </pre>
                                </details>
                            </div>
                        )}
                    </div>

                    <footer className="mt-14 border-t border-white/10 pt-8">
                        <div className="text-sm text-white/60">© 2026 UrlShortener</div>
                    </footer>
                </section>
            </div>
        </div>
    );
}

function ToggleCard({
                        title,
                        subtitle,
                        checked,
                        disabled,
                        onChange,
                    }: {
    title: string;
    subtitle: string;
    checked: boolean;
    disabled: boolean;
    onChange: (v: boolean) => void;
}) {
    return (
        <div className={clsx("rounded-3xl p-4 ring-1", disabled ? "bg-white/3 ring-white/8" : "bg-white/5 ring-white/10")}>
            <div className="flex items-start justify-between gap-4">
                <div>
                    <div className="text-sm font-semibold">{title}</div>
                    <div className={clsx("mt-1 text-sm", disabled ? "text-white/45" : "text-white/65")}>{subtitle}</div>
                </div>

                <label className={clsx("relative inline-flex items-center", disabled && "cursor-not-allowed opacity-70")}>
                    <input
                        type="checkbox"
                        className="peer sr-only"
                        checked={checked}
                        disabled={disabled}
                        onChange={(e) => onChange(e.target.checked)}
                    />
                    <span
                        className={clsx(
                            "h-7 w-12 rounded-full ring-1 transition",
                            disabled ? "bg-white/5 ring-white/10" : checked ? "bg-white ring-white/20" : "bg-white/10 ring-white/15"
                        )}
                    />
                    <span
                        className={clsx(
                            "absolute left-1 top-1 h-5 w-5 rounded-full shadow-sm transition",
                            checked ? "translate-x-5 bg-[#070A12]" : "translate-x-0 bg-white"
                        )}
                    />
                </label>
            </div>
        </div>
    );
}
