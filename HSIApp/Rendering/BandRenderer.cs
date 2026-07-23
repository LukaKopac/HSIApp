namespace HSIApp;

using System.Windows.Media;
using System.Windows.Media.Imaging;

public static class BandRenderer
{
    public static float[,] NormalizeImage(float[,] image)
    {
        int height = image.GetLength(0);
        int width = image.GetLength(1);

        float minValue = image[0, 0];
        float maxValue = image[0, 0];

        float normalizedImagePixel;
        float[,] normalizedImage = new float[height, width];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (image[y, x] < minValue)
                {
                    minValue = image[y, x];
                }

                if (image[y, x] > maxValue)
                {
                    maxValue = image[y, x];
                }
            }
        }

        // if image is constant, return a black image
        if (maxValue == minValue)
        {
            return normalizedImage;
        }

        float range = maxValue - minValue;

        // normalize to 0-255
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                normalizedImagePixel = ((image[y, x] - minValue) / (range)) * 255f;

                normalizedImage[y, x] = normalizedImagePixel;
            }
        }

        return normalizedImage;

    }

    public static byte[] ToByteArray(float[,] image)
    {
        int height = image.GetLength(0);
        int width = image.GetLength(1);

        byte[] bytes = new byte[height * width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bytes[y * width + x] = Convert.ToByte(image[y, x]);
            }
        }

        return bytes;
    }

    
    public static BitmapSource ToBitmap(float[,] image)
    {
        int height = image.GetLength(0);
        int width = image.GetLength(1);

        byte[] pixels = ToByteArray(image);

        int stride = width;

        return BitmapSource.Create(
            width,                 // image width
            height,                // image height
            96,                    // horizontal dpi
            96,                    // vertical dpi
            PixelFormats.Gray8,
            null,
            pixels,
            stride);
    }
    
}
