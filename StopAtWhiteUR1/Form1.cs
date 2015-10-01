/***********************************************
 * 
 * Terminators 2
 * Date: 10/1/2015
 * Class: UR2
 * TERM PROJECT ( LINE FOLLOW )
 * 
 ***********************************************/
using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO.Ports;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Collections.Generic;

namespace StopAtWhiteUR1
{
    public partial class Form1 : Form
    {
        enum State { enabled, disabled };
        State s = State.disabled;
        SerialPort serPort;
        string ComPort = "COM5"; // Port on laptop
        delegate void displayStringDelegate(Label l, String s);
        Capture _capture = null;
        int threshold = 125;
        int totNumPixels = 0;

        public Form1()
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
            numericUpDown1.Value = (threshold * 100) / 255;
            totNumPixels = imageBox1.Width * imageBox1.Height;   
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //serPort = new SerialPort(ComPort);
            //serPort.BaudRate = 9600;
            //serPort.DataBits = 8;
            //serPort.Parity = Parity.None;
            //serPort.StopBits = StopBits.One;
            //serPort.Open();
            //serPort.DataReceived += new SerialDataReceivedEventHandler(serPort_DataReceived);
            _capture = new Capture();
            _capture.ImageGrabbed += Display_Captured;
            _capture.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (s == State.enabled) //Starting the Run 
            {
                s = State.disabled;
                button1.Text = "Start";
                serPort.Write("X");
            }
            else // disabled
            {
                s = State.enabled;
                button1.Text = "Stop";
                serPort.Write("S");
            }
        }

        void Display_Captured(object sender, EventArgs e)
        {
            //Image<Gray, Byte> gi = _capture.RetrieveGrayFrame().Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            //Image<Gray, Byte> gray_frame = _capture.RetrieveGrayFrame().Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            //Image<Gray, Byte> bw_frame = gray_frame.ThresholdBinary(new Gray(threshold), new Gray(255));

            Image<Gray, byte> bw_image = _capture.RetrieveGrayFrame().Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            bw_image = bw_image.ThresholdBinary(new Gray(threshold), new Gray(255)); // if pix > bw_threshold, then 255; Otherwise 0
            Image<Bgr, byte> color_image = _capture.RetrieveBgrFrame().Resize(imageBox1.Width, imageBox1.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);
            Image<Bgr, Byte> bwr_image = bw_image.Convert<Bgr, Byte>(); // color image
            for (int h = 0; h < color_image.Height; h++)
            {
                for (int w = 0; w < color_image.Width; w++)
                { // e.g. 49 40 178
                    // Can we have different logic? Yes. Topics in AI and UR4
                    if (color_image.Data[h, w, 2] > 1 * (color_image.Data[h, w, 1] + color_image.Data[h, w, 0]))
                    {
                        bwr_image.Data[h, w, 0] = 0; // B
                        bwr_image.Data[h, w, 1] = 0; // G
                        bwr_image.Data[h, w, 2] = 255; // R
                    }
                }
            }

            for (int h = 0; h < color_image.Height; h++)
            {
                for (int w = 0; w < color_image.Width; w++)
                { // e.g. 49 40 178
                    // Can we have different logic? Yes. Topics in AI and UR4
                    if (color_image.Data[h, w, 1] > 1 * (color_image.Data[h, w, 2] + color_image.Data[h, w, 0]))
                    {
                        bw_image.Data[h, w, 0] = 0;
                        //bwr_image.Data[h, w, 0] = 0; // B
                        //bwr_image.Data[h, w, 1] = 0; // G
                        //bwr_image.Data[h, w, 2] = 0; // R
                    }
                }
            }

            

            // Setting all start Values to Zero
            int num_white_left = 0; 
            int num_white_middle = 0;
            int num_white_right = 0;
            int percent_white = 0;
            int num_red = 0;
            int percent_red = 0;
            int segmentSize = imageBox1.Width / 3;
            int segmentArea = segmentSize * imageBox1.Height;

            for (int h = 0; h < imageBox1.Size.Height; h++)
            {
                for (int w = 0; w < imageBox1.Size.Width; w++)
                {
                    if (bwr_image.Data[h, w, 2] == 255 && bwr_image.Data[h, w, 0] == 0 && bwr_image.Data[h, w, 1] == 0) //-- Grayscale image
                    {
                        num_red++;
                    }
                    if (bw_image.Data[h, w, 0] == 255) //-- Grayscale image
                    {
                        if (w < segmentSize) //Shows Amount of Pixels in left 3rd
                        {
                            num_white_left++; //Counts the number
                        }
                        else if (w <= (segmentSize * 2)) //shows amount of pixels in middle 3rd
                        {
                            num_white_middle++; //Counts the number
                        }
                        else // if (w >= (segmentSize *2)) shows amount of pixels in right 3rd
                        {
                            num_white_right++; // counts the number 
                        }
                    }
                }
            }
            percent_red = num_red * 100 / totNumPixels;
            percent_white = (num_white_left + num_white_middle + num_white_right) * 100 / totNumPixels;

            dispStr(label1, "L: " + num_white_left.ToString() + "  " + "M: " + num_white_middle.ToString()
                + "  " + "R: " + num_white_right.ToString() + "  " + "%White: " + percent_white.ToString()
                + "  " + "%Red: " + percent_red.ToString());

            // Sgowing the comands on the output screen 

            Image<Gray, Byte> gray2 = null;
            Image<Gray, Byte> gry = bwr_image.Convert<Gray, Byte>();
            gray2 = gry.Canny(100, 255);
            //imageBox2.Image = gray2.Resize(imageBox2.Width, imageBox2.Height, Emgu.CV.CvEnum.INTER.CV_INTER_LINEAR);

            Image<Gray, Byte> gray = gray2.Convert<Gray, Byte>().PyrDown().PyrUp(); // to filter out noise
            Image<Gray, Byte> cannyEdges = gray.Canny(100, 255);
            List<Triangle2DF> triangleList = new List<Triangle2DF>();
            //List<MCvBox2D> boxList = new List<MCvBox2D>();
            // a contour: list of pixels that can represent a curve
            using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
            {
                for (Contour<Point> contours = cannyEdges.FindContours(); contours != null; contours = contours.HNext)
                {
                    Contour<Point> currentPolygon = contours.ApproxPoly(contours.Perimeter * 0.1, storage); // adjust here
                    if (contours.Area > 250) //only consider contours with area greater than 250
                    //if (currentPolygon.Area > 250) //only consider polygon with area greater than 250
                    {
                        if (currentPolygon.Total == 3) //The contour has 3 vertices, it is a triangle
                        {
                            Point[] pts = currentPolygon.ToArray();
                            triangleList.Add(new Triangle2DF(pts[0], pts[1], pts[2]));
                        }
                        //else if (currentPolygon.Total == 4) //The contour has 4 vertices.
                        //{
                        //    // determine if all the angles in the contour are within the range of [80, 100], close to 90 degrees 
                        //    bool isRectangle = true;
                        //    Point[] pts = currentPolygon.ToArray();
                        //    LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                        //    for (int i = 0; i < edges.Length; i++)
                        //    {
                        //        double angle = Math.Abs(
                        //           edges[(i + 1) % edges.Length].GetExteriorAngleDegree(edges[i]));
                        //        if (angle < 80 || angle > 100)
                        //        {
                        //            isRectangle = false;
                        //            break;
                        //        }
                        //    }
                        //    if (isRectangle) boxList.Add(currentPolygon.GetMinAreaRect());
                        //}
                    }
                }
            } // end using
            Image<Bgr, Byte> triangleRectangleImage = bwr_image;



            string str = "Triangle : ";
            string trixyz = "";
            string ctri = "";
            //string btri = "";

            foreach (Triangle2DF triangle in triangleList)
            {
                triangleRectangleImage.Draw(triangle, new Bgr(Color.Yellow), 2);

                trixyz = triangle.V0.ToString();
                trixyz += triangle.V1.ToString() + Environment.NewLine;
                trixyz += triangle.V2.ToString();

                ctri = triangle.Centeroid.ToString();
            }

            str += trixyz + Environment.NewLine;
            str += "  Centeroid : ";
            str += ctri + Environment.NewLine;
            //str += "Box Center : ";

            dispStr(label3, str);

            imageBox1.Image = triangleRectangleImage;

            //foreach (MCvBox2D box in boxList)
            //{
            //    triangleRectangleImage.Draw(box, new Bgr(Color.DarkOrange), 2);
            //    btri = box.center.ToString() + Environment.NewLine;
            //}

            if (s == State.enabled)
            {

                // Strings for all the outputs of the command label 
                string TurnRight = "Turn Right";
                string GoStraight = "Go Straight Forward";
                string TurnLeft = "Turn Left";
                string Stop = "Stop";
              

                // if statment that compairs the num of white right to middle and left 
                if ((num_white_right > num_white_middle))
                {
                    dispStr(label2, "" + TurnLeft.ToString()); // outputs turn left
                    serPort.Write("L"); // sends L to atmel
                }
                // if statment that compairs the num of white left to middle and right 
                if ((num_white_left > num_white_right))
                {
                    dispStr(label2, "" + TurnRight.ToString()); // outputs turn right 
                    serPort.Write("R"); //sends R to atmel 
                }
                // if statment that compairs the num of white on the left and the right to the middle values 
                if ((num_white_middle < num_white_left) && (num_white_middle < num_white_right))
                {
                    dispStr(label2, "" + GoStraight.ToString()); // output to go straight
                    serPort.Write("S");//sends S to atmel 
                }  
                //Stop at red
                if (triangleList.Count != 0)
                {
                    dispStr(label2, "" + Stop.ToString());
                    serPort.Write("X");
                    s = State.disabled;
                }
            }
        }
        void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                threshold += 10;
                if (threshold >= 255)
                    threshold = 255;
            }
            else if (e.KeyCode == Keys.Down)
            {
                threshold -= 10;
                if (threshold <= 0)
                    threshold = 0;
            }
        }
        void serPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            threshold = (Convert.ToInt32(numericUpDown1.Value) * 255) / 100;
        }

        private void numericUpDown1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                threshold += 10;
                if (threshold >= 255)
                    threshold = 255;
            }
            else if (e.KeyCode == Keys.Down)
            {
                threshold -= 10;
                if (threshold <= 0)
                    threshold = 0;
            }
        }
        public void dispStr(Label l, string s)
        {
            if (InvokeRequired)
            {
                displayStringDelegate dispStrDel = dispStr;
                this.BeginInvoke(dispStrDel, l, s);
            }
            else
                l.Text = s;
        }
    }
}
