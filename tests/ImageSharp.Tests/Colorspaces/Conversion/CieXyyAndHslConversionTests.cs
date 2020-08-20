// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using Xunit;

namespace SixLabors.ImageSharp.Tests.Colorspaces.Conversion
{
    /// <summary>
    /// Tests <see cref="CieXyy"/>-<see cref="Hsl"/> conversions.
    /// </summary>
    public class CieXyyAndHslConversionTests
    {
        private static readonly ApproximateColorSpaceComparer ColorSpaceComparer = new ApproximateColorSpaceComparer(.0002F);
        private static readonly ColorSpaceConverter Converter = new ColorSpaceConverter();

        /// <summary>
        /// Tests conversion from <see cref="CieXyy"/> to <see cref="Hsl"/>.
        /// </summary>
        [Theory]
        [InlineData(0, 0, 0, 0, 0, 0)]
        [InlineData(0.360555, 0.936901, 0.1001514, 120, 1, 0.211263)]
        public void Convert_CieXyy_to_Hsl(float x, float y, float yl, float h, float s, float l)
        {
            // Arrange
            var input = new CieXyy(x, y, yl);
            var expected = new Hsl(h, s, l);

            Span<CieXyy> inputSpan = new CieXyy[5];
            inputSpan.Fill(input);

            Span<Hsl> actualSpan = new Hsl[5];

            // Act
            var actual = Converter.ToHsl(input);
            Converter.Convert(inputSpan, actualSpan);

            // Assert
            Assert.Equal(expected, actual, ColorSpaceComparer);

            for (int i = 0; i < actualSpan.Length; i++)
            {
                Assert.Equal(expected, actualSpan[i], ColorSpaceComparer);
            }
        }

        /// <summary>
        /// Tests conversion from <see cref="Hsl"/> to <see cref="CieXyy"/>.
        /// </summary>
        [Theory]
        [InlineData(0, 0, 0, 0, 0, 0)]
        [InlineData(120, 1, 0.211263, 0.3, 0.6, 0.1067051)]
        public void Convert_Hsl_to_CieXyy(float h, float s, float l, float x, float y, float yl)
        {
            // Arrange
            var input = new Hsl(h, s, l);
            var expected = new CieXyy(x, y, yl);

            Span<Hsl> inputSpan = new Hsl[5];
            inputSpan.Fill(input);

            Span<CieXyy> actualSpan = new CieXyy[5];

            // Act
            CieXyy actual = Converter.ToCieXyy(input);
            Converter.Convert(inputSpan, actualSpan);

            // Assert
            Assert.Equal(expected, actual, ColorSpaceComparer);

            for (int i = 0; i < actualSpan.Length; i++)
            {
                Assert.Equal(expected, actualSpan[i], ColorSpaceComparer);
            }
        }
    }
}
