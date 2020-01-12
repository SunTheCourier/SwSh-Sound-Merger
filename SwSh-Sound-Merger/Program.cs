using CommandLine;
using NAudio.Vorbis;
using NAudio.Wave;
using NVorbis;
using System;
using System.IO;
using VGAudio.Containers.NintendoWare;
using VGAudio.Containers.Wave;
using VGAudio.Formats;

namespace SwSh_Sound_Merger
{
    class Program
    {
        static readonly DirectoryInfo CurrentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

        static void Main(string[] args)
        {
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

                Dictionary sounds = new Dictionary(dictionary);

                CurrentDirectory.GetDirectory("out").Create();
                foreach (Sound sound in sounds.Sounds)
                {
                    if (!sound.HasStartFile && !sound.HasStartFile)
                        continue;

                    FileInfo outFile = CurrentDirectory.GetDirectory("out").GetFile($"{sound.Name}.{arguments.Format.ToString().ToLower()}");
                    if (!arguments.Overwrite && outFile.Exists)
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
                        BCFstmWriter writer = new BCFstmWriter(arguments.Target);
                        writer.Configuration.Codec = arguments.Codec;
                        writer.Configuration.Endianness = arguments.Endianness;
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
            });
        }
    }
}
