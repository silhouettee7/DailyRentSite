using Domain.Models.Enums;
using Domain.Models.Payment;

namespace Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public Guid ExternalId { get; set; }
    public PaymentStatus Status { get; set; }
    public bool Paid { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastCheckedAt { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
}