using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using D = System.Diagnostics.Debug;

namespace SushiHangover.Tests
{
	public static class PerfHelper
    {
		static readonly Random prng = new Random();

		public static async Task<List<string>> GenerateDatabase(Realms.Realm targetCache, int size)
        {
            var ret = new List<string>();

            // Write out in groups of 4096
            while (size > 0)
            {
                var toWriteSize = Math.Min(4096, size);
                var toWrite = GenerateRandomDatabaseContents(toWriteSize);

				await targetCache.WriteAsync(realm =>
				{
					foreach (var item in toWrite)
					{
						var obj = realm.CreateObject<KeyValueRecord>();
						obj.Key = item.Key;
						obj.Value = item.Value;
					}
				});

                foreach (var k in toWrite.Keys) ret.Add(k);

                size -= toWrite.Count;
            }
            return ret;
        }

        public static Dictionary<string, byte[]> GenerateRandomDatabaseContents(int toWriteSize)
        {
            var toWrite = Enumerable.Range(0, toWriteSize)
                .Select(_ => GenerateRandomKey())
                .Distinct()
                .ToDictionary(k => k, _ => GenerateRandomBytes());

            return toWrite;
        }

        public static byte[] GenerateRandomBytes()
        {
			// Do not use byte arrays of length 1, dups are being created
            var ret = new byte[prng.Next(20, 256)];

            prng.NextBytes(ret);
            return ret;
        }

        public static string GenerateRandomKey()
        {
            var bytes = GenerateRandomBytes();

            // NB: Mask off the MSB and set bit 5 so we always end up with
            // valid UTF-8 characters that aren't control characters
            for (int i = 0; i < bytes.Length; i++) { bytes[i] = (byte)((bytes[i] & 0x7F) | 0x20); }

			var k = Encoding.UTF8.GetString(bytes, 0, Math.Min(256, bytes.Length));

			if (k.Trim().Length == 0)
				k = GenerateRandomKey();
				//throw new Exception("blank key");

			return k;
        }

		public static int MaxRange = 12;
        public static int[] GetPerfRanges()
        {
			//TODO
			return Enumerable.Range(1, MaxRange).Select(_ => 1 << _).ToArray();
        }
    }
}
