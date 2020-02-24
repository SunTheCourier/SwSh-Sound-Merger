namespace SwSh_Sound_Merger
{
    class Sound
    {
        public string Name;
        public int StartFileIndex { get; }
        public int LoopFileIndex { get; }
        public string StartFileName => $"Stream_{StartFileIndex}.ogg";
        public string LoopFileName => $"Stream_{LoopFileIndex}.ogg";

        public Sound(int startIndex, int loopIndex, string name)
        {
            Name = name;
            StartFileIndex = startIndex;
            LoopFileIndex = loopIndex;
        }

        public bool HasLoopFile => LoopFileIndex != -1;

        public bool HasStartFile => StartFileIndex != -1;
    }
}
