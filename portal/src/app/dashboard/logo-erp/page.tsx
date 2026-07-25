
"use client";
import { useEffect, useState } from "react";
import { logoApi } from "@/lib/api";
import type { LogoConnection } from "@/types/api";
import { DASH_CSS } from "@/lib/dashboardCss";
import { apiError } from "@/lib/errorMessage";


const BLANK = {
  name:"",restUrl:"",clientId:"",clientSecret:"",
  username:"",password:"",firmNo:1,periodNo:1,timeoutSeconds:30,
};

type TR = { isSuccess:boolean; currentFirm?:number|null; responseTimeMs:number; errorMessage?:string|null };
type EF = { name:string; restUrl:string; username:string; clientId:string;
  clientSecret:string; password:string; firmNo:number; periodNo:number;
  timeoutSeconds:number; isActive:boolean };

export default function LogoErpPage() {
  const [list,   setList  ] = useState<LogoConnection[]>([]);
  const [busy,   setBusy  ] = useState(true);
  // create
  const [showC,  setShowC ] = useState(false);
  const [form,   setForm  ] = useState(BLANK);
  const [cSav,   setCSav  ] = useState(false);
  const [cErr,   setCErr  ] = useState("");
  // edit
  const [editConn, setEditConn] = useState<LogoConnection|null>(null);
  const [ef,    setEf    ] = useState<EF>({name:"",restUrl:"",username:"",clientId:"",
    clientSecret:"",password:"",firmNo:1,periodNo:1,timeoutSeconds:30,isActive:true});
  const [eSav,  setESav  ] = useState(false);
  const [eErr,  setEErr  ] = useState("");
  // test
  const [testConn,  setTestConn ] = useState<LogoConnection|null>(null);
  const [tc,        setTc       ] = useState({clientId:"",clientSecret:"",password:""});
  const [testingId, setTestingId] = useState<string|null>(null);
  const [results,   setResults  ] = useState<Record<string,TR>>({});

  const load = () => {
    setBusy(true);
    logoApi.list()
      .then(r => setList(r.data.data ?? []))
      .catch(console.error)
      .finally(() => setBusy(false));
  };
  useEffect(() => { load(); }, []);

  const F = (k:string, v:string|number) => setForm(p => ({...p,[k]:v}));
  const EFn = (k:string, v:string|number|boolean) => setEf(p => ({...p,[k]:v}));

  async function doCreate(e:React.FormEvent) {
    e.preventDefault(); setCErr(""); setCSav(true);
    try {
      await logoApi.create(form);
      setShowC(false); setForm(BLANK); load();
    } catch(err:unknown) {
      setCErr(apiError(err));
    } finally { setCSav(false); }
  }

  function openEdit(c:LogoConnection) {
    setEf({name:c.name,restUrl:c.restUrl,username:c.username,
      clientId:"",clientSecret:"",password:"",
      firmNo:c.firmNo,periodNo:c.periodNo,timeoutSeconds:c.timeoutSeconds,isActive:c.isActive});
    setEErr(""); setEditConn(c);
  }

  async function doEdit(e:React.FormEvent) {
    e.preventDefault(); setEErr(""); setESav(true);
    try {
      await logoApi.update(editConn!.id, ef);
      setEditConn(null); load();
    } catch(err:unknown) {
      setEErr(apiError(err));
    } finally { setESav(false); }
  }

  function openTest(c:LogoConnection) {
    setTc({clientId:"",clientSecret:"",password:""});
    setTestConn(c);
  }

  async function doTest() {
    if (!testConn) return;
    const id = testConn.id;
    setTestingId(id); setTestConn(null);
    try {
      const r = await logoApi.test({
        restUrl:testConn.restUrl, username:testConn.username, firmNo:testConn.firmNo,
        clientId:tc.clientId, clientSecret:tc.clientSecret, password:tc.password,
      });
      setResults(p => ({...p,[id]:r.data.data!}));
    } catch(e) { console.error(e); }
    finally { setTestingId(null); }
  }

  async function doDelete(id:string) {
    if (!confirm("Bu bağlantıyı silmek istediğinize emin misiniz?")) return;
    try { await logoApi.delete(id); load(); } catch(e) { console.error(e); }
  }

  const FIELD = (l:string,k:string,t:string,p:string,required=true,span=false) => (
    <div key={k} className={span?"s2":""} style={{marginBottom:14}}>
      <label className="lbl">{l}{required?" *":""}</label>
      <input className="inp" type={t} placeholder={p}
        required={required}
        value={(form as Record<string,unknown>)[k] as string}
        onChange={e=>F(k,e.target.value)} />
    </div>
  );

  const EFF = (l:string,k:string,t:string,p:string,req=true) => (
    <div key={k} style={{marginBottom:14}}>
      <label className="lbl">{l}{req?" *":""}</label>
      <input className="inp" type={t} placeholder={p}
        required={req}
        value={(ef as Record<string,unknown>)[k] as string}
        onChange={e=>EFn(k,e.target.value)} />
    </div>
  );

  return (<>
    <style>{DASH_CSS}</style>

    {/* CREATE MODAL */}
    {showC && (
      <div className="overlay" onClick={e=>e.target===e.currentTarget&&setShowC(false)}>
        <div className="modal mlg">
          <div className="mhead">
            <span className="mtitle">Yeni Logo ERP Bağlantısı</span>
            <button className="mclose" onClick={()=>setShowC(false)}>✕</button>
          </div>
          <form onSubmit={doCreate} className="mbody">
            {cErr && <div className="alert-err">{cErr}</div>}
            <div className="g2">
              {FIELD("Bağlantı Adı","name","text","Logo Tiger Test",true,true)}
              {FIELD("REST URL","restUrl","text","http://192.168.1.1:32001",true,true)}
              {FIELD("Client ID","clientId","text","GENC")}
              {FIELD("Client Secret","clientSecret","password","Base64...")}
              {FIELD("Kullanıcı Adı","username","text","REST")}
              {FIELD("Şifre","password","password","••••••••")}
            </div>
            <div className="g3">
              <div style={{marginBottom:14}}>
                <label className="lbl">Firma No *</label>
                <input className="inp" type="number" min={1} required
                  value={form.firmNo} onChange={e=>F("firmNo",+e.target.value)} />
              </div>
              <div style={{marginBottom:14}}>
                <label className="lbl">Dönem No</label>
                <input className="inp" type="number" min={1}
                  value={form.periodNo} onChange={e=>F("periodNo",+e.target.value)} />
              </div>
              <div style={{marginBottom:14}}>
                <label className="lbl">Timeout (sn)</label>
                <input className="inp" type="number" min={5}
                  value={form.timeoutSeconds} onChange={e=>F("timeoutSeconds",+e.target.value)} />
              </div>
            </div>
            <div className="mfooter">
              <button type="button" className="btn btn-ghost flex: 1" onClick={()=>setShowC(false)}>İptal</button>
              <button type="submit" className="btn btn-primary flex: 1" disabled={cSav}>
                {cSav?"Kaydediliyor...":"Kaydet"}
              </button>
            </div>
          </form>
        </div>
      </div>
    )}

    {/* EDIT MODAL */}
    {editConn && (
      <div className="overlay" onClick={e=>e.target===e.currentTarget&&setEditConn(null)}>
        <div className="modal mlg">
          <div className="mhead">
            <span className="mtitle">Bağlantıyı Düzenle — {editConn.name}</span>
            <button className="mclose" onClick={()=>setEditConn(null)}>✕</button>
          </div>
          <form onSubmit={doEdit} className="mbody">
            {eErr && <div className="alert-err">{eErr}</div>}
            <div className="g2">
              {EFF("Bağlantı Adı","name","text",editConn.name)}
              {EFF("REST URL","restUrl","text",editConn.restUrl)}
              {EFF("Kullanıcı Adı","username","text",editConn.username)}
              {EFF("Client ID (boş=değişmez)","clientId","text","Değiştirmek için girin",false)}
              {EFF("Client Secret (boş=değişmez)","clientSecret","password","••••••••",false)}
              {EFF("Şifre (boş=değişmez)","password","password","••••••••",false)}
            </div>
            <div className="g3">
              <div style={{marginBottom:14}}>
                <label className="lbl">Firma No *</label>
                <input className="inp" type="number" min={1} required
                  value={ef.firmNo} onChange={e=>EFn("firmNo",+e.target.value)} />
              </div>
              <div style={{marginBottom:14}}>
                <label className="lbl">Dönem No</label>
                <input className="inp" type="number" min={1}
                  value={ef.periodNo} onChange={e=>EFn("periodNo",+e.target.value)} />
              </div>
              <div style={{marginBottom:14}}>
                <label className="lbl">Timeout (sn)</label>
                <input className="inp" type="number" min={5}
                  value={ef.timeoutSeconds} onChange={e=>EFn("timeoutSeconds",+e.target.value)} />
              </div>
            </div>
            <label className="chk" style={{marginBottom:16}}>
              <input type="checkbox" checked={ef.isActive}
                onChange={e=>EFn("isActive",e.target.checked)} />
              <span>Aktif</span>
            </label>
            <div className="mfooter">
              <button type="button" className="btn btn-ghost flex: 1" onClick={()=>setEditConn(null)}>İptal</button>
              <button type="submit" className="btn btn-primary flex: 1" disabled={eSav}>
                {eSav?"Kaydediliyor...":"Güncelle"}
              </button>
            </div>
          </form>
        </div>
      </div>
    )}

    {/* TEST MODAL */}
    {testConn && (
      <div className="overlay" onClick={e=>e.target===e.currentTarget&&setTestConn(null)}>
        <div className="modal msm">
          <div className="mhead">
            <span className="mtitle">Bağlantı Testi</span>
            <button className="mclose" onClick={()=>setTestConn(null)}>✕</button>
          </div>
          <div className="mbody">
            <div style={{fontSize:12,color:"#6e7681",marginBottom:16,lineHeight:1.5}}>
              <strong style={{color:"#f0f6fc"}}>{testConn.name}</strong><br/>
              {testConn.restUrl} · Firma: {testConn.firmNo}<br/>
              Şifreli alanlar saklanmaz, lütfen tekrar girin.
            </div>
            {[
              {l:"Client ID",k:"clientId",t:"text"},
              {l:"Client Secret",k:"clientSecret",t:"password"},
              {l:"Şifre",k:"password",t:"password"},
            ].map(({l,k,t}) => (
              <div key={k} style={{marginBottom:12}}>
                <label className="lbl">{l} *</label>
                <input className="inp" type={t} required
                  value={(tc as Record<string,string>)[k]}
                  onChange={e=>setTc(p=>({...p,[k]:e.target.value}))} />
              </div>
            ))}
            <div className="mfooter">
              <button className="btn btn-ghost flex: 1" onClick={()=>setTestConn(null)}>İptal</button>
              <button className="btn btn-primary flex: 1" onClick={doTest}>Test Et</button>
            </div>
          </div>
        </div>
      </div>
    )}

    {/* PAGE */}
    <div className="ph">
      <div>
        <div className="ph-title">Logo ERP Bağlantıları</div>
        <div className="ph-sub">Logo Tiger REST servis bağlantılarını tanımlayın</div>
      </div>
      <button className="btn btn-primary" onClick={()=>{setShowC(true);setCErr("");}}>
        + Yeni Bağlantı
      </button>
    </div>

    {busy ? <div className="dim">Yükleniyor...</div> :
     list.length===0 ? (
      <div className="card" style={{borderStyle:"dashed",textAlign:"center",padding:56}}>
        <div style={{fontSize:36,marginBottom:12}}>◉</div>
        <div style={{color:"#f0f6fc",fontWeight:500,marginBottom:6}}>Bağlantı yok</div>
        <div className="dim">Yukarıdaki butonu kullanarak ilk bağlantınızı ekleyin.</div>
      </div>
    ) : list.map(c => {
      const tr = results[c.id];
      return (
        <div key={c.id} className="card">
          <div className="row-b">
            <div style={{flex:1,minWidth:0}}>
              <div className="row" style={{marginBottom:8}}>
                <span style={{fontSize:14,fontWeight:500,color:"#f0f6fc"}}>{c.name}</span>
                <span className={`badge ${c.isActive?"bg":"bd"}`}>{c.isActive?"Aktif":"Pasif"}</span>
                {c.isVerified && <span className="badge bb">Doğrulandı</span>}
              </div>
              <div className="mono" style={{color:"#6e7681",marginBottom:6}}>{c.restUrl}</div>
              <div className="meta">
                <span>Kullanıcı: {c.username}</span>
                <span>Firma: {c.firmNo}</span>
                <span>Dönem: {c.periodNo}</span>
                <span>Timeout: {c.timeoutSeconds}sn</span>
                <span>Token: {c.hasCachedToken?"✓ Cache":"Yok"}</span>
              </div>
            </div>
            <div className="btns">
              <button className="btn btn-outline btn-sm" disabled={testingId===c.id}
                onClick={()=>openTest(c)}>
                {testingId===c.id?"Test...":"Test Et"}
              </button>
              <button className="btn btn-ghost btn-sm" onClick={()=>openEdit(c)}>
                Düzenle
              </button>
              <button className="btn btn-danger btn-sm" onClick={()=>doDelete(c.id)}>
                Sil
              </button>
            </div>
          </div>
          {tr && (
            <div className={tr.isSuccess?"rok":"rerr"}>
              {tr.isSuccess ? (<>
                <div className="rokt">✓ Bağlantı başarılı</div>
                <div className="rsub">Firma: {tr.currentFirm} · Yanıt: {tr.responseTimeMs}ms</div>
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
