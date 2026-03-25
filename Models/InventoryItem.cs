using System;

using System.ComponentModel.DataAnnotations;
namespace LogiTrack.Models
{
    /// <summary>
    /// Represents a single item stored in inventory, including optional location and
    /// an optional relationship to an <see cref="Order"/>.
    /// </summary>
    public class InventoryItem
    {
        [Key]
        /// <summary>
        /// Primary key for the inventory item.
        /// </summary>
        public int ItemId { get; set; }

        /// <summary>
        /// Friendly name or description for the inventory item.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Quantity available in stock.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Optional storage location identifier.
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// Optional foreign key to the owning order when the item is allocated.
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>
        /// Navigation property to the order that currently references this item (if any).
        /// </summary>
        public Order? Order { get; set; }

    }
}
