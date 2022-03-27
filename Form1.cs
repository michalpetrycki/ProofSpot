﻿using Spire.Pdf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;
using OpenCvSharp;
using System.Reflection;

namespace ProofSpot
{
    public partial class Form1 : Form
    {

        static string rootPath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string filePath = "";
            this.openFileDialog1.Title = "Choose ISO file";
            this.openFileDialog1.Filter = "Pdf Files | *pdf";
            this.openFileDialog1.InitialDirectory = @"C:\Users\Michal\Desktop\HonoursProject\ISO";

            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {

                    filePath = this.openFileDialog1.FileName;

                    if (File.Exists(filePath))
                    {

                        // Convert pdf to image and return .bmp file path
                        string imgPath = ConvertPdfToImage(filePath);

                        // Extract BoM table 
                        string bom = ExtractBomTableFromImage(imgPath);

                        // Run tesseract and extract flange objects from BoM
                        List<string> flanges = ExtractFlangesFromBom(bom);

                        //foreach (string flange in flanges)
                        //{

                        //    Console.WriteLine(flange);
                        //    Console.WriteLine();

                        //}

                        // Run object detection
                        RunPythonCode();


                        //Bitmap img = (Bitmap)ReadPdfAndExtractBom(filePath);

                        //string imagePath = ConvertPdfToImageOld(filePath);

                        //string bomText = RunTesseractAgainstBomTable(img);

                        //Console.WriteLine(bomText);

                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }

            }

        }

        public static string RunTesseractAgainstBomTable(Bitmap bomBitmap)
        {

            string bomText = string.Empty;

            //Bitmap resizedImage = ResizeImage(bomBitmap, bomBitmap.Width * 2, bomBitmap.Height * 2);

            using (var engine = new TesseractEngine(rootPath + "\\ProofSpot\\tessdata", "eng"))
            {

                engine.SetVariable("user_defined_dpi", 600);

                using (var img = PixConverter.ToPix(bomBitmap))
                {

                    using (var page = engine.Process(img))
                    {

                        bomText = page.GetText();

                    }

                }

            }

            return bomText;

        }

        /* This function converts pdf into image file. It uses PDF Spire library to load the file and save it into Image object
         * @filePath - path to pdf file
         * @fileName - optional filename which would be used to save the image (converted) file
         * @saveFile - optional flag which determines if image (converted) file should be saved to the disk
         * @returns - converted image object
         */
        private static string ConvertPdfToImage(string filePath)
        {

            Assembly rootAssembly = Assembly.GetEntryAssembly();
            string rootAssemblyPath = Path.GetDirectoryName(rootAssembly.Location);
            string saveToPath = $"{rootAssemblyPath}\\image.bmp";
            
            // SpirePDF object
            PdfDocument doc = new PdfDocument();

            // Load PDF from file
            doc.LoadFromFile(filePath);

            // Save above document into image
            Image img = (Bitmap) doc.SaveAsImage(0, Spire.Pdf.Graphics.PdfImageType.Metafile, 1200, 1200);

            img.Save(saveToPath);

            return saveToPath;

        }

        /* This file takes and image, locates Bills of Materials table and puts it into separate image
         * @image - Image object which was previously converted from original pdf file
         * @filePath - optional, used for saving the bills of material image into file
         * @fileName - optional, used for saving the bills of material image into file
         * @saveFile - optional flag which determines if bills of materials image file should be saved to the disk
         * @returns - Bills of materials image object
         */
        private static string ExtractBomTableFromImage(string filePath)
        {

            Assembly rootAssembly = Assembly.GetEntryAssembly();
            string rootAssemblyPath = Path.GetDirectoryName(rootAssembly.Location);
            string saveToPath = $"{rootAssemblyPath}\\imageBom.bmp";

            string bomFilePath = filePath;

            Image image = Bitmap.FromFile(filePath);

            //// Create rectangle which determines where the Bom table is within the image
            Rectangle r = new Rectangle(13555, 615, 5860, 7240);

            // Create an empty image object. It will later store extracted image. 
            Bitmap target = new Bitmap(r.Width, r.Height);

            // Use empty image for: 
            using (Graphics g = Graphics.FromImage(target))
            {

                // Take copied image, set its size, extract whatever is in rectangle area and draw that into empty image object
                g.DrawImage(image, new Rectangle(0, 0, target.Width, target.Height), r, GraphicsUnit.Pixel);

                target.Save(saveToPath);

            }

            return saveToPath;

        }

        /* This function takes BoM file object and runs tesseract against it. Then gets extracted text, brakes it into separate lines
         * and returns it.
         * @bomBitmap - Image object which contains BoM table
         * @returns - List of string, each is a BoM row
         */
        public static List<string> ExtractFlangesFromBom(string filePath)
        {

            string bomText = string.Empty;
            List<string> flangeRows = new List<string>();
            string[] lines;

            Bitmap bomBmp = (Bitmap) Bitmap.FromFile(filePath);

            // Run tesseract
            using (var engine = new TesseractEngine(rootPath + "\\tessdata", "eng"))
            {

                engine.SetVariable("user_defined_dpi", 600);

                using (var img = PixConverter.ToPix(bomBmp))
                {

                    using (var page = engine.Process(img, PageSegMode.AutoOsd))
                    {

                        bomText = page.GetText();

                    }

                }

            }

            // Get text extracted by tesseract and brake it into separate lines
            lines = bomText
            .Split(
                new string[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries
            )
            .Where(x => !string.IsNullOrWhiteSpace(x.Trim()))
            .ToArray();

            foreach (string line in lines)
            {

                bool isRow = char.IsDigit(line[0]);

                if (isRow)
                {

                    // Ignore header
                    if (line.IndexOf("Installation Materials") < 0)
                    {

                        // Add string into list of strings
                        flangeRows.Add(line);

                    }

                }


            }

            return flangeRows;

        }

        private Image ReadPdfAndExtractBom(string fileName)
        {

            string pathToSave = fileName.Replace(".pdf", "");
            PdfDocument doc = new PdfDocument();
            doc.LoadFromFile(fileName);
            Image emf = (Bitmap)doc.SaveAsImage(0, Spire.Pdf.Graphics.PdfImageType.Metafile, 1200, 1200);

            Rectangle r = new Rectangle(13555, 615, 5860, 7240);
            Bitmap target = new Bitmap(r.Width, r.Height);

            emf.Save(pathToSave + "small.bmp");

            using (Graphics g = Graphics.FromImage(target))
            {

                g.DrawImage(emf, new Rectangle(0, 0, target.Width, target.Height), r, GraphicsUnit.Pixel);

                //target.Save(pathToSave + "small.bmp");


            }

            return target;

        }

        private string ConvertPdfToImageOld(string fileName)
        {

            string pathToSave = fileName.Replace(".pdf", "");
            pathToSave += "testkurwa.bmp";
            PdfDocument doc = new PdfDocument();
            doc.LoadFromFile(fileName);
            Image emf = (Bitmap)doc.SaveAsImage(0, Spire.Pdf.Graphics.PdfImageType.Metafile, 300, 300);

            emf.Save(pathToSave);

            return pathToSave;

        }



        private void RunPythonCode()
        {

            List<PidCircle> circles;

            // 1. Create Process Info
            var psi = new ProcessStartInfo();
            psi.FileName = @"C:\Users\Michal\AppData\Local\Programs\PythonCodingPack\python.exe";

            // 2. Provide script and arguments
            var script = $@"{rootPath}\python\scrypt.py";
            //var imagePathArg = pathToImage;
            var imagePathArg = "David2spierdolonegnowno.bmp";

            psi.Arguments = $"\"{script}\" \"{imagePathArg}\"";

            // 3. Process configuration
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            // 4. 
            var errors = "";
            var results = "";

            using (var process = Process.Start(psi))
            {

                errors = process.StandardError.ReadToEnd();
                results = process.StandardOutput.ReadToEnd();

            }

            //if (!string.IsNullOrEmpty(results))
            //{

            //    circles = GetCirclesObjects(results);

            //    if (circles != null)
            //    {

            //        foreach (PidCircle c in circles)
            //        {

            //            Console.WriteLine($"New circle detected at ({c.coord_x}, {c.coord_y}) with radius {c.radius}");
            //            Console.WriteLine();

            //            string circleText = RunTesseractForCircle(c);
            //            Console.WriteLine("Circle text: ");
            //            Console.WriteLine();

            //        }

            //    }

            //}

            // 5. Display output
            Console.WriteLine("ERRORS");
            Console.WriteLine(errors);
            Console.WriteLine();
            Console.WriteLine("Results:");
            Console.WriteLine(results);

        }


        private List<PidCircle> GetCirclesObjects(string circlesString)
        {

            List<PidCircle> circles = new List<PidCircle>();

            string[] lines = circlesString
            .Split(
                new string[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries
            )
            .Where(x => !string.IsNullOrWhiteSpace(x.Trim()))
            .ToArray();

            foreach (string line in lines)
            {
                string[] numbers = line.Split(null);

                // Create a circle object using numbers[0] and numbers[1] as x and y coordinates 
                // and numbers[2] as circle radius
                PidCircle c = new PidCircle(Int32.Parse(numbers[0]), Int32.Parse(numbers[1]), Int32.Parse(numbers[2]));
                circles.Add(c);

            }

            return circles;

        }

        private string RunTesseractForCircle(PidCircle c)
        {

            string circleText = "";

            Bitmap originalImage = new Bitmap("David1testkurwa.bmp");

            int r_x = c.coord_x - c.radius - 5;
            int r_y = c.coord_y - c.radius - 5;

            Rectangle r = new Rectangle(r_x, r_y, c.radius * 2 + 10, c.radius * 2 + 10);
            Bitmap target = new Bitmap(r.Width, r.Height);

            using (Graphics g = Graphics.FromImage(target))
            {

                g.DrawImage(originalImage, new Rectangle(0, 0, target.Width, target.Height), r, GraphicsUnit.Pixel);

                target.Save($"{c.coord_x}{c.coord_y}{c.radius}.bmp");

                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
                path = Path.Combine(path, "tessdata");
                path = path.Replace("file:\\", "");

                using (var engine = new TesseractEngine(path, "eng"))
                {

                    engine.SetVariable("user_defined_dpi", 900);
                    
                    using (var img = PixConverter.ToPix(target))
                    {

                        using (var page = engine.Process(img, PageSegMode.CircleWord))
                        {

                            circleText = page.GetText();
                            bool dupa = true;

                        }

                    }

                }

        }

            return circleText;

        }

    }

    class PidCircle
    {

        public int coord_x { get; set; }
        public int coord_y { get; set; }
        public int radius { get; set; }

        public PidCircle(int x, int y, int r)
        {

            coord_x = x;
            coord_y = y;
            radius = r;

        }

    }

}