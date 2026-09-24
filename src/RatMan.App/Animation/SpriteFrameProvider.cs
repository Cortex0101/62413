using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

namespace RatMan.App.Animation;

public interface ISpriteFrameProvider
{
    ImageSource GetFrame(SpriteFrameDefinition frame);
}

public sealed class SpriteFrameProvider : ISpriteFrameProvider
{
    private readonly IReadOnlyDictionary<SpriteRectangle, ImageSource> frames;

    public SpriteFrameProvider(SpriteSheetDefinition sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        BitmapSource texture = LoadTexture(sheet.ResourceUri);
        Dictionary<SpriteRectangle, ImageSource> loadedFrames = [];

        foreach (SpriteRectangle source in sheet.Clips.Values
                     .SelectMany(clip => clip.Frames)
                     .Select(frame => frame.Source)
                     .Distinct())
        {
            ValidateBounds(source, texture.PixelWidth, texture.PixelHeight);

            CroppedBitmap croppedFrame = new(
                texture,
                new Int32Rect(source.X, source.Y, source.Width, source.Height));
            if (croppedFrame.CanFreeze)
            {
                croppedFrame.Freeze();
            }

            loadedFrames.Add(source, croppedFrame);
        }

        frames = loadedFrames;
    }

    public ImageSource GetFrame(SpriteFrameDefinition frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        return frames.TryGetValue(frame.Source, out ImageSource? image)
            ? image
            : throw new KeyNotFoundException("The requested frame is not part of this sprite sheet.");
    }

    private static BitmapSource LoadTexture(Uri resourceUri)
    {
        try
        {
            Uri resolvedUri = resourceUri.IsAbsoluteUri
                ? resourceUri
                : PackUriHelper.Create(
                    new Uri("application:///", UriKind.Absolute),
                    resourceUri);
            StreamResourceInfo resource = Application.GetResourceStream(resolvedUri)
                ?? throw new FileNotFoundException(
                    $"Sprite sheet resource '{resourceUri}' was not found.");
            using Stream stream = resource.Stream;
            PngBitmapDecoder decoder = new(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            BitmapSource texture = decoder.Frames[0];

            if (texture.CanFreeze)
            {
                texture.Freeze();
            }

            return texture;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or NotSupportedException)
        {
            throw new InvalidOperationException(
                $"Unable to load sprite sheet resource '{resourceUri}'.",
                exception);
        }
    }

    private static void ValidateBounds(SpriteRectangle source, int textureWidth, int textureHeight)
    {
        long right = (long)source.X + source.Width;
        long bottom = (long)source.Y + source.Height;
        if (right > textureWidth || bottom > textureHeight)
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                $"Sprite frame {source} exceeds texture bounds {textureWidth}x{textureHeight}.");
        }
    }
}
