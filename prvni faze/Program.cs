using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GeometryDashUpgrade
{
    public class GameForm : Form
    {
        private System.Windows.Forms.Timer gameTimer;

        // Fyzika a hráè
        private float playerY;
        private float playerVelocity = 0;
        private float gravity = 1.5f;
        private float jumpForce = -18f;
        private bool isJumping = false;
        private float playerRotation = 0;
        private int playerSize = 40;
        private int floorY = 320;

        // Logika hry
        private int score = 0;
        private float gameSpeed = 10f;
        private int spawnDistance = 0;
        private Random rnd = new Random();

        // Seznamy pøekážek
        private List<Rectangle> spikes = new List<Rectangle>();
        private List<Rectangle> blocks = new List<Rectangle>();

        public GameForm()
        {
            // Nastavení okna
            this.Text = "Geometry Dash - Lepší Verze";
            this.Size = new Size(800, 450);
            this.DoubleBuffered = true; // Zásadní pro plynulé vykreslování bez blikání!
            this.BackColor = Color.FromArgb(30, 30, 45); // Tmavé pozadí
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.KeyDown += KeyIsDown;

            playerY = floorY - playerSize;

            // Èasovaè (Herní smyèka bìží na cca 60 FPS)
            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = 16;
            gameTimer.Tick += GameTick;
            gameTimer.Start();
        }

        private void GameTick(object sender, EventArgs e)
        {
            // 1. Fyzika hráèe
            playerVelocity += gravity;
            playerY += playerVelocity;

            // Kontrola dopadu na zem
            if (playerY >= floorY - playerSize)
            {
                playerY = floorY - playerSize;
                playerVelocity = 0;
                isJumping = false;

                // Zarovnání rotace kostky pøi dopadu (na nejbližší 90 stupòù)
                if (playerRotation % 90 != 0)
                {
                    playerRotation = (float)(Math.Round(playerRotation / 90) * 90);
                }
            }
            else
            {
                // Rotace kostky ve vzduchu
                playerRotation += 6f;
            }

            // 2. Generování pøekážek
            spawnDistance -= (int)gameSpeed;
            if (spawnDistance <= 0)
            {
                GenerateObstacle();
                spawnDistance = rnd.Next(250, 500); // Kdy se objeví další
            }

            // 3. Pohyb a kolize - OSTNY
            for (int i = spikes.Count - 1; i >= 0; i--)
            {
                Rectangle s = spikes[i];
                s.X -= (int)gameSpeed;
                spikes[i] = s; // Uložení nové pozice

                if (s.X + s.Width < 0) // Pøekážka je mimo obrazovku
                {
                    spikes.RemoveAt(i);
                    score++;
                    gameSpeed += 0.05f; // Mírné zrychlování hry
                }
                else if (CheckCollision(s, true))
                {
                    GameOver();
                    return;
                }
            }

            // 4. Pohyb a kolize - BLOKY
            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                Rectangle b = blocks[i];
                b.X -= (int)gameSpeed;
                blocks[i] = b;

                if (b.X + b.Width < 0)
                {
                    blocks.RemoveAt(i);
                    score++;
                    gameSpeed += 0.05f;
                }
                else if (CheckCollision(b, false))
                {
                    GameOver();
                    return;
                }
            }

            // 5. Pøikáže oknu, aby se znovu vykreslilo (zavolá OnPaint)
            this.Invalidate();
        }

        private void GenerateObstacle()
        {
            int type = rnd.Next(0, 3);
            int startX = 850; // Objeví se za pravým okrajem obrazovky

            if (type == 0) // Jeden osten
            {
                spikes.Add(new Rectangle(startX, floorY - 40, 30, 40));
            }
            else if (type == 1) // Dva ostny za sebou
            {
                spikes.Add(new Rectangle(startX, floorY - 40, 30, 40));
                spikes.Add(new Rectangle(startX + 30, floorY - 40, 30, 40));
            }
            else // Nízký blok
            {
                blocks.Add(new Rectangle(startX, floorY - 30, 40, 30));
            }
        }

        private bool CheckCollision(Rectangle obstacle, bool isSpike)
        {
            // Vytvoøíme hitbox hráèe (trochu menší než grafika, a je to spravedlivé)
            Rectangle playerBox = new Rectangle(100 + 5, (int)playerY + 5, playerSize - 10, playerSize - 10);

            if (isSpike)
            {
                // Hitbox ostnu oøízneme hlavnì z vrchu, protože je to trojúhelník
                Rectangle spikeBox = new Rectangle(obstacle.X + 8, obstacle.Y + 15, obstacle.Width - 16, obstacle.Height - 15);
                return playerBox.IntersectsWith(spikeBox);
            }
            else
            {
                return playerBox.IntersectsWith(obstacle);
            }
        }

        private void KeyIsDown(object sender, KeyEventArgs e)
        {
            // Skok
            if ((e.KeyCode == Keys.Space || e.KeyCode == Keys.Up) && !isJumping)
            {
                playerVelocity = jumpForce;
                isJumping = true;
            }
        }

        private void GameOver()
        {
            gameTimer.Stop();
            MessageBox.Show($"Au! Dosáhl jsi skóre: {score}\n\nStiskni OK a zkus to znovu.", "Konec hry");

            // Resetování všech promìnných pro novou hru
            spikes.Clear();
            blocks.Clear();
            score = 0;
            gameSpeed = 10f;
            playerY = floorY - playerSize;
            playerVelocity = 0;
            isJumping = false;
            spawnDistance = 0;
            playerRotation = 0;

            gameTimer.Start();
        }

        // TATO METODA KRESLÍ CELOU HRU
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Vyhlazování hran

            // 1. Kreslení zemì a neonové linky
            Rectangle ground = new Rectangle(0, floorY, this.Width, this.Height - floorY);
            g.FillRectangle(new SolidBrush(Color.FromArgb(20, 20, 60)), ground);
            g.DrawLine(new Pen(Color.Cyan, 3), 0, floorY, this.Width, floorY);

            // 2. Kreslení ostnù (jako skuteèné trojúhelníky)
            Brush spikeBrush = new SolidBrush(Color.Red);
            foreach (Rectangle s in spikes)
            {
                Point[] triangle = new Point[] {
                    new Point(s.X, s.Y + s.Height), // levý dolní roh
                    new Point(s.X + (s.Width / 2), s.Y), // špièka
                    new Point(s.X + s.Width, s.Y + s.Height) // pravý dolní roh
                };
                g.FillPolygon(spikeBrush, triangle);
                g.DrawPolygon(new Pen(Color.DarkRed, 2), triangle);
            }

            // 3. Kreslení blokù
            Brush blockBrush = new SolidBrush(Color.DarkOrchid);
            foreach (Rectangle b in blocks)
            {
                g.FillRectangle(blockBrush, b);
                g.DrawRectangle(new Pen(Color.Magenta, 2), b);
            }

            // 4. Kreslení hráèe S ROTACÍ!
            g.TranslateTransform(100 + playerSize / 2, playerY + playerSize / 2); // Pøesun do støedu hráèe
            g.RotateTransform(playerRotation); // Otoèení

            Rectangle playerRect = new Rectangle(-playerSize / 2, -playerSize / 2, playerSize, playerSize);
            g.FillRectangle(new SolidBrush(Color.Yellow), playerRect);
            g.DrawRectangle(new Pen(Color.Orange, 3), playerRect);

            // Oèíèka na kostce
            g.FillRectangle(Brushes.Black, -10, -12, 6, 6);
            g.FillRectangle(Brushes.Black, 4, -12, 6, 6);

            g.ResetTransform(); // Vrácení rotace, aby se netoèil zbytek obrazovky

            // 5. Kreslení skóre
            g.DrawString($"Skóre: {score}", new Font("Arial", 16, FontStyle.Bold), Brushes.White, 10, 10);
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