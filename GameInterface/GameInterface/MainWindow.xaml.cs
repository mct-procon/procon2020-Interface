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
            CreateOrderButtonsOnPlayerGrid();
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
        
        void CreateOrderButtonsOnPlayerGrid()
        {
            List<Controls.UserOrderPanel> removes = new List<Controls.UserOrderPanel>();
            foreach(var a in player1Grid.Children)
            {
                if (a is Controls.UserOrderPanel)
                    removes.Add((Controls.UserOrderPanel)a);
            }
            foreach (var rem in removes)
                player1Grid.Children.Remove(rem);
            removes.Clear();
            foreach (var a in player2Grid.Children)
            {
                if (a is Controls.UserOrderPanel)
                    removes.Add((Controls.UserOrderPanel)a);
            }
            foreach (var rem in removes)
                player2Grid.Children.Remove(rem);
            for (int i = 0; i < App.PlayersCount; i++)
            {
                var currentGrid = i == 0 ? player1Grid : player2Grid;
                var orderButtonUserControl = new Controls.UserOrderPanel() { DataContext = viewModel.Players[i].AgentViewModels[0] };
                currentGrid.Children.Add(orderButtonUserControl);
                Grid.SetRow(orderButtonUserControl, 2);
                var orderButtonUserControl2 = new Controls.UserOrderPanel() { DataContext = viewModel.Players[i].AgentViewModels[1] };
                currentGrid.Children.Add(orderButtonUserControl2);
                Grid.SetRow(orderButtonUserControl2, 4);
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

        private void Decisions1P_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var decided = (Decision)((ListBox)sender).SelectedItem;
            if (decided == null) return;
            gameManager.SetDecision(0, decided);
        }

        private void Decisions2P_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var decided = (Decision)((ListBox)sender).SelectedItem;
            if (decided == null) return;
            gameManager.SetDecision(1, decided);
        }
    }
}
