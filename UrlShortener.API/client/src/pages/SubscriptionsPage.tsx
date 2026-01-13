import { useMemo, useState } from "react";

type UserMinimalDto = {
    id: string;
    firstName: string;
    lastName: string;
    username: string;
    email: string;
};

type SubscriptionDto = {
    id: string;
    userId: string;
    planId: string;
    active: boolean;
    user?: UserMinimalDto | null;
    planName?: string | null;
};

type StatusFilter = "all" | "active" | "inactive";
type SortKey = "user" | "plan" | "status";

const mockSubscriptions: SubscriptionDto[] = [
    {
        id: "8a6a4d8d-2f7a-4a1b-9b57-1b5f9f3b7a01",
        userId: "c1a2b3c4-d5e6-4f70-8a90-111111111111",
        planId: "aaaa1111-bbbb-2222-cccc-333333333333",
        active: true,
        planName: "Pro",
        user: {
            id: "c1a2b3c4-d5e6-4f70-8a90-111111111111",
            firstName: "Andrei",
            lastName: "Popescu",
            username: "andrei.p",
            email: "andrei.popescu@example.com",
        },
    },
    {
        id: "4d1b1f3a-9b49-4f91-8f60-2c5b0c9f30a2",
        userId: "c1a2b3c4-d5e6-4f70-8a90-222222222222",
        planId: "aaaa1111-bbbb-2222-cccc-444444444444",
        active: false,
        planName: "Starter",
        user: {
            id: "c1a2b3c4-d5e6-4f70-8a90-222222222222",
            firstName: "Maria",
            lastName: "Ionescu",
            username: "maria.i",
            email: "maria.ionescu@example.com",
        },
    },
    {
        id: "f3b7a019-6c4e-4f7d-9c2b-3d6e7a1b2c03",
        userId: "c1a2b3c4-d5e6-4f70-8a90-333333333333",
        planId: "aaaa1111-bbbb-2222-cccc-555555555555",
        active: true,
        planName: "Business",
        user: {
            id: "c1a2b3c4-d5e6-4f70-8a90-333333333333",
            firstName: "Vlad",
            lastName: "Rusu",
            username: "vlad.r",
            email: "vlad.rusu@example.com",
        },
    },
    {
        id: "2c03a7b1-5f2a-4b3c-8d9e-44aa55bb66cc",
        userId: "c1a2b3c4-d5e6-4f70-8a90-444444444444",
        planId: "aaaa1111-bbbb-2222-cccc-333333333333",
        active: true,
        planName: "Pro",
        user: {
            id: "c1a2b3c4-d5e6-4f70-8a90-444444444444",
            firstName: "Elena",
            lastName: "Stan",
            username: "elena.s",
            email: "elena.stan@example.com",
        },
    },
];

function cx(...classes: Array<string | false | null | undefined>) {
    return classes.filter(Boolean).join(" ");
}

function initials(name: string) {
    const parts = name.trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) return "U";
    const a = parts[0]?.[0] ?? "U";
    const b = parts.length > 1 ? parts[parts.length - 1]?.[0] ?? "" : "";
    return (a + b).toUpperCase();
}

function formatUserLine(s: SubscriptionDto) {
    const u = s.user;
    if (!u) return { fullName: "Unknown user", meta: "" };
    const fullName = `${u.firstName} ${u.lastName}`.trim() || "Unknown user";
    const meta = `@${u.username} · ${u.email}`;
    return { fullName, meta };
}

export default function SubscriptionsPage() {
    const [query, setQuery] = useState("");
    const [status, setStatus] = useState<StatusFilter>("all");
    const [sortKey, setSortKey] = useState<SortKey>("user");
    const [sortDir, setSortDir] = useState<"asc" | "desc">("asc");

    const rows = useMemo(() => {
        const q = query.trim().toLowerCase();

        const filtered = mockSubscriptions.filter((s) => {
            if (status === "active" && !s.active) return false;
            if (status === "inactive" && s.active) return false;

            if (!q) return true;

            const { fullName, meta } = formatUserLine(s);
            const hay = [
                fullName,
                meta,
                s.planName ?? "",
                s.userId,
                s.planId,
                s.id,
            ]
                .join(" ")
                .toLowerCase();

            return hay.includes(q);
        });

        const sorted = [...filtered].sort((a, b) => {
            const aUser = formatUserLine(a).fullName.toLowerCase();
            const bUser = formatUserLine(b).fullName.toLowerCase();
            const aPlan = (a.planName ?? "").toLowerCase();
            const bPlan = (b.planName ?? "").toLowerCase();

            let cmp = 0;
            if (sortKey === "user") cmp = aUser.localeCompare(bUser);
            if (sortKey === "plan") cmp = aPlan.localeCompare(bPlan);
            if (sortKey === "status") cmp = Number(a.active) - Number(b.active);

            return sortDir === "asc" ? cmp : -cmp;
        });

        return sorted;
    }, [query, status, sortKey, sortDir]);

    const total = mockSubscriptions.length;
    const shown = rows.length;

    return (
        <div className="min-h-screen bg-gradient-to-b from-slate-50 to-white">
            <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-10">
                <div className="flex flex-col gap-6">
                    <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                        <div>
                            <h1 className="text-3xl font-semibold tracking-tight text-slate-900">
                                Subscriptions
                            </h1>
                            <p className="mt-1 text-sm text-slate-500">
                                Mock data now. Later you’ll fetch from .NET Web API.
                            </p>
                        </div>

                        <div className="flex items-center gap-2">
                            <button
                                type="button"
                                className="inline-flex items-center justify-center rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white shadow-sm shadow-slate-900/10 hover:bg-slate-800 focus:outline-none focus:ring-2 focus:ring-slate-900 focus:ring-offset-2"
                                onClick={() => alert("Hook this to a Create flow later")}
                            >
                                + New subscription
                            </button>
                        </div>
                    </div>

                    <div className="rounded-3xl border border-slate-200 bg-white shadow-sm">
                        <div className="flex flex-col gap-4 border-b border-slate-200 px-6 py-5 lg:flex-row lg:items-center lg:justify-between">
                            <div>
                                <h2 className="text-base font-semibold text-slate-900">
                                    All subscriptions
                                </h2>
                                <p className="mt-1 text-sm text-slate-500">
                                    Search by name, username, email, plan, or IDs.
                                </p>
                            </div>

                            <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
                                <div className="relative">
                                    <input
                                        value={query}
                                        onChange={(e) => setQuery(e.target.value)}
                                        placeholder="Search user / plan..."
                                        className="w-full sm:w-80 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-2.5 text-sm text-slate-900 placeholder:text-slate-400 shadow-inner outline-none focus:bg-white focus:ring-2 focus:ring-slate-900"
                                    />
                                    <div className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-slate-400">
                                        ⌕
                                    </div>
                                </div>

                                <div className="flex items-center gap-2">
                                    <select
                                        value={status}
                                        onChange={(e) => setStatus(e.target.value as StatusFilter)}
                                        className="rounded-2xl border border-slate-200 bg-white px-3 py-2.5 text-sm font-semibold text-slate-700 shadow-sm outline-none focus:ring-2 focus:ring-slate-900"
                                    >
                                        <option value="all">All</option>
                                        <option value="active">Active</option>
                                        <option value="inactive">Inactive</option>
                                    </select>

                                    <select
                                        value={`${sortKey}:${sortDir}`}
                                        onChange={(e) => {
                                            const [k, d] = e.target.value.split(":") as [
                                                SortKey,
                                                    "asc" | "desc"
                                            ];
                                            setSortKey(k);
                                            setSortDir(d);
                                        }}
                                        className="rounded-2xl border border-slate-200 bg-white px-3 py-2.5 text-sm font-semibold text-slate-700 shadow-sm outline-none focus:ring-2 focus:ring-slate-900"
                                    >
                                        <option value="user:asc">User (A→Z)</option>
                                        <option value="user:desc">User (Z→A)</option>
                                        <option value="plan:asc">Plan (A→Z)</option>
                                        <option value="plan:desc">Plan (Z→A)</option>
                                        <option value="status:asc">Status (Inactive→Active)</option>
                                        <option value="status:desc">Status (Active→Inactive)</option>
                                    </select>

                                    <button
                                        type="button"
                                        onClick={() => setQuery("")}
                                        className={cx(
                                            "rounded-2xl bg-white px-3 py-2.5 text-sm font-semibold text-slate-700 ring-1 ring-inset ring-slate-200 hover:bg-slate-50",
                                            query.trim().length === 0 && "hidden"
                                        )}
                                    >
                                        Clear
                                    </button>
                                </div>
                            </div>
                        </div>

                        <div className="overflow-x-auto">
                            <table className="min-w-full text-left">
                                <thead className="bg-slate-50">
                                <tr className="text-xs font-semibold uppercase tracking-wider text-slate-500">
                                    <th className="px-6 py-4">User</th>
                                    <th className="px-6 py-4">Plan</th>
                                    <th className="px-6 py-4">Status</th>
                                    <th className="px-6 py-4 text-right">Actions</th>
                                </tr>
                                </thead>

                                <tbody className="divide-y divide-slate-100 bg-white">
                                {rows.length === 0 ? (
                                    <tr>
                                        <td colSpan={4} className="px-6 py-14">
                                            <div className="flex flex-col items-center justify-center text-center">
                                                <div className="h-14 w-14 rounded-2xl bg-slate-100 flex items-center justify-center text-slate-700 font-semibold">
                                                    S
                                                </div>
                                                <h3 className="mt-4 text-base font-semibold text-slate-900">
                                                    No results
                                                </h3>
                                                <p className="mt-1 text-sm text-slate-500 max-w-md">
                                                    Try a different search or clear filters.
                                                </p>
                                                <button
                                                    type="button"
                                                    onClick={() => {
                                                        setQuery("");
                                                        setStatus("all");
                                                        setSortKey("user");
                                                        setSortDir("asc");
                                                    }}
                                                    className="mt-5 inline-flex items-center gap-2 rounded-2xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white shadow-sm hover:bg-slate-800"
                                                >
                                                    Reset
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                ) : (
                                    rows.map((s) => {
                                        const { fullName, meta } = formatUserLine(s);
                                        const planName = s.planName?.trim() || "Unknown plan";

                                        return (
                                            <tr
                                                key={s.id}
                                                className="hover:bg-slate-50/70 transition-colors"
                                            >
                                                <td className="px-6 py-4">
                                                    <div className="flex items-center gap-3">
                                                        <div className="h-10 w-10 rounded-2xl bg-gradient-to-br from-slate-900 to-slate-700 text-white flex items-center justify-center shadow-sm">
                                <span className="text-sm font-semibold">
                                  {initials(fullName)}
                                </span>
                                                        </div>
                                                        <div className="min-w-0">
                                                            <div className="truncate text-sm font-semibold text-slate-900">
                                                                {fullName}
                                                            </div>
                                                            <div className="mt-0.5 text-xs text-slate-500 truncate">
                                                                {meta}
                                                            </div>
                                                            <div className="mt-0.5 text-xs text-slate-400 font-mono truncate">
                                                                {s.id}
                                                            </div>
                                                        </div>
                                                    </div>
                                                </td>

                                                <td className="px-6 py-4">
                                                    <div className="text-sm font-semibold text-slate-900">
                                                        {planName}
                                                    </div>
                                                    <div className="mt-0.5 text-xs text-slate-500">
                                                        PlanId:{" "}
                                                        <span className="font-mono">{s.planId}</span>
                                                    </div>
                                                </td>

                                                <td className="px-6 py-4">
                            <span
                                className={cx(
                                    "inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ring-inset",
                                    s.active
                                        ? "bg-emerald-50 text-emerald-700 ring-emerald-200"
                                        : "bg-slate-50 text-slate-600 ring-slate-200"
                                )}
                            >
                              {s.active ? "Active" : "Inactive"}
                            </span>
                                                </td>

                                                <td className="px-6 py-4">
                                                    <div className="flex items-center justify-end gap-2">
                                                        <button
                                                            type="button"
                                                            className="rounded-2xl bg-white px-3 py-2 text-sm font-semibold text-slate-900 ring-1 ring-inset ring-slate-200 hover:bg-slate-50"
                                                            onClick={() => alert(`Details: ${s.id}`)}
                                                        >
                                                            Details
                                                        </button>

                                                        <button
                                                            type="button"
                                                            className="rounded-2xl bg-slate-900 px-3 py-2 text-sm font-semibold text-white shadow-sm shadow-slate-900/10 hover:bg-slate-800"
                                                            onClick={() => alert(`Edit: ${s.id}`)}
                                                        >
                                                            Edit
                                                        </button>

                                                        <button
                                                            type="button"
                                                            className="rounded-2xl bg-white px-3 py-2 text-sm font-semibold text-rose-700 ring-1 ring-inset ring-rose-200 hover:bg-rose-50"
                                                            onClick={() => alert(`Delete: ${s.id}`)}
                                                        >
                                                            Delete
                                                        </button>
                                                    </div>
                                                </td>
                                            </tr>
                                        );
                                    })
                                )}
                                </tbody>
                            </table>
                        </div>

                        <div className="flex flex-col gap-2 border-t border-slate-200 px-6 py-4 sm:flex-row sm:items-center sm:justify-between">
                            <div className="text-sm text-slate-500">
                                Showing{" "}
                                <span className="font-semibold text-slate-700">{shown}</span> of{" "}
                                <span className="font-semibold text-slate-700">{total}</span>{" "}
                                subscriptions
                            </div>

                            <div className="flex items-center gap-2 text-xs text-slate-400">
                                <span className="hidden sm:inline">Mock dataset</span>
                                <span className="hidden sm:inline">•</span>
                                <span>Ready for API wiring</span>
                            </div>
                        </div>
                    </div>

                    <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
                        <div className="text-sm font-semibold text-slate-900">
                            API integration (later)
                        </div>
                        <p className="mt-1 text-sm text-slate-500">
                            Replace <span className="font-mono">mockSubscriptions</span> with a
                            fetch to your .NET Web API (e.g. GET{" "}
                            <span className="font-mono">/api/subscriptions</span>) and map the
                            JSON to <span className="font-mono">SubscriptionDto[]</span>.
                        </p>
                    </div>
                </div>
            </div>
        </div>
    );
}
