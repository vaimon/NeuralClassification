using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using AForge.Imaging.Filters;

namespace NeuralNetwork1
{
    using FastBitmap;
    public enum LetterType : byte { SH = 0, N, G, E, P, T, TS, Z, A, SOFT, Undef };
    public class DatasetProcessor
    {
        public static string LetterTypeToString(LetterType type)
        {
            switch (type)
            {
                case LetterType.SH:
                    return "Ш";
                case LetterType.N:
                    return "Н";
                case LetterType.G:
                    return "Г";
                case LetterType.E:
                    return "Е";
                case LetterType.SOFT:
                    return "Ь";
                case LetterType.Z:
                    return "З";
                case LetterType.T:
                    return "Т";
                case LetterType.TS:
                    return "Ц";
                case LetterType.P:
                    return "П";
                case LetterType.A:
                    return "А";
                case LetterType.Undef:
                    return "Неизвестно";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        
        // Да, плохо. Но хотя бы так...
        private const string databaseLocation = "..\\..\\dataset";
        private Random random;
        public int LetterCount { get; set; }

        private Dictionary<LetterType, List<string>> structure;
        public DatasetProcessor()
        {
            random = new Random();
            structure = new Dictionary<LetterType, List<string>>();
            structure[LetterType.E] = new List<string>();
            structure[LetterType.G] = new List<string>();
            structure[LetterType.N] = new List<string>();
            structure[LetterType.SOFT] = new List<string>();
            structure[LetterType.SH] = new List<string>();
            structure[LetterType.A] = new List<string>();
            structure[LetterType.P] = new List<string>();
            structure[LetterType.T] = new List<string>();
            structure[LetterType.Z] = new List<string>();
            structure[LetterType.TS] = new List<string>();
            foreach (var letter in structure)
            {
                DirectoryInfo d = new DirectoryInfo(databaseLocation + $"\\{LetterTypeToString(letter.Key)}");
                letter.Value.AddRange(d.GetFiles("*.jpeg").Select(f => f.FullName));
            }
        }

        double[] processImage(Bitmap original)
        {
            var uProcessed = AForge.Imaging.UnmanagedImage.FromManagedImage(original);

            AForge.Imaging.BlobCounter blobber = new AForge.Imaging.BlobCounter();
            blobber.MinHeight = 5;
            blobber.MinWidth = 5;
            blobber.ObjectsOrder = AForge.Imaging.ObjectsOrder.XY;

            AForge.Imaging.Filters.Invert InvertFilter = new AForge.Imaging.Filters.Invert();
            InvertFilter.ApplyInPlace(uProcessed);
            
            blobber.ProcessImage(uProcessed);
            var rects = blobber.GetObjectsRectangles().Where(x=> x.Width > 5 && x.Height > 5);
            double scaleFactor = rects.Max(x => x.Width);
            var res = rects.Take(5).Select(x => x.Width / scaleFactor).Where(x=> x > 0.1).ToList();
            while (res.Count < 5)
            {
                res.Add(0);
            }

            return res.ToArray();
        }

        // Здесь мы полагаемся чисто на рандом
        public SamplesSet getTestDataset(int count)
        {
            SamplesSet set = new SamplesSet();
            for (int type = 0; type < LetterCount; type++)
            {
                for (int i = 0; i < 100; i++)
                {
                    var sample = structure[(LetterType)type][random.Next(structure[(LetterType)type].Count)];
                    set.AddSample(new Sample(processImage(new Bitmap(sample)), LetterCount, (LetterType)type));
                }
            }
            set.shuffle();
            return set;
        }
        
        // А здесь пытаемся сделать что-то похожее на равномерность
        public SamplesSet getTrainDataset(int count)
        {
            SamplesSet set = new SamplesSet();
            for (int type = 0; type < LetterCount; type++)
            {
                for (int i = 0; i < count/LetterCount; i++)
                {
                    var sample = structure[(LetterType)type][random.Next(structure[(LetterType)type].Count)];
                    set.AddSample(new Sample(processImage(new Bitmap(sample)), LetterCount, (LetterType)type));
                }
            }
            set.shuffle();
            return set;
        }

        public Tuple<Sample, Bitmap> getSample()
        {
            var type = (LetterType) random.Next(LetterCount);
            var sample = structure[type][random.Next(structure[type].Count)];
            var bitmap = new Bitmap(sample);
            return Tuple.Create<Sample, Bitmap>(new Sample(processImage(bitmap),LetterCount,type),bitmap);
        }

        public Sample getSample(Bitmap bitmap)
        {
            return new Sample(processImage(bitmap), LetterCount);
        }
    }
}