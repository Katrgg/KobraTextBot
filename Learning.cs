using System;
using System.Collections.Generic;
using System.Linq;

namespace KobraTextBot
{
    internal class Learning
    {
        public static bool CheckExsitsSticker(List<Sticker> stickers, string unicID)
        {
            foreach (var item in stickers)
            {
                if (item.UnicId == unicID)
                    return true;
            }
            return false;
        }

        public static GroupData learnOnMessage(string text, long id, GroupData groupData, Random r)
        {
            List<string> s = MarkovChains.Chunk(text, 1);
            groupData.DictMain = MarkovChains.BuildTDict(s, r.Next(1, 10), groupData.DictMain);
            if (groupData.settings.sLastWord.date.AddSeconds(30) > DateTime.Now && groupData.settings.sLastWord.id == id)
            {
                string last = groupData.settings.sLastWord.lastWord;
                groupData.settings.sLastWord.lastWord = s.Last();
                if (groupData.DictMain.ContainsKey(s.Last()))
                {
                    if (groupData.DictMain[s.Last()].ContainsKey(last))
                        groupData.DictMain[s.Last()][last] += 1;
                    else
                        groupData.DictMain[s.Last()].Add(last, 1);
                }
            }
            else if(groupData.settings.memorizeLastWord)
            {
                groupData.settings.sLastWord.id = id;
                groupData.settings.sLastWord.lastWord = s.Last();
                groupData.settings.sLastWord.date = DateTime.Now;
            }
            return groupData;
        }
    }
}
