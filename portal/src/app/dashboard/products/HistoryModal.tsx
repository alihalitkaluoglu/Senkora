"use client";
import { useEffect, useState } from "react";
import { productApi } from "@/lib/api";
import { apiError } from "@/lib/errorMessage";
import type { ProductHistory } from "@/types/api";

const ACTION_LABELS: Record<string, { label: string; color: string }> = {
  LogoFetch:   { label: "Logo'dan Çekildi",    color: "#79c0ff" },
  LogoRefresh: { label: "Logo Güncellendi",     color: "#79c0ff" },
  Enrich:      { label: "Eşleme Kaydedildi",    color: "#a371f7" },
  WooCreate:   { label: "WooCommerce'e Eklendi", color: "#3fb950" },
  WooUpdate:   { label: "WooCommerce Güncellendi", color: "#3fb950" },
  Error:       { label: "Hata",                 color: "#f85149" },
};

export default function HistoryModal({
  mappingId, productCode, onClose,
}: { mappingId: string; productCode: string; onClose: () => void }) {
  const [list, setList] = useState<ProductHistory[]>([]);
  const [busy, setBusy] = useState(true);
  const [err, setErr]   = useState("");

  useEffect(() => {
    productApi.history(mappingId)
      .then(r => setList(r.data.data ?? []))
      .catch(e => setErr(apiError(e, "Tarihçe alınamadı.")))
      .finally(() => setBusy(false));
  }, [mappingId]);

  function renderChanges(json: string | null) {
    if (!json) return null;
    try {
      const obj = JSON.parse(json) as Record<string, { old: string; new: string }>;
      return (
        <div style={{ marginTop: 8, display: "grid", gap: 4 }}>
          {Object.entries(obj).map(([field, v]) => (
            <div key={field} style={{ display: "flex", gap: 8, fontSize: 11,
              alignItems: "baseline" }}>
              <span style={{ color: "#8b949e", minWidth: 70 }}>{field}</span>
              <span style={{ color: "#f85149", textDecoration: "line-through" }}>
                {v.old || "(boş)"}
              </span>
              <span style={{ color: "#484f58" }}>→</span>
              <span style={{ color: "#3fb950" }}>{v.new || "(boş)"}</span>
            </div>
          ))}
        </div>
      );
    } catch { return null; }
  }

  return (
    <div className="overlay" onClick={e => e.target === e.currentTarget && onClose()}>
      <div className="modal" style={{ maxWidth: 640 }}>
        <div className="mhead">
          <span className="mtitle">
            Aktarım Tarihçesi
            <span style={{ color: "#6e7681", fontWeight: 400 }}> — {productCode}</span>
          </span>
          <button className="mclose" onClick={onClose}>✕</button>
        </div>
        <div className="mbody">
          {busy ? <div className="text-muted">Yükleniyor...</div>
            : err ? <div className="alert-err">{err}</div>
            : list.length === 0 ? (
              <div className="card" style={{ borderStyle: "dashed", textAlign: "center",
                padding: 40, marginBottom: 0 }}>
                <div className="text-muted">
                  Bu ürün için henüz kayıt yok.<br />
                  Eşleme kaydedip WooCommerce&apos;e gönderdiğinizde burada görünecek.
                </div>
              </div>
            ) : (
              <div style={{ display: "grid", gap: 10 }}>
                {list.map(h => {
                  const meta = ACTION_LABELS[h.action]
                    ?? { label: h.action, color: "#8b949e" };
                  return (
                    <div key={h.id} style={{
                      background: "#0d1117", border: "1px solid #21262d",
                      borderLeft: `3px solid ${h.isSuccess ? meta.color : "#f85149"}`,
                      borderRadius: 8, padding: "11px 14px",
                    }}>
                      <div style={{ display: "flex", justifyContent: "space-between",
                        alignItems: "baseline", gap: 12 }}>
                        <span style={{ fontSize: 12.5, fontWeight: 500, color: meta.color }}>
                          {h.isSuccess ? "" : "✗ "}{meta.label}
                        </span>
                        <span style={{ fontSize: 11, color: "#6e7681", whiteSpace: "nowrap" }}>
                          {new Date(h.createdAt).toLocaleString("tr-TR")}
                        </span>
                      </div>
                      {h.message && (
                        <div style={{ fontSize: 11.5, color: "#c9d1d9", marginTop: 5,
                          lineHeight: 1.5, wordBreak: "break-word" }}>
                          {h.message}
                        </div>
                      )}
                      {renderChanges(h.changesJson)}
                      <div style={{ display: "flex", gap: 14, marginTop: 7,
                        fontSize: 10.5, color: "#484f58" }}>
                        {h.performedBy && <span>{h.performedBy}</span>}
                        {h.durationMs > 0 && <span>{h.durationMs}ms</span>}
                        {h.wooProductId && <span>WC #{h.wooProductId}</span>}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          <div className="mfooter">
            <button className="btn btn-ghost" style={{ flex: 1 }} onClick={onClose}>
              Kapat
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
