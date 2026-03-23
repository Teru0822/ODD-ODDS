using System;
using System.Collections.Generic;
using UnityEngine;

namespace QubicNS
{
    /// <summary> Converts strings into bitmask </summary>
    [Serializable]
    public class StringFlagMapper
    {
        [SerializeField]
        private readonly string[] _strings = new string[64];
        [SerializeField]
        private int _stringsCount = 0;

        /// <summary>
        /// Registers a string and assigns it a unique bit position.
        /// Returns the bitmask (UInt64) representing the registered string.
        /// </summary>
        public UInt64 GetOrCreate(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0ul;
            // Check if the string is already registered
            for (int i = 0; i < _stringsCount; i++)
            {
                if (_strings[i] == value)
                {
                    return 1ul << i;
                }
            }

            // If the string is not registered, add it at the next available index
            if (_stringsCount >= _strings.Length)
                throw new InvalidOperationException("Cannot store more than 64 unique strings in UInt64 flags.");

            _strings[_stringsCount++] = value;
            return 1ul << (_stringsCount - 1); // Return the flag for the newly registered string
        }

        public UInt64 GetOrCreate(IEnumerable<string> values)
        {
            UInt64 res = 0ul;
            foreach (string value in values)
                res |= GetOrCreate(value);

            return res;
        }

        /// <summary>
        /// Converts a single string into a bitmask (UInt64).
        /// </summary>
        public UInt64 GetMask(string value)
        {
            for (int i = 0; i < _stringsCount; i++)
            {
                if (_strings[i] == value)
                {
                    return 1ul << i;
                }
            }

            return 0ul;
        }

        /// <summary>
        /// Converts a collection of strings into a combined bitmask (UInt32).
        /// </summary>
        public UInt64 GetMask(IEnumerable<string> values)
        {
            UInt64 flags = 0ul;
            foreach (var value in values)
            {
                flags |= GetMask(value); // Combine the flags using bitwise OR
            }
            return flags;
        }

        /// <summary>
        /// Converts a bitmask (UInt64) back to an enumerable collection of strings.
        /// </summary>
        public IEnumerable<string> ToStrings(UInt64 flags)
        {
            for (int i = 0; i < _stringsCount; i++)
            {
                if ((flags & (1ul << i)) != 0)
                {
                    yield return _strings[i];
                }
            }
        }

        public void Clear()
        {
            _stringsCount = 0;
            for (int i = 0; i < _strings.Length; i++)
                _strings[i] = null;
        }
    }
}