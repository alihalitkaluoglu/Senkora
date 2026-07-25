export const DASH_CSS = `
*,*::before,*::after{box-sizing:border-box;margin:0;padding:0}
body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;background:#030712;color:white}
a{text-decoration:none;color:inherit}

/* Layout */
.shell{display:flex;min-height:100vh}
.side{width:220px;background:#0d1117;border-right:1px solid #21262d;
  display:flex;flex-direction:column;position:fixed;top:0;left:0;height:100vh;z-index:10}
.content{margin-left:220px;flex:1;padding:28px 32px;max-width:1000px}

/* Sidebar */
.brand{padding:16px;border-bottom:1px solid #21262d;display:flex;align-items:center;gap:10px}
.brand-ico{width:30px;height:30px;background:#1d4ed8;border-radius:8px;display:flex;
  align-items:center;justify-content:center;color:#fff;font-weight:800;font-size:14px;flex-shrink:0}
.brand-name{font-size:15px;font-weight:700;color:#f0f6fc}
.sidenav{flex:1;padding:8px;overflow-y:auto}
.slink{display:flex;align-items:center;gap:9px;padding:8px 12px;border-radius:8px;
  font-size:13px;color:#8b949e;margin-bottom:1px;transition:all .15s;white-space:nowrap}
.slink:hover{color:#f0f6fc;background:#161b22}
.slink.on{color:#79c0ff;background:rgba(56,139,253,0.1);font-weight:500}
.slink .ico{font-size:14px;width:18px;text-align:center;flex-shrink:0}
.sfoot{padding:14px 16px;border-top:1px solid #21262d}
.semail{font-size:11px;color:#6e7681;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;margin-bottom:6px}
.sout{font-size:11px;color:#6e7681;background:none;border:none;cursor:pointer;padding:0}
.sout:hover{color:#f85149}

/* Page header */
.ph{display:flex;align-items:flex-start;justify-content:space-between;margin-bottom:24px;gap:16px}
.ph-title{font-size:20px;font-weight:600;color:#f0f6fc}
.ph-sub{font-size:13px;color:#6e7681;margin-top:4px}

/* Cards */
.card{background:#0d1117;border:1px solid #21262d;border-radius:12px;padding:20px;margin-bottom:12px}
.card-empty{border-style:dashed;text-align:center;padding:56px 24px}
.card-ico{font-size:36px;margin-bottom:12px}
.card-empty-title{color:#f0f6fc;font-weight:500;margin-bottom:6px}
.card-empty-sub{font-size:13px;color:#6e7681}

/* Badge */
.badge{font-size:11px;padding:2px 8px;border-radius:20px;font-weight:500}
.bg{background:rgba(35,134,54,0.15);color:#3fb950;border:1px solid rgba(35,134,54,0.3)}
.bd{background:rgba(139,148,158,0.1);color:#8b949e;border:1px solid rgba(139,148,158,0.2)}
.bb{background:rgba(56,139,253,0.1);color:#79c0ff;border:1px solid rgba(56,139,253,0.25)}

/* Buttons */
.btn{border:none;border-radius:8px;padding:8px 16px;font-size:13px;font-weight:500;
  cursor:pointer;transition:all .15s;white-space:nowrap}
.btn:hover:not(:disabled){filter:brightness(1.1)}
.btn:disabled{opacity:.5;cursor:not-allowed}
.btn-primary{background:#1d4ed8;color:white}
.btn-purple{background:#7c3aed;color:white}
.btn-ghost{background:#161b22;color:#8b949e;border:1px solid #30363d}
.btn-danger{background:rgba(248,81,73,0.1);color:#f85149;border:1px solid rgba(248,81,73,0.2)}
.btn-outline{background:rgba(56,139,253,0.1);color:#79c0ff;border:1px solid rgba(56,139,253,0.25)}
.btn-sm{padding:5px 12px;font-size:12px}

/* Form */
.inp{width:100%;background:#161b22;border:1px solid #30363d;border-radius:8px;
  padding:9px 12px;font-size:13px;color:#f0f6fc;outline:none;transition:border-color .15s}
.inp:focus{border-color:#1d4ed8;box-shadow:0 0 0 3px rgba(29,78,216,0.15)}
.inp::placeholder{color:#484f58}
.lbl{font-size:11px;color:#8b949e;margin-bottom:5px;display:block;font-weight:500}
.fld{margin-bottom:14px}
.grid2{display:grid;grid-template-columns:1fr 1fr;gap:12px}
.grid3{display:grid;grid-template-columns:1fr 1fr 1fr;gap:12px}
.span2{grid-column:1/-1}
.chk-row{display:flex;align-items:center;gap:8px;cursor:pointer}
.chk-row input{width:14px;height:14px;cursor:pointer;accent-color:#1d4ed8}
.chk-row span{font-size:13px;color:#8b949e}

/* Alert */
.alert-err{background:rgba(248,81,73,0.08);border:1px solid rgba(248,81,73,0.25);
  border-radius:8px;padding:10px 14px;color:#ffa198;font-size:13px;margin-bottom:14px}

/* Modal */
.overlay{position:fixed;inset:0;background:rgba(0,0,0,.8);display:flex;
  align-items:center;justify-content:center;z-index:50;padding:16px}
.modal{background:#161b22;border:1px solid #30363d;border-radius:14px;
  width:100%;box-shadow:0 20px 60px rgba(0,0,0,.6);max-height:90vh;overflow-y:auto}
.modal-sm{max-width:420px}
.modal-md{max-width:520px}
.modal-lg{max-width:620px}
.mhead{padding:16px 20px;border-bottom:1px solid #21262d;display:flex;
  align-items:center;justify-content:space-between}
.mtitle{font-size:15px;font-weight:600;color:#f0f6fc}
.mclose{background:none;border:none;color:#6e7681;cursor:pointer;font-size:20px;line-height:1;padding:0}
.mclose:hover{color:#f0f6fc}
.mbody{padding:20px}
.mfooter{display:flex;gap:10px;margin-top:20px}

/* Result */
.res-ok{background:rgba(35,134,54,0.07);border:1px solid rgba(35,134,54,0.25);
  border-radius:8px;padding:12px 14px;margin-top:12px}
.res-err{background:rgba(248,81,73,0.07);border:1px solid rgba(248,81,73,0.25);
  border-radius:8px;padding:12px 14px;margin-top:12px}
.res-ok-t{color:#3fb950;font-size:13px;font-weight:500;margin-bottom:4px}
.res-err-t{color:#f85149;font-size:13px;font-weight:500;margin-bottom:4px}
.res-sub{font-size:12px;color:#8b949e}

/* Meta row */
.meta{display:flex;gap:20px;flex-wrap:wrap;margin-top:8px}
.meta span{font-size:11px;color:#484f58}

/* Spinner */
@keyframes spin{to{transform:rotate(360deg)}}
.spin{width:18px;height:18px;border:2px solid #30363d;border-top-color:#1d4ed8;
  border-radius:50%;animation:spin .8s linear infinite}

/* Stat cards */
.stats{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin-bottom:20px}
.scard{background:#0d1117;border:1px solid #21262d;border-radius:12px;padding:20px}
.sval{font-size:30px;font-weight:700;line-height:1}
.slbl{font-size:12px;color:#6e7681;margin-top:6px}

/* Quick links */
.qgrid{display:grid;grid-template-columns:1fr 1fr;gap:12px}
.qcard{background:#0d1117;border:1px solid #21262d;border-radius:12px;padding:20px;
  transition:border-color .15s;display:block}
.qcard:hover{border-color:#388bfd}
.qcard-ico{font-size:22px;margin-bottom:10px}
.qcard-t{font-size:13px;font-weight:500;color:#f0f6fc;margin-bottom:4px}
.qcard-s{font-size:12px;color:#6e7681}

/* Util */
.row{display:flex;align-items:center;gap:8px;flex-wrap:wrap}
.row-between{display:flex;align-items:flex-start;justify-content:space-between;gap:16px}
.col{display:flex;flex-direction:column}
.mono{font-family:monospace;font-size:12px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.dim{color:#6e7681;font-size:13px}
.mt8{margin-top:8px}
.mt12{margin-top:12px}
.mt16{margin-top:16px}
.mb8{margin-bottom:8px}
.flex1{flex:1}
.mw0{min-width:0}

/* Product page additions */
.toolbar{display:flex;gap:10px;align-items:center;margin-bottom:16px;flex-wrap:wrap}
.filter-sel{background:#161b22;border:1px solid #30363d;border-radius:8px;
  padding:7px 10px;font-size:12px;color:#f0f6fc;outline:none;cursor:pointer}
.search-inp{background:#161b22;border:1px solid #30363d;border-radius:8px;
  padding:7px 12px;font-size:12px;color:#f0f6fc;outline:none;min-width:220px}
.search-inp:focus,.filter-sel:focus{border-color:#1d4ed8}
.ptable{width:100%;border-collapse:collapse;font-size:12px}
.ptable th{text-align:left;padding:10px 12px;color:#8b949e;font-weight:500;
  font-size:11px;text-transform:uppercase;letter-spacing:.4px;
  border-bottom:1px solid #21262d;white-space:nowrap}
.ptable td{padding:11px 12px;border-bottom:1px solid #161b22;color:#c9d1d9;vertical-align:middle}
.ptable tr:hover td{background:#0d1117}
.st{font-size:10px;padding:3px 8px;border-radius:12px;font-weight:600;
  text-transform:uppercase;letter-spacing:.3px;white-space:nowrap}
.st-draft{background:rgba(139,148,158,.15);color:#8b949e;border:1px solid rgba(139,148,158,.25)}
.st-enriched{background:rgba(56,139,253,.15);color:#79c0ff;border:1px solid rgba(56,139,253,.3)}
.st-pending{background:rgba(240,136,62,.15);color:#f0883e;border:1px solid rgba(240,136,62,.3)}
.st-synced{background:rgba(35,134,54,.15);color:#3fb950;border:1px solid rgba(35,134,54,.3)}
.st-error{background:rgba(248,81,73,.15);color:#f85149;border:1px solid rgba(248,81,73,.3)}
.st-excluded{background:rgba(110,118,129,.15);color:#6e7681;border:1px solid rgba(110,118,129,.25)}
.img-badge{display:inline-flex;align-items:center;gap:4px;font-size:11px;color:#6e7681}
.pager{display:flex;gap:8px;align-items:center;justify-content:center;margin-top:16px}
.pbtn{background:#161b22;border:1px solid #30363d;color:#8b949e;border-radius:6px;
  padding:5px 12px;font-size:12px;cursor:pointer}
.pbtn:hover:not(:disabled){border-color:#1d4ed8;color:#f0f6fc}
.pbtn:disabled{opacity:.4;cursor:not-allowed}
.pinfo{font-size:12px;color:#6e7681;padding:0 8px}
.tabs{display:flex;gap:2px;border-bottom:1px solid #21262d;margin-bottom:20px}
.tab{padding:9px 16px;font-size:13px;color:#8b949e;cursor:pointer;
  border-bottom:2px solid transparent;transition:all .15s}
.tab:hover{color:#f0f6fc}
.tab.on{color:#79c0ff;border-bottom-color:#1d4ed8;font-weight:500}
.imgs-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(110px,1fr));gap:10px}
.img-card{position:relative;aspect-ratio:1;background:#0d1117;border:1px solid #30363d;
  border-radius:8px;overflow:hidden}
.img-card img{width:100%;height:100%;object-fit:cover}
.img-star{position:absolute;top:4px;left:4px;background:rgba(0,0,0,.7);
  border:none;color:#f0883e;cursor:pointer;font-size:14px;
  width:22px;height:22px;border-radius:4px;line-height:1}
.img-del{position:absolute;top:4px;right:4px;background:rgba(248,81,73,.9);
  border:none;color:white;cursor:pointer;font-size:12px;
  width:22px;height:22px;border-radius:4px;line-height:1}
.img-drop{aspect-ratio:1;border:2px dashed #30363d;border-radius:8px;
  display:flex;flex-direction:column;align-items:center;justify-content:center;
  cursor:pointer;color:#6e7681;font-size:11px;gap:6px;transition:all .15s}
.img-drop:hover{border-color:#1d4ed8;color:#79c0ff}
.cat-list{max-height:200px;overflow-y:auto;border:1px solid #30363d;
  border-radius:8px;padding:8px;background:#0d1117}
.cat-item{display:flex;align-items:center;gap:8px;padding:5px 6px;
  border-radius:5px;cursor:pointer;font-size:12px}
.cat-item:hover{background:#161b22}
.cat-item input{accent-color:#1d4ed8;cursor:pointer}
.chip{display:inline-flex;align-items:center;gap:5px;background:#21262d;
  border:1px solid #30363d;border-radius:14px;padding:3px 9px;
  font-size:11px;color:#c9d1d9;margin:2px}
.chip button{background:none;border:none;color:#6e7681;cursor:pointer;
  font-size:13px;padding:0;line-height:1}
.chip button:hover{color:#f85149}
.info-box{background:#0d1117;border:1px solid #21262d;border-radius:8px;
  padding:12px 14px;font-size:12px;margin-bottom:16px}
.info-row{display:flex;justify-content:space-between;padding:3px 0;color:#8b949e}
.info-row strong{color:#c9d1d9;font-weight:500}
.attr-row{display:grid;grid-template-columns:1fr 2fr auto;gap:8px;
  align-items:end;margin-bottom:8px}
`;
