// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.ImageSharp.Tests.TestUtilities.ImageComparison
{
    public class ImageDimensionsMismatchException : ImagesSimilarityException
    {
        public ImageDimensionsMismatchException(Size expectedSize, Size actualSize)
            : base($"The image dimensions {actualSize} do not match the expected {expectedSize}!")
        {
            this.ExpectedSize = expectedSize;
            this.ActualSize = actualSize;
        }

        public Size ExpectedSize { get; }

        public Size ActualSize { get; }
    }
}
