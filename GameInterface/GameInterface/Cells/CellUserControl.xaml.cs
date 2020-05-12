using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Point = MCTProcon31Protocol.Point;

namespace GameInterface.Cells
{
    /// <summary>
    /// CellUserControl.xaml の相互作用ロジック
    /// </summary>
    public partial class CellUserControl : UserControl
    {
        private GameManagement.GameManager gameManager;
        private Point point;

        public CellUserControl(GameManagement.GameManager gameMan, Point p)
        {
            InitializeComponent();
            gameManager = gameMan;
            point = p;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            gameManager.ChangeCellToNextColor(point);
        }

        private void Context_Place_Blue(object sender, RoutedEventArgs e)
        {
            gameManager.PlaceAgent(0, point);
        }

        private void Context_Place_Red(object sender, RoutedEventArgs e)
        {
            gameManager.PlaceAgent(1, point);
        }
    }
}
