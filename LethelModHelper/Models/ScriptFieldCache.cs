using System.Collections.Concurrent;
using System.Collections.Generic;

namespace LethelModHelper.Models
{
    public static class ScriptFieldCache
    {
        private static readonly ConcurrentDictionary<string, ParsedScript> _cache = new();
        private static readonly ConcurrentDictionary<string, List<ParsedScript>> _listCache = new();

        public static void Store(object obj, string fieldName, ParsedScript parsedScript)
        {
            var key = $"{obj.GetHashCode()}_{fieldName}";
            _cache[key] = parsedScript;
        }

        public static ParsedScript? Get(object obj, string fieldName)
        {
            var key = $"{obj.GetHashCode()}_{fieldName}";
            _cache.TryGetValue(key, out var result);
            return result;
        }

        public static bool HasScript(object obj, string fieldName)
        {
            var key = $"{obj.GetHashCode()}_{fieldName}";
            return _cache.ContainsKey(key);
        }

        public static void StoreList(string key, List<ParsedScript> parsedList)
        {
            _listCache[key] = parsedList;
        }

        public static List<ParsedScript>? GetList(string key)
        {
            _listCache.TryGetValue(key, out var result);
            return result;
        }

        public static bool HasList(string key)
        {
            return _listCache.ContainsKey(key);
        }

        public static void Clear()
        {
            _cache.Clear();
            _listCache.Clear();
        }
    }
}