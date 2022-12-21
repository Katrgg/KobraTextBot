using Newtonsoft.Json;
using System.IO;

namespace KobraTextBot
{
    internal class DataIO
    {
        public static void SaveData(GroupData groupData, long chatID)
        {
            using(StreamWriter writer = File.CreateText($"D:\\KobraData\\DataChat{chatID}.json"))
            {
                string output = JsonConvert.SerializeObject(groupData, Formatting.Indented);
                writer.Write(output);
            }
        }

        public static GroupData LoadData(long chatID)
        {
            if (!File.Exists($"D:\\KobraData\\DataChat{chatID}.json"))
            {
                using(File.CreateText($"D:\\KobraData\\DataChat{chatID}.json"))
                return new GroupData() { chatID = chatID};
            }
            using (StreamReader reader = File.OpenText($"D:\\KobraData\\DataChat{chatID}.json"))
            {
                string input = reader.ReadToEnd();
                GroupData gp = JsonConvert.DeserializeObject<GroupData>(input);
                if(gp == null)
                    return new GroupData() { chatID = chatID};
                return gp;
            }
        }
        
        public static void DeleteData(long chatID)
        {
            File.Delete($"D:\\KobraData\\DataChat{chatID}.json");
            Directory.Delete($"D:\\KobraData\\{chatID}", true);
        }
    }
}
