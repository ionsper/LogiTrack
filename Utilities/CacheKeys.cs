namespace LogiTrack.Utilities
{
    /// <summary>
    /// Centralized cache key definitions used across the application.
    /// Keep keys stable to avoid cache fragmentation across deployments.
    /// </summary>
    public static class CacheKeys
    {
        public const string InventoryAll = "inventory_all";
        public const string OrdersAll = "orders_all";
        public static string OrderById(int id) => $"order_{id}";
        public static string SessionKey(string sessionId) => $"session:{sessionId}";
        public const string SessionCookieName = "session_id";
    }
}
