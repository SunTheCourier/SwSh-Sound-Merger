using CommandLine;
using VGAudio.Containers.NintendoWare;
using VGAudio.Utilities;

namespace SwSh_Sound_Merger
{
    public class ConsoleOptions
    {
        public enum AudioFormat { BFSTM, BCSTM }

        [Option('c', "codec", Required = false, Default = NwCodec.GcAdpcm, HelpText = "Codec types: GcAdpcm, ImaAdpcm, Pcm16Bit, and Pcm8Bit")]
        public NwCodec Codec { get; set; }

        [Option('t', "target", Required = false, Default = NwTarget.Cafe, HelpText = "Targets: Ctr (3DS), Cafe (Wii U and Switch), and Revolution (Wii)")]
        public NwTarget Target { get; set; }

        [Option('f', "format", Required = false, Default = AudioFormat.BFSTM, HelpText = "Format types: BCSTM and BFSTM")]
        public AudioFormat Format { get; set; }

        [Option('e', "Endianness", Required = false, Default = Endianness.LittleEndian, HelpText = "Endianness to encode file in: LittleEndian and BigEndian")]
        public Endianness Endianness { get; set; }

        [Option('o', "Overwrite", Required = false, Default = false, HelpText = "Overwrite sound files in output folder")]
        public bool Overwrite { get; set; }

        [Value(0, MetaName = "Game sound path", Required = true)]
        public string FolderPath { get; set; }

        [Value(1, MetaName = "Dictionary file path", Required = false, Default = "Dictionary.txt")]
        public string DictionaryPath { get; set; }
    }
}
