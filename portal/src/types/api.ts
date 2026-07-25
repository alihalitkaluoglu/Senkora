export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
  meta?: unknown;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

export interface LoginResult {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  userId: string;
  email: string;
  fullName: string;
  roles: string[];
  requiresMfa: boolean;
}

export interface LogoConnection {
  id: string;
  name: string;
  restUrl: string;
  username: string;
  firmNo: number;
  periodNo: number;
  isActive: boolean;
  isVerified: boolean;
  lastVerifiedAt: string | null;
  lastSyncAt: string | null;
  timeoutSeconds: number;
  hasCachedToken: boolean;
}

export interface WooStore {
  id: string;
  name: string;
  storeUrl: string;
  apiVersion: string;
  isActive: boolean;
  isVerified: boolean;
  lastVerifiedAt: string | null;
  lastSyncAt: string | null;
  wpUsername: string | null;
  hasWpCredentials: boolean;
}

export interface LogoTestResult {
  isSuccess: boolean;
  accessToken: string | null;
  currentFirm: number | null;
  errorMessage: string | null;
  responseTimeMs: number;
}

export interface WooTestResult {
  isSuccess: boolean;
  storeName: string | null;
  wooVersion: string | null;
  errorMessage: string | null;
  responseTimeMs: number;
}

export interface LicenseStatus {
  isValid: boolean;
  tier: string;
  expiresAt: string | null;
  daysRemaining: number;
  isExpired: boolean;
  isTrialMode: boolean;
  maxWooStores: number;
  maxLogoConnections: number;
  maxMarketplaces: number;
  realtimeSync: boolean;
  webhookSupport: boolean;
  advancedReporting: boolean;
  syncIntervalMinutes: number;
}

export interface DashboardStats {
  totalSyncJobs: number;
  successfulJobs: number;
  failedJobs: number;
  pendingJobs: number;
  totalProducts: number;
  totalOrders: number;
  lastSyncAt: string | null;
}


// ─── Products ─────────────────────────────────────────────────────────────────

export interface ProductMapping {
  id: string;
  logoItemRef: number;
  logoItemCode: string;
  logoItemName: string;
  logoGroupCode: string | null;
  logoSellPrice: number;
  logoStock: number;
  status: string;
  wooProductId: number | null;
  wooSku: string | null;
  lastSyncedAt: string | null;
  lastSyncError: string | null;
  hasEnrichment: boolean;
  hasImages: boolean;
  imageCount: number;
  logoLastFetched: string;
}

export interface ProductImage {
  storedPath: string;
  remoteUrl: string | null;
  alt: string | null;
  isFeatured: boolean;
  sortOrder: number;
}

export interface ProductAttribute {
  name: string;
  options: string[];
  visible: boolean;
  variation: boolean;
}

export interface ProductDimensions {
  length: string | null;
  width: string | null;
  height: string | null;
}

export interface ProductMeta {
  key: string;
  value: string;
}

export interface ProductEnrichment {
  images: ProductImage[];
  wooCategoryIds: number[];
  tags: string[];
  attributes: ProductAttribute[];
  dimensions: ProductDimensions | null;
  shippingClass: string | null;
  catalogVisibility: string | null;
  featured: boolean;
  overrideName: string | null;
  overrideShortDesc: string | null;
  overrideDescription: string | null;
  overrideSlug: string | null;
  customMeta: ProductMeta[];
  manageStock: boolean;
  backorderPolicy: string;
  regularPriceOverride: number | null;
  salePriceOverride: number | null;
  saleFrom: string | null;
  saleTo: string | null;
}

export interface ProductEnrichmentDetail {
  mappingId: string;
  logoItemCode: string;
  logoItemName: string;
  logoSellPrice: number;
  logoSellPrice2: number;
  logoVatRate: number;
  logoStock: number;
  logoWeight: number;
  logoGroupCode: string | null;
  logoDescription: string | null;
  logoAuxDesc: string | null;
  enrichment: ProductEnrichment;
}

export interface WooCategory {
  id: number;
  name: string;
  slug: string;
  parentId: number | null;
  count: number;
}

export interface FetchLogoResult {
  fetched: number;
  created: number;
  updated: number;
  skipped: number;
}

export interface LogoFetchDiagnostics {
  tokenObtained: boolean;
  tokenPreview: string | null;
  requestUrl: string;
  requestSucceeded: boolean;
  rawResponsePreview: string | null;
  parsedItemCount: number;
  firstItemJson: string | null;
  errorMessage: string | null;
  errorStage: string | null;
  priceRequestUrl: string | null;
  priceRequestOk: boolean;
  priceRecordCount: number;
  firstPriceJson: string | null;
  priceErrorMessage: string | null;
  stockQueryOk: boolean;
  stockRecordCount: number;
  stockErrorMessage: string | null;
}

export interface ImportResult {
  scanned: number;
  created: number;
  alreadyExists: number;
  pricesMatched: number;
  stockMatched: number;
  completed: boolean;
  warning: string | null;
}

export interface ProductChangePreview {
  mappingId: string;
  code: string;
  name: string;
  field: string;
  oldValue: string | null;
  newValue: string | null;
}

export interface RefreshResult {
  total: number;
  updated: number;
  unchanged: number;
  notFoundInLogo: number;
  pricesMatched: number;
  changes: ProductChangePreview[];
}

export interface ProductHistory {
  id: string;
  action: string;
  isSuccess: boolean;
  message: string | null;
  changesJson: string | null;
  wooProductId: number | null;
  durationMs: number;
  performedBy: string | null;
  createdAt: string;
}
