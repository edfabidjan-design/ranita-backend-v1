using EcommerceApi.Data;
using EcommerceApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly OrderPaymentFinalizer _finalizer;

    public PaymentsController(AppDbContext db, OrderPaymentFinalizer finalizer)
    {
        _db = db;
        _finalizer = finalizer;
    }

    [Authorize(Roles = "Client,SuperAdmin,Admin,Manager")]
    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmPaymentDto dto)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId);
        if (order == null) return NotFound(new { ok = false, message = "Commande introuvable." });

        if (order.IsPaid)
            return Ok(new { ok = true, message = "Déjà payée.", orderId = dto.OrderId });

        var paymentRef = string.IsNullOrWhiteSpace(dto.PaymentRef)
            ? $"manual-{Guid.NewGuid():N}"
            : dto.PaymentRef!.Trim();

        await _finalizer.FinalizePaidOrderAsync(dto.OrderId, paymentRef);

        return Ok(new { ok = true, message = "Paiement confirmé + commission figée.", orderId = dto.OrderId });
    }
}

public class ConfirmPaymentDto
{
    public int OrderId { get; set; }
    public string? PaymentRef { get; set; }
}