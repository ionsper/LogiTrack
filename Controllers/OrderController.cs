using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LogiTrack.Models.Auth;
using Microsoft.EntityFrameworkCore;
using LogiTrack.Data;
using LogiTrack.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.Extensions.Caching.Memory;
using LogiTrack.Utilities;

namespace LogiTrack.Controllers
{
    [ApiController]
    [Route("api/orders")]
    /// <summary>
    /// API controller for creating, retrieving and deleting orders.
    /// Only manager users may view or modify orders in most endpoints.
    /// </summary>
    public class OrderController : ControllerBase
    {
        private readonly LogiTrackContext _context;
        private readonly IMemoryCache _cache;

        public OrderController(LogiTrackContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: /api/orders
        /// <summary>
        /// Returns all orders. Cached for a short duration to reduce load.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Roles.Manager)]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            var cacheKey = CacheKeys.OrdersAll;

            if (!_cache.TryGetValue(cacheKey, out IEnumerable<Order>? orders))
            {
                orders = await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.Items)
                    .ToListAsync();

                _cache.Set(cacheKey, orders, CacheOptionsFactory.Short());
            }

            return Ok(orders ?? Array.Empty<Order>());
        }

        // GET: /api/orders/{id}
        /// <summary>
        /// Returns a single order by id. Returns 404 if not found.
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = Roles.Manager)]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var cacheKey = CacheKeys.OrderById(id);

            if (!_cache.TryGetValue(cacheKey, out Order? order))
            {
                order = await _context.Orders
                    .AsNoTracking()
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                {
                    return NotFound(new { message = $"Order with ID {id} not found." });
                }

                _cache.Set(cacheKey, order, CacheOptionsFactory.Short());
            }

            return Ok(order!);
        }

        // POST: /api/orders
        /// <summary>
        /// Creates a new order. Validates that requested inventory item IDs exist.
        /// On success returns 201 with the created order.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = Roles.Manager + "," + Roles.User)]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] Order order)
        {
            if (order == null || order.Items == null)
            {
                return BadRequest();
            }

            // Check if all item IDs exist before placing the order (bulk fetch to avoid N queries)
            var requestedIds = order.Items.Select(i => i.ItemId).ToList();
            if (requestedIds.Any(id => id == 0))
                return BadRequest($"ItemId is required for all items.");

            var distinctIds = requestedIds.Distinct().ToList();
            var existingItems = await _context.InventoryItems.Where(i => distinctIds.Contains(i.ItemId)).ToListAsync();

            var missing = distinctIds.Except(existingItems.Select(i => i.ItemId)).ToList();
            if (missing.Any())
            {
                return BadRequest($"Inventory item(s) not found: {string.Join(',', missing)}");
            }

            // Reconstruct the order items preserving requested multiplicity
            var itemsToAttach = requestedIds.Select(id => existingItems.First(e => e.ItemId == id)).ToList();
            order.Items = itemsToAttach;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Invalidate orders cache
            _cache.Remove(CacheKeys.OrdersAll);
            _cache.Remove(CacheKeys.OrderById(order.OrderId));

            return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, order);
        }

        // DELETE: /api/orders/{id}
        /// <summary>
        /// Deletes an order and detaches items by setting their OrderId to null.
        /// Requires Manager role.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null)
            {
                return NotFound(new { message = $"Order with ID {id} not found." });
            }

            // Optionally, set OrderId of items to null (detach items from order)
            foreach (var item in order.Items)
            {
                item.OrderId = null;
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            // Invalidate orders cache
            _cache.Remove(CacheKeys.OrdersAll);
            _cache.Remove(CacheKeys.OrderById(id));

            return NoContent();
        }
    }
}
