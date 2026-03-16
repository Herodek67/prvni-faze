using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleGeometryDash
{
    public class GameForm : Form
    {
        private PictureBox player;
        private PictureBox obstacle;
        private System.Windows.Forms.Timer gameTimer;
        private Label scoreText;

        // Promìnné pro fyziku a logiku
        private bool isJumping = false;
        private int jumpSpeed = 0;
        private int gravity = 2;
        private int obstacleSpeed = 10;
        private int score = 0;
        private readonly int floorHeight = 300;

        public GameForm()
        {
            // Nastavení hlavního okna
            this.Text = "Herodetry Dash - první fáze";
            this.Size = new Size(800, 450);
            this.BackColor = Color.LightSkyBlue; // Barva pozadí
            this.DoubleBuffered = true; // Zabraòuje problikávání
            this.KeyDown += new KeyEventHandler(KeyIsDown);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Zobrazení skóre
            scoreText = new Label();
            scoreText.Text = "Skóre: 0";
            scoreText.Font = new Font("Arial", 16, FontStyle.Bold);
            scoreText.Location = new Point(10, 10);
            scoreText.AutoSize = true;
            this.Controls.Add(scoreText);

            // Nastavení hráèe (kostky)
            player = new PictureBox();
            player.BackColor = Color.Orange;
            player.Size = new Size(40, 40);
            player.Location = new Point(100, floorHeight);
            this.Controls.Add(player);

            // Nastavení pøekáky
            obstacle = new PictureBox();
            obstacle.BackColor = Color.Red;
            obstacle.Size = new Size(30, 40);
            obstacle.Location = new Point(800, floorHeight);
            this.Controls.Add(obstacle);

            // Zemì (pouze vizuální prvek)
            Label ground = new Label();
            ground.BackColor = Color.DarkGreen;
            ground.Size = new Size(800, 150);
            ground.Location = new Point(0, floorHeight + 40);
            this.Controls.Add(ground);

            // Herní smyèka (Èasovaè)
            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = 20; // Hra bìí na cca 50 FPS (1000ms / 20ms)
            gameTimer.Tick += MainGameTimerEvent;
            gameTimer.Start();
        }

        private void MainGameTimerEvent(object sender, EventArgs e)
        {
            // 1. Fyzika skoku (Gravitace táhne hráèe dolù)
            player.Top += jumpSpeed;
            jumpSpeed += gravity;

            // Zastavení pádu, kdy hráè narazí na zem
            if (player.Top >= floorHeight)
            {
                player.Top = floorHeight;
                jumpSpeed = 0;
                isJumping = false;
            }

            // 2. Pohyb pøekáky smìrem doleva
            obstacle.Left -= obstacleSpeed;

            // Pokud pøekáka zmizí z obrazovky, vra ji doprava a pøièti bod
            if (obstacle.Left < -50)
            {
                obstacle.Left = this.ClientSize.Width + 50;
                score++;
                scoreText.Text = "Skóre: " + score;

                // Hra se postupnì zrychluje
                if (obstacleSpeed < 25) obstacleSpeed++;
            }

            // 3. Detekce kolizí (Náraz do pøekáky)
            if (player.Bounds.IntersectsWith(obstacle.Bounds))
            {
                gameTimer.Stop();
                MessageBox.Show($"Konec hry! Dosáhl jsi skóre: {score}\n\nStiskni OK pro novou hru.", "Game Over");
                ResetGame();
            }
        }

        // Reakce na stisk klávesy
        private void KeyIsDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Up)
            {
                // Hráè mùe skoèit jen kdy stojí na zemi
                if (!isJumping && player.Top == floorHeight)
                {
                    isJumping = true;
                    jumpSpeed = -22; // Síla skoku
                }
            }
        }

        // Funkce pro restart hry
        private void ResetGame()
        {
            player.Top = floorHeight;
            obstacle.Left = 800;
            score = 0;
            obstacleSpeed = 10;
            jumpSpeed = 0;
            isJumping = false;
            scoreText.Text = "Skóre: " + score;
            gameTimer.Start();
        }

        // Hlavní vstupní bod aplikace
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GameForm());
        }
    }
}