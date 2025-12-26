//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.FileStorage.Contract
{
    /// <summary>
    /// Interface for image processing operations on stored files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides capabilities for processing images stored in file storage,
    /// including:
    /// - Thumbnail generation
    /// - Image resizing
    /// - Format conversion
    /// - Image transformations
    /// </para>
    /// <para>
    /// Image processing can be performed synchronously during upload or asynchronously
    /// via background jobs. Processed images are stored as separate files with modified paths.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Upload an image
    /// await fileStorage.UploadAsync("photos/image.jpg", imageBytes, "image/jpeg");
    /// 
    /// // Generate a thumbnail
    /// var thumbnailResult = await imageProcessing.GenerateThumbnailAsync(
    ///     "photos/image.jpg",
    ///     "photos/thumbnails/image_thumb.jpg",
    ///     width: 200,
    ///     height: 200,
    ///     cancellationToken);
    /// 
    /// // Resize an image
    /// var resizedResult = await imageProcessing.ResizeImageAsync(
    ///     "photos/image.jpg",
    ///     "photos/resized/image_800x600.jpg",
    ///     width: 800,
    ///     height: 600,
    ///     cancellationToken);
    /// </code>
    /// </example>
    public interface IImageProcessingStorage
    {
        /// <summary>
        /// Generates a thumbnail from an image file.
        /// </summary>
        /// <param name="sourceFilePath">The path of the source image.</param>
        /// <param name="thumbnailPath">The path where the thumbnail should be saved.</param>
        /// <param name="width">The width of the thumbnail in pixels.</param>
        /// <param name="height">The height of the thumbnail in pixels.</param>
        /// <param name="mode">The thumbnail generation mode (crop, fit, pad).</param>
        /// <param name="quality">The JPEG quality (1-100) if saving as JPEG.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The upload result for the generated thumbnail.</returns>
        /// <remarks>
        /// <para>
        /// The thumbnail is generated from the source image and saved as a separate file.
        /// The source image is not modified.
        /// </para>
        /// <para>
        /// Supported image formats depend on the image processing library used (typically JPEG, PNG, GIF, WebP).
        /// </para>
        /// </remarks>
        Task<Results.FileUploadResult> GenerateThumbnailAsync(
            string sourceFilePath,
            string thumbnailPath,
            int width,
            int height,
            ThumbnailMode mode = ThumbnailMode.Crop,
            int quality = 85,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resizes an image to specified dimensions.
        /// </summary>
        /// <param name="sourceFilePath">The path of the source image.</param>
        /// <param name="destinationPath">The path where the resized image should be saved.</param>
        /// <param name="width">The target width in pixels (null to maintain aspect ratio).</param>
        /// <param name="height">The target height in pixels (null to maintain aspect ratio).</param>
        /// <param name="mode">The resize mode (crop, fit, pad, stretch).</param>
        /// <param name="quality">The JPEG quality (1-100) if saving as JPEG.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The upload result for the resized image.</returns>
        Task<Results.FileUploadResult> ResizeImageAsync(
            string sourceFilePath,
            string destinationPath,
            int? width = null,
            int? height = null,
            ResizeMode mode = ResizeMode.Fit,
            int quality = 85,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Converts an image to a different format.
        /// </summary>
        /// <param name="sourceFilePath">The path of the source image.</param>
        /// <param name="destinationPath">The path where the converted image should be saved.</param>
        /// <param name="targetFormat">The target image format (JPEG, PNG, WebP, etc.).</param>
        /// <param name="quality">The JPEG quality (1-100) if converting to JPEG.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The upload result for the converted image.</returns>
        Task<Results.FileUploadResult> ConvertImageFormatAsync(
            string sourceFilePath,
            string destinationPath,
            ImageFormat targetFormat,
            int quality = 85,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies image transformations (rotate, flip, crop, etc.).
        /// </summary>
        /// <param name="sourceFilePath">The path of the source image.</param>
        /// <param name="destinationPath">The path where the transformed image should be saved.</param>
        /// <param name="transformations">The transformations to apply.</param>
        /// <param name="quality">The JPEG quality (1-100) if saving as JPEG.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The upload result for the transformed image.</returns>
        Task<Results.FileUploadResult> TransformImageAsync(
            string sourceFilePath,
            string destinationPath,
            ImageTransformations transformations,
            int quality = 85,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers an image processing hook that runs automatically when images are uploaded.
        /// </summary>
        /// <param name="hook">The image processing hook to register.</param>
        /// <remarks>
        /// <para>
        /// Hooks are executed automatically after image uploads. Multiple hooks can be registered
        /// and will be executed in registration order.
        /// </para>
        /// <para>
        /// Hooks can generate thumbnails, resize images, or perform other transformations
        /// automatically without requiring explicit calls.
        /// </para>
        /// </remarks>
        void RegisterImageProcessingHook(IImageProcessingHook hook);

        /// <summary>
        /// Unregisters an image processing hook.
        /// </summary>
        /// <param name="hook">The hook to unregister.</param>
        void UnregisterImageProcessingHook(IImageProcessingHook hook);
    }

    /// <summary>
    /// Thumbnail generation mode.
    /// </summary>
    public enum ThumbnailMode
    {
        /// <summary>
        /// Crop the image to fit the exact dimensions (may lose parts of the image).
        /// </summary>
        Crop,

        /// <summary>
        /// Fit the image within the dimensions while maintaining aspect ratio (may add padding).
        /// </summary>
        Fit,

        /// <summary>
        /// Pad the image to fit the exact dimensions while maintaining aspect ratio.
        /// </summary>
        Pad
    }

    /// <summary>
    /// Image resize mode.
    /// </summary>
    public enum ResizeMode
    {
        /// <summary>
        /// Crop the image to fit the exact dimensions.
        /// </summary>
        Crop,

        /// <summary>
        /// Fit the image within the dimensions while maintaining aspect ratio.
        /// </summary>
        Fit,

        /// <summary>
        /// Pad the image to fit the exact dimensions while maintaining aspect ratio.
        /// </summary>
        Pad,

        /// <summary>
        /// Stretch the image to fit the exact dimensions (may distort the image).
        /// </summary>
        Stretch
    }

    /// <summary>
    /// Image format.
    /// </summary>
    public enum ImageFormat
    {
        /// <summary>
        /// JPEG format.
        /// </summary>
        Jpeg,

        /// <summary>
        /// PNG format.
        /// </summary>
        Png,

        /// <summary>
        /// WebP format.
        /// </summary>
        WebP,

        /// <summary>
        /// GIF format.
        /// </summary>
        Gif,

        /// <summary>
        /// BMP format.
        /// </summary>
        Bmp
    }

    /// <summary>
    /// Image transformations to apply.
    /// </summary>
    public class ImageTransformations
    {
        /// <summary>
        /// Gets or sets the rotation angle in degrees (0, 90, 180, 270).
        /// </summary>
        public int? Rotate { get; set; }

        /// <summary>
        /// Gets or sets whether to flip horizontally.
        /// </summary>
        public bool FlipHorizontal { get; set; }

        /// <summary>
        /// Gets or sets whether to flip vertically.
        /// </summary>
        public bool FlipVertical { get; set; }

        /// <summary>
        /// Gets or sets the crop rectangle (x, y, width, height).
        /// </summary>
        public CropRectangle? Crop { get; set; }

        /// <summary>
        /// Gets or sets the brightness adjustment (-100 to 100).
        /// </summary>
        public int? Brightness { get; set; }

        /// <summary>
        /// Gets or sets the contrast adjustment (-100 to 100).
        /// </summary>
        public int? Contrast { get; set; }

        /// <summary>
        /// Gets or sets the saturation adjustment (-100 to 100).
        /// </summary>
        public int? Saturation { get; set; }
    }

    /// <summary>
    /// Represents a crop rectangle.
    /// </summary>
    public class CropRectangle
    {
        /// <summary>
        /// Gets or sets the X coordinate of the top-left corner.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate of the top-left corner.
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Gets or sets the width of the crop rectangle.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the height of the crop rectangle.
        /// </summary>
        public int Height { get; set; }
    }

    /// <summary>
    /// Hook for automatic image processing after upload.
    /// </summary>
    public interface IImageProcessingHook
    {
        /// <summary>
        /// Determines whether this hook should process the uploaded file.
        /// </summary>
        /// <param name="filePath">The path of the uploaded file.</param>
        /// <param name="contentType">The content type of the file.</param>
        /// <returns><c>true</c> if this hook should process the file; otherwise, <c>false</c>.</returns>
        bool ShouldProcess(string filePath, string contentType);

        /// <summary>
        /// Processes the uploaded image file.
        /// </summary>
        /// <param name="sourceFilePath">The path of the uploaded file.</param>
        /// <param name="fileStorage">The file storage instance for saving processed images.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task representing the processing operation.</returns>
        Task ProcessAsync(
            string sourceFilePath,
            IFileStorage fileStorage,
            CancellationToken cancellationToken = default);
    }
}

