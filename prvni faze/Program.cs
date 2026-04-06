using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Media; // TADY JE KNIHOVNA PRO HUDBU!

namespace GeometryDashWithMusic
{
    public class GameForm : Form
    {
        private System.Windows.Forms.Timer gameTimer;
        private Stopwatch stopwatch = new Stopwatch();
        private double timeAccumulator = 0;
        private const double FixedTimeStep = 16.6666;

        private Random rnd = new Random();

        // PØEHRÁVAÈ HUDBY
        private SoundPlayer bgMusic;

        // Fyzika a pozice
        private float playerX = 100;
        private float playerY = 0;
        private float velocityY = 0;
        private float gravity = 1.5f;
        private float jumpForce = -17f;
        private float speedX = 9f;
        private bool isGrounded = false;
        private float playerRotation = 0;
        private int playerSize = 40;

        // Level a kamera
        private List<Rectangle> platforms = new List<Rectangle>();
        private List<Rectangle> spikes = new List<Rectangle>();
        private Rectangle finishLine;
        private float cameraX = 0;

        // Efekty
        private List<PointF> trail = new List<PointF>();
        private List<Particle> deathParticles = new List<Particle>();
        private int tickCounter = 0;

        // Stavy
        private bool gameOver = false;
        private bool levelComplete = false;

        private class Particle
        {
            public float X, Y, VX, VY, Life;
            public Color Color;
        }

        public GameForm()
        {
            this.Text = "Geometry Dash - Hra s Hudbou!";
            this.Size = new Size(900, 500);
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(15, 15, 25);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.KeyDown += KeyIsDown;

            // Naètení hudby
            try
            {
                bgMusic = new SoundPlayer("hudba.wav");
            }
            catch (Exception)
            {
                MessageBox.Show("Pozor: Soubor 'hudba.wav' nebyl nalezen. Hra pobìží bez hudby.", "Chybí hudba");
            }

            LoadLevel();

            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = 1;
            gameTimer.Tick += GameTick;

            stopwatch.Start();
            gameTimer.Start();
        }

        private void LoadLevel()
        {
            platforms.Clear();
            spikes.Clear();
            trail.Clear();
            deathParticles.Clear();

            // Dlouhá mapa z pøedchozí verze
            platforms.Add(new Rectangle(0, 350, 800, 500));
            platforms.Add(new Rectangle(920, 350, 400, 500));
            platforms.Add(new Rectangle(1320, 290, 300, 500));
            platforms.Add(new Rectangle(1620, 350, 600, 500));
            platforms.Add(new Rectangle(2220, 290, 300, 500));
            platforms.Add(new Rectangle(2520, 230, 400, 500));

            spikes.Add(new Rectangle(400, 310, 40, 40));
            spikes.Add(new Rectangle(650, 310, 40, 40));
            spikes.Add(new Rectangle(1100, 310, 40, 40));
            spikes.Add(new Rectangle(1800, 310, 40, 40));
            spikes.Add(new Rectangle(1840, 310, 40, 40));
            spikes.Add(new Rectangle(2050, 310, 40, 40));

            platforms.Add(new Rectangle(3000, 350, 600, 500));
            spikes.Add(new Rectangle(3200, 310, 40, 40));
            spikes.Add(new Rectangle(3400, 310, 40, 40));

            platforms.Add(new Rectangle(3750, 290, 150, 30));
            platforms.Add(new Rectangle(4050, 230, 150, 30));
            platforms.Add(new Rectangle(4350, 290, 150, 30));

            platforms.Add(new Rectangle(4650, 350, 1000, 500));
            spikes.Add(new Rectangle(4800, 310, 40, 40));
            spikes.Add(new Rectangle(5000, 310, 40, 40));
            spikes.Add(new Rectangle(5040, 310, 40, 40));
            spikes.Add(new Rectangle(5300, 310, 40, 40));

            platforms.Add(new Rectangle(5800, 290, 150, 500));
            platforms.Add(new Rectangle(6100, 230, 150, 500));
            platforms.Add(new Rectangle(6400, 170, 800, 500));
            spikes.Add(new Rectangle(6800, 130, 40, 40));

            finishLine = new Rectangle(7000, -200, 100, 1000);

            playerX = 100;
            playerY = 250;
            velocityY = 0;
            playerRotation = 0;
            gameOver = false;
            levelComplete = false;
            tickCounter = 0;

            // Pøehrání hudby ve smyèce, jakmile naèteme level
            if (bgMusic != null)
            {
                try { bgMusic.PlayLooping(); } catch { }
            }
        }

        private void GameTick(object? sender, EventArgs e)
        {
            timeAccumulator += stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Restart();

            if (timeAccumulator > 100) timeAccumulator = 100;

            bool physicsUpdated = false;

            while (timeAccumulator >= FixedTimeStep)
            {
                UpdatePhysics();
                timeAccumulator -= FixedTimeStep;
                physicsUpdated = true;
            }

            if (physicsUpdated)
            {
                this.Invalidate();
            }
        }

        private void UpdatePhysics()
        {
            tickCounter++;

            if (gameOver)
            {
                for (int i = deathParticles.Count - 1; i >= 0; i--)
                {
                    deathParticles[i].X += deathParticles[i].VX;
                    deathParticles[i].Y += deathParticles[i].VY;
                    deathParticles[i].VY += 0.5f;
                    deathParticles[i].Life -= 0.03f;
                    if (deathParticles[i].Life <= 0) deathParticles.RemoveAt(i);
                }
                return;
            }

            if (levelComplete) return;

            float prevBottom = playerY + playerSize;
            playerX += speedX;
            velocityY += gravity;
            playerY += velocityY;
            isGrounded = false;

            RectangleF playerRect = new RectangleF(playerX, playerY, playerSize, playerSize);

            foreach (Rectangle p in platforms)
            {
                if (playerRect.IntersectsWith(p))
                {
                    if (velocityY > 0 && prevBottom <= p.Y + 12)
                    {
                        playerY = p.Y - playerSize;
                        velocityY = 0;
                        isGrounded = true;
                        if (playerRotation % 90 != 0) playerRotation = (float)(Math.Round(playerRotation / 90) * 90);
                    }
                    else
                    {
                        Die();
                        return;
                    }
                }
            }

            if (!isGrounded) playerRotation += 7f;

            if (tickCounter % 2 == 0)
            {
                trail.Add(new PointF(playerX, playerY));
                if (trail.Count > 7) trail.RemoveAt(0);
            }

            if (playerY > 1000) Die();

            RectangleF playerHitbox = new RectangleF(playerX + 5, playerY + 5, playerSize - 10, playerSize - 10);
            foreach (Rectangle s in spikes)
            {
                RectangleF spikeHitbox = new RectangleF(s.X + 10, s.Y + 15, s.Width - 20, s.Height - 15);
                if (playerHitbox.IntersectsWith(spikeHitbox))
                {
                    Die();
                    return;
                }
            }

            if (playerHitbox.IntersectsWith(finishLine)) Win();

            cameraX = playerX - 200;
        }

        private void KeyIsDown(object? sender, KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.Space || e.KeyCode == Keys.Up) && isGrounded && !gameOver && !levelComplete)
            {
                velocityY = jumpForce;
                isGrounded = false;
            }
            if (e.KeyCode == Keys.R && (gameOver || levelComplete))
            {
                LoadLevel();
                timeAccumulator = 0;
                stopwatch.Restart();
                gameTimer.Start();
            }
        }

        private void Die()
        {
            gameOver = true;
            // Když umøeš, hudba se zastaví
            if (bgMusic != null) { try { bgMusic.Stop(); } catch { } }

            for (int i = 0; i < 40; i++)
            {
                deathParticles.Add(new Particle
                {
                    X = playerX + playerSize / 2,
                    Y = playerY + playerSize / 2,
                    VX = (float)(rnd.NextDouble() * 16 - 8),
                    VY = (float)(rnd.NextDouble() * 16 - 10),
                    Life = 1.0f,
                    Color = (rnd.Next(2) == 0) ? Color.Yellow : Color.Orange
                });
            }
        }

        private void Win()
        {
            levelComplete = true;
            // Když vyhraješ, hudba se zastaví
            if (bgMusic != null) { try { bgMusic.Stop(); } catch { } }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;

            Pen gridPen = new Pen(Color.FromArgb(30, 30, 45), 1);
            float bgOffsetX = -(cameraX * 0.3f) % 100;
            for (float x = bgOffsetX; x < this.Width; x += 100) g.DrawLine(gridPen, x, 0, x, this.Height);
            for (float y = 0; y < this.Height; y += 100) g.DrawLine(gridPen, 0, y, this.Width, y);

            g.TranslateTransform(-cameraX, 0);

            foreach (Rectangle p in platforms)
            {
                if (p.Width > 0 && p.Height > 0)
                {
                    LinearGradientBrush brush = new LinearGradientBrush(p, Color.FromArgb(40, 40, 80), Color.FromArgb(10, 10, 25), LinearGradientMode.Vertical);
                    g.FillRectangle(brush, p);
                    g.DrawRectangle(new Pen(Color.DeepSkyBlue, 2), p.X, p.Y, p.Width, p.Height);
                    g.DrawLine(new Pen(Color.Cyan, 4), p.X, p.Y, p.X + p.Width, p.Y);
                }
            }

            Brush spikeBrush = new SolidBrush(Color.Red);
            foreach (Rectangle s in spikes)
            {
                Point[] triangle = new Point[] {
                    new Point(s.X, s.Y + s.Height),
                    new Point(s.X + (s.Width / 2), s.Y),
                    new Point(s.X + s.Width, s.Y + s.Height)
                };
                g.FillPolygon(spikeBrush, triangle);
                g.DrawPolygon(new Pen(Color.White, 1), triangle);
            }

            g.FillRectangle(new SolidBrush(Color.FromArgb(100, 0, 255, 0)), finishLine);
            g.DrawString("CÍL!", new Font("Arial", 24, FontStyle.Bold), Brushes.White, finishLine.X + 10, 200);

            if (!gameOver)
            {
                for (int i = 0; i < trail.Count; i++)
                {
                    int alpha = (int)(((float)(i + 1) / trail.Count) * 100);
                    SolidBrush trailBrush = new SolidBrush(Color.FromArgb(alpha, Color.Orange));
                    g.FillRectangle(trailBrush, trail[i].X, trail[i].Y, playerSize, playerSize);
                }
            }

            if (!gameOver)
            {
                g.TranslateTransform(playerX + playerSize / 2, playerY + playerSize / 2);
                g.RotateTransform(playerRotation);

                Rectangle playerRect = new Rectangle(-playerSize / 2, -playerSize / 2, playerSize, playerSize);
                LinearGradientBrush playerBrush = new LinearGradientBrush(playerRect, Color.Yellow, Color.DarkOrange, LinearGradientMode.ForwardDiagonal);
                g.FillRectangle(playerBrush, playerRect);
                g.DrawRectangle(new Pen(Color.White, 2), playerRect);

                g.FillRectangle(Brushes.Black, -10, -12, 6, 6);
                g.FillRectangle(Brushes.Black, 4, -12, 6, 6);

                g.ResetTransform();
                g.TranslateTransform(-cameraX, 0);
            }

            if (gameOver)
            {
                foreach (Particle p in deathParticles)
                {
                    int alpha = Math.Max(0, Math.Min(255, (int)(p.Life * 255)));
                    g.FillRectangle(new SolidBrush(Color.FromArgb(alpha, p.Color)), p.X, p.Y, 8, 8);
                }
            }

            g.ResetTransform();

            if (gameOver)
            {
                g.DrawString("ZEMØEL JSI!", new Font("Arial", 36, FontStyle.Bold), Brushes.Red, 250, 150);
                g.DrawString("Stiskni 'R' pro restart", new Font("Arial", 16), Brushes.White, 320, 210);
            }
            else if (levelComplete)
            {
                g.DrawString("LEVEL DOKONÈEN!", new Font("Arial", 36, FontStyle.Bold), Brushes.Lime, 200, 150);
                g.DrawString("Stiskni 'R' pro hrání znovu", new Font("Arial", 16), Brushes.White, 300, 210);
            }
            else
            {
                int progress = (int)((playerX / finishLine.X) * 100);
                if (progress > 100) progress = 100;
                if (progress < 0) progress = 0;
                g.DrawString($"Progress: {progress}%", new Font("Arial", 14, FontStyle.Bold), Brushes.White, 10, 10);
            }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GameForm());
        }
    }
}