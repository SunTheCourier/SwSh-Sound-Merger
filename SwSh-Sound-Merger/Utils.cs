using System;
using System.IO;
using VGAudio.Containers.NintendoWare;
using VGAudio.Utilities;

namespace SwSh_Sound_Merger
{
    public static class Utils
    {
        public enum AudioType { BFSTM, BCSTM }
        public struct AudioSpecs
        {
            public AudioType? type;
            public NwCodec? encoding;
            public Endianness? endianness;
            public NwTarget? target;

            public bool HasInvalidOptions => !type.HasValue || !encoding.HasValue || !target.HasValue || !endianness.HasValue;

            public AudioSpecs(string str)
            {
                string[] args = str.ToLowerInvariant().Split(':');

                if (args.Length < 4)
                {
                    this.type = null;
                    encoding = null;
                    target = null;
                    endianness = null;
                    return;
                }

                this.type = !Enum.TryParse(args[0], true, out AudioType type) ? null : (AudioType?)type;

                switch (args[1])
                {
                    case "4":
                        encoding = NwCodec.GcAdpcm;
                        break;
                    case "8":
                        encoding = NwCodec.Pcm8Bit;
                        break;
                    case "16":
                        encoding = NwCodec.Pcm16Bit;
                        break;
                    case "ima":
                        encoding = NwCodec.ImaAdpcm;
                        break;
                    default:
                        encoding = null;
                        break;
                }

                switch (args[2])
                {
                    case "le":
                        endianness = Endianness.LittleEndian;
                        break;
                    case "be":
                        endianness = Endianness.BigEndian;
                        break;
                    default:
                        endianness = null;
                        break;
                }

                switch (args[3])
                {
                    case "ca":
                    case "cafe":
                        target = NwTarget.Cafe;
                        break;
                    case "ct":
                    case "ctr":
                        target = NwTarget.Ctr;
                        break;
                    case "re":
                    case "revolution":
                        target = NwTarget.Revolution;
                        break;
                    default:
                        target = null;
                        break;
                }
            }
        }

        public static FileInfo GetFile(this DirectoryInfo obj, string fileName) => new FileInfo($"{obj.FullName}{Path.DirectorySeparatorChar}{fileName}");

        public static DirectoryInfo GetDirectory(this DirectoryInfo obj, string foldername) => new DirectoryInfo($"{obj.FullName}{Path.DirectorySeparatorChar}{foldername.Replace('/', Path.DirectorySeparatorChar)}");
    }
}
