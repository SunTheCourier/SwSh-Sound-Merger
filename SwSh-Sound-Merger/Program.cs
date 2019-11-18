using NAudio.Vorbis;
using NAudio.Wave;
using NVorbis;
using System;
using System.IO;
using VGAudio.Containers.NintendoWare;
using VGAudio.Containers.Wave;
using VGAudio.Formats;
using VGAudio.Utilities;

namespace SwSh_Sound_Merger
{
    class Program
    {
        static readonly DirectoryInfo CurrentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

        static void Main(string[] args)
        {
            if (args.Length < 1 && args.Length > 3)
            {
                Console.WriteLine("Arguments: <Path to Sounds>, <Overwrite=false>, <Dictionary=CurrentDirectory/Dictonary.txt>");
                Console.Write("Press any key to continue!");
                Console.ReadKey(true);
                return;
            }
            FileInfo dictionary;
            if (args.Length == 3)
                dictionary = new FileInfo(args[2]);
            else
                dictionary = CurrentDirectory.GetFile("Dictionary.txt");

            if (!dictionary.Exists)
            {
                Console.WriteLine("Dictionary file does not exist!");
                Console.Write("Press any key to continue!");
                Console.ReadKey(true);
                return;
            }

            bool Overwrite = false;
            if (args.Length >= 2)
            {
                if (!bool.TryParse(args[1], out Overwrite))
                {
                    Console.WriteLine("2nd argument is invalid!");
                    Console.Write("Press any key to continue!");
                    Console.ReadKey(true);
                    return;
                }
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
                if (sound.StartFileIndex == -1 && sound.LoopFileIndex == -1)
                    continue;

                
                FileInfo outFile = CurrentDirectory.GetDirectory("out").GetFile($"{sound.Name}.bfstm");
                if (!Overwrite && outFile.Exists)
                    continue;

                FileInfo tempWave = new FileInfo(Path.GetTempFileName());

                WaveReader reader = new WaveReader();
                BCFstmWriter writer = new BCFstmWriter(NwTarget.Cafe);
                writer.Configuration.Endianness = Endianness.LittleEndian;
                AudioData audio = null;

                Console.WriteLine($"Processing {sound.Name}...");
                if (sound.StartFileIndex != -1 && sound.LoopFileIndex == -1)
                {
                    VorbisWaveReader vorbisStart = new VorbisWaveReader(gameSounds.GetFile(sound.StartFileName).FullName);
                    WaveFileWriter.CreateWaveFile(tempWave.FullName, vorbisStart.ToWaveProvider16());
                    audio = reader.Read(tempWave.OpenRead());
                }
                else if (sound.StartFileIndex != -1 && sound.LoopFileIndex != -1)
                {
                    VorbisWaveReader vorbisStart = new VorbisWaveReader(gameSounds.GetFile(sound.StartFileName).FullName);
                    VorbisWaveReader vorbisLoop = new VorbisWaveReader(gameSounds.GetFile(sound.LoopFileName).FullName);

                    WaveFileWriter.CreateWaveFile(tempWave.FullName, vorbisStart.FollowedBy(vorbisLoop).ToWaveProvider16());
                    VorbisReader dataStart = new VorbisReader(gameSounds.GetFile(sound.StartFileName).FullName);
                    VorbisReader dataLoop = new VorbisReader(gameSounds.GetFile(sound.LoopFileName).FullName);
                    int startLoop = (int)dataStart.TotalSamples;
                    int endLoop = (int)dataStart.TotalSamples + (int)dataLoop.TotalSamples;
                    dataStart.Dispose();
                    dataLoop.Dispose();

                    WaveReader waveReader = new WaveReader();
                    audio = waveReader.Read(tempWave.OpenRead());

                    audio.SetLoop(true, startLoop, endLoop);
                }
                else if (sound.StartFileIndex == -1 && sound.LoopFileIndex != -1)
                {
                    VorbisWaveReader vorbisLoop = new VorbisWaveReader(gameSounds.GetFile(sound.LoopFileName).FullName);
                    WaveFileWriter.CreateWaveFile(tempWave.FullName, vorbisLoop.ToWaveProvider16());
                    VorbisReader dataLoop = new VorbisReader(gameSounds.GetFile(sound.LoopFileName).FullName);
                    int totalSamples = (int)dataLoop.TotalSamples;
                    dataLoop.Dispose();
                    WaveReader waveReader = new WaveReader();
                    audio = waveReader.Read(tempWave.OpenRead());

                    audio.SetLoop(true, 0, totalSamples);
                }
                writer.WriteToStream(audio, outFile.Open(FileMode.Create));

                Console.WriteLine("Succesfully merged and convertered all tracks!");
                Console.Write("Press any key to continue!");
                Console.ReadKey(true);
                return;
            }
        }
    }
}
