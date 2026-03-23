using System.Collections.Generic;
using System.Linq;

namespace QubicNS
{
    class RandomBuckets : List<Rule>
    {
        public readonly int BucketsCount;
        int lastCRC;

        public RandomBuckets(int bucketsCount = 300)
        {
            this.BucketsCount = bucketsCount;
        }

        /// <summary> Generates set of random lists of rules </summary>
        public void PrepareBuckets(List<Rule> rules)
        {
            var CRC = rules.Aggregate(0, (c, p) => p.Prefab.Seed ^ c ^ (p.Priority * 13) ^ (p.Chance * 17));
            var isCached = lastCRC == CRC && rules.All(p => p.TempSeed != 0);
            //if (!isCached)
            {
                lastCRC = CRC;
                Clear();
                rules.ForEach(p => p.TempRnd = new Rnd(p.Prefab.Seed - 131));
                for (int iBucket = 0; iBucket < BucketsCount; iBucket++)
                {
                    // generate rnd indicies for each prefab for this bucket
                    for (int i = 0; i < rules.Count; i++)
                    {
                        var rule = rules[i];
                        rule.TempSeed = rule.Priority * 100 + rule.TempRnd.Float(rule.Chance);
                    }

                    // sort by rnd and priority
                    rules.Sort((a, b) => -a.TempSeed.CompareTo(b.TempSeed));
                    // create bucket
                    AddRange(rules);
                }
            }
        }
    }
}