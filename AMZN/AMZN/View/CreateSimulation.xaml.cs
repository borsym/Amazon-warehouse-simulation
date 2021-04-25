using AMZN.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AMZN.View
{
    /// <summary>
    /// Interaction logic for CreateSimulation.xaml
    /// </summary>
    public partial class CreateSimulation : Window
    {
        public CreateSimulation()
        {
            InitializeComponent();
        }

        //hát ez így undorító, hogy Coords a típusa, de mivel csak két int kell, ezért felesleges ide egy struct
        //aztán mondjátok ha inkább legyen struct
        public EventHandler<Coords> updateTableSite;

        /// <summary>
        /// Új tábla szélessége
        /// </summary>
        public int SizeY
        {
            get { return Convert.ToInt32(width.Text);  }
            set { width.Text = value.ToString(); }
        }
        
        /// <summary>
        /// Új tábla magassága
        /// </summary>
        public int SizeX
        {
            get { return Convert.ToInt32(height.Text);  }
            set { height.Text = value.ToString(); }
        }

        private void OnUpdateTableSize(Coords size)
        {
            if (updateTableSite != null)
            {
                updateTableSite(this, size);
            }
        }

        private void ConfirmNewSize(object sender, System.Windows.RoutedEventArgs e)
        {
            if (SizeX > 0 && SizeY > 0)
            {
                OnUpdateTableSize(new Coords(SizeX, SizeY));
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("The table must be bigger!");
            }
        }

        private void CancelNewSize(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;
        }

    }
}
