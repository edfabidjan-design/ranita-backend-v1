namespace EcommerceApi.Dtos;

// l'admin saisit 10 = 10%
public record GlobalCommissionDto(decimal Percent);

// l'admin saisit 12 = 12% + active/désactive
public record CategoryCommissionUpsertDto(decimal Percent, bool IsActive = true);