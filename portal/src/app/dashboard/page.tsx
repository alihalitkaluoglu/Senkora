
"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import { dashboardApi, licenseApi } from "@/lib/api";
import type { DashboardStats, LicenseStatus } from "@/types/api";
import { DASH_CSS } from "@/lib/dashboardCss";


export default function DashPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [lic,   setLic  ] = useState<LicenseStatus  | null>(null);
  const [busy,  setBusy ] = useState(true);

  useEffect(() => {
    Promise.all([dashboardApi.stats(), licenseApi.status()])
      .then(([s, l]) => { setStats(s.data.data ?? null); setLic(l.data.data ?? null); })
      .catch(console.error).finally(() => setBusy(false));
  }, []);

  return (
    <>
      <style>{DASH_CSS}</style>
      <div className="ph">
        <div><div className="ph-title">Dashboard</div>
        <div className="ph-sub">Genel bakış ve sistem durumu</div></div>
      </div>

      {busy ? <div className="dim">Yükleniyor...</div> : (<>
        {lic && (
          <div className={`card mb8 ${lic.isValid ? "res-ok" : "res-err"}`}
            style={{ display:"flex", justifyContent:"space-between", alignItems:"center" }}>
            <div className="row">
              <span style={{ fontSize:18 }}>{lic.isValid ? "✓" : "✗"}</span>
              <div>
                <div className={lic.isValid ? "res-ok-t" : "res-err-t"}>
                  {lic.tier} Lisans{lic.isTrialMode ? " (Trial)" : ""}
                </div>
                <div className="res-sub">
                  {lic.daysRemaining > 3650 ? "Süresiz" : `${lic.daysRemaining} gün kaldı`}
                </div>
              </div>
            </div>
            <div className="res-sub">Sync: her {lic.syncIntervalMinutes} dk</div>
          </div>
        )}
        {stats && (
          <div className="stats">
            {[
              { l: "Toplam İş",  v: stats.totalSyncJobs,  c: "#f0f6fc" },
              { l: "Başarılı",   v: stats.successfulJobs, c: "#3fb950" },
              { l: "Başarısız",  v: stats.failedJobs,     c: "#f85149" },
              { l: "Bekleyen",   v: stats.pendingJobs,    c: "#f0883e" },
            ].map(x => (
              <div key={x.l} className="scard">
                <div className="sval" style={{ color: x.c }}>{x.v}</div>
                <div className="slbl">{x.l}</div>
              </div>
            ))}
          </div>
        )}
        <div className="qgrid">
          <Link href="/dashboard/logo-erp" className="qcard">
            <div className="qcard-ico">◉</div>
            <div className="qcard-t">Logo ERP Bağlantıları</div>
            <div className="qcard-s">Bağlantı tanımla ve test et</div>
          </Link>
          <Link href="/dashboard/woo" className="qcard">
            <div className="qcard-ico">⬡</div>
            <div className="qcard-t">WooCommerce Mağazaları</div>
            <div className="qcard-s">Mağaza tanımla ve test et</div>
          </Link>
        </div>
      </>)}
    </>
  );
}
