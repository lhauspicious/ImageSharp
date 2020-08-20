// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace SixLabors.ImageSharp.Tests.PixelFormats
{
    /// <summary>
    /// Tests the <see cref="RgbaVector"/> struct.
    /// </summary>
    public class RgbaVectorTests
    {
        /// <summary>
        /// Tests the equality operators for equality.
        /// </summary>
        [Fact]
        public void AreEqual()
        {
            var color1 = new RgbaVector(0, 0, 0F);
            var color2 = new RgbaVector(0, 0, 0, 1F);
            var color3 = RgbaVector.FromHex("#000");
            var color4 = RgbaVector.FromHex("#000F");
            var color5 = RgbaVector.FromHex("#000000");
            var color6 = RgbaVector.FromHex("#000000FF");

            Assert.Equal(color1, color2);
            Assert.Equal(color1, color3);
            Assert.Equal(color1, color4);
            Assert.Equal(color1, color5);
            Assert.Equal(color1, color6);
        }

        /// <summary>
        /// Tests the equality operators for inequality.
        /// </summary>
        [Fact]
        public void AreNotEqual()
        {
            var color1 = new RgbaVector(1, 0, 0, 1);
            var color2 = new RgbaVector(0, 0, 0, 1);
            var color3 = RgbaVector.FromHex("#000");
            var color4 = RgbaVector.FromHex("#000000");
            var color5 = RgbaVector.FromHex("#FF000000");

            Assert.NotEqual(color1, color2);
            Assert.NotEqual(color1, color3);
            Assert.NotEqual(color1, color4);
            Assert.NotEqual(color1, color5);
        }

        /// <summary>
        /// Tests whether the color constructor correctly assign properties.
        /// </summary>
        [Fact]
        public void ConstructorAssignsProperties()
        {
            var color1 = new RgbaVector(1, .1F, .133F, .864F);
            Assert.Equal(1F, color1.R);
            Assert.Equal(.1F, color1.G);
            Assert.Equal(.133F, color1.B);
            Assert.Equal(.864F, color1.A);

            var color2 = new RgbaVector(1, .1f, .133f);
            Assert.Equal(1F, color2.R);
            Assert.Equal(.1F, color2.G);
            Assert.Equal(.133F, color2.B);
            Assert.Equal(1F, color2.A);
        }

        /// <summary>
        /// Tests whether FromHex and ToHex work correctly.
        /// </summary>
        [Fact]
        public void FromAndToHex()
        {
            var color = RgbaVector.FromHex("#AABBCCDD");
            Assert.Equal(170 / 255F, color.R);
            Assert.Equal(187 / 255F, color.G);
            Assert.Equal(204 / 255F, color.B);
            Assert.Equal(221 / 255F, color.A);

            color.A = 170 / 255F;
            color.B = 187 / 255F;
            color.G = 204 / 255F;
            color.R = 221 / 255F;

            Assert.Equal("DDCCBBAA", color.ToHex());

            color.R = 0;

            Assert.Equal("00CCBBAA", color.ToHex());

            color.A = 255 / 255F;

            Assert.Equal("00CCBBFF", color.ToHex());
        }

        /// <summary>
        /// Tests that the individual float elements are laid out in RGBA order.
        /// </summary>
        [Fact]
        public void FloatLayout()
        {
            var color = new RgbaVector(1F, 2, 3, 4);
            Vector4 colorBase = Unsafe.As<RgbaVector, Vector4>(ref Unsafe.Add(ref color, 0));
            float[] ordered = new float[4];
            colorBase.CopyTo(ordered);

            Assert.Equal(1, ordered[0]);
            Assert.Equal(2, ordered[1]);
            Assert.Equal(3, ordered[2]);
            Assert.Equal(4, ordered[3]);
        }

        [Fact]
        public void RgbaVector_FromRgb48()
        {
            // arrange
            var input = default(RgbaVector);
            var actual = default(Rgb48);
            var expected = new Rgb48(65535, 0, 65535);

            // act
            input.FromRgb48(expected);
            actual.FromScaledVector4(input.ToScaledVector4());

            // assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void RgbaVector_FromRgba64()
        {
            // arrange
            var input = default(RgbaVector);
            var actual = default(Rgba64);
            var expected = new Rgba64(65535, 0, 65535, 0);

            // act
            input.FromRgba64(expected);
            actual.FromScaledVector4(input.ToScaledVector4());

            // assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void RgbaVector_FromBgra5551()
        {
            // arrange
            var rgb = default(RgbaVector);
            Vector4 expected = Vector4.One;

            // act
            rgb.FromBgra5551(new Bgra5551(1.0f, 1.0f, 1.0f, 1.0f));

            // assert
            Assert.Equal(expected, rgb.ToScaledVector4());
        }

        [Fact]
        public void RgbaVector_FromGrey16()
        {
            // arrange
            var rgba = default(RgbaVector);
            Vector4 expected = Vector4.One;

            // act
            rgba.FromL16(new L16(ushort.MaxValue));

            // assert
            Assert.Equal(expected, rgba.ToScaledVector4());
        }

        [Fact]
        public void RgbaVector_FromGrey8()
        {
            // arrange
            var rgba = default(RgbaVector);
            Vector4 expected = Vector4.One;

            // act
            rgba.FromL8(new L8(byte.MaxValue));

            // assert
            Assert.Equal(expected, rgba.ToScaledVector4());
        }
    }
}
