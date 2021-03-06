using CommandLine;
using NAudio.Vorbis;
using NAudio.Wave;
using NVorbis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VGAudio.Containers.NintendoWare;
using VGAudio.Containers.Wave;
using VGAudio.Formats;

namespace SwSh_Sound_Merger
{
    class Program
    {
        private static readonly DirectoryInfo CurrentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

        private static ConsoleOptions Arguments;
        private static DirectoryInfo GameSounds;

        static void Main(string[] args)
        {
            DateTime startTime = DateTime.Now;
            Parser.Default.ParseArguments<ConsoleOptions>(args)
            .WithParsed(arguments =>
            {
                FileInfo dictionary = new FileInfo(arguments.DictionaryPath);

                if (!dictionary.Exists)
                {
                    Console.WriteLine("Dictionary file does not exist!");
                    Console.Write("Press any key to continue!");
                    Console.ReadKey(true);
                    return;
                }

                DirectoryInfo gameSounds = new DirectoryInfo(arguments.FolderPath);
                if (!gameSounds.Exists)
                {
                    Console.WriteLine("Game sounds does not exist!");
                    Console.Write("Press any key to continue!");
                    Console.ReadKey(true);
                    return;
                }

                SoundDictionary sounds = new SoundDictionary(dictionary);

                CurrentDirectory.GetDirectory("out").Create();

                Arguments = arguments;
                GameSounds = gameSounds;

                List<Task> tasks = new List<Task>();
                LimitedConcurrencyLevelTaskScheduler scheduler = new LimitedConcurrencyLevelTaskScheduler(arguments.ThreadCount);
                TaskFactory factory = new TaskFactory(scheduler);
                foreach (Sound sound in sounds.Sounds)
                {
                    Task t = factory.StartNew(() => ConvertSound(sound));
                    tasks.Add(t);
                }
                Task.WaitAll(tasks.ToArray());

                Console.WriteLine($"Coversion took {(startTime - DateTime.Now).ToString("mm':'ss")}");
                Console.WriteLine("Succesfully merged and convertered all tracks!");
                Console.Write("Press any key to continue!");
                Console.ReadKey(true);
                return;
            });
        }

        private static void ConvertSound(Sound sound)
        {
            if (!sound.HasStartFile && !sound.HasStartFile)
                return;

            FileInfo outFile = CurrentDirectory.GetDirectory("out").GetFile($"{sound.Name}.{Arguments.Format.ToString().ToLower()}");
            if (!Arguments.Overwrite && outFile.Exists)
                return;

            FileInfo tempWave = new FileInfo(Path.GetTempFileName());

            WaveReader reader = new WaveReader();
            AudioData audio = null;

            Console.WriteLine($"Processing {sound.Name}...");
            if (sound.HasStartFile && !sound.HasLoopFile)
            {
                using (VorbisWaveReader vorbisStart = new VorbisWaveReader(GameSounds.GetFile(sound.StartFileName).FullName))
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
                VorbisWaveReader vorbisStart = new VorbisWaveReader(GameSounds.GetFile(sound.StartFileName).FullName);
                VorbisWaveReader vorbisLoop = new VorbisWaveReader(GameSounds.GetFile(sound.LoopFileName).FullName);
                if (vorbisStart.WaveFormat.SampleRate < vorbisLoop.WaveFormat.SampleRate)
                {
                    MediaFoundationResampler sampeler = new MediaFoundationResampler(vorbisStart, vorbisLoop.WaveFormat.SampleRate);
                    WaveFileWriter.CreateWaveFile(tempWave.FullName, sampeler.ToSampleProvider().FollowedBy(vorbisLoop).ToWaveProvider16());
                }
                else if (vorbisStart.WaveFormat.SampleRate > vorbisLoop.WaveFormat.SampleRate)
                {
                    MediaFoundationResampler sampeler = new MediaFoundationResampler(vorbisLoop, vorbisStart.WaveFormat.SampleRate);
                    WaveFileWriter.CreateWaveFile(tempWave.FullName, vorbisStart.FollowedBy(sampeler.ToSampleProvider()).ToWaveProvider16());
                }
                else WaveFileWriter.CreateWaveFile(tempWave.FullName, vorbisStart.FollowedBy(vorbisLoop).ToWaveProvider16());
                VorbisReader dataStart = new VorbisReader(GameSounds.GetFile(sound.StartFileName).FullName);
                VorbisReader dataLoop = new VorbisReader(GameSounds.GetFile(sound.LoopFileName).FullName);
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
                VorbisWaveReader vorbisLoop = new VorbisWaveReader(GameSounds.GetFile(sound.LoopFileName).FullName);
                WaveFileWriter.CreateWaveFile(tempWave.FullName, vorbisLoop.ToWaveProvider16());
                VorbisReader dataLoop = new VorbisReader(GameSounds.GetFile(sound.LoopFileName).FullName);
                int totalSamples = (int)dataLoop.TotalSamples;
                vorbisLoop.Dispose();
                dataLoop.Dispose();
                audio = reader.Read(tempWave.OpenRead());

                audio.SetLoop(true, 0, totalSamples);
            }

            if (audio != null)
            {
                BCFstmWriter writer = new BCFstmWriter(Arguments.Target);
                writer.Configuration.Codec = Arguments.Codec;
                writer.Configuration.Endianness = Arguments.Endianness;
                using (FileStream stream = outFile.OpenWrite())
                {
                    writer.WriteToStream(audio, stream);
                }
            }
        }
    }
}
