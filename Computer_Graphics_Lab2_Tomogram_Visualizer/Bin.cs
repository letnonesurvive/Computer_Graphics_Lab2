using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Computer_Graphics_Lab2_Tomogram_Visualizer
{
    class Bin // томограмма сохранена в бинарном файле, по этому обработкой файла занимается данный класс
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
}
