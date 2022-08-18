﻿using System;

namespace Meadow.Foundation.Graphics.Buffers
{
    /// <summary>
    /// Represents a 8bpp color pixel buffer
    /// </summary>
    public class BufferRgb332 : PixelBufferBase
    {
        /// <summary>
        /// Color mode of the buffer
        /// </summary>
        public override ColorType ColorMode => ColorType.Format8bppRgb332;

        public BufferRgb332(int width, int height, byte[] buffer) : base(width, height, buffer) { }

        public BufferRgb332(int width, int height) : base(width, height) { }

        public BufferRgb332() : base() { }

        public int GetPixel8bpp(int x, int y)
        {
            return Buffer[y * Width + x];
        }

        public override Color GetPixel(int x, int y)
        {
            byte r, g, b;
            int index = y * Width + x;

            r = (byte)(Buffer[index] >> 5 & 0x07);
            g = (byte)(Buffer[index] >> 2 & 0x07);
            b = (byte)(Buffer[index]      & 0x03);
            
            r = (byte)(r * 255 / 7);
            g = (byte)(g * 255 / 7);
            b = (byte)(b * 255 / 3);

            return new Color(r, g, b);
        }

        public void SetPixel(int x, int y, byte gray)
        {
            Buffer[y * Width + x] = gray;
        }

        public override void SetPixel(int x, int y, Color color)
        {
            SetPixel(x, y, color.Color8bppRgb332);
        }

        public override void Fill(Color color)
        {
            // split the color in to two byte values
            Buffer[0] = color.Color8bppRgb332;

            int arrayMidPoint = Buffer.Length / 2;
            int copyLength;

            for (copyLength = 1; copyLength < arrayMidPoint; copyLength <<= 1)
            {
                Array.Copy(Buffer, 0, Buffer, copyLength, copyLength);
            }

            Array.Copy(Buffer, 0, Buffer, copyLength, Buffer.Length - copyLength);
        }

        public override void Fill(int x, int y, int width, int height, Color color)
        {
            if (x < 0 || x + width > Width ||
                   y < 0 || y + height > Height)
            {
                throw new ArgumentOutOfRangeException();
            }

            byte value = color.Color8bppRgb332;
            int index = y * Width + x - 1;

            //fill the first line
            for (int i = 0; i < width; i++)
            {
                Buffer[++index] = value;
            }

            //array copy the rest
            for (int j = 0; j < height - 1; j++)
            {
                Array.Copy(Buffer,
                    (y + j) * Width + x,
                    Buffer,
                    (y + j + 1) * Width + x,
                    width);
            }
        }

        /// <summary>
        /// Invert the pixel
        /// </summary>
        /// <param name="x">x position of pixel</param>
        /// <param name="y">y position of pixel</param>
        public override void InvertPixel(int x, int y)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Write a buffer to specific location to the current buffer
        /// </summary>
        /// <param name="x">x origin</param>
        /// <param name="y">y origin</param>
        /// <param name="buffer">buffer to write</param>
        public override void WriteBuffer(int x, int y, IPixelBuffer buffer)
        {
            if (buffer.ColorMode == ColorMode)
            {
                int sourceIndex, destinationIndex;
                int length = buffer.Width;

                for (int i = 0; i < buffer.Height; i++)
                {
                    sourceIndex = length * i;
                    destinationIndex = Width * (y + i) + x;

                    Array.Copy(buffer.Buffer, sourceIndex, Buffer, destinationIndex, length); ;
                }
            }
            else
            {   // fall back to a slow write
                base.WriteBuffer(x, y, buffer);
            }
        }
    }
}
