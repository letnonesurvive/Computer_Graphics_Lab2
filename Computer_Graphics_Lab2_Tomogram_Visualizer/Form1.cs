using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;

namespace Computer_Graphics_Lab2_Tomogram_Visualizer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Bin bin=new Bin();
        View view=new View();
        bool loaded = false;
        int currentLayer=0;

        int FrameCount;//счетчик кадров
        DateTime NextFPSUpdate = DateTime.Now.AddSeconds(1);

        bool needReload = false;

        internal class Bin // томограмма сохранена в бинарном файле, по этому обработкой файла занимается данный класс
        {
            public static int X, Y, Z; 
            public static short[] array;// основной массив
            public Bin() { }

            public void readBin(string path)//передаем путь
            {
                if (File.Exists(path))//если файл с таким путем существует
                {
                    BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open));// объект способный читать бинарный файл

                    X = reader.ReadInt32();
                    Y = reader.ReadInt32();
                    Z = reader.ReadInt32();

                    int arraySize = X * Y * Z;

                    array = new short[arraySize];
                    for (int i = 0; i < arraySize; i++)
                    {
                        array[i] = reader.ReadInt16();//записываем числа из данного файла
                    }
                }
            }
        }

        void DisplayFPS()//выводит фпс на экран
        {
            if(DateTime.Now>=NextFPSUpdate)
            {
                this.Text = String.Format("CT Visualizer (fps={0})", FrameCount);
                NextFPSUpdate = DateTime.Now.AddSeconds(1);
                FrameCount = 0;
            }
            FrameCount++;
        }

        public static int Clamp(int value, int min, int max)//функция как в предыдущей лабе
        {
            if (value > max)
                return max;
            if (value < min)
                return min;
            return value;
        }
        static Color TransferFuncltion(short value)//функция перевода значения плотностей томограммы в цвет
        {
            int min = 0;
            int max = 2000;
            int newVal = Clamp((value - min) * 255 / (max - min), 0, 255);//переводил плотность окна визуализации от 0 до 2000 линейно в цвет от черного до белого (от 0 до 255).
            return Color.FromArgb(255, newVal, newVal, newVal);
        }

        private void Button1_Click(object sender, EventArgs e)//по кнопке открывается файл 
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if(dialog.ShowDialog()==DialogResult.OK)
            {
                string str = dialog.FileName;
                bin.readBin(str);
                view.SetupView(glControl1.Width, glControl1.Height);// передаем окно OpenGL
                loaded = true;
                glControl1.Invalidate();
                trackBar1.Maximum = Bin.Z-1;//число деление соответствует числу слоев томограммы
            }
        }

        private void GlControl1_Paint(object sender, PaintEventArgs e)
        {
            if(loaded)//отрисовываем только когда данные загружены
            {
                if (radioButton1.Checked)//отрисовка четырехугольниками
                    view.DrawQuads(currentLayer);
                else
                {
                    if (needReload)//отрисовка текстурой, текстура накладывается билинейной интерполяцией
                    {
                        view.generateTextureImage(currentLayer);
                        view.Load2DTexture();
                        needReload = false;
                    }
                    view.DrawTexture();
                }
                glControl1.SwapBuffers();//функция SwapBuffers загружает наш буфер в буфер экрана
            }
        }
        private void TrackBar1_Scroll(object sender, EventArgs e)
        {
            currentLayer = trackBar1.Value;//переключаем слои с помощью трекбара
            needReload = true;//Переменную needReload необходимо устанавливать в значение true тогда,когда мы изменяем trackbar.
        }
        void Application_Idle(object sender, EventArgs e)//проверяет, занято ли OpenGL окно работой
        {
            while(glControl1.IsIdle)
            {
                DisplayFPS();
                glControl1.Invalidate();//заставляет кадр рендариться заново
            }
        }

        private void Form1_Load(object sender, EventArgs e)//Чтобы функция Application_Idle работала автоматически
        {
            Application.Idle += Application_Idle;
        }
        internal class View // содержит функции для визуализации томограммы
        {
            Bitmap textureImage;
            int VBOtexture;//хранит номер текстуры в памяти
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
                        textureImage.SetPixel(i, j, TransferFuncltion(Bin.array[pixelNumber]));
                    }
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

                for (int x_coord = 0; x_coord < Bin.X - 1; x_coord++)
                {
                    for (int y_coord = 0; y_coord < Bin.Y - 1; y_coord++)
                    {
                        short value;
                        //1 вершина
                        value = Bin.array[x_coord + y_coord * Bin.X + layerNumber * Bin.X * Bin.Y];
                        GL.Color3(TransferFuncltion(value));
                        GL.Vertex2(x_coord, y_coord);

                        //2 вершина
                        value = Bin.array[x_coord + (y_coord + 1) * Bin.X + layerNumber * Bin.X * Bin.Y];
                        GL.Color3(TransferFuncltion(value));
                        GL.Vertex2(x_coord, y_coord + 1);

                        //3 вершина
                        value = Bin.array[x_coord + 1 + (y_coord + 1) * Bin.X + layerNumber * Bin.X * Bin.Y];
                        GL.Color3(TransferFuncltion(value));
                        GL.Vertex2(x_coord + 1, y_coord + 1);

                        //4 вершина
                        value = Bin.array[x_coord + 1 + y_coord * Bin.X + layerNumber * Bin.X * Bin.Y];
                        GL.Color3(TransferFuncltion(value));
                        GL.Vertex2(x_coord + 1, y_coord);
                    }
                }
                GL.End();
            }
        }
    }
}
