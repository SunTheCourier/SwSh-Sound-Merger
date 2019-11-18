using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SwSh_Sound_Merger
{
    class Dictionary
    {
        public List<Sound> Sounds { get; }
        public Dictionary(FileInfo fileName)
        {
            Sounds = new List<Sound>();
            bool checkEndingComment = false;
            string[] lines = File.ReadAllLines(fileName.FullName);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.StartsWith("/*"))
                {
                    checkEndingComment = true;
                    continue;
                }
                else if (line.Contains(" //"))
                {
                    line = line.Remove(line.IndexOf(" //"));
                }
                else if (line.Contains("//"))
                {
                   line = line.Remove(line.IndexOf("//"));
                }
                else if (checkEndingComment)
                {
                    if (line.StartsWith("*/"))
                        checkEndingComment = false;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                line = line.Replace("*", "");
                line = line.Replace("???", "-1");

                if (line.Where(x => x == '&').Count() == 1 && line.Where(x => x == '|').Count() == 0)
                {
                    string[] args = line.Split(new string[] { " & ", " = " }, StringSplitOptions.None);

                    Sounds.Add(new Sound(int.Parse(args[0]), int.Parse(args[1]), args[2]));
                }
                else if (line.Where(x => x == '&').Count() == 0)
                {
                    string name = line.Substring(line.IndexOf("= ") + 2);
                    line = line.Remove(line.IndexOf(" = "));
                    string[] sounds = line.Split(new string[] { " | " }, StringSplitOptions.None);

                    for (int i1 = 0; i1 < sounds.Length; i1++)
                    {
                        string sound = sounds[i1];
                        string soundName = name;
                        if (sounds.Length > 1)
                            soundName += string.Format("{0:00}", i1);
                        Sounds.Add(new Sound(int.Parse(sound), -1, $"{soundName}"));
                    }
                }
            }
        }
    }
}


