import axios from 'axios';
import type {
  ApiResponse, LoginResult, LogoConnection, WooStore,
  LogoTestResult, WooTestResult, LicenseStatus, DashboardStats,
  ProductMapping, ProductEnrichment, ProductEnrichmentDetail,
  WooCategory, PagedResult, LogoFetchDiagnostics,
  ImportResult, RefreshResult, ProductHistory,
  LogoLookupResult,
  LogoSqlProbe,
} from '@/types/api';

const BASE = 'http://localhost:5000';

export const api = axios.create({
  baseURL: BASE,
  headers: { 'Content-Type': 'application/json' },
  timeout: 300000, // 5 dk - Logo REST yavas olabilir
});

api.interceptors.request.use((cfg) => {
  if (typeof window !== 'undefined') {
    const t = localStorage.getItem('senkora_token');
    if (t) cfg.headers.Authorization = `Bearer ${t}`;
  }
  return cfg;
});

api.interceptors.response.use(
  (r) => r,
  (err) => {
    if (err.response?.status === 401 && typeof window !== 'undefined') {
      if (!window.location.pathname.includes('/login')) {
        localStorage.removeItem('senkora_token');
        localStorage.removeItem('senkora_user');
        window.location.href = '/login';
      }
    }
    return Promise.reject(err);
  }
);

export const authApi = {
  login: (email: string, password: string) =>
    api.post<ApiResponse<LoginResult>>('api/v1/auth/login', { email, password }),
};

export const logoApi = {
  list: () =>
    api.get<ApiResponse<LogoConnection[]>>('api/v1/logo/connections'),
  create: (d: {
    name: string; restUrl: string; clientId: string; clientSecret: string;
    username: string; password: string; firmNo: number; periodNo: number; timeoutSeconds: number;
  }) => api.post<ApiResponse<string>>('api/v1/logo/connections', d),
  update: (id: string, d: {
    name: string; restUrl: string; clientId?: string; clientSecret?: string;
    password?: string; firmNo: number; periodNo: number; timeoutSeconds: number; isActive: boolean;
  }) => api.put<ApiResponse<null>>(`api/v1/logo/connections/${id}`, d),
  delete: (id: string) =>
    api.delete<ApiResponse<null>>(`api/v1/logo/connections/${id}`),
  test: (d: {
    restUrl: string; clientId: string; clientSecret: string;
    username: string; password: string; firmNo: number;
  }) => api.post<ApiResponse<LogoTestResult>>('api/v1/logo/connections/test', d),

  probeSql: (connectionId: string, sql?: string) =>
    api.get<ApiResponse<LogoSqlProbe[]>>(
      `api/v1/logo/connections/${connectionId}/probe-sql`, { params: { sql } }),

  lookups: (connectionId: string) =>
    api.get<ApiResponse<LogoLookupResult>>(
      `api/v1/logo/connections/${connectionId}/lookups`),
};

export const wooApi = {
  list: () =>
    api.get<ApiResponse<WooStore[]>>('api/v1/woo/stores'),
  create: (d: {
    name: string; storeUrl: string; consumerKey: string; consumerSecret: string;
    wpUsername?: string; wpAppPassword?: string;
    priceProjectCode?: string; priceTradingGroupCode?: string; priceCostCenterCode?: string;
  }) => api.post<ApiResponse<string>>('api/v1/woo/stores', d),
  update: (id: string, d: {
    name: string; storeUrl: string; consumerKey?: string; consumerSecret?: string;
    isActive: boolean; wpUsername?: string; wpAppPassword?: string;
    priceProjectCode?: string; priceTradingGroupCode?: string; priceCostCenterCode?: string;
  }) => api.put<ApiResponse<null>>(`api/v1/woo/stores/${id}`, d),
  delete: (id: string) =>
    api.delete<ApiResponse<null>>(`api/v1/woo/stores/${id}`),
  test: (d: {
    storeUrl: string; consumerKey: string; consumerSecret: string;
  }) => api.post<ApiResponse<WooTestResult>>('api/v1/woo/stores/test', d),
};

export const licenseApi = {
  status: () =>
    api.get<ApiResponse<LicenseStatus>>('api/v1/license/status'),
};

export const dashboardApi = {
  stats: () =>
    api.get<ApiResponse<DashboardStats>>('api/v1/dashboard/stats'),
};


// ─── Products ─────────────────────────────────────────────────────────────────

export const productApi = {
  list: (params?: {
    wooStoreId?: string; status?: string; search?: string;
    page?: number; pageSize?: number;
  }) => api.get<ApiResponse<PagedResult<ProductMapping>>>('api/v1/products', { params }),

  importNew: (d: {
    logoConnectionId: string; wooStoreId: string; maxScan?: number;
  }) => api.post<ApiResponse<ImportResult>>('api/v1/products/import', d),

  refresh: (d: {
    logoConnectionId: string; wooStoreId: string; previewOnly?: boolean;
  }) => api.post<ApiResponse<RefreshResult>>('api/v1/products/refresh', d),

  deleteMany: (d: { ids?: string[]; deleteAll?: boolean; statusFilter?: string }) =>
    api.post<ApiResponse<number>>('api/v1/products/delete', d),

  purgeDeleted: () =>
    api.post<ApiResponse<number>>('api/v1/products/purge-deleted'),

  history: (id: string) =>
    api.get<ApiResponse<ProductHistory[]>>(`api/v1/products/${id}/history`),

  getEnrichment: (id: string) =>
    api.get<ApiResponse<ProductEnrichmentDetail>>(`api/v1/products/${id}/enrichment`),

  saveEnrichment: (id: string, e: ProductEnrichment) =>
    api.put<ApiResponse<null>>(`api/v1/products/${id}/enrichment`, e),

  syncToWoo: (id: string) =>
    api.post<ApiResponse<number>>(`api/v1/products/${id}/sync`),

  wooCategories: (wooStoreId: string) =>
    api.get<ApiResponse<WooCategory[]>>('api/v1/products/woo-categories',
      { params: { wooStoreId } }),

  diagnoseLogo: (logoConnectionId: string, limit = 3) =>
    api.get<ApiResponse<LogoFetchDiagnostics>>('api/v1/products/diagnose-logo',
      { params: { logoConnectionId, limit } }),

  uploadImage: (id: string, file: File) => {
    const fd = new FormData();
    fd.append('file', file);
    return api.post<ApiResponse<string>>(`api/v1/products/${id}/images`, fd, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
};

export const diagnosticsApi = {
  checkSchema: () =>
    api.get<ApiResponse<{ isHealthy: boolean; issues: string[]; message: string }>>(
      'api/v1/diagnostics/schema'),
};
