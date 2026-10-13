namespace TimeVerse.Models
{
    public enum MovementType
    {
        Automatic = 1,
        Quartz = 2,
        Chronograph = 3,
        Smart = 4,
        Mechanical = 5
    }

    public enum WatchGender
    {
        Men = 1,
        Women = 2,
        Unisex = 3
    }

    public enum InquiryStatus
    {
        New = 1,
        InProgress = 2,
        Resolved = 3
    }

    public enum OrderStatus
    {
        Pending = 1,
        Confirmed = 2,
        Processing = 3,
        Shipped = 4,
        Delivered = 5,
        Cancelled = 6
    }

    public enum PaymentMethod
    {
        UPI = 1,
        CreditCard = 2,
        NetBanking = 3,
        CashOnDelivery = 4
    }

    public enum PaymentStatus
    {
        Pending = 1,
        Completed = 2,
        Failed = 3,
        Refunded = 4
    }
}
