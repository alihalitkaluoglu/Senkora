
"use client";
import { useEffect, useState } from "react";
import { useRouter, usePathname } from "next/navigation";
import Link from "next/link";
import { useAuthStore } from "@/stores/useAuthStore";
import { DASH_CSS } from "@/lib/dashboardCss";

const NAV = [
  { href: "/dashboard",           ico: "▦", label: "Dashboard" },
  { href: "/dashboard/logo-erp",  ico: "◉", label: "Logo ERP" },
  { href: "/dashboard/woo",       ico: "⬡", label: "WooCommerce" },
  { href: "/dashboard/products",  ico: "▤", label: "Ürünler" },
  { href: "/dashboard/orders",    ico: "▣", label: "Siparişler" },
  { href: "/dashboard/sync",      ico: "↻", label: "Senkronizasyon" },
  { href: "/dashboard/scheduler", ico: "⊙", label: "Zamanlayıcı" },
  { href: "/dashboard/logs",      ico: "≡", label: "Loglar" },
  { href: "/dashboard/license",   ico: "◈", label: "Lisans" },
  { href: "/dashboard/users",     ico: "◎", label: "Kullanıcılar" },
];


export default function DashLayout({ children }: { children: React.ReactNode }) {
  const router   = useRouter();
  const path     = usePathname();
  const { isAuthenticated, user, clearAuth } = useAuthStore();
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const ok = typeof window !== "undefined" && !!localStorage.getItem("senkora_token");
    if (!ok) { router.push("/login"); return; }
    setReady(true);
  }, [router]);

  if (!ready) return (
    <div style={{ minHeight:"100vh", display:"flex", alignItems:"center", justifyContent:"center" }}>
      <style>{DASH_CSS}</style>
      <div className="spin" />
    </div>
  );

  return (
    <>
      <style>{DASH_CSS}</style>
      <div className="shell">
        <aside className="side">
          <div className="brand">
            <div className="brand-ico">S</div>
            <span className="brand-name">Senkora</span>
          </div>
          <nav className="sidenav">
            {NAV.map(n => {
              const active = path === n.href || (n.href !== "/dashboard" && path.startsWith(n.href));
              return (
                <Link key={n.href} href={n.href} className={`slink${active ? " on" : ""}`}>
                  <span className="ico">{n.ico}</span>{n.label}
                </Link>
              );
            })}
          </nav>
          <div className="sfoot">
            <div className="semail">{user?.email}</div>
            <button className="sout" onClick={() => { clearAuth(); router.push("/login"); }}>
              Çıkış Yap →
            </button>
          </div>
        </aside>
        <main className="content">{children}</main>
      </div>
    </>
  );
}
