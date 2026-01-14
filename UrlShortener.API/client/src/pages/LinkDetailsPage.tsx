import React, { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { api } from "../lib/api.ts";

// DTOs
export type DailyClicksDto = { date: string; count: number };
export type TopReferrerDto = { referrer: string; count: number };
export type LinkClickEventDto = {
    id: string;
    clickedAt: string;
    referrer: string;
    ua: string;
};

export type ShortLinkDetailsDto = {
    id: string;
    alias: string;
    shortUrl: string;
    originalUrl: string;
    createdAt: string;
    qrEnabled: boolean;
    qrUrl?: string;

    totalClicks: number;
    uniqueReferrers: number;

    clicksLast7Days: DailyClicksDto[];
    topReferrers: TopReferrerDto[];
    recentEvents: LinkClickEventDto[];
};

function clsx(...parts: Array<string | boolean | undefined | null>) {
    return parts.filter(Boolean).join(" ");
}

function formatDate(iso: string) {
    const d = new Date(iso);
    return d.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "2-digit" });
}

function formatDateTime(iso: string) {
    const d = new Date(iso);
    return d.toLocaleString(undefined, {
        year: "numeric",
        month: "short",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
    });
}

function dayLabel(yyyyMMdd: string) {
    const d = new Date(`${yyyyMMdd}T00:00:00`);
    return d.toLocaleDateString(undefined, { weekday: "short" });
}

function formatDayPretty(yyyyMMdd: string) {
    const d = new Date(`${yyyyMMdd}T00:00:00`);
    return d.toLocaleDateString(undefined, {
        weekday: "long",
        year: "numeric",
        month: "long",
        day: "numeric",
    });
}

function truncateMiddle(s: string, max = 68) {
    if (s.length <= max) return s;
    const left = Math.max(18, Math.floor(max * 0.55));
    const right = Math.max(14, max - left - 3);
    return `${s.slice(0, left)}...${s.slice(-right)}`;
}

function extractErrorMessage(e: any, fallback: string) {
    const data = e?.response?.data;

    if (typeof data === "string") return data;
    if (typeof data?.message === "string") return data.message;

    const status = e?.response?.status;
    if (status === 401) return "Unauthorized. Please log in again.";
    if (status === 403) return "Forbidden.";
    if (status === 404) return "Link not found.";
    return e?.message || fallback;
}

async function fetchLinkDetails(id: string): Promise<ShortLinkDetailsDto> {
    const res = await api.get<ShortLinkDetailsDto>(`/shortlinks/${id}`);
    return res.data;
}

async function deleteLink(id: string): Promise<void> {
    await api.delete(`/shortlinks/${id}`);
}

export default function LinkDetailsPage() {
    const { id } = useParams<{ id: string }>();
    const nav = useNavigate();

    const [data, setData] = useState<ShortLinkDetailsDto | null>(null);
    const [busy, setBusy] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const [tab, setTab] = useState<"overview" | "events">("overview");
    const [selectedDate, setSelectedDate] = useState<string | null>(null);

    const [tooltip, setTooltip] = useState<{
        show: boolean;
        x: number;
        y: number;
        date?: string;
        count?: number;
    }>({ show: false, x: 0, y: 0 });

    // delete UI
    const [confirmDelete, setConfirmDelete] = useState(false);
    const [deleting, setDeleting] = useState(false);
    const [deleteError, setDeleteError] = useState<string | null>(null);

    useEffect(() => {
        if (!id) {
            setError("Missing link id.");
            setBusy(false);
            return;
        }

        setBusy(true);
        setError(null);

        fetchLinkDetails(id)
            .then((dto) => setData(dto))
            .catch((e) => setError(extractErrorMessage(e, "Failed to load link details.")))
            .finally(() => setBusy(false));
    }, [id]);

    const d = data;

    const maxDay = useMemo(() => {
        if (!d?.clicksLast7Days?.length) return 1;
        return Math.max(...d.clicksLast7Days.map((x) => x.count), 1);
    }, [d?.clicksLast7Days]);

    const totalLast7 = useMemo(() => {
        if (!d?.clicksLast7Days?.length) return 0;
        return d.clicksLast7Days.reduce((s, x) => s + x.count, 0);
    }, [d?.clicksLast7Days]);

    const selectedDayCount = useMemo(() => {
        if (!d || !selectedDate) return null;
        const found = d.clicksLast7Days.find((x) => x.date === selectedDate);
        return found?.count ?? 0;
    }, [d, selectedDate]);

    const filteredEvents = useMemo(() => {
        if (!d) return [];
        const events = d.recentEvents.slice().sort((a, b) => +new Date(b.clickedAt) - +new Date(a.clickedAt));
        if (!selectedDate) return events;
        return events.filter((e) => e.clickedAt.slice(0, 10) === selectedDate);
    }, [d, selectedDate]);

    async function copy(text: string) {
        try {
            await navigator.clipboard.writeText(text);
        } catch {
            // ignore
        }
    }

    function selectDay(date: string) {
        setSelectedDate((prev) => (prev === date ? null : date));
        setTab("events");
    }

    async function onDelete() {
        if (!id) return;
        setDeleting(true);
        setDeleteError(null);

        try {
            await deleteLink(id);

            // după delete, link-ul nu mai există (soft delete + query filter),
            // deci navigăm înapoi (sau către listă)
            nav(-1);
            // alternativ: nav("/links");
        } catch (e) {
            setDeleteError(extractErrorMessage(e, "Failed to delete link."));
        } finally {
            setDeleting(false);
            setConfirmDelete(false);
        }
    }

    // ---- UI STATES ----
    if (busy) {
        return (
            <div className="rounded-3xl bg-white/5 p-6 ring-1 ring-white/10">
                <div className="text-sm font-semibold">Loading link details…</div>
                <div className="mt-1 text-sm text-white/60">Fetching stats and events</div>
            </div>
        );
    }

    if (error || !d) {
        return (
            <div className="rounded-3xl bg-white/5 p-6 ring-1 ring-white/10">
                <div className="text-sm font-semibold">Could not load details</div>
                <div className="mt-2 rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-100">
                    {error ?? "Unknown error."}
                </div>

                <div className="mt-4 flex flex-col gap-2 sm:flex-row">
                    <button
                        type="button"
                        onClick={() => nav(-1)}
                        className="rounded-2xl bg-white/5 px-4 py-2.5 text-sm font-semibold text-white/85 ring-1 ring-white/10 hover:bg-white/10"
                    >
                        ← Back
                    </button>

                    {id && (
                        <button
                            type="button"
                            onClick={() => {
                                setBusy(true);
                                setError(null);
                                fetchLinkDetails(id)
                                    .then(setData)
                                    .catch((e) => setError(extractErrorMessage(e, "Failed to load link details.")))
                                    .finally(() => setBusy(false));
                            }}
                            className="rounded-2xl bg-white px-4 py-2.5 text-sm font-semibold text-[#070A12] hover:bg-white/90"
                        >
                            Retry
                        </button>
                    )}
                </div>
            </div>
        );
    }

    // ---- MAIN ----
    return (
        <>
            {/* background */}
            <div className="pointer-events-none fixed inset-0 overflow-hidden">
                <div className="absolute left-1/2 top-[-240px] h-[520px] w-[720px] -translate-x-1/2 rounded-full bg-gradient-to-r from-indigo-500/20 via-fuchsia-500/20 to-cyan-400/20 blur-3xl sm:h-[620px] sm:w-[920px] 2xl:h-[720px] 2xl:w-[1100px]" />
                <div className="absolute bottom-[-240px] right-[-240px] h-[420px] w-[420px] rounded-full bg-gradient-to-r from-cyan-400/10 to-indigo-500/10 blur-3xl sm:h-[520px] sm:w-[520px]" />
                <div className="absolute top-[35%] left-[-220px] h-[360px] w-[360px] rounded-full bg-gradient-to-r from-fuchsia-500/10 to-indigo-500/10 blur-3xl" />
            </div>

            <div className="relative mx-auto w-full max-w-5xl px-4 py-10 sm:px-6 sm:py-14 lg:max-w-6xl lg:px-8 2xl:max-w-7xl 2xl:px-10">
                {/* header */}
                <header className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                    <div className="min-w-0">
                        <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Link details</div>
                        <h1 className="mt-2 text-2xl font-semibold tracking-tight sm:text-3xl">{d.alias}</h1>
                        <p className="mt-2 text-sm text-white/65">
                            Created {formatDate(d.createdAt)} • {d.totalClicks} total clicks • {d.uniqueReferrers} referrers
                        </p>
                    </div>

                    <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
                        <button
                            type="button"
                            onClick={() => nav(-1)}
                            className="rounded-2xl bg-white/5 px-4 py-2.5 text-sm font-semibold text-white/85 ring-1 ring-white/10 hover:bg-white/10"
                        >
                            ← Back
                        </button>

                        <button
                            type="button"
                            onClick={() => window.open(d.shortUrl, "_blank")}
                            className="rounded-2xl bg-white px-4 py-2.5 text-sm font-semibold text-[#070A12] hover:bg-white/90"
                        >
                            Open link
                        </button>

                        <button
                            type="button"
                            onClick={() => {
                                setDeleteError(null);
                                setConfirmDelete(true);
                            }}
                            className="rounded-2xl bg-rose-500/15 px-4 py-2.5 text-sm font-semibold text-rose-100 ring-1 ring-rose-500/30 hover:bg-rose-500/20"
                        >
                            Delete
                        </button>
                    </div>
                </header>

                {deleteError && (
                    <div className="mt-5 rounded-2xl border border-rose-500/30 bg-rose-500/10 px-4 py-3 text-sm text-rose-100 ring-1 ring-white/5">
                        {deleteError}
                    </div>
                )}

                {/* top cards */}
                <section className="mt-8 grid gap-4 lg:grid-cols-3">
                    <div className="lg:col-span-2 rounded-3xl bg-white/5 p-6 ring-1 ring-white/10 backdrop-blur">
                        <div className="grid gap-5">
                            <InfoRow
                                label="Short URL"
                                value={d.shortUrl}
                                right={
                                    <button
                                        type="button"
                                        onClick={() => copy(d.shortUrl)}
                                        className="rounded-2xl bg-white/5 px-4 py-2.5 text-sm font-semibold text-white/85 ring-1 ring-white/10 hover:bg-white/10"
                                    >
                                        Copy
                                    </button>
                                }
                            />

                            <InfoRow
                                label="Original URL"
                                value={d.originalUrl}
                                right={
                                    <button
                                        type="button"
                                        onClick={() => copy(d.originalUrl)}
                                        className="rounded-2xl bg-white/5 px-4 py-2.5 text-sm font-semibold text-white/85 ring-1 ring-white/10 hover:bg-white/10"
                                    >
                                        Copy
                                    </button>
                                }
                            />

                            <div className="grid gap-3 sm:grid-cols-3">
                                <Stat title="Total clicks" value={`${d.totalClicks}`} />
                                <Stat title="Top referrer" value={d.topReferrers[0]?.referrer ?? "—"} />
                                <Stat title="QR" value={d.qrEnabled ? "Enabled" : "Disabled"} />
                            </div>
                        </div>
                    </div>

                    <div className="rounded-3xl bg-white/5 p-6 ring-1 ring-white/10 backdrop-blur">
                        <div className="flex items-start justify-between gap-3">
                            <div>
                                <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">QR Code</div>
                                <div className="mt-1 text-sm text-white/65">
                                    {d.qrEnabled ? "Scan to open the short link" : "No QR for this link"}
                                </div>
                            </div>

                            <span
                                className={clsx(
                                    "rounded-full px-3 py-1 text-xs font-semibold ring-1",
                                    d.qrEnabled ? "bg-emerald-400/10 ring-emerald-400/20 text-emerald-300" : "bg-white/5 ring-white/10 text-white/70"
                                )}
                            >
                {d.qrEnabled ? "Enabled" : "Disabled"}
              </span>
                        </div>

                        <div className="mt-5">
                            {d.qrEnabled && d.qrUrl ? (
                                <div className="grid place-items-center rounded-2xl bg-white p-4">
                                    <img src={d.qrUrl} alt="QR Code" className="h-48 w-48" />
                                </div>
                            ) : (
                                <div className="rounded-2xl border border-white/10 bg-white/5 p-4 text-sm text-white/65">
                                    QR is not available for this link.
                                </div>
                            )}
                        </div>

                        {d.qrEnabled && d.qrUrl && (
                            <button
                                type="button"
                                onClick={() => copy(d.qrUrl!)}
                                className="mt-4 w-full rounded-2xl bg-white px-4 py-2.5 text-sm font-semibold text-[#070A12] hover:bg-white/90"
                            >
                                Copy QR URL
                            </button>
                        )}
                    </div>
                </section>

                {/* tabs */}
                <section className="mt-8 rounded-3xl bg-white/5 ring-1 ring-white/10 backdrop-blur">
                    <div className="flex flex-col gap-3 border-b border-white/10 p-4 sm:flex-row sm:items-center sm:justify-between">
                        <div className="inline-flex rounded-2xl bg-white/5 p-1 ring-1 ring-white/10">
                            <TabButton active={tab === "overview"} onClick={() => setTab("overview")}>
                                Overview
                            </TabButton>
                            <TabButton active={tab === "events"} onClick={() => setTab("events")}>
                                Click events
                            </TabButton>
                        </div>

                        <div className="flex items-center gap-2">
                            {selectedDate ? (
                                <button
                                    type="button"
                                    onClick={() => setSelectedDate(null)}
                                    className="rounded-2xl bg-white/5 px-4 py-2 text-sm font-semibold text-white/85 ring-1 ring-white/10 hover:bg-white/10"
                                >
                                    Clear filter ({selectedDate})
                                </button>
                            ) : (
                                <div className="text-sm text-white/60">Hover bars • Click to open events</div>
                            )}
                        </div>
                    </div>

                    <div className="p-5 sm:p-6">
                        {tab === "overview" ? (
                            <div className="grid gap-6 lg:grid-cols-3">
                                {/* bar chart */}
                                <div className="lg:col-span-2">
                                    <div className="flex items-end justify-between gap-3">
                                        <div>
                                            <div className="text-sm font-semibold">Clicks (last 7 days)</div>
                                            <div className="mt-1 text-sm text-white/60">Click a bar to open Click events filtered by that date.</div>
                                        </div>
                                        <div className="text-sm text-white/60">
                                            Total: <span className="font-semibold text-white/80">{totalLast7}</span>
                                        </div>
                                    </div>

                                    <div className="relative mt-4 rounded-2xl bg-white/5 p-4 ring-1 ring-white/10">
                                        {tooltip.show && (
                                            <div
                                                className="pointer-events-none absolute z-10 rounded-2xl bg-[#0B1022]/95 px-3 py-2 text-xs text-white ring-1 ring-white/10"
                                                style={{ left: Math.min(tooltip.x + 12, 520), top: Math.max(tooltip.y - 40, 8) }}
                                            >
                                                <div className="font-semibold">{tooltip.date}</div>
                                                <div className="text-white/70">{tooltip.count} clicks</div>
                                            </div>
                                        )}

                                        <div className="grid grid-cols-7 items-end gap-2">
                                            {d.clicksLast7Days.map((x) => {
                                                const h = Math.max(8, Math.round((x.count / maxDay) * 92));
                                                const isSelected = selectedDate === x.date;

                                                return (
                                                    <button
                                                        key={x.date}
                                                        type="button"
                                                        onClick={() => selectDay(x.date)}
                                                        onMouseMove={(e) => {
                                                            const host = (e.currentTarget.parentElement as HTMLElement).getBoundingClientRect();
                                                            setTooltip({
                                                                show: true,
                                                                x: e.clientX - host.left,
                                                                y: e.clientY - host.top,
                                                                date: x.date,
                                                                count: x.count,
                                                            });
                                                        }}
                                                        onMouseLeave={() => setTooltip((t) => ({ ...t, show: false }))}
                                                        className="group flex flex-col items-center gap-2 outline-none"
                                                        aria-label={`${x.date}: ${x.count} clicks`}
                                                    >
                                                        <div
                                                            className={clsx("w-full rounded-xl transition", isSelected ? "bg-white" : "bg-white/70 group-hover:bg-white/90")}
                                                            style={{ height: `${h}px`, opacity: x.count === 0 ? 0.25 : 1 }}
                                                        />
                                                        <div className="text-[11px] font-semibold text-white/50">{dayLabel(x.date)}</div>
                                                    </button>
                                                );
                                            })}
                                        </div>

                                        {selectedDate && (
                                            <div className="mt-4 text-sm text-white/60">
                                                Selected: <span className="font-semibold text-white/80">{formatDayPretty(selectedDate)}</span> •{" "}
                                                <span className="font-semibold text-white/80">{selectedDayCount ?? 0}</span> clicks
                                            </div>
                                        )}
                                    </div>

                                    <div className="mt-4 grid gap-3 sm:grid-cols-3">
                                        <Mini title="Best day" value={`${Math.max(...d.clicksLast7Days.map((x) => x.count))} clicks`} />
                                        <Mini title="Average/day" value={`${Math.round(totalLast7 / 7)}`} />
                                        <Mini title="Short URL" value={truncateMiddle(d.shortUrl, 26)} />
                                    </div>
                                </div>

                                {/* referrers */}
                                <div>
                                    <div className="text-sm font-semibold">Top referrers</div>
                                    <div className="mt-4 space-y-2">
                                        {d.topReferrers.length === 0 ? (
                                            <div className="rounded-2xl border border-white/10 bg-white/5 p-4 text-sm text-white/65">No referrers yet.</div>
                                        ) : (
                                            d.topReferrers.map((r) => (
                                                <div key={r.referrer} className="rounded-2xl bg-white/5 p-4 ring-1 ring-white/10">
                                                    <div className="flex items-center justify-between gap-3">
                                                        <div className="text-sm font-semibold text-white/85">{r.referrer}</div>
                                                        <span className="rounded-full bg-white/5 px-3 py-1 text-xs font-semibold text-white/75 ring-1 ring-white/10">
                              {r.count}
                            </span>
                                                    </div>
                                                </div>
                                            ))
                                        )}
                                    </div>
                                </div>
                            </div>
                        ) : (
                            <>
                                {/* events header */}
                                <div className="mb-4 rounded-2xl bg-white/5 p-4 ring-1 ring-white/10">
                                    <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                                        <div>
                                            <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Click events</div>
                                            <div className="mt-1 text-sm text-white/80">
                                                {selectedDate ? (
                                                    <>
                                                        Showing events for <span className="font-semibold text-white">{formatDayPretty(selectedDate)}</span>{" "}
                                                        <span className="text-white/60">({selectedDayCount ?? 0} clicks)</span>
                                                    </>
                                                ) : (
                                                    <>No date selected. Click a bar in Overview to filter.</>
                                                )}
                                            </div>
                                        </div>

                                        {selectedDate && (
                                            <button
                                                type="button"
                                                onClick={() => setSelectedDate(null)}
                                                className="rounded-2xl bg-white px-4 py-2.5 text-sm font-semibold text-[#070A12] hover:bg-white/90"
                                            >
                                                Clear date
                                            </button>
                                        )}
                                    </div>
                                </div>

                                <div className="overflow-hidden rounded-2xl ring-1 ring-white/10">
                                    <div className="border-b border-white/10 bg-white/5 px-4 py-3 text-sm text-white/60">
                                        Showing <span className="font-semibold text-white/80">{filteredEvents.length}</span> events
                                        {selectedDate ? ` for ${selectedDate}` : ""}.
                                    </div>

                                    <div className="overflow-x-auto">
                                        <table className="min-w-full text-left">
                                            <thead className="bg-white/5">
                                            <tr className="text-xs font-semibold uppercase tracking-wider text-white/50">
                                                <th className="px-4 py-3">Time</th>
                                                <th className="px-4 py-3">Referrer</th>
                                                <th className="px-4 py-3">UA</th>
                                            </tr>
                                            </thead>
                                            <tbody className="divide-y divide-white/10">
                                            {filteredEvents.map((e) => (
                                                <tr key={e.id} className="hover:bg-white/5 transition">
                                                    <td className="px-4 py-3 text-sm text-white/75 whitespace-nowrap">{formatDateTime(e.clickedAt)}</td>
                                                    <td className="px-4 py-3 text-sm text-white/75">{e.referrer || "Direct"}</td>
                                                    <td className="px-4 py-3 text-sm text-white/65">{e.ua}</td>
                                                </tr>
                                            ))}

                                            {filteredEvents.length === 0 && (
                                                <tr>
                                                    <td colSpan={3} className="px-4 py-10 text-center text-sm text-white/60">
                                                        No events for this date.
                                                    </td>
                                                </tr>
                                            )}
                                            </tbody>
                                        </table>
                                    </div>

                                    <div className="border-t border-white/10 bg-white/5 px-4 py-3 text-sm text-white/60">
                                        Tip: select another day from Overview to update this list.
                                    </div>
                                </div>
                            </>
                        )}
                    </div>
                </section>
            </div>

            {/* DELETE MODAL */}
            {confirmDelete && (
                <div className="fixed inset-0 z-50 grid place-items-center px-4">
                    <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={() => !deleting && setConfirmDelete(false)} />

                    <div className="relative w-full max-w-lg rounded-3xl bg-[#0B1022] p-6 ring-1 ring-white/10">
                        <div className="text-sm font-semibold">Delete this link?</div>
                        <div className="mt-2 text-sm text-white/65">
                            This action will <span className="text-white/85 font-semibold">soft delete</span> the link. It won’t be resolvable anymore.
                        </div>

                        <div className="mt-4 rounded-2xl bg-white/5 p-4 ring-1 ring-white/10">
                            <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">Alias</div>
                            <div className="mt-1 text-sm font-semibold text-white/85">{d.alias}</div>

                            <div className="mt-3 text-xs font-semibold tracking-wider text-white/60 uppercase">Short URL</div>
                            <div className="mt-1 text-sm text-white/75 break-all">{d.shortUrl}</div>
                        </div>

                        <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
                            <button
                                type="button"
                                onClick={() => setConfirmDelete(false)}
                                disabled={deleting}
                                className="rounded-2xl bg-white/5 px-4 py-2.5 text-sm font-semibold text-white/85 ring-1 ring-white/10 hover:bg-white/10 disabled:opacity-50"
                            >
                                Cancel
                            </button>

                            <button
                                type="button"
                                onClick={onDelete}
                                disabled={deleting}
                                className="rounded-2xl bg-rose-500 px-4 py-2.5 text-sm font-semibold text-white hover:bg-rose-500/90 disabled:opacity-60"
                            >
                                {deleting ? "Deleting..." : "Delete"}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </>
    );
}

function InfoRow({ label, value, right }: { label: string; value: string; right?: React.ReactNode }) {
    return (
        <div className="grid gap-3 sm:grid-cols-[1fr_auto] sm:items-center">
            <div className="min-w-0">
                <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">{label}</div>
                <div className="mt-2 rounded-2xl border border-white/10 bg-white/5 px-4 py-3 text-sm text-white shadow-inner">
                    <div className="truncate">{value}</div>
                </div>
            </div>
            {right && <div className="shrink-0">{right}</div>}
        </div>
    );
}

function Stat({ title, value }: { title: string; value: string }) {
    return (
        <div className="rounded-3xl bg-white/5 p-4 ring-1 ring-white/10">
            <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">{title}</div>
            <div className="mt-2 text-xl font-semibold">{value}</div>
        </div>
    );
}

function Mini({ title, value }: { title: string; value: string }) {
    return (
        <div className="rounded-2xl bg-white/5 p-4 ring-1 ring-white/10">
            <div className="text-xs font-semibold tracking-wider text-white/60 uppercase">{title}</div>
            <div className="mt-2 text-sm font-semibold text-white/85">{value}</div>
        </div>
    );
}

function TabButton({
                       active,
                       onClick,
                       children,
                   }: {
    active: boolean;
    onClick: () => void;
    children: React.ReactNode;
}) {
    return (
        <button
            type="button"
            onClick={onClick}
            className={clsx("rounded-xl px-4 py-2 text-sm font-semibold transition", active ? "bg-white text-[#070A12]" : "text-white/80 hover:bg-white/10")}
        >
            {children}
        </button>
    );
}
