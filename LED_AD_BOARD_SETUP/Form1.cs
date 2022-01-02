using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Drawing.Imaging;
using ZedGraph;
using System.Diagnostics;
using System.Xml.Serialization;
using System.IO;

namespace LED_AD_BOARD_GIF_CONVERTER
{
    public partial class Form1 : Form
    {
        Ad_board board;
        int grid_size = 10;
        bool isTextVisible = true;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GraphPane pane = zedGraphControl1.GraphPane;
            pane.XAxis.Scale.Min = 0;
            pane.YAxis.Scale.Min = 0;
            pane.XAxis.Scale.Max = 100;
            pane.YAxis.Scale.Max = 100;

            pane.Title.IsVisible            = false;
            pane.XAxis.Title.IsVisible      = false;
            pane.YAxis.Title.IsVisible      = false;
            pane.YAxis.MajorGrid.IsZeroLine = false;

            pane.XAxis.MajorGrid.IsVisible = true;
            pane.YAxis.MajorGrid.IsVisible = true;

            pane.XAxis.Scale.MajorStep = 10;
            pane.XAxis.Scale.MinorStep = 1.0;
            pane.YAxis.Scale.MajorStep = 10;
            pane.YAxis.Scale.MinorStep = 1.0;

            pane.XAxis.IsVisible = false;
            pane.YAxis.IsVisible = false;


            pane.Border.IsVisible = false;
            pane.Margin.All = 0;

            zgraph_update();

        }

        private void save_board_options(Board_Settings tmp)
        {
            XmlSerializer formatter = new XmlSerializer(typeof(Board_Settings));
            using (FileStream fs = new FileStream("ad_board_options.xml", FileMode.OpenOrCreate))
            {
                formatter.Serialize(fs, tmp);
                MessageBox.Show("Настройки сохранены");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Debug.Assert(board != null, "Экран не инициализирован!!!");

            Board_Settings tmp = new Board_Settings();
            tmp.Pixels = new Pix[board.Width * board.Heigth];
            tmp.Board_Width = board.Width;
            tmp.Board_Heigth = board.Heigth;

            int k = 0;
            for (int i = 0; i < board.Width; i++)
            {
                for (int j = 0; j < board.Heigth; j++)
                {
                    tmp.Pixels[k] = new Pix();
                    tmp.Pixels[k].x = i;
                    tmp.Pixels[k].y = j;
                    tmp.Pixels[k].line_num = board.pixel_array[i,j].line_num;
                    tmp.Pixels[k].num_in_line = board.pixel_array[i, j].num_in_line;
                    k++;
                }
            }

            List<int>   lines_len   = new List<int>();
            for (int i = 0; i < 128; i++){
                if (board.led_lines_len[i] > 0)
                    lines_len.Add(board.led_lines_len[i]);
            }
            tmp.lines_count = lines_len.Count;
            tmp.lines_length = new int[lines_len.Count];
            tmp.lines_length = lines_len.ToArray();

            save_board_options(tmp);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Board_Settings tmp;
            using (FileStream fs = new FileStream("ad_board_options.xml", FileMode.OpenOrCreate))
            {
                XmlSerializer formatter = new XmlSerializer(typeof(Board_Settings));
                tmp = (Board_Settings)formatter.Deserialize(fs);
                MessageBox.Show("Настройки загружены!");
            }

            board = new Ad_board(tmp.Board_Width, tmp.Board_Heigth);

            for (int i = 0; i < tmp.lines_length.Length; i++)
                board.led_lines_len[i] = tmp.lines_length[i];

            draw_grid();

            for (int i = 0; i < tmp.Pixels.Length; i++)
            {
                int x           = tmp.Pixels[i].x;
                int y           = tmp.Pixels[i].y;
                int line_num    = tmp.Pixels[i].line_num;
                int num_in_line = tmp.Pixels[i].num_in_line;

                board.pixel_array[x, y].line_num    = line_num;
                board.pixel_array[x, y].num_in_line = num_in_line;

                if (line_num>=0)
                {
                    GraphPane pane = zedGraphControl1.GraphPane;
                    TextObj text = new TextObj(line_num.ToString(/*"d3"*/) + "-" + num_in_line.ToString(/*"d4"*/), x * grid_size + 5, y * grid_size + 5);
                    text.FontSpec.Border.IsVisible = false;
                    text.FontSpec.Size = 4;
                    text.Tag = 1;
                    pane.GraphObjList.Add(text);
                }
            }

            zgraph_update();
        }

        private void draw_grid()
        {
            GraphPane pane = zedGraphControl1.GraphPane;
            pane.GraphObjList.Clear();

            for (int x = 0; x <= board.Width; x++)
                pane.GraphObjList.Add(new LineObj(x * grid_size, 0, x * grid_size, board.Heigth * grid_size));
            for (int y = 0; y <= board.Heigth; y++)
                pane.GraphObjList.Add(new LineObj(0, y * grid_size, board.Width * grid_size, y * grid_size));

            board.cursor_index = pane.GraphObjList.Count;
            pane.GraphObjList.Add(new BoxObj((double)(0 * grid_size), (double)(0 * grid_size + grid_size), (double)grid_size, (double)grid_size, Color.Empty, Color.Red));
            zgraph_update();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int x_size = (int)numericUpDown1.Value;
            int y_size = (int)numericUpDown2.Value;
            board = new Ad_board(x_size, y_size);
            draw_grid();
        }

        //************************************************************************
        private bool isPixelFree(int X, int Y) {
            if (board.pixel_array[X, Y].line_num == -1)   return true;
            else                                          return false;
        }

        private bool isRightPixel_Free(Pixel cursor)
        {
            int X = cursor.X, Y = cursor.Y;
            X++;
            if (X < board.Width && isPixelFree(X,Y)) return true;
            else                                     return false;
        }

        private bool isLeftPixel_Free(Pixel cursor)
        {
            int X = cursor.X, Y = cursor.Y;
            X--;
            if (X >= 0 && isPixelFree(X, Y))   return true;
            else                               return false;
        }

        private bool isUpPixel_Free(Pixel cursor)
        {
            int X = cursor.X, Y = cursor.Y;
            Y++;
            if (Y < board.Heigth && isPixelFree(X, Y)) return true;
            else                                       return false;
        }

        //************************************************************************
        private enum State { Direction_Left, Direction_Right, Direction_Up, Direction_Down }

        private void trace_line(int len, int dx_max, int line_num)
        {
            Debug.Assert(isPixelFree(board.cursor.X, board.cursor.Y), "недопустимая позиция курсора");
            State state;
            if (isRightPixel_Free(board.cursor)) state = State.Direction_Right;
            else                                 state = State.Direction_Left;

            GraphPane pane = zedGraphControl1.GraphPane;

            int dx_counter = 0;

            for (int i = 0; i < len; i++){
                int LN  = board.pixel_array[board.cursor.X, board.cursor.Y].line_num = line_num;
                int LLL = board.pixel_array[board.cursor.X, board.cursor.Y].num_in_line = board.led_lines_len[board.current_led_line] + i;

                TextObj text = new TextObj(LN.ToString(/*"d3"*/) + "-" + LLL.ToString(/*"d4"*/), board.cursor.X * grid_size + 5, board.cursor.Y * grid_size + 5);
                text.FontSpec.Border.IsVisible = false;
                text.FontSpec.Size = 4;
                text.Tag = 1;
                pane.GraphObjList.Add(text);

                if (dx_counter == dx_max)
                    dx_counter = 0;

                dx_counter++;
                bool need_shift_up = (dx_counter >= dx_max);

                switch (state)
                {
                    case State.Direction_Right:
                        if (dx_counter == dx_max){
                            if (isUpPixel_Free(board.cursor)) {
                                board.cursor.Y++;
                                state = State.Direction_Left;
                            }
                            else Debug.Assert(false,"err1");
                        }
                        else
                        {
                            if (isRightPixel_Free(board.cursor)) {
                                board.cursor.X++;
                            }
                            else if (isUpPixel_Free(board.cursor)) {
                                board.cursor.Y++;
                                state = State.Direction_Left;
                            }
                            else{
                                if (i != len - 1) MessageBox.Show("Внимание! Нет свободных клеток для размещения!");
                            }
                        }
                        break;

                    case State.Direction_Left:
                        if (dx_counter == dx_max){
                            if (isUpPixel_Free(board.cursor)){
                                board.cursor.Y++;
                                state = State.Direction_Right;
                            }
                            else Debug.Assert(false, "err3");
                        }
                        else
                        {
                            if (isLeftPixel_Free(board.cursor)){
                                board.cursor.X--;
                            }
                            else if (isUpPixel_Free(board.cursor)){
                                board.cursor.Y++;
                                state = State.Direction_Right;
                            }
                            else{
                                if (i != len - 1) MessageBox.Show("Внимание! Нет свободных клеток для размещения!");
                            }
                        }
                        break;

                    default:
                        Debug.Assert(false, "err5");
                        break;
                }
            }

            board.led_lines_len[board.current_led_line] += len;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            GraphPane pane = zedGraphControl1.GraphPane;
            int len = (int)numericUpDown3.Value;
            int m_x = (int)numericUpDown4.Value;

            if (comboBox1.SelectedIndex == 0)   trace_line(len, len+1000, board.current_led_line);
            else                                trace_line(len, m_x, board.current_led_line);
            zgraph_update();
        }

        private void zgraph_update()
        {
            GraphPane pane = zedGraphControl1.GraphPane;
            SetEqualScale(pane);
            zedGraphControl1.AxisChange();
            zedGraphControl1.Invalidate();
            zedGraphControl1.Refresh();
        }

        public void SetEqualScale(GraphPane pane)
        {
            double Xmin = pane.XAxis.Scale.Min;
            double Xmax = pane.XAxis.Scale.Max;

            double Ymin = pane.YAxis.Scale.Min;
            double Ymax = pane.YAxis.Scale.Max;

            PointF PointMin = pane.GeneralTransform(Xmin, Ymin, CoordType.AxisXYScale);
            PointF PointMax = pane.GeneralTransform(Xmax, Ymax, CoordType.AxisXYScale);
            double dX = Math.Abs(Xmax - Xmin);
            double dY = Math.Abs(Ymax - Ymin);

            double Kx = dX / Math.Abs(PointMax.X - PointMin.X);
            double Ky = dY / Math.Abs(PointMax.Y - PointMin.Y);

            double K = Kx / Ky;

            if (K > 1.0)
            {
                pane.YAxis.Scale.Min = pane.YAxis.Scale.Min - dY * (K - 1.0) / 2.0;
                pane.YAxis.Scale.Max = pane.YAxis.Scale.Max + dY * (K - 1.0) / 2.0;
            }
            else
            {
                K = 1.0 / K;
                pane.XAxis.Scale.Min = pane.XAxis.Scale.Min - dX * (K - 1.0) / 2.0;
                pane.XAxis.Scale.Max = pane.XAxis.Scale.Max + dX * (K - 1.0) / 2.0;
            }

            //zedGraphControl1.AxisChange();
            //zedGraphControl1.Invalidate();
        }

        private void zedGraphControl1_ZoomEvent(ZedGraphControl sender, ZoomState oldState, ZoomState newState)
        {
            //if (pane.XAxis.Scale.Min <= 0)   pane.XAxis.Scale.Min   = 0;
            //if (pane.XAxis.Scale.Max >= 1000)  pane.XAxis.Scale.Max   = 1000;
            //if (pane.YAxis.Scale.Min <= 0)   pane.YAxis.Scale.Min   = 0;
            //if (pane.YAxis.Scale.Max >= 1000)    pane.YAxis.Scale.Max   = 1000;

            GraphPane pane = zedGraphControl1.GraphPane;
            if (pane.XAxis.Scale.Max - pane.XAxis.Scale.Min > 1000)
            {
                if (isTextVisible)
                {
                    
                    for (int i = 0; i < pane.GraphObjList.Count; i++)
                    {
                        if (pane.GraphObjList[i].Tag != null)
                            pane.GraphObjList[i].IsVisible = false;
                    }
                    isTextVisible = false;
                }
            }
            else {
                if (!isTextVisible)
                {
                    for (int i = 0; i < pane.GraphObjList.Count; i++)
                    {
                        if (pane.GraphObjList[i].Tag != null)
                            pane.GraphObjList[i].IsVisible = true;
                    }
                    isTextVisible = true;
                }
            }

            zgraph_update();
        }

        private void zedGraphControl1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            GraphPane pane = zedGraphControl1.GraphPane;
            double x, y;
            int current_pixel_x, current_pixel_y;

            zedGraphControl1.GraphPane.ReverseTransform(e.Location, out x, out y);
            current_pixel_x  = (int)(x/grid_size);
            current_pixel_y  = (int)(y/grid_size);

            if ((current_pixel_x < 0 || current_pixel_x >= board.Width) || (current_pixel_y < 0 || current_pixel_y >= board.Heigth))
                return;

            label3.Text = "x=" + current_pixel_x.ToString() + " y=" + current_pixel_y.ToString() ;
            label6.Text = "Номер ленты: "    + board.pixel_array[current_pixel_x, current_pixel_y].line_num.ToString();
            label7.Text = "Номер в ленте: "  + board.pixel_array[current_pixel_x, current_pixel_y].num_in_line.ToString();

            board.cursor.X = current_pixel_x;
            board.cursor.Y = current_pixel_y;
            pane.GraphObjList.RemoveAt(board.cursor_index);
            board.cursor_index = pane.GraphObjList.Count;
            pane.GraphObjList.Add(new BoxObj((double)(current_pixel_x * grid_size), (double)(current_pixel_y * grid_size + grid_size), (double)grid_size, (double)grid_size, Color.Empty, Color.Red));

            zgraph_update();
        }

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            board.current_led_line = (int)numericUpDown5.Value;
        }
    }

    class Ad_board
    {
        public int Width;
        public int Heigth;
        public Ad_board_pixel[,] pixel_array;
        public Pixel cursor;
        public int   cursor_index;

        public int [] led_lines_len;
        public int current_led_line;

        public Ad_board(int width, int height)
        {
            this.pixel_array = new Ad_board_pixel[width, height];
            for (int i = 0; i < width; i++){
                for (int j = 0; j < height; j++){
                    pixel_array[i, j] = new Ad_board_pixel();
                }
            }
            this.Width = width;
            this.Heigth = height;
            this.cursor = new Pixel(0, 0);

            this.led_lines_len = new int[128];
            for (int i = 0; i < this.led_lines_len.Length; i++)
                this.led_lines_len[i] = 0;

            this.current_led_line = 0;
        }
    }

    class Pixel
    {
        public int X;
        public int Y;

        public Pixel(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    class Ad_board_pixel
    {
        public int line_num;
        public int num_in_line;

        public Ad_board_pixel(){
            this.line_num = -1;
            this.num_in_line = -1;
        }
    }

    //for serializer

    public class Board_Settings
    {
        public int Board_Width;
        public int Board_Heigth;
        public int lines_count;
        public int[] lines_length;

        public Pix[] Pixels;

        public Board_Settings()
        {
        }
    }

    public class Pix
    {
        public int x;
        public int y;
        public int line_num;
        public int num_in_line;
    }

}
