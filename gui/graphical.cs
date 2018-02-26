using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace netControl
{
    public partial class Graphical: Form
    {
        public Graphical()
        {
            InitializeComponent();
        }

        private List<double> data = new List<double>();

        public void AddData(double p)
        {
            data.Add(p);
            if (data.Count == 11) data.RemoveAt(0);
            chart1.Series[0].Points.Clear();
            for(int i = 0; i < data.Count; i++)
            {
                chart1.Series[0].Points.AddXY(i+1, data[i]);
            }
        }

        private Point mousePoint;

        private void Graphical_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                mousePoint = new Point(e.X, e.Y);
            }
        }

        private void Graphical_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                Left += e.X - mousePoint.X;
                Top += e.Y - mousePoint.Y;
            }
        }
    }
}
