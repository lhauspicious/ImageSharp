// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Buffers;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;

using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Common.Helpers;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

// ReSharper disable InconsistentNaming
namespace SixLabors.ImageSharp.Tests
{
    public partial class ImageTests
    {
        public class WrapMemory
        {
            /// <summary>
            /// A <see cref="MemoryManager{T}"/> exposing the locked pixel memory of a <see cref="Bitmap"/> instance.
            /// TODO: This should be an example in https://github.com/SixLabors/Samples
            /// </summary>
            public class BitmapMemoryManager : MemoryManager<Bgra32>
            {
                private readonly Bitmap bitmap;

                private readonly BitmapData bmpData;

                private readonly int length;

                public BitmapMemoryManager(Bitmap bitmap)
                {
                    if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
                    {
                        throw new ArgumentException("bitmap.PixelFormat != PixelFormat.Format32bppArgb", nameof(bitmap));
                    }

                    this.bitmap = bitmap;
                    var rectangle = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
                    this.bmpData = bitmap.LockBits(rectangle, ImageLockMode.ReadWrite, bitmap.PixelFormat);
                    this.length = bitmap.Width * bitmap.Height;
                }

                public bool IsDisposed { get; private set; }

                protected override void Dispose(bool disposing)
                {
                    if (this.IsDisposed)
                    {
                        return;
                    }

                    if (disposing)
                    {
                        this.bitmap.UnlockBits(this.bmpData);
                    }

                    this.IsDisposed = true;
                }

                public override unsafe Span<Bgra32> GetSpan()
                {
                    void* ptr = (void*)this.bmpData.Scan0;
                    return new Span<Bgra32>(ptr, this.length);
                }

                public override unsafe MemoryHandle Pin(int elementIndex = 0)
                {
                    void* ptr = (void*)this.bmpData.Scan0;
                    return new MemoryHandle(ptr);
                }

                public override void Unpin()
                {
                }
            }

            [Fact]
            public void WrapMemory_CreatedImageIsCorrect()
            {
                Configuration cfg = Configuration.Default.Clone();
                var metaData = new ImageMetadata();

                var array = new Rgba32[25];
                var memory = new Memory<Rgba32>(array);

                using (var image = Image.WrapMemory(cfg, memory, 5, 5, metaData))
                {
                    Assert.True(image.TryGetSinglePixelSpan(out Span<Rgba32> imageSpan));
                    ref Rgba32 pixel0 = ref imageSpan[0];
                    Assert.True(Unsafe.AreSame(ref array[0], ref pixel0));

                    Assert.Equal(cfg, image.GetConfiguration());
                    Assert.Equal(metaData, image.Metadata);
                }
            }

            [Fact]
            public void WrapSystemDrawingBitmap_WhenObserved()
            {
                if (ShouldSkipBitmapTest)
                {
                    return;
                }

                using (var bmp = new Bitmap(51, 23))
                {
                    using (var memoryManager = new BitmapMemoryManager(bmp))
                    {
                        Memory<Bgra32> memory = memoryManager.Memory;
                        Bgra32 bg = Color.Red;
                        Bgra32 fg = Color.Green;

                        using (var image = Image.WrapMemory(memory, bmp.Width, bmp.Height))
                        {
                            Assert.Equal(memory, image.GetRootFramePixelBuffer().GetSingleMemory());
                            Assert.True(image.TryGetSinglePixelSpan(out Span<Bgra32> imageSpan));
                            imageSpan.Fill(bg);
                            for (var i = 10; i < 20; i++)
                            {
                                image.GetPixelRowSpan(i).Slice(10, 10).Fill(fg);
                            }
                        }

                        Assert.False(memoryManager.IsDisposed);
                    }

                    string fn = System.IO.Path.Combine(
                        TestEnvironment.ActualOutputDirectoryFullPath,
                        $"{nameof(this.WrapSystemDrawingBitmap_WhenObserved)}.bmp");

                    bmp.Save(fn, ImageFormat.Bmp);
                }
            }

            [Fact]
            public void WrapSystemDrawingBitmap_WhenOwned()
            {
                if (ShouldSkipBitmapTest)
                {
                    return;
                }

                using (var bmp = new Bitmap(51, 23))
                {
                    var memoryManager = new BitmapMemoryManager(bmp);
                    Bgra32 bg = Color.Red;
                    Bgra32 fg = Color.Green;

                    using (var image = Image.WrapMemory(memoryManager, bmp.Width, bmp.Height))
                    {
                        Assert.Equal(memoryManager.Memory, image.GetRootFramePixelBuffer().GetSingleMemory());
                        Assert.True(image.TryGetSinglePixelSpan(out Span<Bgra32> imageSpan));
                        imageSpan.Fill(bg);
                        for (var i = 10; i < 20; i++)
                        {
                            image.GetPixelRowSpan(i).Slice(10, 10).Fill(fg);
                        }
                    }

                    Assert.True(memoryManager.IsDisposed);

                    string fn = System.IO.Path.Combine(
                        TestEnvironment.ActualOutputDirectoryFullPath,
                        $"{nameof(this.WrapSystemDrawingBitmap_WhenOwned)}.bmp");

                    bmp.Save(fn, ImageFormat.Bmp);
                }
            }

            private static bool ShouldSkipBitmapTest =>
                !TestEnvironment.Is64BitProcess || (TestHelpers.ImageSharpBuiltAgainst != "netcoreapp3.1" && TestHelpers.ImageSharpBuiltAgainst != "netcoreapp2.1");
        }
    }
}
