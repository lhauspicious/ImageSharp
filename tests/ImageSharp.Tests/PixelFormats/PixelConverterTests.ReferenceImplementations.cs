// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.ImageSharp.Tests.PixelFormats
{
    public abstract partial class PixelConverterTests
    {
        public static class ReferenceImplementations
        {
            public static Rgba32 MakeRgba32(byte r, byte g, byte b, byte a)
            {
                Rgba32 d = default;
                d.R = r;
                d.G = g;
                d.B = b;
                d.A = a;
                return d;
            }

            public static Argb32 MakeArgb32(byte r, byte g, byte b, byte a)
            {
                Argb32 d = default;
                d.R = r;
                d.G = g;
                d.B = b;
                d.A = a;
                return d;
            }

            public static Bgra32 MakeBgra32(byte r, byte g, byte b, byte a)
            {
                Bgra32 d = default;
                d.R = r;
                d.G = g;
                d.B = b;
                d.A = a;
                return d;
            }

            internal static void To<TSourcePixel, TDestinationPixel>(
                Configuration configuration,
                ReadOnlySpan<TSourcePixel> sourcePixels,
                Span<TDestinationPixel> destinationPixels)
                where TSourcePixel : unmanaged, IPixel<TSourcePixel>
                where TDestinationPixel : unmanaged, IPixel<TDestinationPixel>
            {
                Guard.NotNull(configuration, nameof(configuration));
                Guard.DestinationShouldNotBeTooShort(sourcePixels, destinationPixels, nameof(destinationPixels));

                int count = sourcePixels.Length;
                ref TSourcePixel sourceRef = ref MemoryMarshal.GetReference(sourcePixels);

                if (typeof(TSourcePixel) == typeof(TDestinationPixel))
                {
                    Span<TSourcePixel> uniformDest =
                        MemoryMarshal.Cast<TDestinationPixel, TSourcePixel>(destinationPixels);
                    sourcePixels.CopyTo(uniformDest);
                    return;
                }

                // L8 and L16 are special implementations of IPixel in that they do not conform to the
                // standard RGBA colorspace format and must be converted from RGBA using the special ITU BT709 algorithm.
                // One of the requirements of FromScaledVector4/ToScaledVector4 is that it unaware of this and
                // packs/unpacks the pixel without and conversion so we employ custom methods do do this.
                if (typeof(TDestinationPixel) == typeof(L16))
                {
                    ref L16 l16Ref = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<TDestinationPixel, L16>(destinationPixels));
                    for (int i = 0; i < count; i++)
                    {
                        ref TSourcePixel sp = ref Unsafe.Add(ref sourceRef, i);
                        ref L16 dp = ref Unsafe.Add(ref l16Ref, i);
                        dp.ConvertFromRgbaScaledVector4(sp.ToScaledVector4());
                    }

                    return;
                }

                if (typeof(TDestinationPixel) == typeof(L8))
                {
                    ref L8 l8Ref = ref MemoryMarshal.GetReference(
                                             MemoryMarshal.Cast<TDestinationPixel, L8>(destinationPixels));
                    for (int i = 0; i < count; i++)
                    {
                        ref TSourcePixel sp = ref Unsafe.Add(ref sourceRef, i);
                        ref L8 dp = ref Unsafe.Add(ref l8Ref, i);
                        dp.ConvertFromRgbaScaledVector4(sp.ToScaledVector4());
                    }

                    return;
                }

                // Normal conversion
                ref TDestinationPixel destRef = ref MemoryMarshal.GetReference(destinationPixels);
                for (int i = 0; i < count; i++)
                {
                    ref TSourcePixel sp = ref Unsafe.Add(ref sourceRef, i);
                    ref TDestinationPixel dp = ref Unsafe.Add(ref destRef, i);
                    dp.FromScaledVector4(sp.ToScaledVector4());
                }
            }
        }
    }
}
