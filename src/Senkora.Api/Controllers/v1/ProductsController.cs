using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Senkora.Application.Common.Interfaces;
using Senkora.Application.Common.Models;
using Senkora.Application.Features.Products.Commands;
using Senkora.Application.Features.Products.Queries;
using Senkora.Domain.ValueObjects;

namespace Senkora.Api.Controllers.v1;

/// <summary>Ürün yönetimi ve Logo→WooCommerce senkronizasyonu</summary>
[ApiController]
[Route("api/v1/products")]
[Authorize]
[Produces("application/json")]
public sealed class ProductsController(
    IMediator mediator,
    ICurrentUser currentUser) : ControllerBase
{
    private Guid TenantId =>
        HttpContext.Items["TenantId"] as Guid? ?? currentUser.TenantId;

    /// <summary>Ürün listesi (filtre destekli, sayfalı)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid?   wooStoreId = null,
        [FromQuery] string? status     = null,
        [FromQuery] string? search     = null,
        [FromQuery] int     page       = 1,
        [FromQuery] int     pageSize   = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetProductMappingsQuery(TenantId, wooStoreId, status, search, page, pageSize), ct);
        return Ok(ApiResponse<PagedResult<ProductMappingDto>>.Ok(result.Data!));
    }

    /// <summary>Tek ürün zenginleştirme verisi</summary>
    [HttpGet("{id:guid}/enrichment")]
    public async Task<IActionResult> GetEnrichment(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetProductEnrichmentQuery(TenantId, id), ct);
        if (!result.IsSuccess)
            return NotFound(ApiResponse<object>.Fail(result.Error!));
        return Ok(ApiResponse<ProductEnrichmentDto>.Ok(result.Data!));
    }

    /// <summary>
    /// Logo'daki tum yeni urunleri ice aktarir. Mevcut kayitlara dokunmaz.
    /// Otomatik sayfalama yapar, buyuk katalogda uzun surebilir.
    /// </summary>
    [HttpPost("import")]
    [Authorize(Policy = "SyncManager")]
    public async Task<IActionResult> Import(
        [FromBody] ProductImportRequest request, CancellationToken ct)
    {
        var cmd = new ImportLogoProductsCommand(
            TenantId, request.LogoConnectionId, request.WooStoreId, request.MaxItems);
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        return Ok(ApiResponse<ImportResult>.Ok(result.Data!));
    }

    /// <summary>
    /// Mevcut urunlerin Logo verilerini gunceller (fiyat, stok, ad...).
    /// Gorsel/kategori/etiket gibi portal verilerine dokunmaz.
    /// previewOnly=true ile once degisiklik ozeti alinabilir.
    /// </summary>
    [HttpPost("refresh")]
    [Authorize(Policy = "SyncManager")]
    public async Task<IActionResult> Refresh(
        [FromBody] ProductRefreshRequest request, CancellationToken ct)
    {
        var cmd = new RefreshLogoProductsCommand(
            TenantId, request.LogoConnectionId, request.WooStoreId, request.PreviewOnly);
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        return Ok(ApiResponse<RefreshResult>.Ok(result.Data!));
    }

    /// <summary>Secilen urun eslemelerini siler</summary>
    [HttpPost("delete")]
    [Authorize(Policy = "SyncManager")]
    public async Task<IActionResult> DeleteProducts(
        [FromBody] DeleteProductsRequest request, CancellationToken ct)
    {
        var cmd = new DeleteProductsCommand(
            TenantId, request.Ids ?? [], request.DeleteAll, request.StatusFilter);
        var result = await mediator.Send(cmd, ct);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        return Ok(ApiResponse<int>.Ok(result.Data, $"{result.Data} urun silindi."));
    }

    /// <summary>Urun aktarim ve degisiklik gecmisi</summary>
    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetProductHistoryQuery(TenantId, id), ct);
        return Ok(ApiResponse<List<ProductHistoryDto>>.Ok(result.Data!));
    }

    /// <summary>Ürün zenginleştirme verilerini kaydet (görsel, kategori, vb.)</summary>
    [HttpPut("{id:guid}/enrichment")]
    [Authorize(Policy = "SyncManager")]
    public async Task<IActionResult> SaveEnrichment(
        Guid id, [FromBody] ProductEnrichment enrichment, CancellationToken ct)
    {
        var result = await mediator.Send(
            new EnrichProductCommand(TenantId, id, enrichment), ct);
        if (!result.IsSuccess)
            return NotFound(ApiResponse<object>.Fail(result.Error!));
        return Ok(ApiResponse.Ok("Zenginleştirme kaydedildi."));
    }

    /// <summary>Tek ürünü WooCommerce'e gönder</summary>
    [HttpPost("{id:guid}/sync")]
    [Authorize(Policy = "SyncManager")]
    public async Task<IActionResult> SyncToWoo(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(
            new SyncProductToWooCommand(TenantId, id), ct);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        return Ok(ApiResponse<long>.Ok(result.Data!, "WooCommerce'e gönderildi."));
    }

    /// <summary>WooCommerce kategorilerini listele</summary>
    [HttpGet("woo-categories")]
    public async Task<IActionResult> GetWooCategories(
        [FromQuery] Guid wooStoreId, CancellationToken ct)
    {
        var result = await mediator.Send(
            new GetWooCategoriesQuery(TenantId, wooStoreId), ct);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Error!));
        return Ok(ApiResponse<List<WooCategoryDto>>.Ok(result.Data!));
    }

    /// <summary>Görsel yükle (multipart/form-data)</summary>
    [HttpPost("{id:guid}/images")]
    [Authorize(Policy = "SyncManager")]
    public async Task<IActionResult> UploadImage(
        Guid id, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("Dosya boş olamaz."));

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext     = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest(ApiResponse<object>.Fail(
                "Sadece JPG, PNG ve WebP dosyaları kabul edilir."));

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(ApiResponse<object>.Fail("Dosya 10MB'dan büyük olamaz."));

        var result = await mediator.Send(
            new UploadProductImageCommand(TenantId, id, file, ext), ct);
        if (!result.IsSuccess)
            return BadRequest(ApiResponse<object>.Fail(result.Error!));

        return Ok(ApiResponse<string>.Ok(result.Data!, "Görsel yüklendi."));
    }

    /// <summary>Tani: Logo REST'ten gelen ham yaniti gosterir</summary>
    [HttpGet("diagnose-logo")]
    [Authorize(Policy = "SyncManager")]
    public async Task<IActionResult> DiagnoseLogo(
        [FromQuery] Guid logoConnectionId,
        [FromQuery] int limit = 3,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new DiagnoseLogoFetchQuery(TenantId, logoConnectionId, limit), ct);
        return Ok(ApiResponse<LogoFetchDiagnostics>.Ok(result.Data!));
    }
}

public sealed record ProductImportRequest(
    Guid LogoConnectionId,
    Guid WooStoreId,
    int  MaxItems = 0);   // 0 = tum katalog

public sealed record DeleteProductsRequest(
    List<Guid>? Ids          = null,
    bool        DeleteAll    = false,
    string?     StatusFilter = null);

public sealed record ProductRefreshRequest(
    Guid LogoConnectionId,
    Guid WooStoreId,
    bool PreviewOnly = false);
