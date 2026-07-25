"use client";
import { useCallback, useEffect, useState } from "react";
import { productApi, logoApi, wooApi } from "@/lib/api";
import { DASH_CSS } from "@/lib/dashboardCss";
import { apiError } from "@/lib/errorMessage";
import type {
  LogoSqlProbe,
  ProductMapping, LogoConnection, WooStore,
  ImportResult, RefreshResult, LogoFetchDiagnostics,
} from "@/types/api";
import ProductEditor from "./ProductEditor";
import HistoryModal from "./HistoryModal";

const STATUS_LABELS: Record<string, string> = {
  Draft: "Taslak", Enriched: "Hazır", Pending: "Bekliyor",
  Synced: "Gönderildi", Error: "Hata", Excluded: "Hariç",
};

export default function ProductsPage() {
  const [list, setList]   = useState<ProductMapping[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage]   = useState(1);
  const pageSize          = 25;
  const [busy, setBusy]   = useState(true);
  const [status, setStatus] = useState("");
  const [search, setSearch] = useState("");

  const [logoConns, setLogoConns] = useState<LogoConnection[]>([]);
  const [stores, setStores]       = useState<WooStore[]>([]);
  const [conn, setConn] = useState({ logoConnectionId: "", wooStoreId: "" });

  // Import
  const [showImport, setShowImport] = useState(false);
  const [maxScan, setMaxItems]     = useState(0);
  const [importing, setImporting]   = useState(false);
  const [importRes, setImportRes]   = useState<ImportResult | null>(null);
  const [importErr, setImportErr]   = useState("");
  const [diag, setDiag]             = useState<LogoFetchDiagnostics | null>(null);
  const [diagBusy, setDiagBusy]     = useState(false);
  const [sqlProbes, setSqlProbes]   = useState<LogoSqlProbe[] | null>(null);
  const [probeBusy, setProbeBusy]   = useState(false);

  // Refresh
  const [showRefresh, setShowRefresh] = useState(false);
  const [refreshing, setRefreshing]   = useState(false);
  const [preview, setPreview]         = useState<RefreshResult | null>(null);
  const [refreshRes, setRefreshRes]   = useState<RefreshResult | null>(null);
  const [refreshErr, setRefreshErr]   = useState("");

  const [selected, setSelected]     = useState<Set<string>>(new Set());
  const [deleting, setDeleting]     = useState(false);
  const [showDelete, setShowDelete] = useState(false);

  const [editId, setEditId]   = useState<string | null>(null);
  const [histId, setHistId]   = useState<{ id: string; code: string } | null>(null);
  const [syncId, setSyncId]   = useState<string | null>(null);
  const [toast, setToast]     = useState<{ ok: boolean; text: string } | null>(null);

  const load = useCallback(() => {
    setBusy(true);
    productApi.list({
      status: status || undefined, search: search || undefined, page, pageSize,
    })
      .then(r => {
        setList(r.data.data?.items ?? []);
        setTotal(r.data.data?.totalCount ?? 0);
      })
      .catch(e => showToast(false, apiError(e, "Liste alınamadı.")))
      .finally(() => setBusy(false));
  }, [status, search, page]);

  useEffect(() => { load(); }, [load]);

  useEffect(() => {
    Promise.all([logoApi.list(), wooApi.list()])
      .then(([l, w]) => {
        const lc = l.data.data ?? [];
        const ws = w.data.data ?? [];
        setLogoConns(lc); setStores(ws);
        setConn(c => ({
          logoConnectionId: c.logoConnectionId || lc[0]?.id || "",
          wooStoreId:       c.wooStoreId       || ws[0]?.id || "",
        }));
      })
      .catch(() => {});
  }, []);

  function showToast(ok: boolean, text: string) {
    setToast({ ok, text });
    setTimeout(() => setToast(null), 7000);
  }

  const connReady = !!conn.logoConnectionId && !!conn.wooStoreId;

  async function runPurge() {
    if (!confirm("Daha önce silinmiş ürün kayıtları veritabanından tamamen kaldırılacak. Devam edilsin mi?"))
      return;
    setImporting(true);
    try {
      const r = await productApi.purgeDeleted();
      setImportErr("");
      setImportRes(null);
      showToast(true, `${r.data.data} silinmiş kayıt temizlendi. Şimdi tekrar içe aktarabilirsiniz.`);
    } catch (e) {
      setImportErr(apiError(e, "Temizlik başarısız."));
    } finally { setImporting(false); }
  }

  async function runImport() {
    if (!connReady) { setImportErr("Logo bağlantısı ve mağaza seçin."); return; }
    setImporting(true); setImportErr(""); setImportRes(null);
    try {
      const r = await productApi.importNew({ ...conn, maxScan });
      setImportRes(r.data.data!);
      load();
    } catch (e) { setImportErr(apiError(e, "İçe aktarma başarısız.")); }
    finally { setImporting(false); }
  }

  async function runSqlProbe() {
    if (!conn.logoConnectionId) { setImportErr("Logo bağlantısı seçin."); return; }
    setProbeBusy(true); setSqlProbes(null);
    try {
      const r = await logoApi.probeSql(conn.logoConnectionId);
      setSqlProbes(r.data.data ?? []);
    } catch (e) {
      setImportErr(apiError(e, "SQL testi çalıştırılamadı."));
    } finally { setProbeBusy(false); }
  }

  async function runDiagnose() {
    if (!conn.logoConnectionId) { setImportErr("Logo bağlantısı seçin."); return; }
    setDiagBusy(true); setDiag(null);
    try {
      const r = await productApi.diagnoseLogo(conn.logoConnectionId, 3);
      setDiag(r.data.data!);
    } catch (e) { setImportErr(apiError(e)); }
    finally { setDiagBusy(false); }
  }

  async function runPreview() {
    if (!connReady) { setRefreshErr("Logo bağlantısı ve mağaza seçin."); return; }
    setRefreshing(true); setRefreshErr(""); setPreview(null); setRefreshRes(null);
    try {
      const r = await productApi.refresh({ ...conn, previewOnly: true });
      setPreview(r.data.data!);
    } catch (e) { setRefreshErr(apiError(e, "Önizleme başarısız.")); }
    finally { setRefreshing(false); }
  }

  async function applyRefresh() {
    setRefreshing(true); setRefreshErr("");
    try {
      const r = await productApi.refresh({ ...conn, previewOnly: false });
      setRefreshRes(r.data.data!); setPreview(null);
      load();
    } catch (e) { setRefreshErr(apiError(e, "Güncelleme başarısız.")); }
    finally { setRefreshing(false); }
  }

  function toggleOne(id: string) {
    setSelected(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });
  }

  function toggleAllOnPage() {
    setSelected(prev => {
      const allSel = list.length > 0 && list.every(p => prev.has(p.id));
      const next = new Set(prev);
      if (allSel) list.forEach(p => next.delete(p.id));
      else list.forEach(p => next.add(p.id));
      return next;
    });
  }

  async function runDelete(all: boolean) {
    setDeleting(true);
    try {
      const r = await productApi.deleteMany(all
        ? { deleteAll: true, statusFilter: status || undefined }
        : { ids: Array.from(selected) });
      showToast(true, `${r.data.data} ürün silindi.`);
      setSelected(new Set());
      setShowDelete(false);
      setPage(1);
      load();
    } catch (e) {
      showToast(false, apiError(e, "Silme başarısız."));
    } finally { setDeleting(false); }
  }

  async function syncOne(id: string) {
    setSyncId(id);
    try {
      const r = await productApi.syncToWoo(id);
      showToast(true, `WooCommerce'e gönderildi (Ürün ID: ${r.data.data})`);
      load();
    } catch (e) { showToast(false, apiError(e, "Gönderim başarısız.")); }
    finally { setSyncId(null); }
  }

  const pages = Math.max(1, Math.ceil(total / pageSize));

  const ConnSelectors = ({ err }: { err: string }) => (<>
    {err && <div className="alert-err">{err}</div>}
    <div className="g2">
      <div style={{ marginBottom: 14 }}>
        <label className="lbl">Logo ERP Bağlantısı *</label>
        <select className="inp" value={conn.logoConnectionId}
          onChange={e => setConn(c => ({ ...c, logoConnectionId: e.target.value }))}>
          <option value="">Seçin...</option>
          {logoConns.map(c =>
            <option key={c.id} value={c.id}>{c.name} — Firma {c.firmNo}</option>)}
        </select>
      </div>
      <div style={{ marginBottom: 14 }}>
        <label className="lbl">WooCommerce Mağazası *</label>
        <select className="inp" value={conn.wooStoreId}
          onChange={e => setConn(c => ({ ...c, wooStoreId: e.target.value }))}>
          <option value="">Seçin...</option>
          {stores.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
        </select>
      </div>
    </div>
  </>);

  return (<>
    <style>{DASH_CSS}</style>

    {toast && (
      <div style={{
        position: "fixed", top: 20, right: 20, zIndex: 300, maxWidth: 420,
        background: toast.ok ? "rgba(35,134,54,.18)" : "rgba(248,81,73,.18)",
        border: `1px solid ${toast.ok ? "rgba(35,134,54,.45)" : "rgba(248,81,73,.45)"}`,
        borderRadius: 10, padding: "12px 16px", fontSize: 13, lineHeight: 1.55,
        color: toast.ok ? "#3fb950" : "#ff9b95",
        boxShadow: "0 10px 30px rgba(0,0,0,.5)",
      }}>{toast.text}</div>
    )}

    <div className="ph">
      <div>
        <div className="ph-title">Ürünler</div>
        <div className="ph-sub">Logo&apos;dan içe aktar → portal&apos;da eşle → WooCommerce&apos;e gönder</div>
      </div>
      <div style={{ display: "flex", gap: 8 }}>
        <button className="btn btn-ghost"
          onClick={() => { setShowRefresh(true); setRefreshErr(""); setPreview(null); setRefreshRes(null); }}>
          ↻ Logo&apos;dan Güncelle
        </button>
        <button className="btn btn-primary"
          onClick={() => { setShowImport(true); setImportErr(""); setImportRes(null); setDiag(null); }}>
          ↓ Yeni Ürünleri Çek
        </button>
      </div>
    </div>

    {/* ── IMPORT MODAL ─────────────────────────────────────────── */}
    {showImport && (
      <div className="overlay" onClick={e => e.target === e.currentTarget && setShowImport(false)}>
        <div className="modal" style={{ maxWidth: 620 }}>
          <div className="mhead">
            <span className="mtitle">Yeni Ürünleri İçe Aktar</span>
            <button className="mclose" onClick={() => setShowImport(false)}>✕</button>
          </div>
          <div className="mbody">
            <div style={{ background: "rgba(56,139,253,.07)", border: "1px solid rgba(56,139,253,.25)",
              borderRadius: 8, padding: "10px 13px", fontSize: 11.5, lineHeight: 1.6,
              color: "#8b949e", marginBottom: 16 }}>
              Logo&apos;daki tüm malzeme kartları taranır, <strong style={{ color: "#c9d1d9" }}>
              veritabanında olmayanlar</strong> eklenir. Mevcut ürünlere ve
              eşleme bilgilerinize dokunulmaz. Satış fiyatları fiyat kartlarından alınır.
            </div>

            <ConnSelectors err={importErr} />

            <div style={{ marginBottom: 14 }}>
              <label className="lbl">Maksimum kayıt (0 = tüm katalog)</label>
              <input className="inp" type="number" min={0} value={maxScan}
                onChange={e => setMaxItems(+e.target.value)} />
              <div style={{ fontSize: 10.5, color: "#6e7681", marginTop: 4 }}>
                2500 ürün için 3-8 dakika sürebilir. Sayfa açık kalmalı.
              </div>
            </div>

            {importRes && (
              <div className="rok" style={{ marginTop: 0, marginBottom: 14 }}>
                <div className="rokt">
                  {importRes.completed ? "✓ Tamamlandı" : "⚠ Kısmen tamamlandı"}
                </div>
                <div className="rsub" style={{ lineHeight: 1.7 }}>
                  {importRes.scanned} kayıt tarandı<br />
                  <strong style={{ color: "#3fb950" }}>{importRes.created} yeni ürün eklendi</strong><br />
                  {importRes.alreadyExists} kayıt zaten mevcuttu<br />
                  {importRes.pricesMatched} ürüne fiyat kartından fiyat atandı<br />
                  {importRes.stockMatched} ürüne stok miktarı atandı
                  {importRes.warning && (
                    <><br /><span style={{ color: "#f0883e" }}>{importRes.warning}</span></>
                  )}
                </div>
              </div>
            )}

            <div style={{ paddingTop: 14, borderTop: "1px solid #21262d" }}>
              <div style={{ display: "flex", alignItems: "center",
                justifyContent: "space-between", marginBottom: 10 }}>
                <span style={{ fontSize: 12, color: "#8b949e" }}>
                  Logo yanıtını incele
                </span>
                <div style={{ display: "flex", gap: 6 }}>
                  <button className="btn btn-ghost btn-sm" disabled={importing}
                    title="Duplicate key hatası alıyorsanız bir kez çalıştırın"
                    onClick={runPurge}>
                    Silinmişleri Temizle
                  </button>
                  <button className="btn btn-ghost btn-sm" disabled={probeBusy}
                    title="Logo'nun hangi SQL çağrı biçimini kabul ettiğini test eder"
                    onClick={runSqlProbe}>
                    {probeBusy ? "SQL..." : "SQL Testi"}
                  </button>
                  <button className="btn btn-ghost btn-sm" disabled={diagBusy}
                    onClick={runDiagnose}>
                    {diagBusy ? "Test..." : "Bağlantıyı Test Et"}
                  </button>
                </div>
              </div>
              {sqlProbes && (
                <div style={{ background: "#0d1117", border: "1px solid #21262d",
                  borderRadius: 8, padding: 12, fontSize: 11.5, marginBottom: 10 }}>
                  <div style={{ color: "#79c0ff", fontWeight: 600, marginBottom: 8 }}>
                    Logo SQL Çağrı Biçimi Testi
                  </div>
                  {sqlProbes.map((p, i) => (
                    <div key={i} style={{
                      padding: "6px 0",
                      borderBottom: i < sqlProbes.length - 1 ? "1px solid #161b22" : "none",
                    }}>
                      <div style={{ display: "flex", justifyContent: "space-between",
                        alignItems: "baseline", gap: 8 }}>
                        <span style={{ color: p.success ? "#3fb950" : "#6e7681",
                          fontWeight: p.success ? 600 : 400 }}>
                          {p.success ? "✓" : "✗"} {p.variant}
                        </span>
                        {p.success && (
                          <span style={{ color: "#3fb950", fontSize: 10.5 }}>
                            {p.rowCount} satır
                          </span>
                        )}
                      </div>
                      {p.error && (
                        <div style={{ color: "#8b949e", fontSize: 10, marginTop: 3,
                          wordBreak: "break-word" }}>
                          {p.error}
                        </div>
                      )}
                      {p.success && p.samplePayload && (
                        <div style={{ color: "#484f58", fontSize: 10, marginTop: 3,
                          fontFamily: "monospace", wordBreak: "break-all" }}>
                          {p.samplePayload}
                        </div>
                      )}
                    </div>
                  ))}
                  {!sqlProbes.some(p => p.success) && (
                    <div style={{ marginTop: 10, padding: "8px 10px",
                      background: "rgba(240,136,62,.1)", borderRadius: 6,
                      color: "#f0883e", fontSize: 10.5, lineHeight: 1.5 }}>
                      Hiçbir SQL biçimi çalışmadı. Logo REST kurulumunda
                      &quot;Generic SQL Sorgusu&quot; servisi kapalı olabilir.
                      Bu durumda stok ve ticari işlem grubu bilgileri alınamaz.
                    </div>
                  )}
                </div>
              )}

              {diag && (
                <div style={{ background: "#0d1117", border: "1px solid #21262d",
                  borderRadius: 8, padding: 12, fontSize: 11.5 }}>
                  <div className="info-row"><span>Token</span>
                    <strong style={{ color: diag.tokenObtained ? "#3fb950" : "#f85149" }}>
                      {diag.tokenObtained ? "✓" : "✗"}</strong></div>
                  <div className="info-row"><span>İstek</span>
                    <strong style={{ color: diag.requestSucceeded ? "#3fb950" : "#f85149" }}>
                      {diag.requestSucceeded ? "✓" : "✗"}</strong></div>
                  <div className="info-row"><span>Ayrıştırılan</span>
                    <strong>{diag.parsedItemCount}</strong></div>
                  {diag.errorMessage && (
                    <div style={{ marginTop: 8, color: "#ff9b95", wordBreak: "break-word" }}>
                      {diag.errorMessage}
                    </div>
                  )}
                  <details style={{ marginTop: 8 }}>
                    <summary style={{ cursor: "pointer", color: "#79c0ff", fontSize: 11 }}>
                      Ham yanıt
                    </summary>
                    <pre style={{ marginTop: 8, maxHeight: 200, overflow: "auto",
                      background: "#010409", padding: 10, borderRadius: 6, fontSize: 10,
                      color: "#c9d1d9", whiteSpace: "pre-wrap", wordBreak: "break-all" }}>
                      {diag.rawResponsePreview ?? "(yok)"}
                    </pre>
                  </details>
                </div>
              )}
            </div>

            <div className="mfooter">
              <button className="btn btn-ghost" style={{ flex: 1 }}
                onClick={() => setShowImport(false)}>Kapat</button>
              <button className="btn btn-primary" style={{ flex: 1 }}
                disabled={importing || !connReady} onClick={runImport}>
                {importing ? "Aktarılıyor..." : "İçe Aktar"}
              </button>
            </div>
          </div>
        </div>
      </div>
    )}

    {/* ── REFRESH MODAL ────────────────────────────────────────── */}
    {showRefresh && (
      <div className="overlay" onClick={e => e.target === e.currentTarget && setShowRefresh(false)}>
        <div className="modal" style={{ maxWidth: 680 }}>
          <div className="mhead">
            <span className="mtitle">Logo Verilerini Güncelle</span>
            <button className="mclose" onClick={() => setShowRefresh(false)}>✕</button>
          </div>
          <div className="mbody">
            <div style={{ background: "rgba(35,134,54,.07)", border: "1px solid rgba(35,134,54,.25)",
              borderRadius: 8, padding: "10px 13px", fontSize: 11.5, lineHeight: 1.6,
              color: "#8b949e", marginBottom: 16 }}>
              Mevcut ürünlerin <strong style={{ color: "#c9d1d9" }}>fiyat, stok, ad, KDV,
              grup</strong> bilgileri Logo&apos;dan güncellenir.<br />
              <strong style={{ color: "#3fb950" }}>Görsel, kategori, etiket ve özellik
              eşlemelerinize dokunulmaz.</strong>
            </div>

            <ConnSelectors err={refreshErr} />

            {preview && (
              <div style={{ marginTop: 6 }}>
                <div style={{ background: "rgba(240,136,62,.07)",
                  border: "1px solid rgba(240,136,62,.3)", borderRadius: 8,
                  padding: "11px 14px", fontSize: 12, marginBottom: 12 }}>
                  <strong style={{ color: "#f0883e" }}>Onay bekleniyor</strong>
                  <div style={{ color: "#c9d1d9", marginTop: 5, lineHeight: 1.7 }}>
                    {preview.total} ürün incelendi<br />
                    <strong style={{ color: "#f0883e" }}>{preview.updated} üründe değişiklik var</strong><br />
                    {preview.unchanged} ürün değişmedi
                    {preview.notFoundInLogo > 0 &&
                      <><br />{preview.notFoundInLogo} ürün Logo&apos;da bulunamadı</>}
                    <br />{preview.pricesMatched} ürüne fiyat kartından fiyat atandı
                  </div>
                </div>

                {preview.changes.length > 0 && (
                  <div style={{ maxHeight: 260, overflowY: "auto",
                    border: "1px solid #21262d", borderRadius: 8 }}>
                    <table className="ptable">
                      <thead><tr>
                        <th>Kod</th><th>Alan</th><th>Eski</th><th>Yeni</th>
                      </tr></thead>
                      <tbody>
                        {preview.changes.map((c, i) => (
                          <tr key={i}>
                            <td style={{ fontFamily: "monospace", color: "#79c0ff" }}>{c.code}</td>
                            <td>{c.field}</td>
                            <td style={{ color: "#f85149" }}>{c.oldValue || "—"}</td>
                            <td style={{ color: "#3fb950" }}>{c.newValue || "—"}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            )}

            {refreshRes && (
              <div className="rok" style={{ marginTop: 0 }}>
                <div className="rokt">✓ Güncelleme tamamlandı</div>
                <div className="rsub" style={{ lineHeight: 1.7 }}>
                  {refreshRes.updated} ürün güncellendi<br />
                  {refreshRes.unchanged} ürün değişmedi<br />
                  {refreshRes.pricesMatched} ürüne fiyat atandı
                </div>
              </div>
            )}

            <div className="mfooter">
              <button className="btn btn-ghost" style={{ flex: 1 }}
                onClick={() => setShowRefresh(false)}>Kapat</button>
              {!preview ? (
                <button className="btn btn-primary" style={{ flex: 1 }}
                  disabled={refreshing || !connReady} onClick={runPreview}>
                  {refreshing ? "İnceleniyor..." : "Değişiklikleri Göster"}
                </button>
              ) : (
                <button className="btn btn-primary" style={{ flex: 1 }}
                  disabled={refreshing} onClick={applyRefresh}>
                  {refreshing ? "Uygulanıyor..." : `${preview.updated} Ürünü Güncelle`}
                </button>
              )}
            </div>
          </div>
        </div>
      </div>
    )}

    {showDelete && (
      <div className="overlay" onClick={e => e.target === e.currentTarget && setShowDelete(false)}>
        <div className="modal msm">
          <div className="mhead">
            <span className="mtitle">Ürünleri Sil</span>
            <button className="mclose" onClick={() => setShowDelete(false)}>✕</button>
          </div>
          <div className="mbody">
            <div style={{ fontSize: 13, color: "#c9d1d9", lineHeight: 1.6, marginBottom: 18 }}>
              <strong style={{ color: "#f0883e" }}>{selected.size} ürün</strong> eşlemesi silinecek.<br />
              <span style={{ fontSize: 11.5, color: "#8b949e" }}>
                WooCommerce&apos;e gönderilmiş ürünler mağazada kalır, yalnızca Senkora
                kaydı silinir.
              </span>
            </div>
            <div className="mfooter" style={{ marginTop: 0 }}>
              <button className="btn btn-ghost" style={{ flex: 1 }}
                onClick={() => setShowDelete(false)}>Vazgeç</button>
              <button className="btn btn-danger" style={{ flex: 1 }}
                disabled={deleting} onClick={() => runDelete(false)}>
                {deleting ? "Siliniyor..." : `${selected.size} Ürünü Sil`}
              </button>
            </div>
            <div style={{ marginTop: 14, paddingTop: 14, borderTop: "1px solid #21262d" }}>
              <button className="btn btn-ghost btn-sm" style={{ width: "100%" }}
                disabled={deleting}
                onClick={() => {
                  if (confirm(status
                    ? `"${STATUS_LABELS[status]}" durumundaki TÜM ürünler silinecek. Emin misiniz?`
                    : "TÜM ürün eşlemeleri silinecek. Emin misiniz?")) runDelete(true);
                }}>
                {status
                  ? `Bunun yerine "${STATUS_LABELS[status]}" durumundaki tümünü sil`
                  : "Bunun yerine tüm ürünleri sil"}
              </button>
            </div>
          </div>
        </div>
      </div>
    )}

    {editId && (
      <ProductEditor mappingId={editId} wooStoreId={conn.wooStoreId}
        onClose={() => setEditId(null)}
        onSaved={() => { setEditId(null); load(); showToast(true, "Eşleme kaydedildi."); }} />
    )}

    {histId && (
      <HistoryModal mappingId={histId.id} productCode={histId.code}
        onClose={() => setHistId(null)} />
    )}

    <div className="toolbar">
      <select className="filter-sel" value={status}
        onChange={e => { setStatus(e.target.value); setPage(1); }}>
        <option value="">Tüm durumlar</option>
        {Object.entries(STATUS_LABELS).map(([k, v]) =>
          <option key={k} value={k}>{v}</option>)}
      </select>
      <input className="search-inp" placeholder="Kod veya ürün adı ara..."
        value={search} onChange={e => setSearch(e.target.value)}
        onKeyDown={e => { if (e.key === "Enter") { setPage(1); load(); } }} />
      <button className="btn btn-ghost btn-sm" onClick={() => { setPage(1); load(); }}>Ara</button>

      {selected.size > 0 && (
        <>
          <span style={{ fontSize: 12, color: "#f0883e", marginLeft: 4 }}>
            {selected.size} seçili
          </span>
          <button className="btn btn-danger btn-sm" onClick={() => setShowDelete(true)}>
            Seçilenleri Sil
          </button>
          <button className="btn btn-ghost btn-sm" onClick={() => setSelected(new Set())}>
            Temizle
          </button>
        </>
      )}

      <div style={{ marginLeft: "auto", fontSize: 12, color: "#6e7681" }}>{total} kayıt</div>
    </div>

    {busy ? <div className="text-muted">Yükleniyor...</div>
      : list.length === 0 ? (
        <div className="card" style={{ borderStyle: "dashed", textAlign: "center", padding: 56 }}>
          <div style={{ fontSize: 36, marginBottom: 12 }}>▤</div>
          <div style={{ color: "#f0f6fc", fontWeight: 500, marginBottom: 6 }}>Henüz ürün yok</div>
          <div className="text-muted">&quot;Yeni Ürünleri Çek&quot; ile Logo katalogunu içe aktarın.</div>
        </div>
      ) : (<>
        <div className="card" style={{ padding: 0, overflowX: "auto" }}>
          <table className="ptable">
            <thead><tr>
              <th style={{ width: 32 }}>
                <input type="checkbox"
                  style={{ accentColor: "#1d4ed8", cursor: "pointer" }}
                  checked={list.length > 0 && list.every(p => selected.has(p.id))}
                  onChange={toggleAllOnPage} />
              </th>
              <th>Kod</th><th>Ürün Adı</th><th>Grup</th>
              <th style={{ textAlign: "right" }}>Fiyat</th>
              <th style={{ textAlign: "right" }}>Stok</th>
              <th>Görsel</th><th>Durum</th><th>WC ID</th>
              <th style={{ textAlign: "right" }}>İşlem</th>
            </tr></thead>
            <tbody>
              {list.map(p => (
                <tr key={p.id}
                  style={{ background: selected.has(p.id) ? "rgba(37,99,235,.08)" : undefined }}>
                  <td>
                    <input type="checkbox"
                      style={{ accentColor: "#1d4ed8", cursor: "pointer" }}
                      checked={selected.has(p.id)}
                      onChange={() => toggleOne(p.id)} />
                  </td>
                  <td style={{ fontFamily: "monospace", color: "#79c0ff" }}>{p.logoItemCode}</td>
                  <td style={{ maxWidth: 220, overflow: "hidden", textOverflow: "ellipsis",
                    whiteSpace: "nowrap" }} title={p.logoItemName}>{p.logoItemName}</td>
                  <td style={{ color: "#6e7681" }}>{p.logoGroupCode || "—"}</td>
                  <td style={{ textAlign: "right",
                    color: p.logoSellPrice > 0 ? "#c9d1d9" : "#f0883e" }}>
                    {p.logoSellPrice.toLocaleString("tr-TR", { minimumFractionDigits: 2 })} ₺
                  </td>
                  <td style={{ textAlign: "right",
                    color: p.logoStock > 0 ? "#3fb950" : "#f85149" }}>
                    {p.logoStock.toLocaleString("tr-TR")}
                  </td>
                  <td>{p.imageCount > 0
                    ? <span className="img-badge">🖼 {p.imageCount}</span>
                    : <span style={{ color: "#484f58" }}>—</span>}</td>
                  <td>
                    <span className={`st st-${p.status.toLowerCase()}`}>
                      {STATUS_LABELS[p.status] ?? p.status}
                    </span>
                  </td>
                  <td style={{ color: "#6e7681", fontFamily: "monospace" }}>
                    {p.wooProductId ?? "—"}
                  </td>
                  <td style={{ textAlign: "right", whiteSpace: "nowrap" }}>
                    <button className="btn btn-ghost btn-sm" style={{ marginRight: 4 }}
                      title="Aktarım tarihçesi"
                      onClick={() => setHistId({ id: p.id, code: p.logoItemCode })}>
                      ⏱
                    </button>
                    <button className="btn btn-ghost btn-sm" style={{ marginRight: 4 }}
                      onClick={() => setEditId(p.id)}>Düzenle</button>
                    <button className="btn btn-outline btn-sm"
                      disabled={syncId === p.id || p.status === "Draft"}
                      title={p.status === "Draft" ? "Önce Düzenle ile eşleme yapın" : "Gönder"}
                      onClick={() => syncOne(p.id)}>
                      {syncId === p.id ? "..." : "Gönder"}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="pager">
          <button className="pbtn" disabled={page <= 1}
            onClick={() => setPage(p => p - 1)}>← Önceki</button>
          <span className="pinfo">{page} / {pages}</span>
          <button className="pbtn" disabled={page >= pages}
            onClick={() => setPage(p => p + 1)}>Sonraki →</button>
        </div>
      </>)}
  </>);
}
