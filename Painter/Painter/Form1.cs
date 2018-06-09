﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Collections;

namespace Painter
{
    public partial class Form1 : Form
    {
        // toolSelected
        // 0 = Brush
        // 1 = Line
        // 2 = Eraser
        // 3 = Eyedropper
        // 4 = Rectangle
        // 5 = Ellipse

        bool drawFlag = false;
        int[] xPos = new int[2];
        int[] yPos = new int[2];
        int xDown;
        int yDown;
        int xUp;
        int yUp;
        List<Point> points = new List<Point>();
        Graphics G;
        Color fgColor = Color.Black;
        int brushSize = 1;
        int toolSelected = 0;
        Pen pen;

        Bitmap temp;
        List<Bitmap> layers = new List<Bitmap>();   //list of bitmaps, photoshop order
        List<bool> drawLayers = new List<bool>();   //list of whether each layer is checked, photoshop order
        int activeLayer = 0;

        Graphics testG;

        Bitmap memoryImage;

        bool paintCursor = false;   // whether cursor should be circle
        Point mouseLoc;

        public Form1()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;

            pen = new Pen(fgColor, brushSize);
            pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;

            this.DoubleBuffered = true;
            DoubleBuffered = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //
        }

        private void panel_Paint(object sender, PaintEventArgs e)
        {
            //iterate through each layer and draw them
            int numItems = layerPanel.Items.Count;
            for (int i = 0; i < numItems; i++)
            {
                if (drawLayers[i])
                {
                    e.Graphics.DrawImage(layers[i], 0, 0, layers[i].Width, layers[i].Height);
                }
            }

            // "temp" layer is for drawing line previews, etc
            e.Graphics.DrawImage(temp, 0, 0, temp.Width, temp.Height);

            // draw cursor as a circle, but not if eyedropper is selected
            if (paintCursor && toolSelected != 3)
            {
                e.Graphics.DrawEllipse(new Pen(Color.Black, 0.5f), mouseLoc.X-(brushSize/2f), mouseLoc.Y-(brushSize/2f), brushSize, brushSize);
            } else
            {
                Cursor.Show();
            }
        }

        private void panel_MouseDown(object sender, MouseEventArgs e)
        {
            drawFlag = true;
            xPos[0] = e.X;
            xPos[1] = e.X;
            yPos[0] = e.Y;
            yPos[1] = e.Y;
            xDown = e.X;
            yDown = e.Y;
            points = new List<Point>();
            points.Add(new Point(e.X, e.Y));
        }

        private void panel_MouseMove(object sender, MouseEventArgs e)
        {
            mouseLoc = e.Location;
            panel.Refresh();
            if (drawFlag && toolSelected == 0)      // BRUSH
            {
                xPos[0] = xPos[1];
                xPos[1] = e.X;
                yPos[0] = yPos[1];
                yPos[1] = e.Y;
                points.Add(new Point(e.X, e.Y));
                Point[] pointsArr = points.ToArray<Point>();

                pen.Color = Color.FromArgb(opacityBar.Value, fgColor);
                pen.Width = brushSize;
                testG = Graphics.FromImage(temp);
                testG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                //Pen eraser = new Pen(Color.FromArgb(0, fgColor), brushSize);
                //testG.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                //testG.DrawLines(eraser, pointsArr);
                //testG.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                //testG.DrawLines(pen, pointsArr);
                testG.DrawLine(pen, xPos[0], yPos[0], xPos[1], yPos[1]);
                panel.Refresh();
            } else if (drawFlag && toolSelected == 1)   // LINE PREVIEW
            {
                pen.Color = fgColor;
                pen.Width = brushSize;
                testG = Graphics.FromImage(temp);
                testG.Clear(Color.Transparent);
                testG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                testG.DrawLine(pen, xPos[0], yPos[0], e.X, e.Y);
                panel.Refresh();
            } else if (drawFlag && toolSelected == 2)   // ERASER
            {
                xPos[0] = xPos[1];
                xPos[1] = e.X;
                yPos[0] = yPos[1];
                yPos[1] = e.Y;
                pen.Color = Color.Transparent;
                pen.Width = brushSize;
                testG = Graphics.FromImage(layers[activeLayer]);
                testG.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                testG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                testG.DrawLine(pen, xPos[0], yPos[0], xPos[1], yPos[1]);
                panel.Refresh();
            } else if (drawFlag && toolSelected == 4)   // RECTANGLE PREVIEW
            {
                pen.Color = fgColor;
                pen.Width = brushSize;
                testG = Graphics.FromImage(temp);
                testG.Clear(Color.Transparent);
                testG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                testG.DrawRectangle(pen, drawRect(xDown, yDown, e.X, e.Y));
                panel.Refresh();
            } else if (drawFlag && toolSelected == 5)   // ELLIPSE PREVIEW
            {
                pen.Color = fgColor;
                pen.Width = brushSize;
                testG = Graphics.FromImage(temp);
                testG.Clear(Color.Transparent);
                testG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                testG.DrawEllipse(pen, drawRect(xDown, yDown, e.X, e.Y));
                panel.Refresh();
            }
        }

        private void panel_MouseUp(object sender, MouseEventArgs e)
        {
            drawFlag = false;
            xUp = e.X;
            yUp = e.Y;
            if (toolSelected == 0)  // BRUSH
            {
                testG = Graphics.FromImage(temp);
                testG.Clear(Color.Transparent);

                pen.Color = Color.FromArgb(opacityBar.Value, fgColor);
                pen.Width = brushSize;
                Point[] pointsArr = points.ToArray<Point>();
                if (pointsArr.Length >= 3)  // because error will occur otherwise
                {
                    testG = Graphics.FromImage(layers[activeLayer]);
                    testG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    testG.DrawCurve(pen, pointsArr);
                }
                panel.Refresh();
            }
            if (toolSelected == 1)      // LINE
            {
                pen.Color = fgColor;
                pen.Width = brushSize;
                testG = Graphics.FromImage(layers[activeLayer]);
                testG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                testG.DrawLine(pen, xPos[0], yPos[0], e.X, e.Y);
                panel.Refresh();
            }
            if (toolSelected == 4)      // RECTANGLE
            {
                pen.Color = fgColor;
                pen.Width = brushSize;
                testG = Graphics.FromImage(layers[activeLayer]);
                testG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                testG.DrawRectangle(pen, drawRect(xDown, yDown, xUp, yUp));
                panel.Refresh();
            }
            if (toolSelected == 5)      // ELLIPSE
            {
                pen.Color = fgColor;
                pen.Width = brushSize;
                testG = Graphics.FromImage(layers[activeLayer]);
                testG.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                testG.DrawEllipse(pen, drawRect(xDown, yDown, xUp, yUp));
                panel.Refresh();
            }
        }

        private Rectangle drawRect(int xDown, int yDown, int xUp, int yUp)
        {
            Rectangle rect = new Rectangle();
            if (xDown < xUp && yDown < yUp)
            {
                rect = new Rectangle(xDown, yDown, xUp - xDown, yUp - yDown);
            }
            else if (xDown < xUp && yDown > yUp)
            {
                rect = new Rectangle(xDown, yUp, xUp - xDown, yDown - yUp);
            }
            else if (xDown > xUp && yDown > yUp)
            {
                rect = new Rectangle(xUp, yUp, xDown - xUp, yDown - yUp);
            }
            else if (xDown > xUp && yDown < yUp)
            {
                rect = new Rectangle(xUp, yDown, xDown - xUp, yUp - yDown);
            }
            return rect;
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 newDialog = new Form2();
            if (newDialog.ShowDialog() == DialogResult.OK)
            {
                saveToolStripMenuItem.Enabled = true;
                panel.Width = newDialog.getWidth();
                panel.Height = newDialog.getHeight();
                setUpLayers();
                panel.Visible = true;
                panel.Enabled = true;

                newDialog.Close();
            }
        }

        private void setUpLayers()
        {
            // Set up layers panel
            layers.Insert(0, new Bitmap(panel.Width, panel.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb));
            layers[0].MakeTransparent();
            drawLayers.Insert(0, true);

            layers.Insert(1, new Bitmap(panel.Width, panel.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb));
            layers[1].MakeTransparent();
            drawLayers.Insert(1, true);

            temp = new Bitmap(panel.Width, panel.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            temp.MakeTransparent();

            layerPanel.SetItemChecked(0, true);     //CAREFUL: using SetItemChecked() causes ItemCheck() to be called!
            layerPanel.SetItemChecked(1, true);
            layerPanel.SetSelected(1, true);        // set bottom layer to selected
        }

        private void colorFG_Click(object sender, EventArgs e)
        {
            DialogResult result = colorDialog.ShowDialog();
            if (result.Equals(DialogResult.OK))
            {
                colorFG.BackColor = colorDialog.Color;
                fgColor = colorDialog.Color;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Stream myStream = null;
            openFileDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((myStream = openFileDialog.OpenFile()) != null)
                    {
                        using (myStream)
                        {
                            Image img = Image.FromStream(myStream);
                            panel.Width = img.Width;
                            panel.Height = img.Height;
                            setUpLayers();
                            layers[0] = new Bitmap(img);
                            panel.Enabled = true;
                            panel.Visible = true;       // panel paint called
                            //debug.Text = panel.Width + ", " + panel.Height;
                            //debug.Text = panelContainer.Width + ", " + panelContainer.Height;
                            //layers[0] = new Bitmap(img);
                        }
                    }
                } catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private void fitToScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //pictureBox.Size = panel.Size;
            //pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            //pictureBox.Dock = DockStyle.Fill;
            Graphics g;
            for (int i = 0; i < layers.Count; i++)
            {
                g = Graphics.FromImage(layers[i]);
                int widthDiff = Math.Abs(panel.Width - panelContainer.Width);
                int heightDiff = Math.Abs(panel.Height - panelContainer.Height);
                if (widthDiff > heightDiff)
                {
                    float oldWidth = panel.Width;
                    float oldHeight = panel.Height;
                    panel.Width = panelContainer.Width;
                    panel.Height = (int)((panelContainer.Width / oldWidth) * oldHeight);
                    debug.Text = panel.Width + ", " + panel.Height;
                } else
                {
                    //panel.Height = panelContainer.Height;
                    //panel.Width = (panelContainer.Height / panel.Height) * panel.Width;
                }
                g.DrawImage(layers[i], 0, 0, panel.Width, panel.Height);
            }
        }

        private void zoomInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //pictureBox.Height += 50;
            //pictureBox.Width += 50;
            //pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            //pictureBox.Dock = DockStyle.None;
        }

        private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //pictureBox.Height -= 50;
            //pictureBox.Width -= 50;
            //pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            //pictureBox.Dock = DockStyle.None;
        }

        private void actualToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //pictureBox.SizeMode = PictureBoxSizeMode.AutoSize;
            panel.AutoScroll = true;
            //pictureBox.Dock = DockStyle.None;
        }

        private void brushSizeBox_ValueChanged(object sender, EventArgs e)
        {
            brushSize = Decimal.ToInt32(brushSizeBox.Value);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                Bitmap merged = new Bitmap(layers[0]);
                Graphics g = Graphics.FromImage(merged);
                for (int i = 0; i < layers.Count; i++)
                {
                    g.DrawImageUnscaled(layers[i], 0, 0);
                }
                merged.Save(saveFileDialog.FileName);

                //pictureBox.Image.Save(saveFileDialog.FileName);
            }
        }

        private void lineButton_Click(object sender, EventArgs e)
        {
            toolSelected = 1;
        }

        private void brushButton_Click(object sender, EventArgs e)
        {
            toolSelected = 0;
        }

        private void layerPanel_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            int numItems = layerPanel.Items.Count;
            for (int i = 0; i < numItems; i++)
            {
                if (e.Index == (numItems - 1 - i))
                {
                    drawLayers[i] = e.NewValue == CheckState.Checked;
                }
            }
            panel.Refresh();
        }

        private void layerPanel_SelectedIndexChanged(object sender, EventArgs e)
        {
            activeLayer = layerPanel.SelectedIndex;
            activeLayer = layerPanel.Items.Count - 1 - activeLayer;
        }

        private void newLayer_Click(object sender, EventArgs e)
        {
            layerPanel.Items.Insert(0, "Layer");

            //layers.Insert(0, new Bitmap(panel.Width, panel.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb));
            layers.Add(new Bitmap(panel.Width, panel.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb));
            layers[layers.Count-1].MakeTransparent();

            //drawLayers.Insert(0, true);
            drawLayers.Add(true);

            layerPanel.SetItemChecked(0, true);     // CAREFUL: causes ItemCheck() to be called
        }

        private void eraseButton_Click(object sender, EventArgs e)
        {
            toolSelected = 2;
        }

        private void eyeDropButton_Click(object sender, EventArgs e)
        {
            toolSelected = 3;
        }

        private void panel_MouseClick(object sender, MouseEventArgs e)
        {
            if (toolSelected == 3)
            {
                Color clr = layers[activeLayer].GetPixel(e.X, e.Y);
                colorFG.BackColor = clr;
            }
        }

        private void panel_MouseEnter(object sender, EventArgs e)
        {
            Cursor.Hide();
            paintCursor = true;
        }

        private void panel_MouseLeave(object sender, EventArgs e)
        {
            Cursor.Show();
            paintCursor = false;
            panel.Refresh();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void rectButton_Click(object sender, EventArgs e)
        {
            toolSelected = 4;
        }

        private void ellipseButton_Click(object sender, EventArgs e)
        {
            toolSelected = 5;
        }
    }

    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint, true);
        }
    }
}
