﻿using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace Computer_Graphics_Lab2_Tomogram_Visualizer
{
    class View // содержит функции для визуализации томограммы
    {
        Bitmap textureImage;
        int VBOtexture;//хранит номер текстуры в памяти
        public int minimum = 0;
        public int window = 2000;
        public int k = 0;//счетчик вершин
        public static int Clamp(int value, int min, int max)//функция как в предыдущей лабе
        {
            if (value > max)
                return max;
            if (value < min)
                return min;
            return value;
        }

        Color TransferFunction(int value)//функция перевода значения плотностей томограммы в цвет
        {
            int min = minimum;
            int max = minimum + window;
            int newVal = Clamp((value - min) * 255 / (max - min), 0, 255);//переводил плотность окна визуализации от 0 до 2000 линейно в цвет от черного до белого (от 0 до 255).
            return Color.FromArgb(255, newVal, newVal, newVal);
        }
        public void Load2DTexture()
        {
            GL.BindTexture(TextureTarget.Texture2D, VBOtexture);//связывает текстурку, делает ее активной
            BitmapData data = textureImage.LockBits(
                new Rectangle(0, 0, textureImage.Width, textureImage.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                data.Width, data.Height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                PixelType.UnsignedByte, data.Scan0);//загружает текстуру в память видеокарты

            textureImage.UnlockBits(data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);

            ErrorCode Er = GL.GetError();
            string str = Er.ToString();
        }
        public void generateTextureImage(int layerNumber)//генерировать изображение из томограммы
        {
            textureImage = new Bitmap(Bin.X, Bin.Y);
            for (int i = 0; i < Bin.X; i++)
            {
                for (int j = 0; j < Bin.Y; j++)
                {
                    int pixelNumber = i + j * Bin.X + layerNumber * Bin.X * Bin.Y;
                    textureImage.SetPixel(i, j, TransferFunction(Bin.array[pixelNumber]));
                }
            }
        }
        public void SetupView(int width, int height) // настраивает окно вывода
        {
            GL.ShadeModel(ShadingModel.Smooth);//интерполяция цветов
            GL.MatrixMode(MatrixMode.Projection);//матрица проекции инициализируется
            GL.LoadIdentity();//тождественное преобразование
            GL.Ortho(0, Bin.X, 0, Bin.Y, -1, 1);//ортогональное проецирование массива данных томограммы в окно вывода
            GL.Viewport(0, 0, width, height);

        }
        public void DrawQuads(int layerNumber)//этот метод рисует с помощью четырехугольников, подаем на вход номер слоя
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Begin(BeginMode.Quads);
            short value;
            k = 0;
            for (int x = 0; x < Bin.X - 1; x++)
            {

                for (int y = 0; y < Bin.Y - 1; y++)
                {
                    //1 вершина
                    value = Bin.array[x + y * Bin.X + layerNumber * Bin.X * Bin.Y];
                    GL.Color3(TransferFunction(value));
                    GL.Vertex2(x, y);
                    k++;
                    //2 вершина
                    value = Bin.array[x + (y + 1) * Bin.X + layerNumber * Bin.X * Bin.Y];
                    GL.Color3(TransferFunction(value));
                    GL.Vertex2(x, y + 1);
                    k++;

                    //3 вершина
                    value = Bin.array[x + 1 + (y + 1) * Bin.X + layerNumber * Bin.X * Bin.Y];
                    GL.Color3(TransferFunction(value));
                    GL.Vertex2(x + 1, y + 1);
                    k++;

                    //4 вершина
                    value = Bin.array[x + 1 + y * Bin.X + layerNumber * Bin.X * Bin.Y];
                    GL.Color3(TransferFunction(value));
                    GL.Vertex2(x + 1, y);
                    k++;
                }
            }
            GL.End();
        }
        public void DrawQuadStrips(int layer_number)
        {
            int value;
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            k = 0;
            for (int x = 0; x < Bin.X; x++)
            {
                GL.Begin(BeginMode.QuadStrip);
                value = Bin.array[x + 0 * Bin.X + layer_number * Bin.X * Bin.Y];
                GL.Color3(TransferFunction(value));
                GL.Vertex2(x, 0);
                k ++;

                value = Bin.array[x + 1 + 0 * Bin.X + layer_number * Bin.X * Bin.Y];
                GL.Color3(TransferFunction(value));
                GL.Vertex2(x + 1, 0);
                k++;

                value = Bin.array[x + (0 + 1) * Bin.X + layer_number * Bin.X * Bin.Y];
                GL.Color3(TransferFunction(value));
                GL.Vertex2(x, 0 + 1);
                k++;

                value = Bin.array[x + 1 + (0 + 1) * Bin.X + layer_number * Bin.X * Bin.Y];
                GL.Color3(TransferFunction(value));
                GL.Vertex2(x + 1, 0 + 1);
                k++;

                for (int y = 1; y < Bin.Y; y++)
                {
                    value = Bin.array[x + (y + 1) * Bin.X + layer_number * Bin.X * Bin.Y];
                    GL.Color3(TransferFunction(value));
                    GL.Vertex2(x, y + 1);
                    k++;

                    value = Bin.array[x + 1 + (y + 1) * Bin.X + layer_number * Bin.X * Bin.Y];
                    GL.Color3(TransferFunction(value));
                    GL.Vertex2(x + 1, y + 1);
                    k++;
                }

                GL.End();
            }
        }
        public void DrawTexture()//выбирает текстуру и рисует один прямоугольник с наложенное текстурой 
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, VBOtexture);

            GL.Begin(BeginMode.Quads);
            GL.Color3(Color.White);
            GL.TexCoord2(0f, 0f);
            GL.Vertex2(0, 0);
            GL.TexCoord2(0f, 1f);
            GL.Vertex2(0, Bin.Y);
            GL.TexCoord2(1f, 1f);
            GL.Vertex2(Bin.X, Bin.Y);
            GL.TexCoord2(1f, 0f);
            GL.Vertex2(Bin.X, 0);
            GL.End();

            GL.Disable(EnableCap.Texture2D);
        }
    }
}
