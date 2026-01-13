import { useMemo, useState } from "react";

export type ShortLinkDto = {
    id: string;
    originalUrl: string;
    shortUrl: string;
    alias: string;
    createdAt: string; // ISO
    qrEnabled: boolean;
    clicks: number;
};

export type UserProfileDto = {
    id: string;
    firstName: string;
    lastName: string;
    username: string;
    email: string;
    planName: string;
};

function clsx(...parts: Array<string | boolean | undefined | null>) {
    return parts.filter(Boolean).join(" ");
}

function formatDate(iso: string) {
    const d = new Date(iso);
    return d.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "2-digit" });
}

function truncateMiddle(s: string, max = 44) {
    if (s.length <= max) return s;
    const left = Math.max(12, Math.floor(max * 0.55));
    const right = Math.max(10, max - left - 3);
    return `${s.slice(0, left)}...${s.slice(-right)}`;
}

const mockUser: UserProfileDto = {
    id: "1f5c1c6e-2a0c-4d36-98c5-9f09e4f13caa",
    firstName: "Mihail",
    lastName: "Musteata",
    username: "mihail.dev",
    email: "mihail@example.com",
    planName: "Pro",
};

const mockLinks: ShortLinkDto[] = [
    {
        id: "a9f4c0be-6c48-4c95-b5c2-3d0d7a5b0e12",
        originalUrl: "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        shortUrl: "https://sho.rt/rickroll",
        alias: "rickroll",
        createdAt: "2026-01-10T09:22:00Z",
        qrEnabled: true,
        clicks: 142,
    },
    {
        id: "2-6c48-4c95-b5c2-3d0d7a5b0e12",
        originalUrl: "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        shortUrl: "https://sho.rt/rickroll",
        alias: "rickroll",
        createdAt: "2026-01-10T09:22:00Z",
        qrEnabled: false,
        clicks: 142,
    },
    {
        id: "a3c1d2e3-1111-4aaa-9bbb-7e5a5c5b1234",
        originalUrl: "https://docs.microsoft.com/en-us/aspnet/core/",
        shortUrl: "https://sho.rt/aspnet",
        alias: "aspnet",
        createdAt: "2026-01-07T18:10:00Z",
        qrEnabled: false,
        clicks: 18,
    },
    {
        id: "b55a8c12-6d3a-4e5a-8a2d-0e6d2b5a9c10",
        originalUrl: "https://react.dev/learn",
        shortUrl: "https://sho.rt/react-learn",
        alias: "react-learn",
        createdAt: "2025-12-29T12:02:00Z",
        qrEnabled: true,
        clicks: 63,
    },
    {
        id: "c7a1d90b-2a5c-4d88-9b3e-2a9d5b1c7d8e",
        originalUrl: "https://tailwindcss.com/docs/installation",
        shortUrl: "https://sho.rt/tw-install",
        alias: "tw-install",
        createdAt: "2025-12-22T08:44:00Z",
        qrEnabled: true,
        clicks: 7,
    },
];

export default function ProfilePage() {
    const [query, setQuery] = useState("");
    const [sort, setSort] = useState<"newest" | "clicks">("newest");

    const filtered = useMemo(() => {
        const q = query.trim().toLowerCase();

        let items = [...mockLinks];
        if (q) {
            items = items.filter((l) => {
                const hay = `${l.alias} ${l.originalUrl} ${l.shortUrl}`.toLowerCase();
                return hay.includes(q);
            });
        }

        if (sort === "newest") {
            items.sort((a, b) => +new Date(b.createdAt) - +new Date(a.createdAt));
        } else {
            items.sort((a, b) => b.clicks - a.clicks);
        }

        return items;
    }, [query, sort]);

    function onOpenDetails(linkId: string) {
        // Future: navigate(`/links/${linkId}`)
        alert(`Open details for link: ${linkId}`);
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
                <div
                    className="absolute left-1/2 top-[-240px] h-[520px] w-[720px] -translate-x-1/2 rounded-full bg-gradient-to-r from-indigo-500/20 via-fuchsia-500/20 to-cyan-400/20 blur-3xl sm:h-[620px] sm:w-[920px] 2xl:h-[720px] 2xl:w-[1100px]" />
                <div
                    className="absolute bottom-[-240px] right-[-240px] h-[420px] w-[420px] rounded-full bg-gradient-to-r from-cyan-400/10 to-indigo-500/10 blur-3xl sm:h-[520px] sm:w-[520px]" />
                <div
                    className="absolute top-[35%] left-[-220px] h-[360px] w-[360px] rounded-full bg-gradient-to-r from-fuchsia-500/10 to-indigo-500/10 blur-3xl" />
            </div>

            <div
                className="relative mx-auto w-full max-w-5xl px-4 py-10 sm:px-6 sm:py-14 lg:max-w-6xl lg:px-8 2xl:max-w-7xl 2xl:px-10">
                {/* header */}
                <header className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                    <div className="flex items-center gap-3">
                        <div
                            className="h-12 w-12 rounded-2xl bg-white/10 ring-1 ring-white/15 backdrop-blur flex items-center justify-center">
              <span className="text-sm font-semibold">
                {mockUser.firstName.slice(0, 1).toUpperCase()}
                  {mockUser.lastName.slice(0, 1).toUpperCase()}
              </span>
                        </div>

                        <div className="leading-tight">
                            <div className="text-lg font-semibold tracking-tight">
                                {mockUser.firstName} {mockUser.lastName}
                            </div>
                            <div className="text-sm text-white/60">
                                @{mockUser.username} • {mockUser.email}
                            </div>
                        </div>
                    </div>

                    <div className="flex items-center gap-2">
                        <div className="rounded-2xl bg-white/5 px-3 py-2 ring-1 ring-white/10">
                            <div className="text-[11px] font-semibold uppercase tracking-wider text-white/50">
                                Plan
                            </div>
                            <div className="text-sm font-semibold text-white">{mockUser.planName}</div>
                        </div>

                        <button
                            className="rounded-2xl px-4 py-2 text-sm font-semibold bg-white text-[#070A12] hover:bg-white/90">
                            New link
                        </button>
                    </div>
                </header>

                {/* controls */}
                <section className="mt-10">
                    <div className="grid gap-4 lg:grid-cols-[1fr_auto] lg:items-end">
                        {/* LEFT: title */}
                        <div>
                            <h1 className="text-2xl font-semibold tracking-tight">Your links</h1>
                            <p className="mt-2 text-sm text-white/65">
                                All your shortened links in one place. Search, sort, and open details.
                            </p>
                        </div>

                        {/* RIGHT: search + sort */}
                        <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-end">
                            {/* search */}
                            <div className="relative w-full sm:w-[22rem] lg:w-[26rem]">
                                <input
                                    value={query}
                                    onChange={(e) => setQuery(e.target.value)}
                                    placeholder="Search by alias or URL..."
                                    className="w-full rounded-2xl border border-white/10 bg-white/5 px-4 py-2.5 text-sm text-white placeholder:text-white/40 shadow-inner outline-none focus:bg-white/10 focus:ring-2 focus:ring-white/40"
                                />
                                <div
                                    className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-white/40">
                                    ⌕
                                </div>
                            </div>

                            {/* sort */}
                            <div>
                                <div
                                    className="text-xs font-semibold tracking-wider text-white/60 uppercase text-right">
                                    Sort
                                </div>
                                <div className="mt-2 inline-flex rounded-2xl bg-white/5 p-1 ring-1 ring-white/10">
                                    <button
                                        type="button"
                                        onClick={() => setSort("newest")}
                                        className={clsx(
                                            "rounded-xl px-4 py-2 text-sm font-semibold transition",
                                            sort === "newest"
                                                ? "bg-white text-[#070A12]"
                                                : "text-white/80 hover:bg-white/10"
                                        )}
                                    >
                                        Newest
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => setSort("clicks")}
                                        className={clsx(
                                            "rounded-xl px-4 py-2 text-sm font-semibold transition",
                                            sort === "clicks"
                                                ? "bg-white text-[#070A12]"
                                                : "text-white/80 hover:bg-white/10"
                                        )}
                                    >
                                        Clicks
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* list */}
                    <div className="mt-6 space-y-3">
                        {filtered.length === 0 ? (
                            <div className="rounded-3xl bg-white/5 p-6 ring-1 ring-white/10">
                                <div className="text-sm font-semibold">No links found</div>
                                <div className="mt-1 text-sm text-white/65">
                                    Try a different search, or create your first link.
                                </div>
                            </div>
                        ) : (
                            filtered.map((l) => (
                                <article
                                    key={l.id}
                                    className="rounded-3xl bg-white/5 p-5 ring-1 ring-white/10 hover:bg-white/10 transition"
                                >
                                    <div className="grid gap-4 sm:grid-cols-[1fr_auto] sm:items-center">
                                        <div className="min-w-0">
                                            <div className="flex items-start gap-3">
                                                <div
                                                    className="h-10 w-10 rounded-2xl bg-white/5 ring-1 ring-white/10 flex items-center justify-center shrink-0">
                                                    {l.qrEnabled ? "▦" : "🔗"}
                                                </div>

                                                <div className="min-w-0">
                                                    <div className="flex flex-wrap items-center gap-2">
                                                        <div className="text-lg font-semibold truncate">{l.alias}</div>

                                                        <span
                                                            className="rounded-full bg-white/5 px-3 py-1 text-xs font-semibold text-white/80 ring-1 ring-white/10">
                                                            {l.clicks} clicks
                                                        </span>

                                                        <span
                                                            className="rounded-full bg-white/5 px-3 py-1 text-xs font-semibold text-white/70 ring-1 ring-white/10">
                                                            {formatDate(l.createdAt)}
                                                        </span>

                                                        <span
                                                            className={clsx(
                                                                "rounded-full px-3 py-1 text-xs font-semibold ring-1",
                                                                l.qrEnabled
                                                                    ? "bg-emerald-400/10 ring-emerald-400/20 text-emerald-300"
                                                                    : "bg-white/5 ring-white/10 text-white/70"
                                                            )}
                                                        >
                                                            {l.qrEnabled ? "QR enabled" : "No QR"}
                                                        </span>

                                                    </div>

                                                    <div className="mt-2 text-sm text-white/70">
                                                        <span className="text-white/50">Short:</span>{" "}
                                                        <span
                                                            className="font-semibold text-white/90">{truncateMiddle(l.shortUrl, 62)}</span>
                                                    </div>

                                                    <div className="mt-1 text-sm text-white/60">
                                                        <span className="text-white/50">Original:</span>{" "}
                                                        <span
                                                            className="font-semibold text-white/75">{truncateMiddle(l.originalUrl, 62)}</span>
                                                    </div>
                                                </div>
                                            </div>

                                            <div className="mt-4 flex flex-wrap gap-2">
                                                <button
                                                    type="button"
                                                    onClick={() => copy(l.shortUrl)}
                                                    className="rounded-2xl bg-white/5 px-4 py-2.5 text-sm font-semibold text-white/85 ring-1 ring-white/10 hover:bg-white/10"
                                                >
                                                    Copy short URL
                                                </button>
                                                <button
                                                    type="button"
                                                    onClick={() => window.open(l.shortUrl, "_blank")}
                                                    className="rounded-2xl bg-white/5 px-4 py-2.5 text-sm font-semibold text-white/85 ring-1 ring-white/10 hover:bg-white/10"
                                                >
                                                    Open
                                                </button>
                                            </div>
                                        </div>

                                        <div className="shrink-0">
                                            <button
                                                type="button"
                                                onClick={() => onOpenDetails(l.id)}
                                                className="w-full sm:w-auto rounded-2xl bg-white px-5 py-3 text-sm font-semibold text-[#070A12] hover:bg-white/90"
                                            >
                                                Details
                                            </button>
                                        </div>
                                    </div>
                                </article>
                            ))
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
