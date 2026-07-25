"use client";
import { DASH_CSS } from "@/lib/dashboardCss";

export default function Page() {
  return (<>
    <style>{DASH_CSS}</style>
    <div className="ph">
      <div>
        <div className="ph-title">Siparişler</div>
        <div className="ph-sub">Yakında eklenecek</div>
      </div>
    </div>
    <div className="card" style={{ borderStyle: "dashed", textAlign: "center", padding: 56 }}>
      <div className="text-muted">Bu modül sonraki fazlarda geliştirilecek.</div>
    </div>
  </>);
}
