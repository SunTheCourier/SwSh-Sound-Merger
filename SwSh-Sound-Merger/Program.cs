using NAudio.Vorbis;
using NAudio.Wave;
using NVorbis;
using System;
using System.IO;
using VGAudio.Containers.NintendoWare;
using VGAudio.Containers.Wave;
using VGAudio.Formats;
using static SwSh_Sound_Merger.Utils;

namespace SwSh_Sound_Merger
{
    class Program
    {
        static readonly DirectoryInfo CurrentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

        static void Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 3)
            {
                Console.WriteLine("Arguments: <Path to Sounds>, <Format=bfstm:16:le:ca>, <Overwrite=false>, <Dictionary=CurrentDirectory/Dictonary.txt>");
                Console.WriteLine("Format Options: bfstm, bcstm : ima, 4, 8, 16:le (switch), be (wii u) : ca (wii u/switch), ct (3ds), re");
                Console.Write("Press any key to continue!");
                Console.ReadKey(true);
                return;
            }
            AudioSpecs specs;
            if (args.Length >= 2)
            {
                specs = new AudioSpecs(args[1]);

                if (specs.HasInvalidOptions)
                {
                    Console.WriteLine("Invalid format options!");
                    Console.WriteLine("Format Options: bfstm, bcstm, brstm : ima, 4, 8, 16:le (switch), be (wii u) : ca (wii u/switch), ct (3ds), re");
                    Console.Write("Press any key to continue!");
                    Console.ReadKey(true);
                    return;
                }
            }
            else
            {
                specs = new AudioSpecs("bfstm:16:le:ca");
            }

            bool Overwrite = false;
            if (args.Length >= 3)
            {
                if (!bool.TryParse(args[2], out Overwrite))
                {
                    Console.WriteLine("Overwrite argument is invalid!");
                    Console.Write("Press any key to continue!");
                    Console.ReadKey(true);
                    return;
                }
            }

            FileInfo dictionary;
            if (args.Length >= 4)
                dictionary = new FileInfo(args[3]);
            else
                dictionary = CurrentDirectory.GetFile("Dictionary.txt");

            if (!dictionary.Exists)
            {
                Console.WriteLine("Dictionary file does not exist!");
                Console.Write("Press any key to continue!");
                Console.ReadKey(true);
                return;
            }

            DirectoryInfo gameSounds = new DirectoryInfo(args[0]);
            if (!gameSounds.Exists)
            {
                Console.WriteLine("Game sounds does not exist!");
                Console.Write("Press any key to continue!");
                Console.ReadKey(true);
                return;
            }

            Dictionary sounds = new Dictionary(dictionary);

            CurrentDirectory.GetDirectory("out").Create();
            foreach (Sound sound in sounds.Sounds)
            {
                if (!sound.HasStartFile && !sound.HasStartFile)
                    continue;

                FileInfo outFile = CurrentDirectory.GetDirectory("out").GetFile($"{sound.Name}.{specs.type.ToString().ToLower()}");
                if (!Overwrite && outFile.Exists)
                    continue;

                FileInfo tempWave = new FileInfo(Path.GetTempFileName());

                WaveReader reader = new WaveReader();
                AudioData audio = null;

                Console.WriteLine($"Processing {sound.Name}...");
                if (sound.HasStartFile && !sound.HasLoopFile)
                {
                    using (VorbisWaveReader vorbisStart = new VorbisWaveReader(gameSounds.GetFile(sound.StartFileName).FullName))
                    {
                        WaveFileWriter.CreateWaveFile(tempWave.FullName, vorbisStart.ToWaveProvider16());
                    }
                    using (FileStream stream = tempWave.OpenRead())
                    {
                        audio = reader.Read(stream);
                    }

                }
                else if (sound.HasStartFile && sound.HasLoopFile)
                {
                    VorbisWaveReader vorbisStart = new VorbisWaveReader(gameSounds.GetFile(sound.StartFileName).FullName);
                    VorbisWaveReader vorbisLoop = new VorbisWaveReader(gameSounds.GetFile(sound.LoopFileName).FullName);

                    WaveFileWriter.CreateWaveFile(tempWave.FullName, vorbisStart.FollowedBy(vorbisLoop).ToWaveProvider16());
                    VorbisReader dataStart = new VorbisReader(gameSounds.GetFile(sound.StartFileName).FullName);
                    VorbisReader dataLoop = new VorbisReader(gameSounds.GetFile(sound.LoopFileName).FullName);
                    int startLoop = (int)dataStart.TotalSamples;
                    int endLoop = (int)dataStart.TotalSamples + (int)dataLoop.TotalSamples;
                    vorbisStart.Dispose();
                    vorbisLoop.Dispose();
                    dataStart.Dispose();
                    dataLoop.Dispose();
                    using (FileStream stream = tempWave.OpenRead())
                    {
                        audio = reader.Read(stream);
                    }

                    audio.SetLoop(true, startLoop, endLoop);
                }
                else if (!sound.HasStartFile && sound.HasLoopFile)
                {
                    VorbisWaveReader vorbisLoop = new VorbisWaveReader(gameSounds.GetFile(sound.LoopFileName).FullName);
                    WaveFileWriter.CreateWaveFile(tempWave.FullName, vorbisLoop.ToWaveProvider16());
                    VorbisReader dataLoop = new VorbisReader(gameSounds.GetFile(sound.LoopFileName).FullName);
                    int totalSamples = (int)dataLoop.TotalSamples;
                    vorbisLoop.Dispose();
                    dataLoop.Dispose();
                    audio = reader.Read(tempWave.OpenRead());

                    audio.SetLoop(true, 0, totalSamples);
                }

                if (audio != null)
                {
                    BCFstmWriter writer = new BCFstmWriter(specs.target.Value);
                    writer.Configuration.Codec = specs.encoding.Value;
                    writer.Configuration.Endianness = specs.endianness.Value;
                    using (FileStream stream = outFile.OpenWrite())
                    {
                        writer.WriteToStream(audio, stream);
                    }
                }
            }
            Console.WriteLine("Succesfully merged and convertered all tracks!");
            Console.Write("Press any key to continue!");
            Console.ReadKey(true);
            return;
        }
    }
}
