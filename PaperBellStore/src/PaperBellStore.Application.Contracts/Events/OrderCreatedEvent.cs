using System;
using Volo.Abp.EventBus;

namespace PaperBellStore.Events;

/// <summary>
/// 订单创建事件
/// </summary>
[EventName("PaperBellStore.Orders.OrderCreated")] // 可选：自定义事件名称
public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string? OrderNumber { get; set; }
}

