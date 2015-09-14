/***********************************************
 * 
 * Creater: Joey Yudasz 
 * Date: 4/21/2015
 * Class: UR1
 * TERM PROJECT ( LINE FOLLOW )
 * 
 ***********************************************/
using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO.Ports;
using Emgu.CV;
using Emgu.CV.Structure;

namespace StopAtWhiteUR1
{
    public partial class Form1 : Form
    {
        enum State { enabled, disabled };
        State s = State.disabled;
        SerialPort serPort;
        string ComPort = "COM4"; // Port on laptop
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
            serPort = new SerialPort(ComPort);
            serPort.BaudRate = 9600;
            serPort.DataBits = 8;
            serPort.Parity = Parity.None;
            serPort.StopBits = StopBits.One;
            serPort.Open();
            serPort.DataReceived += new SerialDataReceivedEventHandler(serPort_DataReceived);
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
                    if (color_image.Data[h, w, 2] > .75 * (color_image.Data[h, w, 1] + color_image.Data[h, w, 0]))
                    {
                        bwr_image.Data[h, w, 0] = 0; // B
                        bwr_image.Data[h, w, 1] = 0; // G
                        bwr_image.Data[h, w, 2] = 255; // R
                    }
                }
            }
            imageBox1.Image = bwr_image;

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
                if (percent_red > 5)
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
