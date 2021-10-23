using System;
using System.Collections.Generic;
using System.Linq;

namespace TestApp.TestData
{
    public static class TestDataGenerator
    {
        public static List<SimpleTestEntry> GetSimpleTestEntryList0()
        {
            return new List<SimpleTestEntry>
            {
                new("1A", "111", "1", "B"),
                new("1B", "111", "2", "A"),
                new("2A", "222", "1", "A")
            };
        }

        public static List<SimpleTestEntry> GetSimpleTestEntryList1()
        {
            return new List<SimpleTestEntry>
            {
                new("1A", "111", "1", "A"),
                new("1B", "111", "2", "A"),
                new("2B", "222", "2", "A")
            };
        }

        public static List<SimpleTestEntry> GetSimpleTestEntryList2()
        {
            return new List<SimpleTestEntry>
            {
                new("1A", "111", "1", "A"),
                new("2A", "222", "1", "B"),
                new("2B", "222", "2", "A")
            };
        }

        public static List<SimpleTestEntry> GetSimpleTestEntryList3()
        {
            return new List<SimpleTestEntry>
            {
                new("1B", "222", "2", "B"),
                new("2A", "222", "2", "A"),
                new("2B", "111", "1", "A")
            };
        }

        public static List<SimpleTestEntry>[] GetSimpleTestEntryListArray()
        {
            return new[]
            {
                GetSimpleTestEntryList0(),
                GetSimpleTestEntryList1(),
                GetSimpleTestEntryList2(),
                GetSimpleTestEntryList3()
            };
        }

        private static List<SimpleTestEntry> GetEntryTemplateList(int n)
        {
            var entryTemplateList = new List<SimpleTestEntry>();
            var num = (int)Math.Floor(Math.Pow(n, 1.0 / 3));
            for (var i = 0; i < num; i++)
            {
                for (var j = 0; j < num; j++)
                {
                    for (var k = 0; k < num && entryTemplateList.Count < n; k++)
                    {
                        entryTemplateList.Add(new SimpleTestEntry("", i.ToString(), j.ToString(), k.ToString()));
                    }
                }
            }

            for (var k = 0; k < entryTemplateList.Count-n; k++)
            {
                var kModNum = k % num;
                var j = k / num;
                entryTemplateList.Add(new SimpleTestEntry("", num.ToString(), j.ToString(), kModNum.ToString()));
            }
 
            return entryTemplateList;
        }

        public static List<SimpleTestEntry> GetRandomSimpleTestEntryList(int entryCount, int idCount, int? templateCount = null)
        {
            if (idCount < entryCount)
            {
                throw new ArgumentException("entryCount must be larger than idCount!");
            }

            var idList = Enumerable.Range(0, idCount)
                .Select(idInt => idInt.ToString())
                .ToList();
            var templateNum = templateCount ?? (int)Math.Floor(Math.Sqrt(entryCount));
            var entryTemplateList = GetEntryTemplateList(templateNum);
            var rnd = new Random();
            var entryList = new List<SimpleTestEntry>();
            while (entryList.Count < entryCount)
            {
                var rndIdIndex = rnd.Next(idList.Count);
                var id = idList[rndIdIndex];
                idList.RemoveAt(rndIdIndex);
                var rndIndex = rnd.Next(entryTemplateList.Count);
                var entry = entryTemplateList[rndIndex] with { Id = id };
                entryList.Add(entry);
            }
            return entryList;
        }
    }
}
