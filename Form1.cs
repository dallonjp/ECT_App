using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Windows.Forms.VisualStyles;
using System.Linq.Expressions;
using System.Drawing.Drawing2D;
using System.Numerics;
using MathNet.Numerics.LinearAlgebra;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;


namespace ECT_App
{
    public partial class Form1 : Form
    {
        static SerialPort _serial;
        public string comport;
        public int image_width = 50;
        public int image_height = 50;
        public int picbox_width;
        public int picbox_height;
        public double lamda;
        public double[] czero;
        public double[] cmax;
        public double[] cvector;
        public double[][] svector;
        public double[][] svectort;
        public double[][] stik;
        public int[,] ident;
        public Matrix<double> Q;
        public double[,] imagevector;
        public int algorithm;
        public bool iszeroed = false;
        public bool ismaxed = false;
        public bool freeze=false;
        public bool connected = false;
        public bool comportselected = false;
        public string filepath;
        public Form1()
        {
            InitializeComponent();
            picbox_height = pictureBox1.Height;
            picbox_width = pictureBox1.Width;
            
            pictureBox1.Paint += pictureBox_Paint;
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            //using (Graphics g = pictureBox2.CreateGraphics())
            //{
            //    Rectangle rect = new Rectangle(0, 0, pictureBox2.Width, pictureBox2.Height);
            //    using (LinearGradientBrush brush = new LinearGradientBrush(rect, Color.Blue, Color.Red, 90f))
            //    {
            //        // Set up color stops at specific positions
                    
            //        ColorBlend colorBlend = new ColorBlend();
            //        colorBlend.Colors = new Color[] { Color.Blue, Color.Green, Color.Red };
            //        colorBlend.Positions = new float[] { 0.0f, 0.5f, 1.0f };
            //        brush.InterpolationColors = colorBlend;
            //        // Fill the rectangle with the gradient brush
            //        g.FillRectangle(brush, rect);
            //    }
            //}

        }
        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen tickPen = new Pen(Color.Black);
            Font tickFont = new Font("Arial", 8);
            Brush tickBrush = Brushes.Black;

            int tickSize = 7; // Length of tick marks

            int xTickStart = 0; // X-coordinate of tick marks on the left side
            int xTickEnd = tickSize;
            int yTickStart = pictureBox1.Height - tickSize; // Y-coordinate of tick marks on the bottom side
            int yTickEnd = pictureBox1.Height;

            double minValue = -175;
            double maxValue = 175;
            double tickIncrement = 7;

            int numTicks = (int)((maxValue - minValue) / tickIncrement) + 1;

            double tickValue = minValue;
            float tickPosition = 0;

            for (int i = 0; i < numTicks; i++)
            {
                // Draw tick mark on the left side
                tickPosition = (float)(((tickValue - minValue) / (maxValue - minValue)) * pictureBox1.Height);
                g.DrawLine(tickPen, xTickStart, tickPosition, xTickEnd, tickPosition);
               // g.DrawString(tickValue.ToString("+#0.0;-#0.0;0"), tickFont, tickBrush, xTickEnd + 2, tickPosition - tickFont.Height / 2);

                // Draw tick mark on the bottom side
                tickPosition = (float)(((tickValue - minValue) / (maxValue - minValue)) * pictureBox1.Width);
                g.DrawLine(tickPen, tickPosition, yTickStart, tickPosition, yTickEnd);
               // g.DrawString(tickValue.ToString("+#0.0;-#0.0;0"), tickFont, tickBrush, tickPosition - tickFont.Height / 2, yTickEnd + 2);

                tickValue += tickIncrement;
            }
        }

        public Color ConvertToRGB(double value)
        {
            
            Color color = new Color();
            double halfmax = 0.5;

            if (0 <= value && value <= halfmax)
            {
                int r = 0;
                int g = Convert.ToInt32(255/halfmax * value);
                int b = Convert.ToInt32(255 + -255/halfmax * value);
                color = Color.FromArgb(255, r, g, b);
                
            }
            else if (halfmax < value && value <= 1)
            {
                int r = Convert.ToInt32(255/(1 - halfmax) * (value - halfmax));
                int g = Convert.ToInt32(255 + -255/(1 - halfmax) * (value - halfmax));
                int b = 0;
                color = Color.FromArgb(255, r, g, b);
            }
            return color;
        }
        public Bitmap getChartImg(double[,] data, Size sz)
        {
            
            List<Color> coloredData = new List<Color> {};
            //data is already in the shape of the image and needs to be flattened into a list of colors
            for (int x=0;x<data.GetLength(0);x++) 
            {
                for (int y = 0; y < data.GetLength(1); y++)
                {
                    Color color = ConvertToRGB(data[x, y]);
                    coloredData.Add(color);
                    
                }
            }
            
            //reshape colored data values into original dimensions of image to be displayed
            Color[,] colorArr = new Color[data.GetLength(0), data.GetLength(1)];
            
            int indexCounter = 0;

            for (int x = 0; x < data.GetLength(0); x++)
            {
                for (int y = 0; y < data.GetLength(1); y++)
                {
                    colorArr[x, y] = coloredData[indexCounter];
                    indexCounter++;
                }
            }
            
            Bitmap bmp = new Bitmap(sz.Width, sz.Height);

            int w = sz.Width / colorArr.GetLength(0);
            int h = sz.Height / colorArr.GetLength(1);
            int offx;
            int offy;
            for (int x = 0; x < colorArr.GetLength(0); x++)
            {
                offx = x*w;
                for (int y = 0; y < colorArr.GetLength(1); y++)
                {
                    offy = y * h;

                    for (int px = 0; px < w; px++)
                    {
                        for (int py = 0; py < h; py++)
                        {
                            bmp.SetPixel(offx + px, offy + py, colorArr[x, y]);
                        }
                    }
                    

                }
            }
            return bmp;
        }
        private async void connect_Click(object sender, EventArgs e)
        {
            
            if (comportselected && !connected)
            {
                connect.Text = "Connecting...";
                _serial = new SerialPort(comport, 115200);
                _serial.Open();
                string handshake = "c";
                _serial.Write(handshake);
                await Task.Delay(50);
                char charvalue = '\0';
                if (_serial.BytesToRead > 0)
                {
                    byte[] buffer = new byte[_serial.BytesToRead];
                    _serial.Read(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer);

                    if (response.Contains("s"))
                    {
                        connected = true;
                        connect.Text = "Connected";
                    }
                    else
                    {
                        connected = false;
                        connect.Text = "Connection Failed!";
                        await Task.Delay(1000);
                        connect.Text = "Connect";
                    }
                }
            }
            else
            {
                MessageBox.Show("No COM port selected!");
            }
        }
        public double[,] ReshapeArray(double[] originalArray, int rows, int cols)
        {
            
            double[,] reshapedArray = new double[rows, cols];
            int index = 0;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    reshapedArray[i, j] = originalArray[index];
                    index++;
                }
            }

            return reshapedArray;
        }

        public bool check_conn()
        {
            
            string handshake = "c";
            if (connected)
            {
                _serial.Write(handshake);

                if (_serial.BytesToRead > 0)
                {
                    byte[] buffer = new byte[_serial.BytesToRead];
                    _serial.Read(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer);


                    if (response.Contains("s"))
                    {
                        connected = true;
                    }
                    else
                    {
                        connected = false;
                        connect.Text = "Connect";
                    }
                }
            }
            return connected;
        }
        public int choose_algo()
        {
            if (LBP.Checked)
            {
                algorithm= 1;
            }else if (tikhonov.Checked)
            {
                algorithm = 2;

            }else if (!LBP.Checked && !tikhonov.Checked )
            {
                algorithm = 0;
            }
            return algorithm;
        }

        public double[] rescale(double[] data)
        {
            double min = data.Min();
            double max = data.Max();
            for(int x = 0; x < data.GetLength(0); x++)
            {
                double rvalue = (data[x] - min) / (max - min);
                data[x] = rvalue;
            }
            return data;
        }

        private double[] getcvector(int g)
        {
            string collect_data = "m";
            _serial.Write(collect_data);
            string n = g.ToString();
            _serial.Write(n);
            bool data_collected = false;
            while (!data_collected)
            {
                byte[] buffer = new byte[_serial.BytesToRead];
                _serial.Read(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer);

                if (response.Contains("r"))
                {
                    string serialData = _serial.ReadLine();
                    string[] values = serialData.Split(',');
                    double[] cvectornew = new double[values.Length];
                    for (int i = 0; i < values.Length; i++)
                    {
                        try { cvectornew[i] = double.Parse(values[i]); }
                        catch (Exception ex){
                        
                        }
                        
                    }

                    cvector = cvectornew;
                    data_collected = true;
                }

            }
            //double[] cvector = { 0.0108438, 0.0495783, 0.171901, 0.00124643, 0.0260973, 0.0813453, 0.271895, 0.0347303, 0.124747, 0.150234, 0.294549, 0.123274, 0.360082, 0.301178, 0.600148, 0.0018371, 0.110996, 0.025224, 0.0453171, 0.00200051, 0.159745, 0.132362, 0.176321, 0.0478254, 0.185501, 0.337736, 0.305032, 0.297432, 0.537526, 0.0228823, 0.104375, 0.0, 0.0890441, 0.0318977, 0.107637, 0.0433832, 0.295681, 0.175629, 0.28616, 0.257949, 0.537591, 0.616444, 0.292674, 0.152954, 0.0309044, 0.00758197, 0.281677, 0.202485, 0.168379, 0.0720522, 0.43948, 0.436809, 0.646827, 0.279123, 0.057631, 0.154009, 0.402403, 0.00913319, 0.0232578, 0.123516, 0.447213, 0.0873269, 0.214221, 0.243745, 0.55167, 0.0110671, 0.234676, 0.0218393, 0.0547223, 0.00929692, 0.203925, 0.196819, 0.382434, 0.135593, 0.488143, 0.0725057, 0.111415, 0.00655309, 0.0884067, 0.035185, 0.286551, 0.162194, 0.676208, 0.428273, 0.35691, 0.192001, 0.0384766, 0.0228142, 0.611446, 0.452201, 0.414286, 0.209619, 0.0887831, 0.327648, 1.0, 0.0243219, 0.061051, 0.295318, 0.61338, 0.0576071, 0.665193, 0.058637, 0.170573, 0.0310551, 0.309565, 0.222329, 0.292159, 0.0310197, 0.173377, 0.0768701, 0.732827, 0.340646, 0.0812584, 0.0373932, 0.0206617, 0.139125, 0.263754, 0.0134436, 0.111594, 0.0221224 };

            return cvector;                       
        }



    public double[][] loadsvector()
        {
            filepath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "s_175mm_16.txt");

            //List<double[]> data = new List<double[]>();
            

            using (StreamReader reader = new StreamReader(filepath))
            {
                string fileContent = reader.ReadToEnd();

                // Remove curly brackets and split by '},{'
                string rowStringinit = fileContent.Trim(new char[] { '{', '}' });
                string[] rowStrings=rowStringinit.Split(new string[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
                double[][] data =new double[rowStrings.Length][];
                for (int k=0;k<rowStrings.Length;k++)
                {
                    string rowString = rowStrings[k];
                    // Split each row string by comma and parse into double values
                    string[] valueStrings = rowString.Split(',');
                    double[] rowValues = new double[valueStrings.Length];

                    for (int i = 0; i < valueStrings.Length; i++)
                    {
                        if (double.TryParse(valueStrings[i], out double value))
                        {
                            rowValues[i] = value;
                        }
                        else
                        {
                            // Handle parsing error, if needed
                            // You can set a default value or skip the row
                        }
                    }

                    
                    data[k]=rowValues;
                }
                return data;
            }

            
        }




        public Matrix<double> rescaleM(Matrix<double> matrix)
        {
            double min = matrix.Enumerate().Min();
            double max = matrix.Enumerate().Max();
            for (int i = 0; i < matrix.ColumnCount; i++)
            {
                Vector<double> column = matrix.Column(i);
                matrix.SetColumn(i, (column - min) / (max - min));
            }
            return matrix;
        }
        public void data_stream()
        {
           if (freeze)
            {
                
                svector = loadsvector();
                int rows = svector.Length;
                int cols = svector[0].Length;
                Matrix<double> sMatrix = Matrix<double>.Build.Dense(rows, cols);

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        sMatrix[i, j] = svector[i][j];
                    }
                }
                Matrix<double> stMatrix = sMatrix.Transpose();

                switch (algorithm)
                {
                    case 1:                       
                        Q=stMatrix.Clone();
                    break;

                    case 2:
                        double lambda = double.Parse(textBox1.Text);
                        Matrix<double> dotProduct = sMatrix.Multiply(stMatrix);
                        Matrix<double> identity = Matrix<double>.Build.DenseIdentity(rows) * lambda;
                        Matrix<double> sum = dotProduct.Add(identity);
                        Matrix<double> inverse = sum.Inverse();

                        // Calculate QTik using the dot product and inverse
                        Q = stMatrix.Multiply(inverse);
                        
                    break;
                }
            }
            double[] calibrationFactors = new double[120];
            for (int i = 0; i < cvector.Length; i++)
            {
                calibrationFactors[i] = cmax[i] - czero[i];
            }


            while (freeze)
            {
                Application.DoEvents();
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Size size = new Size(picbox_width, picbox_height);
                cvector = getcvector(1);
                
                if (cvector.Length == 120 && freeze)
                {
                   // cvector = rescale(cvector);
                    for (int i = 0; i < cvector.Length; i++)
                    {
                        if (calibrationFactors[i] != 0)
                        {
                            cvector[i] = (cvector[i] - czero[i]) / calibrationFactors[i];
                        }
                        else
                        {
                            cvector[i] = 0; // Assign a default value when calibration factor is zero
                        }
                    }
                    cvector = rescale(cvector);
                    chart1.Series.Clear();
                    Series series = new Series("Capacitance Values");
                    series.ChartType = SeriesChartType.Column;
                    for (int i = 0; i < cvector.Length; i++)
                    {
                        series.Points.Add(cvector[i]);
                    }
                    chart1.Series.Add(series);
                    Vector<double> vectorC = Vector<double>.Build.Dense(cvector);
                    Vector<double> imageVector = Q.Multiply(vectorC);
                    double[] image =imageVector.ToArray();
                    image = rescale(image);
                    double[,] imagevector = ReshapeArray(image, 50, 50);
                    Bitmap map = getChartImg(imagevector, size);
                    //...
                    //debug.Text = "did it";
                    pictureBox1.Image = map;
                    stopwatch.Stop();
                    long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    double hz = 1.0 / (elapsedMilliseconds / 1000.0);
                    refresh.Text = hz.ToString("0.000");
                }
            }
        }
        private void display_tomograph_Click(object sender, EventArgs e)
        {

            if (!freeze)//&& connected
            {
                freeze = true;
                if (!check_conn())
                {
                    MessageBox.Show("Not Connected to the Sensor!");
                    freeze = false;
                    return;
                }
                if (!iszeroed || !ismaxed)//
                {
                    MessageBox.Show("Sensor has not been calibrated!");
                    freeze = false;
                    return;
                }
                int algo=choose_algo();
                if (algo == 0)
                {
                    MessageBox.Show("No reconstruction algorithm selected!");
                    freeze= false;
                    return;
                }
                display_tomograph.Text = "Freeze Tomograph";
                
                data_stream();
            }
            else
            {
                
                display_tomograph.Text = "Capture Tomograph";
                freeze = false;
            }
        }
       
        private void comboBox1_Click(object sender, EventArgs e)
        {
            comportselected = false;
            comboBox1.Items.Clear();
            string[] comPorts = SerialPort.GetPortNames();

            // Add each COM port as a menu item
            foreach (string comPort in comPorts)
            {
                comboBox1.Items.Add(comPort);

            }
        }

        private void comboBox1_SelectionChangeCommitted(object sender, EventArgs e)
        {
            comport = comboBox1.SelectedItem.ToString();
            comportselected = true;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void zerosensor_Click(object sender, EventArgs e)
        {
            Application.DoEvents();
            if (!check_conn())
            {
                MessageBox.Show("Not Connected to the Sensor!");
                iszeroed = false;
                button1.Enabled = false;
                return;
            }
            czero = getcvector(3);
            if (czero.Length == 120)
            {
                //czero = rescale(czero);
                iszeroed = true;
                button1.Enabled = true;
                chart1.Series.Clear();
                Series series = new Series("Zero Point Calibration");
                series.ChartType = SeriesChartType.Column;
                double[] rescaledczero = rescale(czero);
                for (int i = 0; i < cvector.Length; i++)
                {
                    series.Points.Add(rescaledczero[i]);
                }
                chart1.Series.Add(series);

            }
            else
            {
                iszeroed = false;
                button1.Enabled = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.DoEvents();
           
            if (!check_conn())
            {
                MessageBox.Show("Not Connected to the Sensor!");
                ismaxed = false;
                return;
            }
            cmax = getcvector(3);
            if (cmax.Length == 120)
            {
                //cmax = rescale(cmax);
                ismaxed = true;
                chart1.Series.Clear();
                Series series = new Series("High Point Calibration");
                series.ChartType = SeriesChartType.Column;
                double[] rescaledcmax = rescale(cmax);
                for (int i = 0; i < cvector.Length; i++)
                {
                    series.Points.Add(rescaledcmax[i]);
                }
                chart1.Series.Add(series);

            }
            else
            {
                ismaxed = false;
            }
        }
    }

}
