using Microsoft.EntityFrameworkCore;
using EcommerceApi.Models;

namespace EcommerceApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AdminDeviceToken> AdminDeviceTokens => Set<AdminDeviceToken>();
    public DbSet<VendorDailyStat> VendorDailyStats => Set<VendorDailyStat>();
    public DbSet<VendorProductDailyStat> VendorProductDailyStats => Set<VendorProductDailyStat>();
    public DbSet<VendorWalletTransaction> VendorWalletTransactions => Set<VendorWalletTransaction>();
    public DbSet<VendorAccount> VendorAccounts => Set<VendorAccount>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<VendorUser> VendorUsers => Set<VendorUser>();
    public DbSet<Notification> Notifications => Set<Notification>();
    // Variants
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<VariantAxis> VariantAxes => Set<VariantAxis>();
    public DbSet<VariantValue> VariantValues => Set<VariantValue>();
    public DbSet<ProductVariantValue> ProductVariantValues => Set<ProductVariantValue>();

    // Attributs dynamiques (NOUVEAU système)
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductAttributeOption> ProductAttributeOptions => Set<ProductAttributeOption>();
    public DbSet<CategoryAttribute> CategoryAttributes => Set<CategoryAttribute>();
    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
    public DbSet<AdminNotification> AdminNotifications => Set<AdminNotification>();
    public DbSet<VendorPayoutBatch> VendorPayoutBatches => Set<VendorPayoutBatch>();
    public DbSet<VendorPayout> VendorPayouts => Set<VendorPayout>();
    public DbSet<VendorPayoutSale> VendorPayoutSales => Set<VendorPayoutSale>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<CustomerPasswordReset> CustomerPasswordResets => Set<CustomerPasswordReset>();
    public DbSet<CategoryCommissionRule> CategoryCommissionRules => Set<CategoryCommissionRule>();
    public DbSet<AdminWalletTransaction> AdminWalletTransactions => Set<AdminWalletTransaction>();
    public DbSet<ReturnImage> ReturnImages => Set<ReturnImage>();
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<ReturnItem> ReturnItems => Set<ReturnItem>();
    public DbSet<HeroSlide> HeroSlides => Set<HeroSlide>();
    public DbSet<City> Cities { get; set; }
    public DbSet<District> Districts { get; set; }
    public DbSet<Neighborhood> Neighborhoods { get; set; }
    public DbSet<ProductReview> ProductReviews { get; set; } = null!;
    public DbSet<FlashDeal> FlashDeals { get; set; }
    public DbSet<HomeEventCampaign> HomeEventCampaigns { get; set; }
    public DbSet<HomeSection> HomeSections => Set<HomeSection>();
    public DbSet<HomeSectionItem> HomeSectionItems => Set<HomeSectionItem>();
    public DbSet<PromoBanner> PromoBanners => Set<PromoBanner>();
    public DbSet<HomeEvent> HomeEvents { get; set; }
    public DbSet<VendorPasswordReset> VendorPasswordResets => Set<VendorPasswordReset>();
    public DbSet<CustomerReview> CustomerReviews => Set<CustomerReview>();
    public DbSet<HomePromoBanner> HomePromoBanners { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var b = modelBuilder;



        modelBuilder.Entity<OrderItem>()
    .HasOne(oi => oi.Variant)
    .WithMany()
    .HasForeignKey(oi => oi.VariantId)
    .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<VendorPasswordReset>()
    .HasOne(x => x.Vendor)
    .WithMany()
    .HasForeignKey(x => x.VendorId)
    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CustomerPasswordReset>(e =>
        {
            e.ToTable("CustomerPasswordResets");
            e.HasKey(x => x.Id);

            e.Property(x => x.LoginValue).HasMaxLength(150).IsRequired();
            e.Property(x => x.ResetCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.IsUsed).HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            e.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<HomeEvent>()
    .HasOne(x => x.Category)
    .WithMany()
    .HasForeignKey(x => x.CategoryId)
    .OnDelete(DeleteBehavior.SetNull);


        modelBuilder.Entity<HomeEvent>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(150);
            entity.Property(x => x.Subtitle).HasMaxLength(300);
            entity.Property(x => x.BadgeText).HasMaxLength(80);
            entity.Property(x => x.DesktopImageUrl).HasMaxLength(500);
            entity.Property(x => x.MobileImageUrl).HasMaxLength(500);
            entity.Property(x => x.ButtonText).HasMaxLength(80);
            entity.Property(x => x.ButtonLink).HasMaxLength(500);
            entity.Property(x => x.TargetType).HasMaxLength(30);
            entity.Property(x => x.BackgroundColor).HasMaxLength(30);
            entity.Property(x => x.TextColor).HasMaxLength(30);
        });


        modelBuilder.Entity<HomeEventCampaign>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Subtitle).HasMaxLength(300);
            entity.Property(x => x.BadgeText).HasMaxLength(80);
            entity.Property(x => x.DesktopImageUrl).HasMaxLength(500);
            entity.Property(x => x.MobileImageUrl).HasMaxLength(500);
            entity.Property(x => x.ButtonText).HasMaxLength(80);
            entity.Property(x => x.ButtonLink).HasMaxLength(500);
            entity.Property(x => x.TargetType).HasMaxLength(30).HasDefaultValue("url");
            entity.Property(x => x.BackgroundColor).HasMaxLength(30);
            entity.Property(x => x.TextColor).HasMaxLength(30);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(x => x.IsActive);
            entity.HasIndex(x => x.DisplayOrder);
            entity.HasIndex(x => x.IsFeatured);

            entity.HasOne(x => x.Category)
                  .WithMany()
                  .HasForeignKey(x => x.CategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });


        modelBuilder.Entity<HomeSection>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SectionKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.Title).HasMaxLength(250);
            e.Property(x => x.SubTitle).HasMaxLength(500);
            e.Property(x => x.PrimaryButtonText).HasMaxLength(120);
            e.Property(x => x.PrimaryButtonLink).HasMaxLength(500);
            e.Property(x => x.SecondaryButtonText).HasMaxLength(120);
            e.Property(x => x.SecondaryButtonLink).HasMaxLength(500);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.BadgeText).HasMaxLength(120);
            e.Property(x => x.BackgroundColor).HasMaxLength(30);
            e.Property(x => x.TextColor).HasMaxLength(30);

            e.HasIndex(x => x.SectionKey).IsUnique();

            e.HasMany(x => x.Items)
             .WithOne(x => x.HomeSection)
             .HasForeignKey(x => x.HomeSectionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HomeSectionItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ItemType).HasMaxLength(50).IsRequired();
            e.Property(x => x.Title).HasMaxLength(250);
            e.Property(x => x.SubTitle).HasMaxLength(250);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.IconClass).HasMaxLength(120);
            e.Property(x => x.ButtonText).HasMaxLength(120);
            e.Property(x => x.ButtonLink).HasMaxLength(500);
            e.Property(x => x.BadgeText).HasMaxLength(120);
            e.Property(x => x.PriceText).HasMaxLength(120);
            e.Property(x => x.MetaText).HasMaxLength(200);
        });

        modelBuilder.Entity<PromoBanner>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(250).IsRequired();
            e.Property(x => x.SubTitle).HasMaxLength(500);
            e.Property(x => x.PromoCode).HasMaxLength(120);
            e.Property(x => x.PrimaryButtonText).HasMaxLength(120);
            e.Property(x => x.PrimaryButtonLink).HasMaxLength(500);
            e.Property(x => x.SecondaryButtonText).HasMaxLength(120);
            e.Property(x => x.SecondaryButtonLink).HasMaxLength(500);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.MobileImageUrl).HasMaxLength(500);
            e.Property(x => x.BackgroundColor).HasMaxLength(30);
            e.Property(x => x.TextColor).HasMaxLength(30);
        });

        modelBuilder.Entity<CustomerReview>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.CustomerName).HasMaxLength(150).IsRequired();
            e.Property(x => x.CustomerRole).HasMaxLength(150);
            e.Property(x => x.ReviewText).HasMaxLength(1000).IsRequired();
            e.Property(x => x.AvatarUrl).HasMaxLength(500);
            e.Property(x => x.ProductName).HasMaxLength(200);
            e.Property(x => x.ProductLink).HasMaxLength(500);
            e.Property(x => x.City).HasMaxLength(120);
        });


        modelBuilder.Entity<FlashDeal>()
    .HasOne(x => x.Product)
    .WithMany()
    .HasForeignKey(x => x.ProductId)
    .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<District>()
    .HasOne(d => d.City)
    .WithMany(c => c.Districts)
    .HasForeignKey(d => d.CityId)
    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Neighborhood>()
            .HasOne<District>()
            .WithMany()
            .HasForeignKey(n => n.DistrictId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // ROLES / PERMISSIONS
        // =========================
        b.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(x => x.Id);

            e.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            e.Property(x => x.Code)
                .HasMaxLength(100)
                .IsRequired();

            e.Property(x => x.Description)
                .HasMaxLength(255);

            e.Property(x => x.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<Permission>(e =>
        {
            e.ToTable("Permissions");
            e.HasKey(x => x.Id);

            e.Property(x => x.Code)
                .HasMaxLength(150)
                .IsRequired();

            e.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            e.Property(x => x.Category)
                .HasMaxLength(100);

            e.Property(x => x.Description)
                .HasMaxLength(255);

            e.Property(x => x.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<RolePermission>(e =>
        {
            e.ToTable("RolePermissions");
            e.HasKey(x => new { x.RoleId, x.PermissionId });

            e.HasOne(x => x.Role)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Permission)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<User>(e =>
        {
            e.HasOne(x => x.RoleRef)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.RoleId);
            e.HasIndex(x => x.Username).IsUnique();
        });







        b.Entity<ReturnRequest>()
    .HasMany(r => r.Images)
    .WithOne(i => i.ReturnRequest)
    .HasForeignKey(i => i.ReturnRequestId)
    .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ReturnImage>().HasIndex(x => x.ReturnRequestId);

        b.Entity<VendorProductDailyStat>()
    .Property(x => x.Revenue)
    .HasPrecision(18, 2);

        b.Entity<ProductAttributeValue>()
            .Property(x => x.ValueDecimal)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>().Property(x => x.VendorAmount).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(x => x.PlatformFee).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(x => x.CommissionRate).HasPrecision(18, 4);
        modelBuilder.Entity<OrderItem>().Property(x => x.UnitPriceSnapshot).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(x => x.CommissionAmount).HasPrecision(18, 2);
        modelBuilder.Entity<OrderItem>().Property(x => x.VendorNetAmount).HasPrecision(18, 2);


        b.Entity<ReturnRequest>().Property(x => x.RefundAmount).HasPrecision(18, 2);
        b.Entity<ReturnItem>().Property(x => x.RefundLineAmount).HasPrecision(18, 2);
        b.Entity<ReturnItem>().Property(x => x.UnitPriceSnapshot).HasPrecision(18, 2);
        b.Entity<ReturnItem>().Property(x => x.VendorAmountSnapshot).HasPrecision(18, 2);
        b.Entity<ReturnItem>().Property(x => x.PlatformFeeSnapshot).HasPrecision(18, 2);
        b.Entity<ReturnItem>().Property(x => x.CommissionRateSnapshot).HasPrecision(18, 4);
        b.Entity<ReturnItem>().Property(x => x.CommissionAmountSnapshot).HasPrecision(18, 2);
        b.Entity<ReturnItem>().Property(x => x.VendorNetAmountSnapshot).HasPrecision(18, 2);



        b.Entity<ReturnRequest>()
    .HasMany(r => r.Items)
    .WithOne(i => i.ReturnRequest)
    .HasForeignKey(i => i.ReturnRequestId)
    .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ReturnRequest>()
            .HasOne(r => r.Order)
            .WithMany() // ou Order.ReturnRequests si tu ajoutes une nav
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<ReturnRequest>()
            .HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<ReturnItem>()
            .HasOne(i => i.OrderItem)
            .WithMany()
            .HasForeignKey(i => i.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index utiles
        b.Entity<ReturnRequest>().HasIndex(x => x.OrderId);
        b.Entity<ReturnRequest>().HasIndex(x => x.CustomerId);
        b.Entity<ReturnRequest>().HasIndex(x => x.Status);
        b.Entity<ReturnItem>().HasIndex(x => x.OrderItemId);


        b.Entity<ProductReview>(e =>
        {
            e.ToTable("ProductReviews");
            e.Property(x => x.Rating).HasColumnType("tinyint");
            e.Property(x => x.Title).HasMaxLength(120);
            e.Property(x => x.Comment).HasMaxLength(2000);

            e.HasIndex(x => new { x.ProductId, x.CustomerId }).IsUnique();
            e.HasIndex(x => new { x.ProductId, x.IsDeleted, x.CreatedAtUtc });

            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
            e.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId);
        });

        // Products stats
        b.Entity<Product>(e =>
        {
            e.Property(x => x.RatingAvg).HasColumnType("decimal(3,2)");
            e.Property(x => x.RatingCount);
        });
    




    // precision money
    modelBuilder.Entity<VendorPayout>()
            .Property(x => x.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<VendorPayoutBatch>()
            .Property(x => x.Status)
            .HasMaxLength(30);

        modelBuilder.Entity<VendorPayout>()
            .HasOne(p => p.Batch)
            .WithMany(b => b.Payouts)
            .HasForeignKey(p => p.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VendorPayout>()
            .HasOne(p => p.Vendor)
            .WithMany()
            .HasForeignKey(p => p.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        // évite double attache d'un orderItem dans 2 payouts
        modelBuilder.Entity<OrderItem>()
            .HasIndex(x => x.VendorPayoutId);






        b.Entity<VendorPayoutBatch>()
      .HasIndex(x => new { x.PeriodStart, x.PeriodEnd })
      .IsUnique();

        b.Entity<OrderItem>()
            .HasOne(x => x.VendorPayout)
            .WithMany()
            .HasForeignKey(x => x.VendorPayoutId)
            .OnDelete(DeleteBehavior.SetNull);


        modelBuilder.Entity<VendorWalletTransaction>()
    .Property(x => x.Amount)
    .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
    .HasOne(oi => oi.Product)
    .WithMany()
    .HasForeignKey(oi => oi.ProductId)
    .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<VendorDailyStat>()
    .HasIndex(x => new { x.VendorId, x.Day })
    .IsUnique();

        modelBuilder.Entity<VendorProductDailyStat>()
            .HasIndex(x => new { x.VendorId, x.ProductId, x.Day })
            .IsUnique();


        b.Entity<AdminWalletTransaction>()
 .Property(x => x.Amount)
 .HasColumnType("decimal(18,2)");

        b.Entity<Order>()
         .Property(x => x.AdminCommissionTotal)
         .HasColumnType("decimal(18,2)");


        // SETTINGS
        modelBuilder.Entity<Setting>(e =>
        {
            e.HasKey(x => x.Key);                 // ✅ PAS Id
            e.Property(x => x.Key).HasMaxLength(120);
            e.Property(x => x.Value).HasMaxLength(400);
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // CATEGORY COMMISSION RULES
        b.Entity<CategoryCommissionRule>(e =>
        {
            e.ToTable("CategoryCommissionRules");
            e.HasKey(x => x.Id);

            e.Property(x => x.CommissionRate)
                .HasColumnType("decimal(5,4)");

            e.HasCheckConstraint("CK_CategoryCommissionRules_Rate", "[CommissionRate] >= 0 AND [CommissionRate] <= 1");
            e.HasCheckConstraint("CK_CategoryCommissionRules_Period", "[EffectiveTo] IS NULL OR [EffectiveFrom] IS NULL OR [EffectiveTo] > [EffectiveFrom]");

            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.Category)
                .WithMany() // ou .WithMany(c => c.CommissionRules) si tu ajoutes la nav
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.CategoryId, x.IsActive, x.EffectiveFrom, x.EffectiveTo })
                .HasDatabaseName("IX_CategoryCommissionRules_Category_Active");
        });



        modelBuilder.Entity<AdminWalletTransaction>()
    .HasOne(x => x.OrderItem)
    .WithMany() // pas de collection dans OrderItem
    .HasForeignKey(x => x.OrderItemId)
    .OnDelete(DeleteBehavior.NoAction);


        // =========================
        // INDEXES UNIQUES
        // =========================
        b.Entity<Product>().HasIndex(p => p.Slug).IsUnique();
        b.Entity<Category>().HasIndex(c => c.Slug).IsUnique();

        b.Entity<Product>()
            .HasIndex(p => p.Sku)
            .IsUnique()
            .HasFilter("[Sku] IS NOT NULL");

        b.Entity<Customer>().HasIndex(x => x.Phone).IsUnique();
        b.Entity<Customer>().HasIndex(x => x.Email);

        b.Entity<AdminDeviceToken>().HasIndex(x => x.Token).IsUnique();

        // =========================
        // CATEGORY TREE
        // =========================
        b.Entity<Category>()
            .HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // =========================
        // FAVORITES
        // =========================
        b.Entity<Favorite>()
            .HasIndex(x => new { x.CustomerId, x.ProductId })
            .IsUnique();

        b.Entity<Favorite>()
            .HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Favorite>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // PRODUCT IMAGES
        // =========================
        b.Entity<ProductImage>()
            .HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // VENDORS
        // =========================
        b.Entity<Vendor>().HasIndex(x => x.Slug).IsUnique();

        b.Entity<VendorUser>()
  .HasIndex(x => new { x.VendorId, x.Username })
  .IsUnique();

        b.Entity<VendorUser>()
         .HasIndex(x => x.Email)
         .IsUnique();


        b.Entity<Product>()
            .HasOne(p => p.Vendor)
            .WithMany(v => v.Products)
            .HasForeignKey(p => p.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<OrderItem>()
            .HasOne(x => x.Vendor)
            .WithMany()
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<OrderItem>()
            .HasIndex(x => new { x.OrderId, x.VendorId });

        // =========================
        // VENDOR ACCOUNT (Wallet)
        // =========================
        b.Entity<VendorAccount>(e =>
        {
            e.ToTable("VendorAccounts");
            e.HasKey(x => x.Id);

            e.HasIndex(x => x.VendorId).IsUnique(); // 1 wallet par vendeur

            e.Property(x => x.WalletBalance)
             .HasPrecision(18, 2)
             .HasDefaultValue(0m);

            // ✅ relation 1-1 explicite + FK explicite
            e.HasOne(x => x.Vendor)
             .WithOne(v => v.Account)
             .HasForeignKey<VendorAccount>(x => x.VendorId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Optionnel mais recommandé (precision)
        b.Entity<Vendor>()
          .Property(v => v.CommissionRate)
          .HasPrecision(5, 2);

        // =========================
        // VARIANTS
        // =========================
        b.Entity<ProductVariant>()
            .HasIndex(x => new { x.ProductId, x.Key1, x.Key2 })
            .IsUnique();

        b.Entity<VariantAxis>().HasIndex(x => x.Key).IsUnique();

        b.Entity<VariantValue>()
            .HasIndex(x => new { x.AxisId, x.Value })
            .IsUnique();

        b.Entity<ProductVariantValue>()
            .HasIndex(x => new { x.ProductVariantId, x.AxisId })
            .IsUnique();

        b.Entity<ProductVariantValue>()
            .HasOne(x => x.ProductVariant)
            .WithMany(v => v.Values)
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<VariantValue>()
            .HasOne(vv => vv.Axis)
            .WithMany(a => a.Values)
            .HasForeignKey(vv => vv.AxisId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ProductVariantValue>()
            .HasOne(x => x.Axis)
            .WithMany()
            .HasForeignKey(x => x.AxisId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<ProductVariantValue>()
            .HasOne(x => x.Value)
            .WithMany()
            .HasForeignKey(x => x.ValueId)
            .OnDelete(DeleteBehavior.Restrict);




        // =========================
        // ATTRIBUTS DYNAMIQUES (PROPRE)
        // =========================

        // Tables SQL
        b.Entity<ProductAttribute>().ToTable("Attributes", "dbo");
        b.Entity<ProductAttributeOption>().ToTable("AttributeOptions", "dbo");
        b.Entity<ProductAttributeValue>().ToTable("ProductAttributeValues", "dbo");
        b.Entity<CategoryAttribute>().ToTable("CategoryAttributes", "dbo");


        // ProductAttribute : Code unique
        b.Entity<ProductAttribute>()
            .HasIndex(x => x.Code)
            .IsUnique();

        // Enum stocké en string une seule fois (pas de doublon)
        b.Entity<ProductAttribute>()
     .Property(x => x.DataType)
     .HasColumnType("nvarchar(30)");


        // ProductAttributeOption : (AttributeId, Value) unique
        b.Entity<ProductAttributeOption>()
            .HasIndex(x => new { x.ProductAttributeId, x.Value })
            .IsUnique();

        // DB column name: AttributeId (au lieu de ProductAttributeId)
        b.Entity<ProductAttributeOption>()
            .Property(x => x.ProductAttributeId)
            .HasColumnName("AttributeId");

        // CategoryAttribute : (CategoryId, AttributeId) unique / PK
        b.Entity<CategoryAttribute>()
            .HasKey(x => new { x.CategoryId, x.AttributeId });

        b.Entity<CategoryAttribute>()
     .HasOne(x => x.Category)
     .WithMany(c => c.CategoryAttributes)
     .HasForeignKey(x => x.CategoryId)
     .OnDelete(DeleteBehavior.Cascade);

        b.Entity<CategoryAttribute>()
            .HasOne(x => x.Attribute) // <-- nav vers ProductAttribute
            .WithMany()
            .HasForeignKey(x => x.AttributeId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProductAttributeValue : index utiles
        b.Entity<ProductAttributeValue>()
            .HasIndex(x => new { x.ProductId, x.AttributeId });

        b.Entity<ProductAttributeValue>()
            .HasIndex(x => new { x.ProductVariantId, x.AttributeId });

        // Column name AttributeId (si ta table utilise AttributeId)
        b.Entity<ProductAttributeValue>()
            .Property(x => x.AttributeId)
            .HasColumnName("AttributeId");

        b.Entity<ProductAttributeValue>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<HeroSlide>(e =>
        {
            e.ToTable("HeroSlides");

            e.HasKey(x => x.Id);

            e.Property(x => x.Title).HasMaxLength(220).IsRequired();
            e.Property(x => x.Subtitle).HasMaxLength(1200);

            e.Property(x => x.BadgeText).HasMaxLength(120);
            e.Property(x => x.SmallTag).HasMaxLength(120);

            e.Property(x => x.PrimaryButtonText).HasMaxLength(80);
            e.Property(x => x.PrimaryButtonUrl).HasMaxLength(400);

            e.Property(x => x.SecondaryButtonText).HasMaxLength(80);
            e.Property(x => x.SecondaryButtonUrl).HasMaxLength(400);

            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.Theme).HasMaxLength(50);
            e.Property(x => x.AccentColor).HasMaxLength(30);
            e.Property(x => x.HighlightText).HasMaxLength(150);

            e.HasIndex(x => new { x.IsActive, x.DisplayOrder });
        });
    }
}
