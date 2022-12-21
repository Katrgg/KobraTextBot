using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, uint>>;
using WDict = System.Collections.Generic.Dictionary<string, uint>;


namespace KobraTextBot
{
    internal class MarkovChains
    {
        static public TDict BuildTDict(List<string> s, int size, TDict DictMain)
        {
            string prev = "";
            foreach (string word in s)
            {
                if (DictMain.ContainsKey(prev))
                {
                    WDict w = DictMain[prev];
                    if (w.ContainsKey(word))
                        w[word] += 1;
                    else
                        w.Add(word, 1);
                }
                else
                    DictMain.Add(prev, new WDict() { { word, 1 } });

                prev = word;
            }

            return DictMain;
        }

        static public List<string> Chunk(string s, int size)
        {
            string[] ls = s.Split(' ');
            List<string> chunk = new List<string>();

            for (int i = 0; i < ls.Length; ++i)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(ls.Skip(i).Take(size).Aggregate((w, k) => w + " " + k));
                chunk.Add(sb.ToString());
            }

            return chunk;
        }

        static public string BuildString(TDict t, bool exact, Random r)
        {
            int len = 150;
            if (t == null | t.Count == 0)
                return null;
            string last;
            List<string> ucStr = new List<string>();
            StringBuilder sb = new StringBuilder();

            if (ucStr.Count == 0)
            {
                if (r.Next(0, 1) == 1)
                {
                    List<string> keys = new List<string>(t.Keys);
                    int ind = r.Next(0, keys.Count);
                    ucStr.Add(keys[ind]);
                }
                else
                {
                    ucStr.Add("");
                }
            }


            if (ucStr.Count > 0)
                sb.Append(ucStr.ElementAt(r.Next(0, ucStr.Count)));

            last = sb.ToString();
            sb.Append(" ");
            WDict w = new WDict();

            for (uint i = 0; i < len; ++i)
            {
                if (t.ContainsKey(last))
                    w = t[last];
                else
                    break;

                last = Choose(w, r);

                sb.Append(last.Split(' ').Last()).Append(" ");
            }

            if (!exact)
            {
                while (last.Last() != '.')
                {
                    if (t.ContainsKey(last))
                        w = t[last];
                    else
                        w = t[""];

                    last = Choose(w, r);
                    sb.Append(last.Split(' ').Last()).Append(" ");
                }
            }

            return sb.ToString();
        }

        static public string BuildString(TDict t, int len, bool exact, Random r)
        {
            if (t == null | t.Count == 0)
                return null;
            string last;
            List<string> ucStr = new List<string>();
            StringBuilder sb = new StringBuilder();

            if (ucStr.Count == 0)
            {
                ucStr.Add("");
            }


            if (ucStr.Count > 0)
                sb.Append(ucStr.ElementAt(r.Next(0, ucStr.Count)));

            last = sb.ToString();
            sb.Append(" ");
            WDict w = new WDict();

            for (uint i = 0; i < len; ++i)
            {
                if (t.ContainsKey(last))
                    w = t[last];
                else
                    w = t[""];

                last = Choose(w, r);

                sb.Append(last.Split(' ').Last()).Append(" ");
            }

            if (!exact)
            {
                while (last.Last() != '.')
                {
                    if (t.ContainsKey(last))
                        w = t[last];
                    else
                        w = t[""];

                    last = Choose(w, r);
                    sb.Append(last.Split(' ').Last()).Append(" ");
                }
            }

            return sb.ToString();
        }

        static public string Choose(WDict w, Random r)
        {
            long total = w.Sum(t => t.Value);

            while (true)
            {
                int i = r.Next(0, w.Count);
                double c = r.NextDouble();
                System.Collections.Generic.KeyValuePair<string, uint> k = w.ElementAt(i);

                if (c < (double)k.Value / total)
                    return k.Key;
            }
        }
    }
}
