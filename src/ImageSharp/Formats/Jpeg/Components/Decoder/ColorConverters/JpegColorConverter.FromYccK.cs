﻿// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;

namespace SixLabors.ImageSharp.Formats.Jpeg.Components.Decoder.ColorConverters
{
    internal abstract partial class JpegColorConverter
    {
        internal sealed class FromYccK : JpegColorConverter
        {
            public FromYccK(int precision)
                : base(JpegColorSpace.Ycck, precision)
            {
            }

            public override void ConvertToRgba(in ComponentValues values, Span<Vector4> result)
            {
                // TODO: We can optimize a lot here with Vector<float> and SRCS.Unsafe()!
                ReadOnlySpan<float> yVals = values.Component0;
                ReadOnlySpan<float> cbVals = values.Component1;
                ReadOnlySpan<float> crVals = values.Component2;
                ReadOnlySpan<float> kVals = values.Component3;

                var v = new Vector4(0, 0, 0, 1F);

                var maximum = 1 / this.MaximumValue;
                var scale = new Vector4(maximum, maximum, maximum, 1F);

                for (int i = 0; i < result.Length; i++)
                {
                    float y = yVals[i];
                    float cb = cbVals[i] - this.HalfValue;
                    float cr = crVals[i] - this.HalfValue;
                    float k = kVals[i] / this.MaximumValue;

                    v.X = (this.MaximumValue - MathF.Round(y + (1.402F * cr), MidpointRounding.AwayFromZero)) * k;
                    v.Y = (this.MaximumValue - MathF.Round(y - (0.344136F * cb) - (0.714136F * cr), MidpointRounding.AwayFromZero)) * k;
                    v.Z = (this.MaximumValue - MathF.Round(y + (1.772F * cb), MidpointRounding.AwayFromZero)) * k;
                    v.W = 1F;

                    v *= scale;

                    result[i] = v;
                }
            }
        }
    }
}