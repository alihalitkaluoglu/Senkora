"use client";
import { useEffect, useState } from "react";
import { wooApi } from "@/lib/api";
import { DASH_CSS } from "@/lib/dashboardCss";
import { apiError } from "@/lib/errorMessage";
import type { WooStore } from "@/types/api";

const BLANK = {
  name: "", storeUrl: "", consumerKey: "", consumerSecret: "",
  wpUsername: "", wpAppPassword: "",
};

type TestR = {
  isSuccess: boolean; storeName?: string | null; wooVersion?: string | null;
  responseTimeMs: number; errorMessage?: string | null;
};

type EF = {
  name: string; storeUrl: string; consumerKey: string; consumerSecret: string;
  isActive: boolean; wpUsername: string; wpAppPassword: string;
};

export default function WooPage() {
  const [list, setList]   = useState<WooStore[]>([]);
  const [busy, setBusy]   = useState(true);
  const [showC, setShowC] = useState(false);
  const [form, setForm]   = useState(BLANK);
  const [cSav, setCSav]   = useState(false);
  const [cErr, setCErr]   = useState("");

  const [editStore, setEditStore] = useState<WooStore | null>(null);
  const [ef, setEf] = useState<EF>({
    name: "", storeUrl: "", consumerKey: "", consumerSecret: "",
    isActive: true, wpUsername: "", wpAppPassword: "",
  });
  const [eSav, setESav] = useState(false);
  const [eErr, setEErr] = useState("");

  const [testStore, setTestStore] = useState<WooStore | null>(null);
  const [tc, setTc] = useState({ consumerKey: "", consumerSecret: "" });
  const [testingId, setTestingId] = useState<string | null>(null);
  const [results, setResults] = useState<Record<string, TestR>>({});

  const load = () => {
    setBusy(true);
    wooApi.list()
      .then(r => setList(r.data.data ?? []))
      .catch(() => {})
      .finally(() => setBusy(false));
  };
  useEffect(() => { load(); }, []);

  async function doCreate(ev: React.FormEvent) {
    ev.preventDefault(); setCErr(""); setCSav(true);
    try {
      await wooApi.create({
        name: form.name, storeUrl: form.storeUrl,
        consumerKey: form.consumerKey, consumerSecret: form.consumerSecret,
        wpUsername: form.wpUsername || undefined,
        wpAppPassword: form.wpAppPassword || undefined,
      });
      setShowC(false); setForm(BLANK); load();
    } catch (e) { setCErr(apiError(e)); }
    finally { setCSav(false); }
  }

  function openEdit(s: WooStore) {
    setEf({
      name: s.name, storeUrl: s.storeUrl,
      consumerKey: "", consumerSecret: "",
      isActive: s.isActive,
      wpUsername: s.wpUsername ?? "", wpAppPassword: "",
    });
    setEErr(""); setEditStore(s);
  }

  async function doEdit(ev: React.FormEvent) {
    ev.preventDefault(); setEErr(""); setESav(true);
    try {
      await wooApi.update(editStore!.id, {
        name: ef.name, storeUrl: ef.storeUrl,
        consumerKey: ef.consumerKey || undefined,
        consumerSecret: ef.consumerSecret || undefined,
        isActive: ef.isActive,
        wpUsername: ef.wpUsername || undefined,
        wpAppPassword: ef.wpAppPassword || undefined,
      });
      setEditStore(null); load();
    } catch (e) { setEErr(apiError(e, "Güncelleme başarısız.")); }
    finally { setESav(false); }
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

  async function doDelete(id: string) {
    if (!confirm("Bu mağazayı silmek istediğinize emin misiniz?")) return;
    try { await wooApi.delete(id); load(); } catch { /* noop */ }
  }

  const WP_HELP = (
    <div style={{ background: "rgba(56,139,253,.07)", border: "1px solid rgba(56,139,253,.25)",
      borderRadius: 8, padding: "10px 13px", fontSize: 11.5, lineHeight: 1.6,
      color: "#8b949e", marginBottom: 14 }}>
      <strong style={{ color: "#79c0ff" }}>Ürün görselleri için gerekli</strong><br />
      WooCommerce anahtarları görsel yüklemeye izin vermez. Görsellerin
      WordPress medya kütüphanesine aktarılması için Application Password gerekir:<br />
      <span style={{ color: "#c9d1d9" }}>
        WP Admin → Kullanıcılar → Profil → Application Passwords → Yeni oluştur
      </span>
    </div>
  );

  return (<>
    <style>{DASH_CSS}</style>

    <div className="ph">
      <div>
        <div className="ph-title">WooCommerce Mağazaları</div>
        <div className="ph-sub">Mağaza bağlantılarını tanımlayın ve yönetin</div>
      </div>
      <button className="btn btn-purple" onClick={() => { setShowC(true); setCErr(""); }}>
        + Yeni Mağaza
      </button>
    </div>

    {/* CREATE */}
    {showC && (
      <div className="overlay" onClick={e => e.target === e.currentTarget && setShowC(false)}>
        <div className="modal" style={{ maxWidth: 520 }}>
          <div className="mhead">
            <span className="mtitle">Yeni WooCommerce Mağazası</span>
            <button className="mclose" onClick={() => setShowC(false)}>✕</button>
          </div>
          <form onSubmit={doCreate} className="mbody">
            {cErr && <div className="alert-err">{cErr}</div>}
            {[
              { l: "Mağaza Adı", k: "name", t: "text", p: "Altın Mağazam", req: true },
              { l: "Store URL", k: "storeUrl", t: "text", p: "https://magaza.com", req: true },
              { l: "Consumer Key", k: "consumerKey", t: "text", p: "ck_xxxxxxxx", req: true },
              { l: "Consumer Secret", k: "consumerSecret", t: "password", p: "cs_xxxxxxxx", req: true },
            ].map(({ l, k, t, p, req }) => (
              <div key={k} style={{ marginBottom: 14 }}>
                <label className="lbl">{l}{req ? " *" : ""}</label>
                <input className="inp" type={t} required={req} placeholder={p}
                  value={(form as Record<string, string>)[k]}
                  onChange={e => setForm(f => ({ ...f, [k]: e.target.value }))} />
              </div>
            ))}

            <div style={{ marginTop: 20, paddingTop: 16, borderTop: "1px solid #21262d" }}>
              {WP_HELP}
              <div className="g2">
                <div>
                  <label className="lbl">WordPress Kullanıcı Adı</label>
                  <input className="inp" placeholder="admin" value={form.wpUsername}
                    onChange={e => setForm(f => ({ ...f, wpUsername: e.target.value }))} />
                </div>
                <div>
                  <label className="lbl">Application Password</label>
                  <input className="inp" type="password" placeholder="xxxx xxxx xxxx xxxx"
                    value={form.wpAppPassword}
                    onChange={e => setForm(f => ({ ...f, wpAppPassword: e.target.value }))} />
                </div>
              </div>
            </div>

            <div className="mfooter">
              <button type="button" className="btn btn-ghost" style={{ flex: 1 }}
                onClick={() => setShowC(false)}>İptal</button>
              <button type="submit" className="btn btn-purple" style={{ flex: 1 }} disabled={cSav}>
                {cSav ? "Kaydediliyor..." : "Kaydet"}
              </button>
            </div>
          </form>
        </div>
      </div>
    )}

    {/* EDIT */}
    {editStore && (
      <div className="overlay" onClick={e => e.target === e.currentTarget && setEditStore(null)}>
        <div className="modal" style={{ maxWidth: 520 }}>
          <div className="mhead">
            <span className="mtitle">Mağazayı Düzenle — {editStore.name}</span>
            <button className="mclose" onClick={() => setEditStore(null)}>✕</button>
          </div>
          <form onSubmit={doEdit} className="mbody">
            {eErr && <div className="alert-err">{eErr}</div>}
            <div style={{ marginBottom: 14 }}>
              <label className="lbl">Mağaza Adı *</label>
              <input className="inp" required value={ef.name}
                onChange={e => setEf(p => ({ ...p, name: e.target.value }))} />
            </div>
            <div style={{ marginBottom: 14 }}>
              <label className="lbl">Store URL *</label>
              <input className="inp" required value={ef.storeUrl}
                onChange={e => setEf(p => ({ ...p, storeUrl: e.target.value }))} />
            </div>
            <div className="g2">
              <div style={{ marginBottom: 14 }}>
                <label className="lbl">Consumer Key (boş = değişmez)</label>
                <input className="inp" placeholder="ck_xxxxxxxx" value={ef.consumerKey}
                  onChange={e => setEf(p => ({ ...p, consumerKey: e.target.value }))} />
              </div>
              <div style={{ marginBottom: 14 }}>
                <label className="lbl">Consumer Secret (boş = değişmez)</label>
                <input className="inp" type="password" placeholder="cs_xxxxxxxx"
                  value={ef.consumerSecret}
                  onChange={e => setEf(p => ({ ...p, consumerSecret: e.target.value }))} />
              </div>
            </div>

            <div style={{ marginTop: 6, paddingTop: 16, borderTop: "1px solid #21262d" }}>
              {WP_HELP}
              {editStore.hasWpCredentials && (
                <div style={{ fontSize: 11, color: "#3fb950", marginBottom: 10 }}>
                  ✓ WordPress kimlik bilgileri tanımlı — görsel yükleme aktif
                </div>
              )}
              <div className="g2">
                <div>
                  <label className="lbl">WordPress Kullanıcı Adı</label>
                  <input className="inp" placeholder="admin" value={ef.wpUsername}
                    onChange={e => setEf(p => ({ ...p, wpUsername: e.target.value }))} />
                </div>
                <div>
                  <label className="lbl">Application Password (boş = değişmez)</label>
                  <input className="inp" type="password" placeholder="xxxx xxxx xxxx xxxx"
                    value={ef.wpAppPassword}
                    onChange={e => setEf(p => ({ ...p, wpAppPassword: e.target.value }))} />
                </div>
              </div>
            </div>

            <label className="chk-row" style={{ marginTop: 16 }}>
              <input type="checkbox" checked={ef.isActive}
                onChange={e => setEf(p => ({ ...p, isActive: e.target.checked }))} />
              <span>Aktif</span>
            </label>

            <div className="mfooter">
              <button type="button" className="btn btn-ghost" style={{ flex: 1 }}
                onClick={() => setEditStore(null)}>İptal</button>
              <button type="submit" className="btn btn-purple" style={{ flex: 1 }} disabled={eSav}>
                {eSav ? "Kaydediliyor..." : "Güncelle"}
              </button>
            </div>
          </form>
        </div>
      </div>
    )}

    {/* TEST */}
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
                  {s.isVerified && <span className="badge bb">Doğrulandı</span>}
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
                  {s.lastSyncAt && (
                    <span>Son sync: {new Date(s.lastSyncAt).toLocaleString("tr-TR")}</span>
                  )}
                </div>
              </div>
              <div className="btns">
                <button className="btn btn-outline btn-sm" disabled={testingId === s.id}
                  onClick={() => { setTestStore(s); setTc({ consumerKey: "", consumerSecret: "" }); }}>
                  {testingId === s.id ? "Test..." : "Test Et"}
                </button>
                <button className="btn btn-ghost btn-sm" onClick={() => openEdit(s)}>
                  Düzenle
                </button>
                <button className="btn btn-danger btn-sm" onClick={() => doDelete(s.id)}>
                  Sil
                </button>
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
