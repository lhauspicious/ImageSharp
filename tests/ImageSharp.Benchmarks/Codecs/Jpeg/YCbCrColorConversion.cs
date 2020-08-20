// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;

using BenchmarkDotNet.Attributes;

using SixLabors.ImageSharp.Formats.Jpeg.Components.Decoder.ColorConverters;
using SixLabors.ImageSharp.Memory;

namespace SixLabors.ImageSharp.Benchmarks.Codecs.Jpeg
{
    [Config(typeof(Config.ShortClr))]
    public class YCbCrColorConversion
    {
        private Buffer2D<float>[] input;

        private Vector4[] output;

        public const int Count = 128;

        [GlobalSetup]
        public void Setup()
        {
            this.input = CreateRandomValues(3, Count);
            this.output = new Vector4[Count];
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            foreach (Buffer2D<float> buffer in this.input)
            {
                buffer.Dispose();
            }
        }

        [Benchmark]
        public void Scalar()
        {
            var values = new JpegColorConverter.ComponentValues(this.input, 0);

            JpegColorConverter.FromYCbCrBasic.ConvertCore(values, this.output, 255F, 128F);
        }

        [Benchmark(Baseline = true)]
        public void SimdVector4()
        {
            var values = new JpegColorConverter.ComponentValues(this.input, 0);

            JpegColorConverter.FromYCbCrSimd.ConvertCore(values, this.output, 255F, 128F);
        }

        [Benchmark]
        public void SimdVector8()
        {
            var values = new JpegColorConverter.ComponentValues(this.input, 0);

            JpegColorConverter.FromYCbCrSimdVector8.ConvertCore(values, this.output, 255F, 128F);
        }

        private static Buffer2D<float>[] CreateRandomValues(
            int componentCount,
            int inputBufferLength,
            float minVal = 0f,
            float maxVal = 255f)
        {
            var rnd = new Random(42);
            var buffers = new Buffer2D<float>[componentCount];
            for (int i = 0; i < componentCount; i++)
            {
                var values = new float[inputBufferLength];

                for (int j = 0; j < inputBufferLength; j++)
                {
                    values[j] = ((float)rnd.NextDouble() * (maxVal - minVal)) + minVal;
                }

                // no need to dispose when buffer is not array owner
                buffers[i] = Configuration.Default.MemoryAllocator.Allocate2D<float>(values.Length, 1);
            }

            return buffers;
        }
    }
}
