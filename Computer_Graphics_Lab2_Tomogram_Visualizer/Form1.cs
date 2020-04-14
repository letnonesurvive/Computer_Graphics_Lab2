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
        internal class Bin
        {
            public static int X, Y, Z;
            public static short[] array;
            public Bin() { }

            public void readBin(string path)
            {
                if (File.Exists(path))
                {
                    BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open));

                    X = reader.ReadInt32();
                    Y = reader.ReadInt32();
                    Z = reader.ReadInt32();

                    int arraySize = X * Y * Z;

                    array = new short[arraySize];
                    for (int i = 0; i < arraySize; i++)
                    {
                        array[i] = reader.ReadInt16();
                    }
                }
            }
        }
        public static int Clamp(int value, int min, int max)
        {
            if (value > max)
                return max;
            if (value < min)
                return min;
            return value;
        }
        internal class  View
        {
            public void SetupView(int width, int height)
            {
                GL.ShadeModel(ShadingModel.Smooth);
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Ortho(0, Bin.X, 0, Bin.Y, -1, 1);
                GL.Viewport(0, 0, width, height);

            }
            Color TransferFuncltion(short value)
            {
                int min = 0;
                int max = 2000;
                int newVal = Clamp((value - min) * 255 / (max - min), 0, 255);
                return Color.FromArgb(255, newVal, newVal, newVal);
            }

            public void DrawQuads(int layerNumber)
            {
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.Begin(BeginMode.Quads);

                for(int x_coord=0;x_coord<Bin.X-1;x_coord++)
                {
                    for(int y_coord=0;y_coord<Bin.Y-1;y_coord++)
                    {
                        short value;
                        value = Bin.array[x_coord + y_coord * Bin.X + layerNumber * Bin.X * Bin.Y];
                        GL.Color3(TransferFuncltion(value));
                        GL.Vertex2(x_coord, y_coord);

                        value= Bin.array[x_coord + (y_coord+1) * Bin.X + layerNumber * Bin.X * Bin.Y];
                        GL.Color3(TransferFuncltion(value));
                        GL.Vertex2(x_coord, y_coord+1);

                        value = Bin.array[x_coord+1 + (y_coord + 1) * Bin.X + layerNumber * Bin.X * Bin.Y];
                        GL.Color3(TransferFuncltion(value));
                        GL.Vertex2(x_coord+1, y_coord + 1);

                        value = Bin.array[x_coord + 1 + y_coord * Bin.X + layerNumber * Bin.X * Bin.Y];
                        GL.Color3(TransferFuncltion(value));
                        GL.Vertex2(x_coord + 1, y_coord);
                    }
                }
                GL.End();
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if(dialog.ShowDialog()==DialogResult.OK)
            {
                string str = dialog.FileName;
                bin.readBin(str);
                view.SetupView(glControl1.Width, glControl1.Height);
                loaded = true;
                glControl1.Invalidate();
                trackBar1.Maximum = Bin.Z-1;
            }
        }

        private void GlControl1_Paint(object sender, PaintEventArgs e)
        {
            if(loaded)
            {
                view.DrawQuads(currentLayer);
                glControl1.SwapBuffers();
            }
        }
        private void TrackBar1_Scroll(object sender, EventArgs e)
        {
            currentLayer = trackBar1.Value;
            if (loaded)
            {
                view.DrawQuads(currentLayer);
                glControl1.SwapBuffers();
            }
        }
    }
}
