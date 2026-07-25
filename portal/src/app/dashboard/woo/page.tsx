"use client";
import { useCallback, useEffect, useState } from "react";
import { wooApi, logoApi } from "@/lib/api";
import { DASH_CSS } from "@/lib/dashboardCss";
import { apiError } from "@/lib/errorMessage";
import type { WooStore, LogoConnection, LogoLookupResult, LogoLookupSet } from "@/types/api";

type Form = {
  name: string; storeUrl: string;
  consumerKey: string; consumerSecret: string;
  wpUsername: string; wpAppPassword: string;
  priceProjectCode: string; priceTradingGroupCode: string; priceCostCenterCode: string;
  isActive: boolean;
};

const BLANK: Form = {
  name: "", storeUrl: "", consumerKey: "", consumerSecret: "",
  wpUsername: "", wpAppPassword: "",
  priceProjectCode: "", priceTradingGroupCode: "", priceCostCenterCode: "",
  isActive: true,
};

type TestR = {
  isSuccess: boolean; storeName?: string | null; wooVersion?: string | null;
  responseTimeMs: number; errorMessage?: string | null;
};

/** Liste geldiyse açılır menü, gelmediyse serbest metin */
function CriteriaField({ label, value, onChange, set, placeholder }: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  set: LogoLookupSet | undefined;
  placeholder: string;
}) {
  const items = set?.items ?? [];
  const hasList = items.length > 0;

  return (
    <div style={{ marginBottom: 14 }}>
      <label className="lbl">{label}</label>
      {hasList ? (
        <select className="inp" value={value} onChange={e => onChange(e.target.value)}>
          <option value="">(kriter yok)</option>
          {items.map(x => (
            <option key={x.code} value={x.code}>
              {x.code}{x.name ? ` — ${x.name}` : ""}
            </option>
          ))}
        </select>
      ) : (
        <input className="inp" placeholder={placeholder}
          value={value} onChange={e => onChange(e.target.value)} />
      )}

      {set?.error && (
        <div style={{ fontSize: 10.5, color: "#f0883e", marginTop: 4, lineHeight: 1.45 }}>
          {set.error}
        </div>
      )}
      {!set?.error && hasList && set?.source && (
        <div style={{ fontSize: 10, color: "#484f58", marginTop: 3 }}>
          {items.length} kayıt · {set.source}
        </div>
      )}
    </div>
  );
}

export default function WooPage() {
  const [list, setList] = useState<WooStore[]>([]);
  const [busy, setBusy] = useState(true);

  const [mode, setMode]     = useState<"create" | "edit" | null>(null);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm]     = useState<Form>(BLANK);
  const [saving, setSaving] = useState(false);
  const [formErr, setFormErr] = useState("");

  const [logoConns, setLogoConns]       = useState<LogoConnection[]>([]);
  const [lookups, setLookups]           = useState<LogoLookupResult | null>(null);
  const [lookupBusy, setLookupBusy]     = useState(false);
  const [lookupConnId, setLookupConnId] = useState("");

  const [testStore, setTestStore] = useState<WooStore | null>(null);
  const [tc, setTc]               = useState({ consumerKey: "", consumerSecret: "" });
  const [testingId, setTestingId] = useState<string | null>(null);
  const [results, setResults]     = useState<Record<string, TestR>>({});
  const [toast, setToast]         = useState<{ ok: boolean; text: string } | null>(null);

  const load = useCallback(() => {
    setBusy(true);
    wooApi.list()
      .then(r => setList(r.data.data ?? []))
      .catch(() => {})
      .finally(() => setBusy(false));
  }, []);

  useEffect(() => { load(); }, [load]);

  useEffect(() => {
    logoApi.list()
      .then(r => {
        const lc = r.data.data ?? [];
        setLogoConns(lc);
        if (lc[0]) setLookupConnId(lc[0].id);
      })
      .catch(() => {});
  }, []);

  function showToast(ok: boolean, text: string) {
    setToast({ ok, text });
    setTimeout(() => setToast(null), 6000);
  }

  async function loadLookups() {
    if (!lookupConnId) { setFormErr("Önce Logo bağlantısı tanımlayın."); return; }
    setLookupBusy(true);
    try {
      const r = await logoApi.lookups(lookupConnId);
      setLookups(r.data.data ?? null);
    } catch (e) {
      setFormErr(apiError(e, "Logo listeleri alınamadı."));
    } finally { setLookupBusy(false); }
  }

  function openCreate() {
    setForm(BLANK); setFormErr(""); setEditId(null); setMode("create");
    if (!lookups && lookupConnId) loadLookups();
  }

  function openEdit(s: WooStore) {
    setForm({
      name: s.name, storeUrl: s.storeUrl,
      consumerKey: "", consumerSecret: "",
      wpUsername: s.wpUsername ?? "", wpAppPassword: "",
      priceProjectCode: s.priceProjectCode ?? "",
      priceTradingGroupCode: s.priceTradingGroupCode ?? "",
      priceCostCenterCode: s.priceCostCenterCode ?? "",
      isActive: s.isActive,
    });
    setFormErr(""); setEditId(s.id); setMode("edit");
    if (!lookups && lookupConnId) loadLookups();
  }

  async function save(ev: React.FormEvent) {
    ev.preventDefault();
    setFormErr(""); setSaving(true);
    try {
      if (mode === "create") {
        await wooApi.create({
          name: form.name,
          storeUrl: form.storeUrl,
          consumerKey: form.consumerKey,
          consumerSecret: form.consumerSecret,
          wpUsername: form.wpUsername,
          wpAppPassword: form.wpAppPassword,
          priceProjectCode: form.priceProjectCode,
          priceTradingGroupCode: form.priceTradingGroupCode,
          priceCostCenterCode: form.priceCostCenterCode,
        });
        showToast(true, "Mağaza oluşturuldu.");
      } else {
        await wooApi.update(editId!, {
          name: form.name,
          storeUrl: form.storeUrl,
          consumerKey: form.consumerKey || undefined,
          consumerSecret: form.consumerSecret || undefined,
          isActive: form.isActive,
          wpUsername: form.wpUsername,
          wpAppPassword: form.wpAppPassword || undefined,
          priceProjectCode: form.priceProjectCode,
          priceTradingGroupCode: form.priceTradingGroupCode,
          priceCostCenterCode: form.priceCostCenterCode,
        });
        showToast(true, "Mağaza güncellendi.");
      }
      setMode(null); load();
    } catch (e) {
      setFormErr(apiError(e, "Kaydetme başarısız."));
    } finally { setSaving(false); }
  }

  async function doTest() {
    if (!testStore) return;
    const id = testStore.id;
    setTestingId(id); setTestStore(null);
    try {
      const r = await wooApi.test({
        storeUrl: testStore.storeUrl,
        consumerKey: tc.consumerKey,
        consumerSecret: tc.consumerSecret,
      });
      setResults(p => ({ ...p, [id]: r.data.data! }));
    } catch (e) {
      setResults(p => ({ ...p, [id]: {
        isSuccess: false, responseTimeMs: 0, errorMessage: apiError(e),
      }}));
    } finally { setTestingId(null); }
  }

  async function del(id: string) {
    if (!confirm("Bu mağazayı silmek istediğinize emin misiniz?")) return;
    try { await wooApi.delete(id); load(); } catch { /* noop */ }
  }

  const F = (k: keyof Form, v: string | boolean) => setForm(p => ({ ...p, [k]: v }));

  return (<>
    <style>{DASH_CSS}</style>

    {toast && (
      <div style={{
        position: "fixed", top: 20, right: 20, zIndex: 300, maxWidth: 380,
        background: toast.ok ? "rgba(35,134,54,.18)" : "rgba(248,81,73,.18)",
        border: `1px solid ${toast.ok ? "rgba(35,134,54,.45)" : "rgba(248,81,73,.45)"}`,
        borderRadius: 10, padding: "12px 16px", fontSize: 13,
        color: toast.ok ? "#3fb950" : "#ff9b95",
        boxShadow: "0 10px 30px rgba(0,0,0,.5)",
      }}>{toast.text}</div>
    )}

    <div className="ph">
      <div>
        <div className="ph-title">WooCommerce Mağazaları</div>
        <div className="ph-sub">Mağaza bağlantıları ve fiyat seçim kriterleri</div>
      </div>
      <button className="btn btn-purple" onClick={openCreate}>+ Yeni Mağaza</button>
    </div>

    {mode && (
      <div className="overlay" onClick={e => e.target === e.currentTarget && setMode(null)}>
        <div className="modal" style={{ maxWidth: 620 }}>
          <div className="mhead">
            <span className="mtitle">
              {mode === "create" ? "Yeni WooCommerce Mağazası" : "Mağazayı Düzenle"}
            </span>
            <button className="mclose" onClick={() => setMode(null)}>✕</button>
          </div>
          <form onSubmit={save} className="mbody">
            {formErr && <div className="alert-err">{formErr}</div>}

            <div style={{ marginBottom: 14 }}>
              <label className="lbl">Mağaza Adı *</label>
              <input className="inp" required value={form.name}
                onChange={e => F("name", e.target.value)} />
            </div>
            <div style={{ marginBottom: 14 }}>
              <label className="lbl">Store URL *</label>
              <input className="inp" required placeholder="https://magaza.com"
                value={form.storeUrl} onChange={e => F("storeUrl", e.target.value)} />
            </div>
            <div className="g2">
              <div style={{ marginBottom: 14 }}>
                <label className="lbl">
                  Consumer Key {mode === "edit" ? "(boş = değişmez)" : "*"}
                </label>
                <input className="inp" placeholder="ck_xxxxxxxx"
                  required={mode === "create"} value={form.consumerKey}
                  onChange={e => F("consumerKey", e.target.value)} />
              </div>
              <div style={{ marginBottom: 14 }}>
                <label className="lbl">
                  Consumer Secret {mode === "edit" ? "(boş = değişmez)" : "*"}
                </label>
                <input className="inp" type="password" placeholder="cs_xxxxxxxx"
                  required={mode === "create"} value={form.consumerSecret}
                  onChange={e => F("consumerSecret", e.target.value)} />
              </div>
            </div>

            {/* WordPress medya */}
            <div style={{ marginTop: 6, paddingTop: 16, borderTop: "1px solid #21262d" }}>
              <div style={{ background: "rgba(56,139,253,.07)",
                border: "1px solid rgba(56,139,253,.25)", borderRadius: 8,
                padding: "10px 13px", fontSize: 11.5, lineHeight: 1.6,
                color: "#8b949e", marginBottom: 14 }}>
                <strong style={{ color: "#79c0ff" }}>Ürün görselleri için gerekli</strong><br />
                WooCommerce anahtarları görsel yüklemeye izin vermez. WordPress
                Application Password gerekir:{" "}
                <span style={{ color: "#c9d1d9" }}>
                  WP Admin → Kullanıcılar → Profil → Application Passwords
                </span>
              </div>
              <div className="g2">
                <div style={{ marginBottom: 14 }}>
                  <label className="lbl">WordPress Kullanıcı Adı</label>
                  <input className="inp" placeholder="admin" value={form.wpUsername}
                    onChange={e => F("wpUsername", e.target.value)} />
                </div>
                <div style={{ marginBottom: 14 }}>
                  <label className="lbl">
                    Application Password {mode === "edit" ? "(boş = değişmez)" : ""}
                  </label>
                  <input className="inp" type="password" placeholder="xxxx xxxx xxxx xxxx"
                    value={form.wpAppPassword}
                    onChange={e => F("wpAppPassword", e.target.value)} />
                </div>
              </div>
            </div>

            {/* Fiyat kriterleri */}
            <div style={{ marginTop: 6, paddingTop: 16, borderTop: "1px solid #21262d" }}>
              <div style={{ background: "rgba(163,113,247,.07)",
                border: "1px solid rgba(163,113,247,.25)", borderRadius: 8,
                padding: "10px 13px", fontSize: 11.5, lineHeight: 1.6,
                color: "#8b949e", marginBottom: 14 }}>
                <strong style={{ color: "#a371f7" }}>Fiyat seçim kriterleri</strong><br />
                Bir malzemenin birden fazla satış fiyat kartı olabilir. Bu alanlar
                doluysa fiyat kartındaki proje / ticari işlem grubu / masraf merkezi
                bilgisine göre bu mağazaya uygun fiyat seçilir. Boş alan kriter sayılmaz.
              </div>

              <div style={{ display: "flex", gap: 8, alignItems: "flex-end", marginBottom: 12 }}>
                <div style={{ flex: 1 }}>
                  <label className="lbl">Listeler hangi Logo bağlantısından gelsin?</label>
                  <select className="inp" value={lookupConnId}
                    onChange={e => { setLookupConnId(e.target.value); setLookups(null); }}>
                    {logoConns.length === 0 && <option value="">Logo bağlantısı yok</option>}
                    {logoConns.map(c => (
                      <option key={c.id} value={c.id}>{c.name} — Firma {c.firmNo}</option>
                    ))}
                  </select>
                </div>
                <button type="button" className="btn btn-ghost btn-sm"
                  disabled={lookupBusy || !lookupConnId} onClick={loadLookups}>
                  {lookupBusy ? "Yükleniyor..." : lookups ? "Yenile" : "Listeleri Getir"}
                </button>
              </div>

              <div className="g2">
                <CriteriaField label="Proje Kodu"
                  value={form.priceProjectCode}
                  onChange={v => F("priceProjectCode", v)}
                  set={lookups?.projects}
                  placeholder="Proje kodu" />

                <CriteriaField label="Ticari İşlem Grubu"
                  value={form.priceTradingGroupCode}
                  onChange={v => F("priceTradingGroupCode", v)}
                  set={lookups?.tradingGroups}
                  placeholder="TİG kodu" />

                <CriteriaField label="Masraf Merkezi"
                  value={form.priceCostCenterCode}
                  onChange={v => F("priceCostCenterCode", v)}
                  set={lookups?.costCenters}
                  placeholder="Masraf merkezi kodu" />
              </div>
            </div>

            {mode === "edit" && (
              <label className="chk-row" style={{ marginTop: 10 }}>
                <input type="checkbox" checked={form.isActive}
                  onChange={e => F("isActive", e.target.checked)} />
                <span>Aktif</span>
              </label>
            )}

            <div className="mfooter">
              <button type="button" className="btn btn-ghost" style={{ flex: 1 }}
                onClick={() => setMode(null)}>İptal</button>
              <button type="submit" className="btn btn-purple" style={{ flex: 1 }}
                disabled={saving}>
                {saving ? "Kaydediliyor..." : mode === "create" ? "Kaydet" : "Güncelle"}
              </button>
            </div>
          </form>
        </div>
      </div>
    )}

    {testStore && (
      <div className="overlay" onClick={e => e.target === e.currentTarget && setTestStore(null)}>
        <div className="modal msm">
          <div className="mhead">
            <span className="mtitle">Mağaza Bağlantı Testi</span>
            <button className="mclose" onClick={() => setTestStore(null)}>✕</button>
          </div>
          <div className="mbody">
            <div style={{ fontSize: 12, color: "#6e7681", marginBottom: 16, lineHeight: 1.6 }}>
              <strong style={{ color: "#f0f6fc" }}>{testStore.name}</strong><br />
              {testStore.storeUrl}
            </div>
            <div style={{ marginBottom: 12 }}>
              <label className="lbl">Consumer Key *</label>
              <input className="inp" placeholder="ck_xxxxxxxx" value={tc.consumerKey}
                onChange={e => setTc(p => ({ ...p, consumerKey: e.target.value }))} />
            </div>
            <div style={{ marginBottom: 16 }}>
              <label className="lbl">Consumer Secret *</label>
              <input className="inp" type="password" placeholder="cs_xxxxxxxx"
                value={tc.consumerSecret}
                onChange={e => setTc(p => ({ ...p, consumerSecret: e.target.value }))} />
            </div>
            <div className="mfooter">
              <button className="btn btn-ghost" style={{ flex: 1 }}
                onClick={() => setTestStore(null)}>İptal</button>
              <button className="btn btn-purple" style={{ flex: 1 }} onClick={doTest}>
                Test Et
              </button>
            </div>
          </div>
        </div>
      </div>
    )}

    {busy ? <div className="text-muted">Yükleniyor...</div>
      : list.length === 0 ? (
        <div className="card" style={{ borderStyle: "dashed", textAlign: "center", padding: 56 }}>
          <div style={{ fontSize: 36, marginBottom: 12 }}>⬡</div>
          <div style={{ color: "#f0f6fc", fontWeight: 500, marginBottom: 6 }}>Mağaza yok</div>
          <div className="text-muted">İlk WooCommerce mağazanızı ekleyin.</div>
        </div>
      ) : list.map(s => {
        const tr = results[s.id];
        return (
          <div key={s.id} className="card">
            <div className="row-b">
              <div style={{ flex: 1, minWidth: 0 }}>
                <div className="row" style={{ marginBottom: 8 }}>
                  <span style={{ fontSize: 14, fontWeight: 500, color: "#f0f6fc" }}>{s.name}</span>
                  <span className={`badge ${s.isActive ? "bg" : "bd"}`}>
                    {s.isActive ? "Aktif" : "Pasif"}
                  </span>
                  {s.hasWpCredentials
                    ? <span className="badge bb">🖼 Görsel aktif</span>
                    : <span className="badge bd">Görsel kapalı</span>}
                </div>
                <div className="mono" style={{ color: "#6e7681", marginBottom: 6 }}>
                  {s.storeUrl}
                </div>
                <div className="meta">
                  <span>API: {s.apiVersion}</span>
                  {s.wpUsername && <span>WP: {s.wpUsername}</span>}
                  {s.priceProjectCode && <span>Proje: {s.priceProjectCode}</span>}
                  {s.priceTradingGroupCode && <span>TİG: {s.priceTradingGroupCode}</span>}
                  {s.priceCostCenterCode && <span>MM: {s.priceCostCenterCode}</span>}
                </div>
              </div>
              <div className="btns">
                <button className="btn btn-outline btn-sm" disabled={testingId === s.id}
                  onClick={() => { setTestStore(s); setTc({ consumerKey: "", consumerSecret: "" }); }}>
                  {testingId === s.id ? "Test..." : "Test Et"}
                </button>
                <button className="btn btn-ghost btn-sm" onClick={() => openEdit(s)}>Düzenle</button>
                <button className="btn btn-danger btn-sm" onClick={() => del(s.id)}>Sil</button>
              </div>
            </div>
            {tr && (
              <div className={tr.isSuccess ? "rok" : "rerr"}>
                {tr.isSuccess ? (<>
                  <div className="rokt">✓ Bağlantı başarılı</div>
                  <div className="rsub">
                    {tr.storeName && `${tr.storeName} · `}
                    WooCommerce {tr.wooVersion} · {tr.responseTimeMs}ms
                  </div>
                </>) : (<>
                  <div className="rerrt">✗ Bağlantı başarısız</div>
                  <div className="rsub">{tr.errorMessage}</div>
                </>)}
              </div>
            )}
          </div>
        );
      })}
  </>);
}
