using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ASCIIficator
{
    class Converter
    {
        //загрузка статической картинки
        public static Bitmap LoadImage(Stream stream)
        {
            using (Image image = Image.FromStream(stream))
            {
                Bitmap bitmapImage = new Bitmap(image, new Size(image.Width, image.Height));
                image.Dispose();
                return bitmapImage;
            }    
        }

        //конветация статической картинки
        public static string ConvertImage(Bitmap bitmapImage, int sizeModifier)
        {

            StringBuilder asciiart = new StringBuilder();
            Rectangle bounds = new Rectangle(0, 0, bitmapImage.Width, bitmapImage.Height);

            #region greyscale image
            ColorMatrix matrix = new ColorMatrix();

            matrix[0, 0] = 1 / 3f;
            matrix[0, 1] = 1 / 3f;
            matrix[0, 2] = 1 / 3f;
            matrix[1, 0] = 1 / 3f;
            matrix[1, 1] = 1 / 3f;
            matrix[1, 2] = 1 / 3f;
            matrix[2, 0] = 1 / 3f;
            matrix[2, 1] = 1 / 3f;
            matrix[2, 2] = 1 / 3f;

            ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(matrix);

            Graphics gphGrey = Graphics.FromImage(bitmapImage);
            gphGrey.DrawImage(bitmapImage, bounds, 0, 0, bitmapImage.Width, bitmapImage.Height,
                GraphicsUnit.Pixel, attributes);

            gphGrey.Dispose();
            #endregion

            #region ascii image

            int pixhight = sizeModifier * 2;
            int pixseg = sizeModifier * pixhight;

            for (int h = 0; h < bitmapImage.Height / pixhight; h++)
            {
                int startY = (h * pixhight);
                for (int w = 0; w < bitmapImage.Width / sizeModifier; w++)
                {
                    int startX = (w * sizeModifier);
                    int allBrightness = 0;

                    for (int y = 0; y < sizeModifier; y++)
                    {
                        for (int x = 0; x < pixhight; x++)
                        {
                            int cY = y + startY;
                            int cX = x + startX;
                            try
                            {
                                Color c = bitmapImage.GetPixel(cX, cY);
                                int b = (int)(c.GetBrightness() * 100);
                                allBrightness = allBrightness + b;
                            }
                            catch
                            {
                                allBrightness = (allBrightness + 50);
                            }
                        }
                    }

                    int sb = (allBrightness / pixseg);
                    if (sb < 10)
                    {
                        asciiart.Append("#");
                    }
                    else if (sb < 17)
                    {
                        asciiart.Append("@");
                    }
                    else if (sb < 24)
                    {
                        asciiart.Append("&");
                    }
                    else if (sb < 31)
                    {
                        asciiart.Append("0");
                    }
                    else if (sb < 38)
                    {
                        asciiart.Append("*");
                    }
                    else if (sb < 45)
                    {
                        asciiart.Append("|");
                    }
                    else if (sb < 52)
                    {
                        asciiart.Append("!");
                    }
                    else if (sb < 59)
                    {
                        asciiart.Append(";");
                    }
                    else if (sb < 66)
                    {
                        asciiart.Append(":");
                    }
                    else if (sb < 73)
                    {
                        asciiart.Append("'");
                    }
                    else if (sb < 80)
                    {
                        asciiart.Append("`");
                    }
                    else if (sb < 87)
                    {
                        asciiart.Append(".");
                    }
                    else
                    {
                        asciiart.Append(" ");
                    }
                }
                asciiart.Append("\n");
            }
            #endregion

            return asciiart.ToString();

        }

        //конвертация всего сета кадров анимации
        public static void ConvertAnimation(List<ASCIIAnimationFrame> frames, int sizeModifier)
        {
            foreach(ASCIIAnimationFrame frame in frames)
            {
                frame.ConvertFrame(sizeModifier);
            }
        }

        //загрузка и разбор по кадрам анимации
        public static List<ASCIIAnimationFrame> GetASCIIAnimationFrames(Stream stream)
        {
            List<ASCIIAnimationFrame> frames = new List<ASCIIAnimationFrame>();
            using (var gifImage = Image.FromStream(stream))
            {
                var dimension = new FrameDimension(gifImage.FrameDimensionsList[0]); 
                int frameCount = gifImage.GetFrameCount(dimension); 
                for (var i = 0; i < frameCount; i++)
                {
                    gifImage.SelectActiveFrame(dimension, i); 
                    ASCIIAnimationFrame frame = new ASCIIAnimationFrame();
                    frame.bmpFrame = (Bitmap)gifImage.Clone(); 
                    frame.delay = gifImage.GetPropertyItem(20736).Value[i * 4] * 10;//получение задержки отображенич каждого кадра
                    if (frame.delay == 0) frame.delay = 100;
                    frames.Add(frame);
                }
                return frames;
            }
        }
    }

    //класс хранит данные одного кадра анимации
    public class ASCIIAnimationFrame
    {
        public Bitmap bmpFrame; //неконвертированнвый кадр
        public string asciiFrame; //конвертированныи кадр
        public int delay; //задержка отображения кадра

        //конвертирует текущий кадр
        public void ConvertFrame(int sizeModifier)
        {
            asciiFrame = Converter.ConvertImage(bmpFrame, sizeModifier);
        }
    }
}
