using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GeometryDashLevel
{
    public class GameForm : Form
    {
        private System.Windows.Forms.Timer gameTimer;

        // Fyzika a pozice hráèe
        private float playerX = 100;
        private float playerY = 0;
        private float velocityY = 0;
        private float gravity = 1.5f;
        private float jumpForce = -17f;
        private float speedX = 8f; // Rychlost pohybu dopøedu
        private bool isGrounded = false;
        private float playerRotation = 0;
        private int playerSize = 40;

        // Level (Plošiny, ostny a cíl)
        private List<Rectangle> platforms = new List<Rectangle>();
        private List<Rectangle> spikes = new List<Rectangle>();
        private Rectangle finishLine;
        private float cameraX = 0; // Pozice kamery

        // Stavy hry
        private bool gameOver = false;
        private bool levelComplete = false;

        public GameForm()
        {
            this.Text = "Geometry Dash - Skuteèný Level";
            this.Size = new Size(900, 500);
            this.DoubleBuffered = true;
            this.BackColor = Color.FromArgb(20, 20, 35);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.KeyDown += KeyIsDown;

            LoadLevel();

            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = 16; // cca 60 FPS
            gameTimer.Tick += GameTick;
            gameTimer.Start();
        }

        // TADY SE STAVÍ MAPA
        private void LoadLevel()
        {
            platforms.Clear();
            spikes.Clear();

            // Plošina = (X pozice, Y výška, šíøka, hloubka/tlouška bloku)
            platforms.Add(new Rectangle(0, 350, 800, 500));        // Startovní rovinka

            // Ostrùvek nahoøe
            platforms.Add(new Rectangle(950, 280, 300, 500));      // Musíš pøeskoèit propast

            // Schody dolù a nahoru
            platforms.Add(new Rectangle(1400, 350, 400, 500));
            platforms.Add(new Rectangle(1800, 250, 300, 500));     // Vyšší schod
            platforms.Add(new Rectangle(2100, 150, 400, 500));     // Ještì vyšší schod

            // Ostny (X pozice, Y pozice, šíøka, výška)
            spikes.Add(new Rectangle(500, 310, 40, 40));           // Osten na startu
            spikes.Add(new Rectangle(1550, 310, 40, 40));          // Osten v dolíku
            spikes.Add(new Rectangle(1650, 310, 40, 40));

            // Cíl (Zelená zóna na konci levelu)
            finishLine = new Rectangle(2400, -100, 100, 800);

            // Reset hráèe na start
            playerX = 100;
            playerY = 250;
            velocityY = 0;
            playerRotation = 0;
            gameOver = false;
            levelComplete = false;
        }

        private void GameTick(object sender, EventArgs e)
        {
            if (gameOver || levelComplete) return;

            // Uložení pozice z pøedchozího snímku (dùležité pro kolize)
            float prevBottom = playerY + playerSize;

            // 1. Pohyb hráèe dopøedu
            playerX += speedX;

            // 2. Fyzika - Pád
            velocityY += gravity;
            playerY += velocityY;
            isGrounded = false;

            RectangleF playerRect = new RectangleF(playerX, playerY, playerSize, playerSize);

            // 3. Kolize s plošinami (Zemì a zdi)
            foreach (Rectangle p in platforms)
            {
                if (playerRect.IntersectsWith(p))
                {
                    // Pokud hráè padal seshora a v minulém snímku byl nad plošinou = dopadl na zem
                    if (velocityY > 0 && prevBottom <= p.Y + 10) // +10 je tolerance, aby nepropadl skrz pøi vysoké rychlosti
                    {
                        playerY = p.Y - playerSize;
                        velocityY = 0;
                        isGrounded = true;

                        // Zarovnání rotace pøi dopadu
                        if (playerRotation % 90 != 0) playerRotation = (float)(Math.Round(playerRotation / 90) * 90);
                    }
                    else // Narazil z boku do zdi plošiny
                    {
                        Die();
                        return;
                    }
                }
            }

            // 4. Rotace ve vzduchu
            if (!isGrounded) playerRotation += 7f;

            // 5. Omezení pádu (pokud hráè spadne do propasti)
            if (playerY > 1000) Die();

            // 6. Kolize s ostny
            RectangleF playerHitbox = new RectangleF(playerX + 5, playerY + 5, playerSize - 10, playerSize - 10);
            foreach (Rectangle s in spikes)
            {
                // Zmenšený hitbox ostnu (spravedlivìjší trojúhelník)
                RectangleF spikeHitbox = new RectangleF(s.X + 10, s.Y + 15, s.Width - 20, s.Height - 15);
                if (playerHitbox.IntersectsWith(spikeHitbox))
                {
                    Die();
                    return;
                }
            }

            // 7. Prùchod cílem
            if (playerHitbox.IntersectsWith(finishLine))
            {
                Win();
                return;
            }

            // 8. Posun kamery (Kamera sleduje hráèe, aby byl na levé stranì obrazovky)
            cameraX = playerX - 200;

            this.Invalidate(); // Pøekreslení obrazovky
        }

        private void KeyIsDown(object sender, KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.Space || e.KeyCode == Keys.Up) && isGrounded && !gameOver && !levelComplete)
            {
                velocityY = jumpForce;
                isGrounded = false;
            }
            // Klávesa 'R' pro restart v pøípadì, že jsi naboural nebo vyhrál
            if (e.KeyCode == Keys.R && (gameOver || levelComplete))
            {
                LoadLevel();
                gameTimer.Start();
            }
        }

        private void Die()
        {
            gameOver = true;
            this.Invalidate();
        }

        private void Win()
        {
            levelComplete = true;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // POSUN KAMERY - Tímhle trikem se celý svìt posouvá doleva, zatímco hráè "bìží" doprava
            g.TranslateTransform(-cameraX, 0);

            // 1. Kreslení plošin
            Brush blockBrush = new SolidBrush(Color.FromArgb(50, 50, 90));
            Pen blockOutline = new Pen(Color.Cyan, 3);
            foreach (Rectangle p in platforms)
            {
                g.FillRectangle(blockBrush, p);
                g.DrawRectangle(blockOutline, p.X, p.Y, p.Width, p.Height);
                // Svítící linka na vršku plošiny
                g.DrawLine(new Pen(Color.White, 2), p.X, p.Y, p.X + p.Width, p.Y);
            }

            // 2. Kreslení ostnù
            Brush spikeBrush = new SolidBrush(Color.Red);
            foreach (Rectangle s in spikes)
            {
                Point[] triangle = new Point[] {
                    new Point(s.X, s.Y + s.Height),
                    new Point(s.X + (s.Width / 2), s.Y),
                    new Point(s.X + s.Width, s.Y + s.Height)
                };
                g.FillPolygon(spikeBrush, triangle);
                g.DrawPolygon(new Pen(Color.DarkRed, 2), triangle);
            }

            // 3. Kreslení cíle
            g.FillRectangle(new SolidBrush(Color.FromArgb(100, 0, 255, 0)), finishLine);
            g.DrawString("CÍL!", new Font("Arial", 24, FontStyle.Bold), Brushes.White, finishLine.X + 10, 200);

            // 4. Kreslení hráèe (Pokud neumøel)
            if (!gameOver)
            {
                g.TranslateTransform(playerX + playerSize / 2, playerY + playerSize / 2);
                g.RotateTransform(playerRotation);

                Rectangle playerRect = new Rectangle(-playerSize / 2, -playerSize / 2, playerSize, playerSize);
                g.FillRectangle(new SolidBrush(Color.Yellow), playerRect);
                g.DrawRectangle(new Pen(Color.Orange, 3), playerRect);

                // Oblièej
                g.FillRectangle(Brushes.Black, -10, -12, 6, 6);
                g.FillRectangle(Brushes.Black, 4, -12, 6, 6);

                g.ResetTransform();
                g.TranslateTransform(-cameraX, 0); // Vracíme zpìt transformaci kamery
            }

            // Zrušení transformace pro kreslení UI prvkù (texty, které stojí na místì na obrazovce)
            g.ResetTransform();

            // 5. Herní texty a UI
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
                // Ukazatel pokroku v procentech
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