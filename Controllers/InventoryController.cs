using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LogiTrack.Models.Auth;
using Microsoft.EntityFrameworkCore;
using LogiTrack.Data;
using Microsoft.Extensions.Caching.Memory;
using System;
using LogiTrack.Models;
using LogiTrack.Utilities;

namespace LogiTrack.Controllers
{
    [ApiController]
    [Route("api/inventory")]
    /// <summary>
    /// API controller for managing inventory items. Provides endpoints to list, add and delete inventory.
    /// Uses in-memory caching to reduce database load for read operations.
    /// </summary>
    public class InventoryController : ControllerBase
    {
        private readonly LogiTrackContext _context;
        private readonly IMemoryCache _cache;

        public InventoryController(LogiTrackContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: /api/inventory
        /// <summary>
        /// Returns all inventory items. Cached for short periods to improve performance.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Roles.Manager + "," + Roles.User)]
        public async Task<ActionResult<IEnumerable<InventoryItem>>> GetInventory()
        {
            var cacheKey = CacheKeys.InventoryAll;

            if (!_cache.TryGetValue(cacheKey, out IEnumerable<InventoryItem>? items))
            {
                items = await _context.InventoryItems
                    .AsNoTracking()
                    .Include(i => i.Order)
                    .ToListAsync();

                _cache.Set(cacheKey, items, CacheOptionsFactory.Short());
            }

            return Ok(items ?? Array.Empty<InventoryItem>());
        }

        // POST: /api/inventory
        /// <summary>
        /// Adds a new inventory item. Only users with the Manager role are authorized.
        /// Invalidates the inventory cache after a successful insert.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = Roles.Manager)]
        public async Task<ActionResult<InventoryItem>> AddInventoryItem([FromBody] InventoryItem item)
        {
            if (item == null)
            {
                return BadRequest();
            }
            _context.InventoryItems.Add(item);
            await _context.SaveChangesAsync();

            // Invalidate inventory cache after adding an item
            _cache.Remove(CacheKeys.InventoryAll);

            return CreatedAtAction(nameof(GetInventory), new { id = item.ItemId }, item);
        }

        // DELETE: /api/inventory/{id}
        /// <summary>
        /// Deletes an inventory item by id. Only users with the Manager role are authorized.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Manager)]
        public async Task<IActionResult> DeleteInventoryItem(int id)
        {
            var item = await _context.InventoryItems.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync();

            // Invalidate inventory cache after deleting an item
            _cache.Remove(CacheKeys.InventoryAll);

            return NoContent();
        }
    }
}
