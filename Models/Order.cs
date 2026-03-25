using System;
using System.Collections.Generic;
using System.Linq;

namespace LogiTrack.Models
{
    /// <summary>
    /// Represents a customer order which can contain multiple inventory items.
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Primary key for the order.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Customer name for the order.
        /// </summary>
        public string? CustomerName { get; set; }

        /// <summary>
        /// When the order was placed.
        /// </summary>
        public DateTime DatePlaced { get; set; }

        /// <summary>
        /// Items included in the order. Preserves multiplicity through repeated entries.
        /// </summary>
        public List<InventoryItem> Items { get; set; } = new List<InventoryItem>();

        /// <summary>
        /// Adds an item to the order.
        /// </summary>
        public void AddItem(InventoryItem item)
        {
            Items.Add(item);
        }

        /// <summary>
        /// Removes the first occurrence of an item with the given id from the order.
        /// </summary>
        public void RemoveItem(int itemId)
        {
            var item = Items.FirstOrDefault(i => i.ItemId == itemId);
            if (item != null)
            {
                Items.Remove(item);
            }
        }

        /// <summary>
        /// Returns a short human readable summary for display in logs or admin UIs.
        /// </summary>
        public string GetOrderSummary()
        {
            return $"Order #{OrderId} for {CustomerName} | Items: {Items.Count} | Placed: {DatePlaced:M/d/yyyy}";
        }
    }
}
