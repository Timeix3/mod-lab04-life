using cli_life;

namespace LifeTests
{
    public class GameTests
    {
        string projectDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
        [Fact]
        public void CellTest1()
        {
            var cell = new Cell(0, 0) { IsAlive = false };
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.False(cell.IsAlive);
        }

        [Fact]
        public void CellTest2()
        {
            var cell = new Cell(0, 0) { IsAlive = false };
            cell.neighbors.AddRange([
                new Cell(0, 1) { IsAlive = true },
                new Cell(1, 0) { IsAlive = true },
                new Cell(1, 1) { IsAlive = true }
            ]);
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.True(cell.IsAlive);
        }

        [Fact]
        public void CellTest3()
        {
            var cell = new Cell(0, 0) { IsAlive = true };
            cell.neighbors.AddRange([
                new Cell(0, 1) { IsAlive = true },
                new Cell(1, 0) { IsAlive = true }
            ]);
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.True(cell.IsAlive);
        }

        [Fact]
        public void CellTest4()
        {
            var cell = new Cell(0, 0) { IsAlive = true };
            cell.neighbors.Add(new Cell(1, 0) { IsAlive = false });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.False(cell.IsAlive);
        }

        [Fact]
        public void BoardTest1()
        {
            var board = new Board(100, 100, 10, Topology.Grid);
            Assert.Equal(10, board.Columns);
            Assert.Equal(10, board.Rows);
        }

        [Fact]
        public void BoardTest3()
        {
            var board = new Board(20, 20, 1, Topology.Grid);
            Assert.Equal(3, board.Cells[0, 0].neighbors.Count);
        }

        [Fact]
        public void BoardTest4()
        {
            var board = new Board(20, 20, 1, Topology.Sphere);
            Assert.Equal(8, board.Cells[0, 0].neighbors.Count);
        }

        [Fact]
        public void BoardTest5()
        {
            var board = new Board(20, 20, 1, Topology.Cylinder);
            Assert.Equal(5, board.Cells[0, 0].neighbors.Count);
        }

        [Fact]
        public void BoardTest6()
        {
            var board = new Board(20, 20, 1, Topology.Sphere);
            board.LoadFromFile(projectDirectory, "boards/clock.txt");
            Assert.Equal(5, board.SelectFigures().Count);
        }

        [Fact]
        public void BoardTest7()
        {
            var board = new Board(20, 20, 1, Topology.Sphere);
            board.LoadFromFile(projectDirectory, "boards/ship.txt");
            HashSet<(int x, int y)> ship = new()
            {
                (0, 0), (0, 1), (1, 0), (1, 2), (2, 1), (2, 2)
            };
            var figures = board.SelectFigures();
            Assert.Equal(ship, FigureAnalyzer.NormalizeFigure(figures[0]));
        }

        [Fact]
        public void BoardTest8()
        {
            var board = new Board(20, 20, 1, Topology.Sphere);
            board.LoadFromFile(projectDirectory, "boards/glider.txt");
            var before = FigureAnalyzer.NormalizeFigure(board.SelectFigures()[0]);
            for (int i = 0; i < 4; i++)
                board.Advance();
            var after = FigureAnalyzer.NormalizeFigure(board.SelectFigures()[0]);
            Assert.Equal(before, after);
        }

        [Fact]
        public void BoardTest9()
        {
            var board = new Board(5, 5, 1, Topology.Sphere);
            Cell cell = board.Cells[0, 0];
            Assert.Contains(board.Cells[4, 4], cell.neighbors);
        }

        [Fact]
        public void BoardTest10()
        {
            var board = new Board(5, 5, 1, Topology.Sphere);
            var exception = Assert.Throws<FileNotFoundException>(() => board.LoadFromFile(projectDirectory, "maze.txt"));
            Assert.Equal("File not found", exception.Message);
        }

        [Fact]
        public void FigureRotationTest()
        {
            HashSet<(int x, int y)> blinker = new()
            {
                (0, 0), (1, 0), (2, 0)
            };
            HashSet<(int x, int y)> rotatedBlinker = new()
            {
                (0, 0), (0, 1), (0, 2)
            };
            Assert.Equal(FigureAnalyzer.NormalizeFigure(FigureAnalyzer.RotateFigure(blinker)), rotatedBlinker);
        }

        [Fact]
        public void FigureClassificationTest()
        {
            var dict = new Dictionary<string, HashSet<(int x, int y)>>
            {
            { "blinker", new HashSet<(int x, int y)> { (0, 0), (1, 0), (2, 0) } }
            };
            var board = new Board(20, 20, 1, Topology.Sphere);
            board.LoadFromFile(projectDirectory, "boards/4blinkers.txt");
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
            FigureAnalyzer.ClassifyFigures(dict, board.SelectFigures());
            var output = stringWriter.ToString();
            Assert.Equal("blinker: size 3\nblinker: size 3\nblinker: size 3\nblinker: size 3\n", output);
        }
    }
}