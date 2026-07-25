"use client";
import { useEffect, useRef, useState } from "react";
import { productApi } from "@/lib/api";
import { apiError } from "@/lib/errorMessage";
import type {
  ProductEnrichment, ProductEnrichmentDetail, WooCategory, ProductAttribute,
} from "@/types/api";

const API_BASE = "http://localhost:5000";

const EMPTY: ProductEnrichment = {
  images: [], wooCategoryIds: [], tags: [], attributes: [],
  dimensions: null, shippingClass: null, catalogVisibility: "visible",
  featured: false, overrideName: null, overrideShortDesc: null,
  overrideDescription: null, overrideSlug: null, customMeta: [],
  manageStock: true, backorderPolicy: "no",
  regularPriceOverride: null, salePriceOverride: null, saleFrom: null, saleTo: null,
};

type Props = {
  mappingId: string;
  wooStoreId: string;
  onClose: () => void;
  onSaved: () => void;
};

export default function ProductEditor({ mappingId, wooStoreId, onClose, onSaved }: Props) {
  const [detail, setDetail]   = useState<ProductEnrichmentDetail | null>(null);
  const [e, setE]             = useState<ProductEnrichment>(EMPTY);
  const [cats, setCats]       = useState<WooCategory[]>([]);
  const [catsBusy, setCatsBusy] = useState(false);
  const [busy, setBusy]       = useState(true);
  const [saving, setSaving]   = useState(false);
  const [err, setErr]         = useState("");
  const [tab, setTab]         = useState<"general" | "images" | "cats" | "attrs" | "advanced">("general");
  const [uploading, setUploading] = useState(false);
  const [tagInput, setTagInput]   = useState("");
  const fileRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    productApi.getEnrichment(mappingId)
      .then(r => {
        const d = r.data.data!;
        setDetail(d);
        setE({ ...EMPTY, ...d.enrichment });
      })
      .catch(e => setErr(apiError(e, "Ürün bilgileri yüklenemedi.")))
      .finally(() => setBusy(false));
  }, [mappingId]);

  useEffect(() => {
    if (tab !== "cats" || cats.length > 0 || !wooStoreId) return;
    setCatsBusy(true);
    productApi.wooCategories(wooStoreId)
      .then(r => setCats(r.data.data ?? []))
      .catch(e => setErr(apiError(e, "WooCommerce kategorileri alınamadı.")))
      .finally(() => setCatsBusy(false));
  }, [tab, cats.length, wooStoreId]);

  const U = <K extends keyof ProductEnrichment>(k: K, v: ProductEnrichment[K]) =>
    setE(p => ({ ...p, [k]: v }));

  async function handleUpload(files: FileList | null) {
    if (!files || files.length === 0) return;
    setUploading(true); setErr("");
    try {
      for (const f of Array.from(files)) {
        await productApi.uploadImage(mappingId, f);
      }
      const r = await productApi.getEnrichment(mappingId);
      setE({ ...EMPTY, ...r.data.data!.enrichment });
    } catch (error) {
      setErr(apiError(error, "Görsel yüklenemedi. Maks 10MB, JPG/PNG/WebP."));
    } finally {
      setUploading(false);
      if (fileRef.current) fileRef.current.value = "";
    }
  }

  function setFeatured(idx: number) {
    U("images", e.images.map((img, i) => ({ ...img, isFeatured: i === idx })));
  }

  function removeImage(idx: number) {
    U("images", e.images.filter((_, i) => i !== idx)
      .map((img, i) => ({ ...img, sortOrder: i })));
  }

  function addTag() {
    const t = tagInput.trim();
    if (!t || e.tags.includes(t)) return;
    U("tags", [...e.tags, t]);
    setTagInput("");
  }

  function addAttr() {
    U("attributes", [...e.attributes,
      { name: "", options: [], visible: true, variation: false }]);
  }

  function updAttr(i: number, patch: Partial<ProductAttribute>) {
    U("attributes", e.attributes.map((a, idx) => idx === i ? { ...a, ...patch } : a));
  }

  async function save() {
    setSaving(true); setErr("");
    try {
      await productApi.saveEnrichment(mappingId, e);
      onSaved();
    } catch (error) {
      setErr(apiError(error, "Kaydetme başarısız."));
    } finally { setSaving(false); }
  }

  const TABS: [typeof tab, string][] = [
    ["general", "Genel"], ["images", `Görseller (${e.images.length})`],
    ["cats", `Kategoriler (${e.wooCategoryIds.length})`],
    ["attrs", `Özellikler (${e.attributes.length})`], ["advanced", "Gelişmiş"],
  ];

  return (
    <div className="overlay" onClick={ev => ev.target === ev.currentTarget && onClose()}>
      <div className="modal" style={{ maxWidth: 760 }}>
        <div className="mhead">
          <span className="mtitle">
            Ürün Zenginleştirme
            {detail && <span style={{ color: "#6e7681", fontWeight: 400 }}>
              {" "}— {detail.logoItemCode}
            </span>}
          </span>
          <button className="mclose" onClick={onClose}>✕</button>
        </div>

        <div className="mbody">
          {busy ? <div className="text-muted">Yükleniyor...</div> : !detail ? (
            <div className="alert-err">{err || "Ürün bulunamadı."}</div>
          ) : (<>
            {err && <div className="alert-err">{err}</div>}

            {/* Logo bilgi kutusu */}
            <div className="info-box">
              <div className="info-row">
                <span>Logo Kodu</span><strong>{detail.logoItemCode}</strong>
              </div>
              <div className="info-row">
                <span>Logo Adı</span><strong>{detail.logoItemName}</strong>
              </div>
              <div className="info-row">
                <span>Logo Fiyat / KDV</span>
                <strong style={{ color: detail.logoSellPrice === 0 ? "#f0883e" : "#c9d1d9" }}>
                  {detail.logoSellPrice > 0
                    ? `${detail.logoSellPrice.toLocaleString("tr-TR", { minimumFractionDigits: 2 })} ₺`
                    : "Fiyat kartında tanımlı"}
                  {" · "}%{detail.logoVatRate}
                </strong>
              </div>
              <div className="info-row">
                <span>Stok / Ağırlık</span>
                <strong>{detail.logoStock} · {detail.logoWeight} kg</strong>
              </div>
              {detail.logoGroupCode && (
                <div className="info-row">
                  <span>Logo Grup Kodu</span><strong>{detail.logoGroupCode}</strong>
                </div>
              )}
            </div>

            {/* Tabs */}
            <div className="tabs">
              {TABS.map(([k, label]) => (
                <div key={k} className={`tab${tab === k ? " on" : ""}`}
                  onClick={() => setTab(k)}>{label}</div>
              ))}
            </div>

            {/* GENERAL */}
            {tab === "general" && (<>
              <div style={{ marginBottom: 14 }}>
                <label className="lbl">Ürün Adı (boş = Logo adı kullanılır)</label>
                <input className="inp" placeholder={detail.logoItemName}
                  value={e.overrideName ?? ""}
                  onChange={ev => U("overrideName", ev.target.value || null)} />
              </div>
              <div style={{ marginBottom: 14 }}>
                <label className="lbl">Kısa Açıklama</label>
                <textarea className="inp" rows={2}
                  placeholder={detail.logoAuxDesc ?? "WooCommerce kısa açıklama"}
                  value={e.overrideShortDesc ?? ""}
                  onChange={ev => U("overrideShortDesc", ev.target.value || null)} />
              </div>
              <div style={{ marginBottom: 14 }}>
                <label className="lbl">Uzun Açıklama</label>
                <textarea className="inp" rows={5}
                  placeholder={detail.logoDescription ?? "Ürün detay açıklaması (HTML destekler)"}
                  value={e.overrideDescription ?? ""}
                  onChange={ev => U("overrideDescription", ev.target.value || null)} />
              </div>
              <div className="g2">
                <div>
                  <label className="lbl">
                    Satış Fiyatı (₺)
                    {detail.logoSellPrice === 0 && (
                      <span style={{ color: "#f0883e" }}> — Logo&apos;dan gelmedi, girin</span>
                    )}
                  </label>
                  <input className="inp" type="number" step="0.01"
                    placeholder={detail.logoSellPrice > 0
                      ? `Logo: ${detail.logoSellPrice.toFixed(2)} ₺`
                      : "0.00"}
                    value={e.regularPriceOverride ?? ""}
                    onChange={ev => U("regularPriceOverride",
                      ev.target.value ? +ev.target.value : null)} />
                </div>
                <div>
                  <label className="lbl">İndirimli Fiyat (₺)</label>
                  <input className="inp" type="number" step="0.01" placeholder="Boş = indirim yok"
                    value={e.salePriceOverride ?? ""}
                    onChange={ev => U("salePriceOverride",
                      ev.target.value ? +ev.target.value : null)} />
                </div>
                <div>
                  <label className="lbl">Kargo Sınıfı (slug)</label>
                  <input className="inp" placeholder="ör: agir-kargo"
                    value={e.shippingClass ?? ""}
                    onChange={ev => U("shippingClass", ev.target.value || null)} />
                </div>
              </div>
              <div style={{ display: "flex", gap: 20, marginTop: 8 }}>
                <label className="chk-row">
                  <input type="checkbox" checked={e.featured}
                    onChange={ev => U("featured", ev.target.checked)} />
                  <span>Öne çıkan ürün</span>
                </label>
                <label className="chk-row">
                  <input type="checkbox" checked={e.manageStock}
                    onChange={ev => U("manageStock", ev.target.checked)} />
                  <span>Stok takibi yapılsın</span>
                </label>
              </div>
            </>)}

            {/* IMAGES */}
            {tab === "images" && (<>
              <div style={{ fontSize: 12, color: "#6e7681", marginBottom: 12 }}>
                Yıldıza (★) basarak kapak resmini seçin — WooCommerce&apos;de ürünün
                küçük resmi olarak kullanılır. Maks. 10MB · JPG, PNG, WebP
              </div>
              <div className="imgs-grid">
                {e.images.map((img, i) => (
                  <div key={i} className="img-card"
                    style={{
                      borderColor: img.isFeatured ? "#f0883e" : "#30363d",
                      borderWidth: img.isFeatured ? 2 : 1,
                    }}>
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img src={`${API_BASE}/uploads/${img.storedPath}`} alt={img.alt ?? ""} />
                    <button className="img-star"
                      title={img.isFeatured ? "Kapak resmi" : "Kapak resmi yap"}
                      onClick={() => setFeatured(i)}
                      style={{
                        color: img.isFeatured ? "#f0883e" : "#6e7681",
                        background: img.isFeatured ? "rgba(240,136,62,.25)" : "rgba(0,0,0,.7)",
                      }}>★</button>
                    <button className="img-del" title="Sil"
                      onClick={() => removeImage(i)}>✕</button>
                    {img.isFeatured && (
                      <div style={{
                        position: "absolute", bottom: 0, left: 0, right: 0,
                        background: "rgba(240,136,62,.9)", color: "#0d1117",
                        fontSize: 9.5, fontWeight: 700, textAlign: "center",
                        padding: "2px 0", letterSpacing: .3,
                      }}>KAPAK</div>
                    )}
                    {img.remoteUrl && (
                      <div style={{
                        position: "absolute", top: 4, left: "50%",
                        transform: "translateX(-50%)",
                        background: "rgba(35,134,54,.9)", color: "white",
                        fontSize: 9, borderRadius: 3, padding: "1px 5px",
                      }}>✓ WP</div>
                    )}
                  </div>
                ))}
                <div className="img-drop" onClick={() => fileRef.current?.click()}>
                  {uploading ? <div className="spin" /> : <>
                    <span style={{ fontSize: 22 }}>+</span>
                    <span>Görsel Ekle</span>
                  </>}
                </div>
              </div>
              <input ref={fileRef} type="file" accept="image/*" multiple
                style={{ display: "none" }}
                onChange={ev => handleUpload(ev.target.files)} />
            </>)}

            {/* CATEGORIES */}
            {tab === "cats" && (<>
              <div style={{ fontSize: 12, color: "#6e7681", marginBottom: 12 }}>
                Bu ürünün WooCommerce&apos;de görüneceği kategorileri seçin.
              </div>
              {catsBusy ? <div className="text-muted">Kategoriler yükleniyor...</div>
                : cats.length === 0 ? (
                  <div className="card" style={{ borderStyle: "dashed", textAlign: "center", padding: 32 }}>
                    <div className="text-muted">
                      Kategori bulunamadı. WooCommerce mağaza bağlantısını kontrol edin.
                    </div>
                  </div>
                ) : (
                  <div className="cat-list">
                    {cats.map(c => (
                      <label key={c.id} className="cat-item">
                        <input type="checkbox"
                          checked={e.wooCategoryIds.includes(c.id)}
                          onChange={ev => U("wooCategoryIds", ev.target.checked
                            ? [...e.wooCategoryIds, c.id]
                            : e.wooCategoryIds.filter(x => x !== c.id))} />
                        <span style={{ paddingLeft: c.parentId ? 16 : 0 }}>
                          {c.name}
                          <span style={{ color: "#484f58", fontSize: 11 }}> ({c.count})</span>
                        </span>
                      </label>
                    ))}
                  </div>
                )}

              <div style={{ marginTop: 18 }}>
                <label className="lbl">Etiketler</label>
                <div style={{ display: "flex", gap: 8, marginBottom: 8 }}>
                  <input className="inp" placeholder="Etiket yazıp Enter'a basın"
                    value={tagInput}
                    onChange={ev => setTagInput(ev.target.value)}
                    onKeyDown={ev => { if (ev.key === "Enter") { ev.preventDefault(); addTag(); } }} />
                  <button className="btn btn-ghost btn-sm" onClick={addTag}>Ekle</button>
                </div>
                <div>
                  {e.tags.map(t => (
                    <span key={t} className="chip">
                      {t}
                      <button onClick={() => U("tags", e.tags.filter(x => x !== t))}>✕</button>
                    </span>
                  ))}
                  {e.tags.length === 0 && (
                    <span style={{ fontSize: 11, color: "#484f58" }}>Etiket yok</span>
                  )}
                </div>
              </div>
            </>)}

            {/* ATTRIBUTES */}
            {tab === "attrs" && (<>
              <div style={{ fontSize: 12, color: "#6e7681", marginBottom: 12 }}>
                Renk, Beden, Materyal gibi ürün özellikleri. Değerleri virgülle ayırın.
              </div>
              {e.attributes.map((a, i) => (
                <div key={i} className="attr-row">
                  <div>
                    <label className="lbl">Özellik Adı</label>
                    <input className="inp" placeholder="Renk" value={a.name}
                      onChange={ev => updAttr(i, { name: ev.target.value })} />
                  </div>
                  <div>
                    <label className="lbl">Değerler (virgülle)</label>
                    <input className="inp" placeholder="Kırmızı, Mavi, Siyah"
                      value={a.options.join(", ")}
                      onChange={ev => updAttr(i, {
                        options: ev.target.value.split(",").map(s => s.trim()).filter(Boolean)
                      })} />
                  </div>
                  <button className="btn btn-danger btn-sm"
                    onClick={() => U("attributes", e.attributes.filter((_, x) => x !== i))}>✕</button>
                </div>
              ))}
              <button className="btn btn-ghost btn-sm" onClick={addAttr}
                style={{ marginTop: 6 }}>+ Özellik Ekle</button>
            </>)}

            {/* ADVANCED */}
            {tab === "advanced" && (<>
              <div className="g3">
                <div>
                  <label className="lbl">Uzunluk (cm)</label>
                  <input className="inp" value={e.dimensions?.length ?? ""}
                    onChange={ev => U("dimensions", {
                      length: ev.target.value || null,
                      width:  e.dimensions?.width ?? null,
                      height: e.dimensions?.height ?? null,
                    })} />
                </div>
                <div>
                  <label className="lbl">Genişlik (cm)</label>
                  <input className="inp" value={e.dimensions?.width ?? ""}
                    onChange={ev => U("dimensions", {
                      length: e.dimensions?.length ?? null,
                      width:  ev.target.value || null,
                      height: e.dimensions?.height ?? null,
                    })} />
                </div>
                <div>
                  <label className="lbl">Yükseklik (cm)</label>
                  <input className="inp" value={e.dimensions?.height ?? ""}
                    onChange={ev => U("dimensions", {
                      length: e.dimensions?.length ?? null,
                      width:  e.dimensions?.width ?? null,
                      height: ev.target.value || null,
                    })} />
                </div>
              </div>
              <div className="g2" style={{ marginTop: 14 }}>
                <div>
                  <label className="lbl">Katalog Görünürlüğü</label>
                  <select className="inp" value={e.catalogVisibility ?? "visible"}
                    onChange={ev => U("catalogVisibility", ev.target.value)}>
                    <option value="visible">Mağaza ve arama</option>
                    <option value="catalog">Sadece mağaza</option>
                    <option value="search">Sadece arama</option>
                    <option value="hidden">Gizli</option>
                  </select>
                </div>
                <div>
                  <label className="lbl">Ön Sipariş</label>
                  <select className="inp" value={e.backorderPolicy}
                    onChange={ev => U("backorderPolicy", ev.target.value)}>
                    <option value="no">İzin verilmiyor</option>
                    <option value="notify">İzin ver, bilgilendir</option>
                    <option value="yes">İzin ver</option>
                  </select>
                </div>
              </div>
              <div style={{ marginTop: 14 }}>
                <label className="lbl">URL Slug (boş = otomatik)</label>
                <input className="inp" placeholder="urun-adi-slug"
                  value={e.overrideSlug ?? ""}
                  onChange={ev => U("overrideSlug", ev.target.value || null)} />
              </div>
              <div style={{ marginTop: 18 }}>
                <label className="lbl">Özel Meta Alanları (SEO eklentileri için)</label>
                {e.customMeta.map((m, i) => (
                  <div key={i} style={{ display: "grid",
                    gridTemplateColumns: "1fr 1fr auto", gap: 8, marginBottom: 8 }}>
                    <input className="inp" placeholder="_yoast_wpseo_title" value={m.key}
                      onChange={ev => U("customMeta", e.customMeta.map((x, idx) =>
                        idx === i ? { ...x, key: ev.target.value } : x))} />
                    <input className="inp" placeholder="Değer" value={m.value}
                      onChange={ev => U("customMeta", e.customMeta.map((x, idx) =>
                        idx === i ? { ...x, value: ev.target.value } : x))} />
                    <button className="btn btn-danger btn-sm"
                      onClick={() => U("customMeta",
                        e.customMeta.filter((_, x) => x !== i))}>✕</button>
                  </div>
                ))}
                <button className="btn btn-ghost btn-sm"
                  onClick={() => U("customMeta", [...e.customMeta, { key: "", value: "" }])}>
                  + Meta Alanı Ekle
                </button>
              </div>
            </>)}

            <div className="mfooter">
              <button className="btn btn-ghost" style={{ flex: 1 }} onClick={onClose}>
                İptal
              </button>
              <button className="btn btn-primary" style={{ flex: 1 }}
                disabled={saving} onClick={save}>
                {saving ? "Kaydediliyor..." : "Kaydet"}
              </button>
            </div>
          </>)}
        </div>
      </div>
    </div>
  );
}
