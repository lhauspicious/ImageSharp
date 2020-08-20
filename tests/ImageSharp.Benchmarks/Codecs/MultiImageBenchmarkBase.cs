// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.ImageSharp.Benchmarks.Codecs
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Numerics;

    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Environments;
    using SixLabors.ImageSharp.Tests;

    using CoreImage = SixLabors.ImageSharp.Image;

    public abstract class MultiImageBenchmarkBase
    {
        public class Config : ManualConfig
        {
            public Config()
            {
                // Uncomment if you want to use any of the diagnoser
                this.Add(MemoryDiagnoser.Default);
            }

            public class ShortClr : Benchmarks.Config
            {
                public ShortClr()
                {
                    this.Add(Job.Default.With(CoreRuntime.Core21).WithLaunchCount(1).WithWarmupCount(1).WithIterationCount(2));
                }
            }
        }

        protected Dictionary<string, byte[]> fileNamesToBytes = new Dictionary<string, byte[]>();
        protected Dictionary<string, Image<Rgba32>> fileNamesToImageSharpImages = new Dictionary<string, Image<Rgba32>>();
        protected Dictionary<string, Bitmap> fileNamesToSystemDrawingImages = new Dictionary<string, System.Drawing.Bitmap>();

        /// <summary>
        /// The values of this enum separate input files into categories.
        /// </summary>
        public enum InputImageCategory
        {
            /// <summary>
            /// Use all images.
            /// </summary>
            AllImages,

            /// <summary>
            /// Use small images only.
            /// </summary>
            SmallImagesOnly,

            /// <summary>
            /// Use large images only.
            /// </summary>
            LargeImagesOnly
        }

        [Params(InputImageCategory.AllImages, InputImageCategory.SmallImagesOnly, InputImageCategory.LargeImagesOnly)]
        public virtual InputImageCategory InputCategory { get; set; }

        protected virtual string BaseFolder => TestEnvironment.InputImagesDirectoryFullPath;

        protected virtual IEnumerable<string> SearchPatterns => new[] { "*.*" };

        /// <summary>
        /// Gets the file names containing these strings are substrings are not processed by the benchmark.
        /// </summary>
        protected virtual IEnumerable<string> ExcludeSubstringsInFileNames => new[] { "badeof", "BadEof", "CriticalEOF" };

        /// <summary>
        /// Gets folders containing files OR files to be processed by the benchmark.
        /// </summary>
        protected IEnumerable<string> AllFoldersOrFiles => this.InputImageSubfoldersOrFiles.Select(f => Path.Combine(this.BaseFolder, f));

        /// <summary>
        /// Gets the large image threshold.
        /// The images sized above this threshold will be included in.
        /// </summary>
        protected virtual int LargeImageThresholdInBytes => 100000;

        protected IEnumerable<KeyValuePair<string, T>> EnumeratePairsByBenchmarkSettings<T>(
            Dictionary<string, T> input,
            Predicate<T> checkIfSmall)
        {
            switch (this.InputCategory)
            {
                case InputImageCategory.AllImages:
                    return input;
                case InputImageCategory.SmallImagesOnly:
                    return input.Where(kv => checkIfSmall(kv.Value));
                case InputImageCategory.LargeImagesOnly:
                    return input.Where(kv => !checkIfSmall(kv.Value));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected IEnumerable<KeyValuePair<string, byte[]>> FileNames2Bytes
            =>
            this.EnumeratePairsByBenchmarkSettings(
                this.fileNamesToBytes,
                arr => arr.Length < this.LargeImageThresholdInBytes);

        protected abstract IEnumerable<string> InputImageSubfoldersOrFiles { get; }

        [GlobalSetup]
        public virtual void Setup()
        {
            if (!Vector.IsHardwareAccelerated)
            {
                throw new Exception("Vector.IsHardwareAccelerated == false! Check your build settings!");
            }

            // Console.WriteLine("Vector.IsHardwareAccelerated: " + Vector.IsHardwareAccelerated);
            this.ReadFilesImpl();
        }

        protected virtual void ReadFilesImpl()
        {
            foreach (string path in this.AllFoldersOrFiles)
            {
                if (File.Exists(path))
                {
                    this.fileNamesToBytes[path] = File.ReadAllBytes(path);
                    continue;
                }

                string[] excludeStrings = this.ExcludeSubstringsInFileNames.Select(s => s.ToLower()).ToArray();

                string[] allFiles =
                    this.SearchPatterns.SelectMany(
                        f =>
                            Directory.EnumerateFiles(path, f, SearchOption.AllDirectories)
                                .Where(fn => !excludeStrings.Any(excludeStr => fn.ToLower().Contains(excludeStr)))).ToArray();

                foreach (string fn in allFiles)
                {
                    this.fileNamesToBytes[fn] = File.ReadAllBytes(fn);
                }
            }
        }

        /// <summary>
        /// Execute code for each image stream. If the returned object of the operation <see cref="Func{T, TResult}"/> is <see cref="IDisposable"/> it will be disposed.
        /// </summary>
        /// <param name="operation">The operation to execute. If the returned object is &lt;see cref="IDisposable"/&gt; it will be disposed </param>
        protected void ForEachStream(Func<MemoryStream, object> operation)
        {
            foreach (KeyValuePair<string, byte[]> kv in this.FileNames2Bytes)
            {
                using (var memoryStream = new MemoryStream(kv.Value))
                {
                    try
                    {
                        object obj = operation(memoryStream);
                        (obj as IDisposable)?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Operation on {kv.Key} failed with {ex.Message}");
                    }
                }
            }
        }

        public abstract class WithImagesPreloaded : MultiImageBenchmarkBase
        {
            protected override void ReadFilesImpl()
            {
                base.ReadFilesImpl();

                foreach (KeyValuePair<string, byte[]> kv in this.fileNamesToBytes)
                {
                    byte[] bytes = kv.Value;
                    string fn = kv.Key;

                    using (var ms1 = new MemoryStream(bytes))
                    {
                        this.fileNamesToImageSharpImages[fn] = CoreImage.Load<Rgba32>(ms1);
                    }

                    this.fileNamesToSystemDrawingImages[fn] = new Bitmap(new MemoryStream(bytes));
                }
            }

            protected IEnumerable<KeyValuePair<string, Image<Rgba32>>> FileNames2ImageSharpImages
                =>
                this.EnumeratePairsByBenchmarkSettings(
                    this.fileNamesToImageSharpImages,
                    img => img.Width * img.Height < this.LargeImageThresholdInPixels);

            protected IEnumerable<KeyValuePair<string, System.Drawing.Bitmap>> FileNames2SystemDrawingImages
                =>
                this.EnumeratePairsByBenchmarkSettings(
                    this.fileNamesToSystemDrawingImages,
                    img => img.Width * img.Height < this.LargeImageThresholdInPixels);

            protected virtual int LargeImageThresholdInPixels => 700000;

            protected void ForEachImageSharpImage(Func<Image<Rgba32>, object> operation)
            {
                foreach (KeyValuePair<string, Image<Rgba32>> kv in this.FileNames2ImageSharpImages)
                {
                    try
                    {
                        object obj = operation(kv.Value);
                        (obj as IDisposable)?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Operation on {kv.Key} failed with {ex.Message}");
                    }
                }
            }

            protected void ForEachImageSharpImage(Func<Image<Rgba32>, MemoryStream, object> operation)
            {
                using (var workStream = new MemoryStream())
                {
                    this.ForEachImageSharpImage(
                        img =>
                        {
                            // ReSharper disable AccessToDisposedClosure
                            object result = operation(img, workStream);
                            workStream.Seek(0, SeekOrigin.Begin);

                            // ReSharper restore AccessToDisposedClosure
                            return result;
                        });
                }
            }

            protected void ForEachSystemDrawingImage(Func<System.Drawing.Bitmap, object> operation)
            {
                foreach (KeyValuePair<string, Bitmap> kv in this.FileNames2SystemDrawingImages)
                {
                    try
                    {
                        object obj = operation(kv.Value);
                        (obj as IDisposable)?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Operation on {kv.Key} failed with {ex.Message}");
                    }
                }
            }

            protected void ForEachSystemDrawingImage(Func<System.Drawing.Bitmap, MemoryStream, object> operation)
            {
                using (var workStream = new MemoryStream())
                {
                    this.ForEachSystemDrawingImage(
                        img =>
                        {
                            // ReSharper disable AccessToDisposedClosure
                            object result = operation(img, workStream);
                            workStream.Seek(0, SeekOrigin.Begin);

                            // ReSharper restore AccessToDisposedClosure
                            return result;
                        });
                }
            }
        }
    }
}
