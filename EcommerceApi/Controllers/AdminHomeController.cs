using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/admin/home")]
    public class AdminHomeController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _db;

        public AdminHomeController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }
        private static bool IsHomeEventVisible(HomeEvent e, DateTime now)
        {
            if (!e.IsActive)
                return false;

            if (e.IsSeasonal && e.AutoActivate)
            {
                if (string.Equals(e.AutoScheduleType, "yearly-fixed", StringComparison.OrdinalIgnoreCase))
                {
                    if (e.MonthStart.HasValue && e.DayStart.HasValue &&
                        e.MonthEnd.HasValue && e.DayEnd.HasValue)
                    {
                        var start = new DateTime(now.Year, e.MonthStart.Value, e.DayStart.Value, 0, 0, 0, DateTimeKind.Utc);
                        var end = new DateTime(now.Year, e.MonthEnd.Value, e.DayEnd.Value, 23, 59, 59, DateTimeKind.Utc);

                        return now >= start && now <= end;
                    }

                    return false;
                }

                if (string.Equals(e.AutoScheduleType, "manual-range", StringComparison.OrdinalIgnoreCase))
                {
                    if (!e.StartDate.HasValue || !e.EndDate.HasValue)
                        return false;

                    return now >= e.StartDate.Value && now <= e.EndDate.Value;
                }
            }

            if (e.StartDate.HasValue && now < e.StartDate.Value)
                return false;

            if (e.EndDate.HasValue && now > e.EndDate.Value)
                return false;

            return true;
        }

        private static string? ValidateHomeEvent(HomeEvent model)
        {
            if (model == null)
                return "Données invalides.";

            if (string.IsNullOrWhiteSpace(model.Title))
                return "Le titre est obligatoire.";

            if (model.StartDate.HasValue && model.EndDate.HasValue && model.StartDate > model.EndDate)
                return "La date de fin doit être supérieure à la date de début.";

            if (model.IsSeasonal)
            {
                if (string.IsNullOrWhiteSpace(model.SeasonKey))
                    return "SeasonKey est obligatoire pour une campagne saisonnière.";

                if (string.IsNullOrWhiteSpace(model.AutoScheduleType))
                    return "AutoScheduleType est obligatoire pour une campagne saisonnière.";

                if (string.Equals(model.AutoScheduleType, "yearly-fixed", StringComparison.OrdinalIgnoreCase))
                {
                    if (!model.MonthStart.HasValue || !model.DayStart.HasValue ||
                        !model.MonthEnd.HasValue || !model.DayEnd.HasValue)
                    {
                        return "Les dates fixes annuelles sont obligatoires.";
                    }

                    if (model.MonthStart < 1 || model.MonthStart > 12 || model.MonthEnd < 1 || model.MonthEnd > 12)
                        return "Les mois doivent être entre 1 et 12.";

                    if (model.DayStart < 1 || model.DayStart > 31 || model.DayEnd < 1 || model.DayEnd > 31)
                        return "Les jours doivent être entre 1 et 31.";
                }

                if (string.Equals(model.AutoScheduleType, "manual-range", StringComparison.OrdinalIgnoreCase))
                {
                    if (!model.StartDate.HasValue || !model.EndDate.HasValue)
                        return "Pour manual-range, les dates début et fin sont obligatoires.";
                }
            }

            return null;
        }
        /* =========================================================
   PROMO BANNER
========================================================= */

        [HttpGet("promo-banner")]
        public async Task<IActionResult> GetPromoBanner()
        {
            var banner = await _db.PromoBanners
                .OrderBy(x => x.Priority)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (banner == null)
                return Ok(null);

            return Ok(banner);
        }

        [HttpPost("promo-banner")]
        public async Task<IActionResult> SavePromoBanner([FromBody] PromoBanner model)
        {
            if (model == null)
                return BadRequest("Données invalides.");

            if (model.StartAt.HasValue && model.EndAt.HasValue && model.StartAt > model.EndAt)
                return BadRequest("La date de début doit être inférieure à la date de fin.");

            var banner = await _db.PromoBanners
                .OrderBy(x => x.Priority)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (banner == null)
            {
                banner = new PromoBanner
                {
                    CreatedAt = DateTime.UtcNow
                };

                _db.PromoBanners.Add(banner);
            }

            banner.Title = (model.Title ?? "").Trim();
            banner.SubTitle = (model.SubTitle ?? "").Trim();
            banner.Description = (model.Description ?? "").Trim();
            banner.PromoCode = (model.PromoCode ?? "").Trim();

            banner.PrimaryButtonText = (model.PrimaryButtonText ?? "").Trim();
            banner.PrimaryButtonLink = (model.PrimaryButtonLink ?? "").Trim();

            banner.SecondaryButtonText = (model.SecondaryButtonText ?? "").Trim();
            banner.SecondaryButtonLink = (model.SecondaryButtonLink ?? "").Trim();

            banner.ImageUrl = (model.ImageUrl ?? "").Trim();
            banner.MobileImageUrl = (model.MobileImageUrl ?? "").Trim();

            banner.BackgroundColor = (model.BackgroundColor ?? "").Trim();
            banner.TextColor = (model.TextColor ?? "").Trim();

            banner.SideTitle = (model.SideTitle ?? "").Trim();
            banner.SideText = (model.SideText ?? "").Trim();

            banner.StartAt = model.StartAt;
            banner.EndAt = model.EndAt;

            banner.BackgroundImageUrl = (model.BackgroundImageUrl ?? "").Trim();
            banner.Theme = string.IsNullOrWhiteSpace(model.Theme) ? "promo" : model.Theme.Trim().ToLowerInvariant();
            banner.AccentColor = string.IsNullOrWhiteSpace(model.AccentColor) ? "#22c55e" : model.AccentColor.Trim();
            banner.Position = string.IsNullOrWhiteSpace(model.Position) ? "after-hero" : model.Position.Trim().ToLowerInvariant();
            banner.Priority = model.Priority <= 0 ? 1 : model.Priority;

            banner.IsActive = model.IsActive;
            banner.DisplayOrder = model.DisplayOrder;
            banner.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(banner);
        }

        [HttpGet("/api/home/promo-banner")]
        public async Task<IActionResult> GetActivePromoBanner()
        {
            var now = DateTime.UtcNow;

            var banner = await _db.PromoBanners
                .Where(x => x.IsActive)
                .Where(x => !x.StartAt.HasValue || x.StartAt.Value <= now)
                .Where(x => !x.EndAt.HasValue || x.EndAt.Value >= now)
                .OrderBy(x => x.Priority)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (banner == null)
                return Ok(null);

            return Ok(banner);
        }


        [HttpGet("reviews")]
        public async Task<IActionResult> GetReviews()
        {
            var reviews = await _db.CustomerReviews
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpPost("reviews")]
        public async Task<IActionResult> SaveReview([FromBody] CustomerReview model)
        {
            if (model == null)
                return BadRequest("Données invalides.");

            model.CreatedAt = DateTime.UtcNow;
            _db.CustomerReviews.Add(model);

            await _db.SaveChangesAsync();
            return Ok(model);
        }

        [HttpGet("sections")]
        public async Task<IActionResult> GetSections()
        {
            var sections = await _db.HomeSections
                .OrderBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            return Ok(sections);
        }

        [HttpPost("sections")]
        public async Task<IActionResult> SaveSection([FromBody] HomeSection model)
        {
            if (model == null)
                return BadRequest("Données invalides.");

            model.SectionKey = (model.SectionKey ?? "").Trim();
            model.Title = (model.Title ?? "").Trim();
            model.SubTitle = (model.SubTitle ?? "").Trim();
            model.Description = (model.Description ?? "").Trim();
            model.LayoutType = string.IsNullOrWhiteSpace(model.LayoutType)
                ? "services-grid"
                : model.LayoutType.Trim().ToLowerInvariant();

            model.CreatedAt = DateTime.UtcNow;

            _db.HomeSections.Add(model);
            await _db.SaveChangesAsync();

            return Ok(model);
        }

        [HttpPut("sections/{id}")]
        public async Task<IActionResult> UpdateSection(int id, [FromBody] HomeSection model)
        {
            var section = await _db.HomeSections.FirstOrDefaultAsync(x => x.Id == id);
            if (section == null)
                return NotFound("Section introuvable.");

            section.SectionKey = (model.SectionKey ?? "").Trim();
            section.Title = (model.Title ?? "").Trim();
            section.SubTitle = (model.SubTitle ?? "").Trim();
            section.Description = (model.Description ?? "").Trim();
            section.DisplayOrder = model.DisplayOrder;
            section.IsActive = model.IsActive;
            section.LayoutType = string.IsNullOrWhiteSpace(model.LayoutType)
                ? "services-grid"
                : model.LayoutType.Trim().ToLowerInvariant();
            section.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(section);
        }

        [HttpDelete("sections/{id}")]
        public async Task<IActionResult> DeleteSection(int id)
        {
            var section = await _db.HomeSections
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (section == null)
                return NotFound("Section introuvable.");

            if (section.Items != null && section.Items.Any())
            {
                _db.HomeSectionItems.RemoveRange(section.Items);
            }

            _db.HomeSections.Remove(section);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Section supprimée." });
        }

        [HttpGet("section-items")]
        public async Task<IActionResult> GetSectionItems()
        {
            var items = await _db.HomeSectionItems
                .OrderBy(x => x.HomeSectionId)
                .ThenBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("section-items")]
        public async Task<IActionResult> SaveSectionItem([FromBody] HomeSectionItem model)
        {
            if (model == null)
                return BadRequest("Données invalides.");

            model.CreatedAt = DateTime.UtcNow;
            _db.HomeSectionItems.Add(model);

            await _db.SaveChangesAsync();
            return Ok(model);
        }

        [HttpPut("section-items/{id}")]
        public async Task<IActionResult> UpdateSectionItem(int id, [FromBody] HomeSectionItem model)
        {
            var item = await _db.HomeSectionItems.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null)
                return NotFound("Item introuvable.");

            item.HomeSectionId = model.HomeSectionId;
            item.ItemType = model.ItemType;
            item.Title = model.Title;
            item.SubTitle = model.SubTitle;
            item.Description = model.Description;
            item.ImageUrl = model.ImageUrl;
            item.IconClass = model.IconClass;
            item.ButtonText = model.ButtonText;
            item.ButtonLink = model.ButtonLink;
            item.BadgeText = model.BadgeText;
            item.PriceText = model.PriceText;
            item.MetaText = model.MetaText;
            item.DisplayOrder = model.DisplayOrder;
            item.IsActive = model.IsActive;
            item.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("section-items/{id}")]
        public async Task<IActionResult> DeleteSectionItem(int id)
        {
            var item = await _db.HomeSectionItems.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null)
                return NotFound("Item introuvable.");

            _db.HomeSectionItems.Remove(item);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Item supprimé." });
        }

        [HttpPost("upload-section-item-image")]
        public async Task<IActionResult> UploadSectionItemImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Aucun fichier reçu.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(ext))
                return BadRequest("Format non autorisé.");

            var folder = Path.Combine(_env.WebRootPath, "uploads", "home-sections");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = $"section_item_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var publicUrl = $"/uploads/home-sections/{fileName}";
            return Ok(new { url = publicUrl });
        }


        [HttpPost("upload-promo-background")]
        public async Task<IActionResult> UploadPromoBackground(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Aucun fichier reçu.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(ext))
                return BadRequest("Format non autorisé.");

            var folder = Path.Combine(_env.WebRootPath, "uploads", "home");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = $"promo_bg_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var publicUrl = $"/uploads/home/{fileName}";
            return Ok(new { url = publicUrl });
        }



        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Aucun fichier reçu.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(ext))
                return BadRequest("Format non autorisé.");

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"avatar_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var publicUrl = $"/uploads/avatars/{fileName}";

            return Ok(new { url = publicUrl });
        }

        [HttpPut("reviews/{id}")]
        public async Task<IActionResult> UpdateReview(int id, [FromBody] CustomerReview model)
        {
            var review = await _db.CustomerReviews.FirstOrDefaultAsync(x => x.Id == id);
            if (review == null)
                return NotFound("Avis introuvable.");

            review.CustomerName = model.CustomerName;
            review.CustomerRole = model.CustomerRole;
            review.City = model.City;
            review.AvatarUrl = model.AvatarUrl;
            review.Rating = model.Rating;
            review.ReviewText = model.ReviewText;
            review.DisplayOrder = model.DisplayOrder;
            review.IsVerified = model.IsVerified;
            review.IsActive = model.IsActive;
            review.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(review);
        }

        [HttpDelete("reviews/{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _db.CustomerReviews.FirstOrDefaultAsync(x => x.Id == id);
            if (review == null)
                return NotFound("Avis introuvable.");

            _db.CustomerReviews.Remove(review);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Avis supprimé." });
        }
    
    /* =========================================================
   EVENTS / FÊTES & ÉVÉNEMENTS
========================================================= */

[HttpGet("events")]
        public async Task<IActionResult> GetEvents()
        {
            var items = await _db.HomeEvents
                .AsNoTracking()
                .OrderByDescending(x => x.IsFeatured)
                .ThenBy(x => x.Priority)
                .ThenBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Subtitle,
                    x.BadgeText,
                    x.DesktopImageUrl,
                    x.MobileImageUrl,
                    x.ButtonText,
                    x.ButtonLink,
                    x.TargetType,
                    x.CategoryId,
                    CategoryName = x.CategoryId != null
                        ? _db.Categories.Where(c => c.Id == x.CategoryId).Select(c => c.Name).FirstOrDefault()
                        : null,
                    x.BackgroundColor,
                    x.TextColor,
                    x.DisplayOrder,
                    x.IsActive,
                    x.IsFeatured,
                    x.StartDate,
                    x.EndDate,
                    x.IsSeasonal,
                    x.SeasonKey,
                    x.AutoScheduleType,
                    x.MonthStart,
                    x.DayStart,
                    x.MonthEnd,
                    x.DayEnd,
                    x.Priority,
                    x.AutoActivate
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("events")]
        public async Task<IActionResult> SaveEvent([FromBody] HomeEvent model)
        {
            var error = ValidateHomeEvent(model);
            if (error != null)
                return BadRequest(error);

            model.Title = (model.Title ?? "").Trim();
            model.Subtitle = (model.Subtitle ?? "").Trim();
            model.BadgeText = (model.BadgeText ?? "").Trim();
            model.DesktopImageUrl = (model.DesktopImageUrl ?? "").Trim();
            model.MobileImageUrl = (model.MobileImageUrl ?? "").Trim();
            model.ButtonText = (model.ButtonText ?? "").Trim();
            model.ButtonLink = (model.ButtonLink ?? "").Trim();
            model.TargetType = string.IsNullOrWhiteSpace(model.TargetType) ? "url" : model.TargetType.Trim().ToLowerInvariant();
            model.BackgroundColor = (model.BackgroundColor ?? "").Trim();
            model.TextColor = (model.TextColor ?? "").Trim();
            model.SeasonKey = string.IsNullOrWhiteSpace(model.SeasonKey) ? null : model.SeasonKey.Trim().ToLowerInvariant();
            model.AutoScheduleType = string.IsNullOrWhiteSpace(model.AutoScheduleType) ? null : model.AutoScheduleType.Trim().ToLowerInvariant();

            model.DisplayOrder = model.DisplayOrder <= 0 ? 1 : model.DisplayOrder;
            model.Priority = model.Priority <= 0 ? 1 : model.Priority;
            model.CreatedAt = DateTime.UtcNow;

            _db.HomeEvents.Add(model);
            await _db.SaveChangesAsync();

            return Ok(model);
        }

        [HttpPut("events/{id}")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] HomeEvent model)
        {
            var item = await _db.HomeEvents.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null)
                return NotFound("Campagne introuvable.");

            var error = ValidateHomeEvent(model);
            if (error != null)
                return BadRequest(error);

            item.Title = (model.Title ?? "").Trim();
            item.Subtitle = (model.Subtitle ?? "").Trim();
            item.BadgeText = (model.BadgeText ?? "").Trim();
            item.DesktopImageUrl = (model.DesktopImageUrl ?? "").Trim();
            item.MobileImageUrl = (model.MobileImageUrl ?? "").Trim();
            item.ButtonText = (model.ButtonText ?? "").Trim();
            item.ButtonLink = (model.ButtonLink ?? "").Trim();
            item.TargetType = string.IsNullOrWhiteSpace(model.TargetType) ? "url" : model.TargetType.Trim().ToLowerInvariant();
            item.CategoryId = model.CategoryId;
            item.BackgroundColor = (model.BackgroundColor ?? "").Trim();
            item.TextColor = (model.TextColor ?? "").Trim();
            item.DisplayOrder = model.DisplayOrder <= 0 ? 1 : model.DisplayOrder;
            item.IsActive = model.IsActive;
            item.IsFeatured = model.IsFeatured;
            item.StartDate = model.StartDate;
            item.EndDate = model.EndDate;

            item.IsSeasonal = model.IsSeasonal;
            item.SeasonKey = string.IsNullOrWhiteSpace(model.SeasonKey) ? null : model.SeasonKey.Trim().ToLowerInvariant();
            item.AutoScheduleType = string.IsNullOrWhiteSpace(model.AutoScheduleType) ? null : model.AutoScheduleType.Trim().ToLowerInvariant();
            item.MonthStart = model.MonthStart;
            item.DayStart = model.DayStart;
            item.MonthEnd = model.MonthEnd;
            item.DayEnd = model.DayEnd;
            item.Priority = model.Priority <= 0 ? 1 : model.Priority;
            item.AutoActivate = model.AutoActivate;

            item.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("events/{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var item = await _db.HomeEvents.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null)
                return NotFound("Campagne introuvable.");

            _db.HomeEvents.Remove(item);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Campagne supprimée." });
        }

        [HttpPost("upload-event-image")]
        public async Task<IActionResult> UploadEventImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Aucun fichier reçu.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(ext))
                return BadRequest("Format non autorisé.");

            var folder = Path.Combine(_env.WebRootPath, "uploads", "home-events");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fileName = $"event_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var publicUrl = $"/uploads/home-events/{fileName}";
            return Ok(new { url = publicUrl });
        }

        [HttpGet("/api/home/events")]
        public async Task<IActionResult> GetHomeEvents()
        {
            var now = DateTime.UtcNow;

            var items = await _db.HomeEvents
                .AsNoTracking()
                .OrderByDescending(x => x.IsFeatured)
                .ThenBy(x => x.Priority)
                .ThenBy(x => x.DisplayOrder)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            var visible = items
                .Where(x => IsHomeEventVisible(x, now))
                .Select(x => new
                {
                    x.Id,
                    x.Title,
                    x.Subtitle,
                    x.BadgeText,
                    x.DesktopImageUrl,
                    x.MobileImageUrl,
                    x.ButtonText,
                    x.ButtonLink,
                    x.TargetType,
                    x.CategoryId,
                    x.BackgroundColor,
                    x.TextColor,
                    x.DisplayOrder,
                    x.IsActive,
                    x.IsFeatured,
                    x.StartDate,
                    x.EndDate,
                    x.IsSeasonal,
                    x.SeasonKey,
                    x.AutoScheduleType,
                    x.MonthStart,
                    x.DayStart,
                    x.MonthEnd,
                    x.DayEnd,
                    x.Priority,
                    x.AutoActivate
                })
                .ToList();

            return Ok(visible);
        }
    }
    }