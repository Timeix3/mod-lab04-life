using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading;
using SkiaSharp;
using ScottPlot;
using System.Text.Json;
using System.IO;

namespace cli_life
{
    public enum Topology
    {
        Grid,
        Sphere,
        Cylinder
    }

    public class GameConfig
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int CellSize { get; set; }
        public double LiveDensity { get; set; }
        public Topology BoardTopology { get; set; }
    }

    public class Cell(int x, int y)
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;
        public bool Checked = false;
        public int X = x;
        public int Y = y;

        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }

    public class Board
    {
        public Cell[,] Cells;
        public int CellSize;
        public Topology BoardTopology;
        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, Topology topology, double liveDensity = .1)
        {
            BoardTopology = topology;
            CellSize = cellSize;
            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell(x, y);
            ConnectNeighbors();
            Randomize(liveDensity);
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }

        private void ConnectNeighbors()
        {
            switch (BoardTopology)
            {
                case Topology.Grid:
                    for (int x = 0; x < Columns; x++)
                    {
                        for (int y = 0; y < Rows; y++)
                        {
                            if (y > 0)
                            {
                                Cells[x, y].neighbors.Add(Cells[x, y - 1]);
                            }
                            if (y < Rows - 1)
                            {
                                Cells[x, y].neighbors.Add(Cells[x, y + 1]);
                            }
                            if (x > 0)
                            {
                                Cells[x, y].neighbors.Add(Cells[x - 1, y]);
                            }
                            if (x < Columns - 1)
                            {
                                Cells[x, y].neighbors.Add(Cells[x + 1, y]);
                            }
                            if (x > 0 && y > 0)
                            {
                                Cells[x, y].neighbors.Add(Cells[x - 1, y - 1]);
                            }
                            if (x < Columns - 1 && y > 0)
                            {
                                Cells[x, y].neighbors.Add(Cells[x + 1, y - 1]);
                            }
                            if (x > 0 && y < Rows - 1)
                            {
                                Cells[x, y].neighbors.Add(Cells[x - 1, y + 1]);
                            }
                            if (x < Columns - 1 && y < Rows - 1)
                            {
                                Cells[x, y].neighbors.Add(Cells[x + 1, y + 1]);
                            }
                        }
                    }
                    break;
                case Topology.Sphere:
                    for (int x = 0; x < Columns; x++)
                    {
                        for (int y = 0; y < Rows; y++)
                        {
                            int xL = (x > 0) ? x - 1 : Columns - 1;
                            int xR = (x < Columns - 1) ? x + 1 : 0;

                            int yT = (y > 0) ? y - 1 : Rows - 1;
                            int yB = (y < Rows - 1) ? y + 1 : 0;

                            Cells[x, y].neighbors.Add(Cells[xL, yT]);
                            Cells[x, y].neighbors.Add(Cells[x, yT]);
                            Cells[x, y].neighbors.Add(Cells[xR, yT]);
                            Cells[x, y].neighbors.Add(Cells[xL, y]);
                            Cells[x, y].neighbors.Add(Cells[xR, y]);
                            Cells[x, y].neighbors.Add(Cells[xL, yB]);
                            Cells[x, y].neighbors.Add(Cells[x, yB]);
                            Cells[x, y].neighbors.Add(Cells[xR, yB]);
                        }
                    }
                    break;
                case Topology.Cylinder:
                    for (int x = 0; x < Columns; x++)
                    {
                        for (int y = 0; y < Rows; y++)
                        {
                            if (y > 0)
                            {
                                Cells[x, y].neighbors.Add(Cells[x, y - 1]);
                            }

                            if (y < Rows - 1)
                            {
                                Cells[x, y].neighbors.Add(Cells[x, y + 1]);
                            }

                            int xL = (x == 0) ? Columns - 1 : x - 1;
                            Cells[x, y].neighbors.Add(Cells[xL, y]);

                            int xR = (x == Columns - 1) ? 0 : x + 1;
                            Cells[x, y].neighbors.Add(Cells[xR, y]);

                            if (y > 0)
                            {
                                Cells[x, y].neighbors.Add(Cells[xL, y - 1]);
                                Cells[x, y].neighbors.Add(Cells[xR, y - 1]);
                            }

                            if (y < Rows - 1)
                            {
                                Cells[x, y].neighbors.Add(Cells[xL, y + 1]);
                                Cells[x, y].neighbors.Add(Cells[xR, y + 1]);
                            }
                        }
                    }
                    break;
            }
        }

        public int CountCellsNumber()
        {
            int count = 0;
            foreach (var cell in Cells)
                if (cell.IsAlive) count++;
            return count;
        }

        public List<HashSet<(int x, int y)>> SelectFigures()
        {
            foreach (var cell in Cells)
                cell.Checked = false;
            List<HashSet<(int x, int y)>> figures = new();
            foreach (var cell in Cells)
                if (cell.IsAlive && !cell.Checked)
                {
                    HashSet<(int x, int y)> figure = new();
                    figures.Add(AddFigure(figure, cell));
                }
            return figures;
        }

        private HashSet<(int x, int y)> AddFigure(HashSet<(int x, int y)> figure, Cell cell)
        {
            figure.Add((cell.X, cell.Y));
            cell.Checked = true;
            foreach (var neighbor in cell.neighbors)
                if (neighbor.IsAlive && !neighbor.Checked)
                    AddFigure(figure, neighbor);
            return figure;
        }

        public void SaveToFile(string projectDirectory)
        {
            StreamWriter writer = new(Path.Combine(projectDirectory, "board.txt"));
            SKBitmap bitmap = new SKBitmap(Columns * 10, Rows * 10);
            SKCanvas canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);
            writer.WriteLine(Width + " " + Height + " " + CellSize + " " + BoardTopology.ToString());
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    writer.Write(Cells[x, y].IsAlive ? 1 : 0);
                    SKPaint paint = new()
                    {
                        Color = Cells[x, y].IsAlive ? SKColors.Black : SKColors.White
                    };
                    canvas.DrawRect(x * 10, y * 10, 10, 10, paint);
                }
                writer.WriteLine();
            }
            SKImage image = SKImage.FromBitmap(bitmap);
            var data = image.Encode(SKEncodedImageFormat.Png, 80);
            using (var stream = File.OpenWrite(Path.Combine(projectDirectory, "board.png")))
            {
                data.SaveTo(stream);
            }
            writer.Close();
        }

        public void LoadFromFile(string projectDirectory, string filePath = "board.txt")
        {
            if (!File.Exists(Path.Combine(projectDirectory, filePath)))
                throw new FileNotFoundException("File not found");
            using (StreamReader reader = new StreamReader(Path.Combine(projectDirectory, filePath)))
            {
                string firstLine = reader.ReadLine();
                string[] parameters = firstLine.Split(' ');

                int width = int.Parse(parameters[0]);
                int height = int.Parse(parameters[1]);
                int cellSize = int.Parse(parameters[2]);
                Topology topology = (Topology)Enum.Parse(typeof(Topology), parameters[3]);

                Cell[,] cells = new Cell[width / cellSize, height / cellSize];
                for (int y = 0; y < cells.GetLength(1); y++)
                {
                    string line = reader.ReadLine();
                    for (int x = 0; x < cells.GetLength(0); x++)
                    {
                        cells[x, y] = new Cell(x, y);
                        cells[x, y].IsAlive = line[x] == '1';
                    }
                }
                BoardTopology = topology;
                CellSize = cellSize;
                Cells = cells;
                ConnectNeighbors();
            }
        }
    }

    public class FigureAnalyzer
    {
        public static HashSet<(int x, int y)> RotateFigure(HashSet<(int x, int y)> figure)
           => figure.Select(point => point = (point.y, -point.x)).ToHashSet();

        public static HashSet<(int x, int y)> NormalizeFigure(HashSet<(int x, int y)> figure)
        {
            int minX = figure.Min(point => point.x);
            int minY = figure.Min(point => point.y);
            return figure.Select(point => point = (point.x - minX, point.y - minY)).ToHashSet();
        }

        public static void ClassifyFigures(Dictionary<string, HashSet<(int x, int y)>> loadedFigures,
            List<HashSet<(int x, int y)>> figures)
        {
            foreach (var figure in figures)
            {
                bool matchFound = false;
                var normalizedFigure = figure;
                foreach (var loadedFigure in loadedFigures)
                {
                    if (figure.Count != loadedFigure.Value.Count) continue;
                    for (int i = 0; i < 4; i++)
                    {
                        normalizedFigure = NormalizeFigure(RotateFigure(normalizedFigure));
                        if (normalizedFigure.SetEquals(loadedFigure.Value))
                        {
                            Console.WriteLine($"{loadedFigure.Key}: size {figure.Count}");
                            matchFound = true;
                            break;
                        }
                    }
                }
                if (!matchFound) Console.WriteLine($"unknown figure: size {figure.Count}");
            }
        }

        public static Dictionary<string, HashSet<(int x, int y)>> LoadFigures(string projectDirectory)
        {
            Dictionary<string, HashSet<(int x, int y)>> figures = new();
            string[] figuresFiles = Directory.GetFiles(Path.Combine(projectDirectory, "figures"), "*.txt");
            foreach (string filePath in figuresFiles)
            {
                HashSet<(int x, int y)> figure = new();
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    int y = 0;
                    while ((line = reader.ReadLine()) != null)
                    {
                        for (int x = 0; x < line.Length; x++)
                        {
                            if (line[x] == '1') figure.Add((x, y));
                        }
                        y++;
                    }
                }
                figures.Add(Path.GetFileNameWithoutExtension(filePath), figure);
            }
            return figures;
        }
    }

    class PlotCreator
    {
        public static void CreatePlot(string projectDirectory)
        {
            int maxGeneraions = 700;
            if (!File.Exists(Path.Combine(projectDirectory, "data.txt")))
                GatherDataForPlot(projectDirectory, maxGeneraions);
            DrawPlot(projectDirectory, maxGeneraions);
        }

        public static void GatherDataForPlot(string projectDirectory, int maxGeneraions)
        {
            double[] densities = { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9 };
            StreamWriter writer = new(Path.Combine(projectDirectory, "data.txt"));
            for (int i = 0; i < densities.Length; i++)
            {
                Board board = new Board(
                width: 50,
                height: 20,
                cellSize: 1,
                topology: Topology.Sphere,
                liveDensity: densities[i]);
                writer.WriteLine(densities[i]);
                for (int generation = 1; generation <= maxGeneraions; generation++)
                {
                    writer.WriteLine(board.CountCellsNumber());
                    board.Advance();
                }
            }
            writer.Close();
        }

        public static void DrawPlot(string projectDirectory, int maxGeneraions)
        {
            Color[] colors = { Colors.Red, Colors.Black, Colors.Yellow, Colors.Green,
                Colors.Pink, Colors.Purple, Colors.Blue, Colors.Orange, Colors.LightBlue };
            Plot myPlot = new();
            using (StreamReader reader = new StreamReader(Path.Combine(projectDirectory, "data.txt")))
            {
                for (int i = 0; i < colors.Length; i++)
                {
                    string density = reader.ReadLine();
                    Board board = new Board(
                    width: 50,
                    height: 20,
                    cellSize: 1,
                    topology: Topology.Sphere,
                    liveDensity: double.Parse(density));
                    int[] cellsNumber = new int[maxGeneraions];
                    int[] generations = new int[maxGeneraions];
                    for (int j = 0; j < maxGeneraions; j++)
                    {
                        string value = reader.ReadLine();
                        cellsNumber[j] = int.Parse(value);
                        generations[j] = j + 1;
                    }
                    var myScatter = myPlot.Add.ScatterLine(generations, cellsNumber, colors[i]);
                    myScatter.LineWidth = 2;
                    myScatter.LegendText = density;
                }
            }
            myPlot.XLabel("Generations");
            myPlot.YLabel("Alive Cells");
            myPlot.Legend.Alignment = Alignment.UpperRight;
            myPlot.ShowLegend();
            myPlot.SavePng(Path.Combine(projectDirectory, "plot.png"), 1920, 1080);
        }
    }

    class Program
    {
        static Board board;
        private static void Reset(string projectDirectory)
        {
            if (File.Exists(Path.Combine(projectDirectory, "config.json")))
            {
                string jsonString = File.ReadAllText(Path.Combine(projectDirectory, "config.json"));
                GameConfig config = JsonSerializer.Deserialize<GameConfig>(jsonString);
                board = new Board(
                    width: config.Width,
                    height: config.Height,
                    cellSize: config.CellSize,
                    topology: config.BoardTopology,
                    liveDensity: config.LiveDensity);
            }
            else
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var config = new GameConfig
                {
                    Width = 50,
                    Height = 20,
                    CellSize = 1,
                    LiveDensity = 0.5,
                    BoardTopology = Topology.Grid
                };
                string jsonString = JsonSerializer.Serialize(config, options);
                File.WriteAllText(Path.Combine(projectDirectory, "config.json"), jsonString);
                board = new Board(
                    width: config.Width,
                    height: config.Height,
                    cellSize: config.CellSize,
                    topology: config.BoardTopology,
                    liveDensity: config.LiveDensity);
            }
        }
        private static void Render()
        {
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    var cell = board.Cells[col, row];
                    if (cell.IsAlive)
                    {
                        Console.Write('*');
                    }
                    else
                    {
                        Console.Write(' ');
                    }
                }
                Console.Write('\n');
            }
        }
        static void Main()
        {
            string projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            Reset(projectDirectory);
            int generation = 0;
            while (true)
            {
                generation++;
                if (Console.KeyAvailable)
                {
                    ConsoleKey key = Console.ReadKey(true).Key;
                    if (key == ConsoleKey.Q)
                        break;
                    else if (key == ConsoleKey.S)
                    {
                        board.SaveToFile(projectDirectory);
                    }
                    else if (key == ConsoleKey.L)
                    {
                        board.LoadFromFile(projectDirectory);
                        gens.Clear();
                        generation = 1;
                    }
                }
                Console.Clear();
                Render();
                Console.WriteLine($"Generation: {generation}");
                if (StableStateCheck(board.CountCellsNumber()))
                {
                    Console.WriteLine($"Board is stable in generation {generation}");
                    List<HashSet<(int x, int y)>> figures = board.SelectFigures();
                    Console.WriteLine("Number of figures: " + figures.Count);
                    FigureAnalyzer.ClassifyFigures(FigureAnalyzer.LoadFigures(projectDirectory), figures);
                    break;
                }
                board.Advance();
                Thread.Sleep(500);
            }
            PlotCreator.CreatePlot(projectDirectory);
            Console.WriteLine("\nThe average time of transition to a stable phase: " + Math.Round(StablePhaseTransitionAverageTime()));
        }

        private static double StablePhaseTransitionAverageTime(double density = .5)
        {
            double sum = 0;
            int experimentsNumber = 10;
            for (int i = 0; i < experimentsNumber; i++)
            {
                gens.Clear();
                Board board = new Board(
                width: 50,
                height: 20,
                cellSize: 1,
                topology: Topology.Sphere,
                liveDensity: density);
                int generation = 0;
                while (true)
                {
                    generation++;
                    if (StableStateCheck(board.CountCellsNumber()))
                    {
                        sum += generation;
                        break;
                    }
                    if (generation >= 10000) break;
                    board.Advance();
                }
            }
            return sum / experimentsNumber;
        }

        private static List<int> gens = new();
        private static bool StableStateCheck(int count)
        {
            int unchangeGenerationsCount = 5;
            gens.Add(count);
            if (gens.Count == unchangeGenerationsCount)
            {
                if (gens.All(x => x == gens[0])) return true;
                gens.RemoveAt(0);
            }
            return false;
        }
    }
}