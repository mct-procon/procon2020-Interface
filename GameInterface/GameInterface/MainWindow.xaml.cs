using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Markup;
using GameInterface.Cells;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using GameInterface.GameManagement;
using Point = MCTProcon30Protocol.Point;
using MCTProcon30Protocol;
using GameInterface.ViewModels;

namespace GameInterface
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel viewModel;
        private GameManager gameManager;

        private PlayerControlPanel Player1Window;
        private PlayerControlPanel Player2Window;

        public MainWindow()
        {
            InitializeComponent();
            this.viewModel = new MainWindowViewModel();
            this.viewModel.MainWindowDispatcher = Dispatcher;
            this.DataContext = this.viewModel;
            this.gameManager = new GameManager(viewModel,this);
            this.viewModel.gameManager = this.gameManager;
        }

        public void InitGame(GameSettings.SettingStructure settings)
        {
            gameManager.InitGameData(settings);
            CreateCellOnCellGrid(gameManager.Data.BoardWidth, gameManager.Data.BoardHeight);
            Player1Window = new PlayerControlPanel(gameManager, viewModel.Players[0]);
            Player2Window = new PlayerControlPanel(gameManager, viewModel.Players[1]);
            if (settings.IsUser1P)
                Player1Window.Show();
            if (settings.IsUser2P)
                Player2Window.Show();
        }

        void CreateCellOnCellGrid(int boardWidth, int boardHeight)
        {
            // Clear before game.
            cellGrid.RowDefinitions.Clear();
            cellGrid.ColumnDefinitions.Clear();

            List<Cells.CellUserControl> cells = new List<CellUserControl>();
            foreach(var ctrl in cellGrid.Children)
            {
                if (ctrl is Cells.CellUserControl)
                    cells.Add((Cells.CellUserControl)ctrl);
            }
            foreach (var ctrl in cells)
                cellGrid.Children.Remove(ctrl);
            // end

            //Gridに列、行を追加
            for (int i = 0; i < boardHeight; i++)
                cellGrid.RowDefinitions.Add(new RowDefinition());
            for (int i = 0; i < boardWidth; i++)
                cellGrid.ColumnDefinitions.Add(new ColumnDefinition());

            //i行目のj列目にテキストを追加
            for (int i = 0; i < boardWidth; i++)
            {
                for (int j = 0; j < boardHeight; j++)
                {
                    var cellUserControl = new CellUserControl();
                    cellUserControl.DataContext = gameManager.Data.CellData[i, j];
                    cellGrid.Children.Add(cellUserControl);
                    Grid.SetColumn(cellUserControl, i);
                    Grid.SetRow(cellUserControl, j);

                    var changeColorUserCtrl = new ChangeColorUserCtrl(new Point((byte)i, (byte)j));
                    cellGrid.Children.Add(changeColorUserCtrl);
                    Grid.SetColumn(changeColorUserCtrl, i);
                    Grid.SetRow(changeColorUserCtrl, j);
                }
            }
        }
        
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            MenuButton.ContextMenu.IsOpen = true;
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            //まだEndTurn()していないなら、しておく
            if (gameManager.Data.IsNextTurnStart) gameManager.EndTurn();
            gameManager.StartTurn();
        }

        private void BreakMenu_Clicked(object sender, RoutedEventArgs e)
        {
            if (!System.Diagnostics.Debugger.IsAttached)
                if (!System.Diagnostics.Debugger.Launch())
                    return;
            System.Diagnostics.Debugger.Break();
        }

        private void NewGameMenu_Clicked(object sender, RoutedEventArgs e)
        {
            viewModel.gameManager.TimerStop();
            if (viewModel != null && viewModel.gameManager != null && viewModel.gameManager.Data != null && viewModel.gameManager.Data.IsGameStarted && (viewModel.gameManager.Data.NowTurn < viewModel.gameManager.Data.FinishTurn))
                viewModel.gameManager.Server.SendGameEnd();
            if (GameSettings.GameSettingDialog.ShowDialog(out var result))
            {
                Player1Window?.Close();
                Player2Window?.Close();
                InitGame(result);
                if (!(result.IsUser1P & result.IsUser2P))
                    (new GameSettings.WaitForAIDialog(viewModel.gameManager.Server, result)).ShowDialog();
                gameManager.StartGame();
            }
        }

        public void ShotAndSave()
        {
            double actualHeight = ((UIElement)Content).RenderSize.Height;
            double actualWidth = ((UIElement)Content).RenderSize.Width;


            RenderTargetBitmap renderTarget = new RenderTargetBitmap((int)actualWidth, (int)actualHeight, 96, 96, PixelFormats.Pbgra32);
            VisualBrush sourceBrush = new VisualBrush((UIElement)Content);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();

            using (drawingContext)
            {
                drawingContext.DrawRectangle(sourceBrush, null, new Rect(new System.Windows.Point(0, 0), new System.Windows.Point(actualWidth, actualHeight)));
            }
            renderTarget.Render(drawingVisual);

            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTarget));
            using (System.IO.FileStream stream = new System.IO.FileStream(System.IO.Path.Combine("Saves", $"{ DateTime.Now.ToString("MM日H時m分")}.jpg"), System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                encoder.Save(stream);
            }
        }

        private void Show1PButton_Click(object sender, RoutedEventArgs e)
        {
            if (Player1Window == null)
                return;
            if (Player1Window.Visibility == Visibility.Visible)
                Player1Window.Visibility = Visibility.Hidden;
            else
                Player1Window.Visibility = Visibility.Visible;
        }

        private void Show2PButton_Click(object sender, RoutedEventArgs e)
        {
            if (Player2Window == null)
                return;
            if (Player2Window.Visibility == Visibility.Visible)
                Player2Window.Visibility = Visibility.Hidden;
            else
                Player2Window.Visibility = Visibility.Visible;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
