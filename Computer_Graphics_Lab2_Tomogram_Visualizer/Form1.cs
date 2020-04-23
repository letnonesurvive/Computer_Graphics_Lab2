using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        Bin bin = new Bin();
        View view = new View();
        bool loaded = false;
        int currentLayer = 0;
        int FrameCount;//счетчик кадров
        DateTime NextFPSUpdate = DateTime.Now.AddSeconds(1);

        bool needReload = false;

        void DisplayFPS()//выводит фпс на экран
        {
            if (DateTime.Now >= NextFPSUpdate)
            {
                this.Text = String.Format("CT Visualizer (fps={0})", FrameCount);
                NextFPSUpdate = DateTime.Now.AddSeconds(1);
                FrameCount = 0;
            }
            FrameCount++;
        }

        private void Button1_Click(object sender, EventArgs e)//по кнопке открывается файл 
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string str = dialog.FileName;
                bin.readBin(str);
                view.SetupView(glControl1.Width, glControl1.Height);// передаем окно OpenGL
                loaded = true;
                glControl1.Invalidate();
                trackBar1.Maximum = Bin.Z - 1;//число деление соответствует числу слоев томограммы
            }
        }

        private void GlControl1_Paint(object sender, PaintEventArgs e)
        {
            if (loaded)//отрисовываем только когда данные загружены
            {
                if (radioButton1.Checked)//отрисовка четырехугольниками
                {
                    view.DrawQuads(currentLayer);
                    label4.Text = "Число нарисованных вершин" + Convert.ToString(view.k);
                    view.k=0;
                }
                else if(radioButton3.Checked)
                {
                    view.DrawQuadStrips(currentLayer);
                    label4.Text = "Число нарисованных вершин" + Convert.ToString(view.k);
                    view.k = 0;
                }
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
            while (glControl1.IsIdle)
            {
                DisplayFPS();
                glControl1.Invalidate();//заставляет кадр рендариться заново
            }
        }

        private void Form1_Load(object sender, EventArgs e)//Чтобы функция Application_Idle работала автоматически
        {
            Application.Idle += Application_Idle;
        }
        private void TrackBar2_Scroll(object sender, EventArgs e)
        {
            view.minimum = trackBar2.Value;
            needReload = true;
        }
        private void TrackBar3_Scroll(object sender, EventArgs e)
        {
            view.window = trackBar3.Value;
            needReload = true;
        }
    }
}
