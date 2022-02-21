using Spire.Pdf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;

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
            string pathToFile = "";
            this.openFileDialog1.Title = "Choose ISO file";
            this.openFileDialog1.Filter = "Pdf Files | *pdf";
            this.openFileDialog1.InitialDirectory = @"C:\Users\Michal\Desktop\HonoursProject\ISO";

            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {

                    pathToFile = this.openFileDialog1.FileName;

                    if (File.Exists(pathToFile))
                    {

                        Bitmap img = (Bitmap) ReadPdfAndExtractBom(pathToFile);

                        string bomText = RunTesseractAgainstBomTable(img);

                        string[] lines = bomText
                        .Split(
                            new string[] { "\r\n", "\r", "\n" },
                            StringSplitOptions.RemoveEmptyEntries
                        )
                        .Where(x => !string.IsNullOrWhiteSpace(x.Trim()))
                        .ToArray();

                        List<string> flangeRows = new List<string>();

                        foreach (string line in lines)
                        {

                            bool isRow = char.IsDigit(line[0]);

                            if (isRow)
                            {

                                if (line.IndexOf("Installation Materials") < 0)
                                {

                                    flangeRows.Add(line);

                                }

                            }


                        }

                        foreach (string line in flangeRows)
                        {
                            this.textBox1.AppendText(line + Environment.NewLine);
                        }

                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }

            }

        }

        private Image ReadPdfAndExtractBom(string fileName)
        {

            string pathToSave = fileName.Replace(".pdf", "");
            PdfDocument doc = new PdfDocument();
            doc.LoadFromFile(fileName);
            Image emf = (Bitmap)doc.SaveAsImage(0, Spire.Pdf.Graphics.PdfImageType.Metafile, 1200, 1200);

            Rectangle r = new Rectangle(13555, 615, 5860, 7240);
            Bitmap target = new Bitmap(r.Width, r.Height);

            using (Graphics g = Graphics.FromImage(target))
            {

                g.DrawImage(emf, new Rectangle(0, 0, target.Width, target.Height), r, GraphicsUnit.Pixel);

                target.Save(pathToSave + ".bmp", System.Drawing.Imaging.ImageFormat.Bmp);

            }

            return target;

            // Dlaczego caly obrazek ma 5 MB, a wyciety 160mb?

            //emf.Save(pathToSave + ".bmp");

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

    }
}
