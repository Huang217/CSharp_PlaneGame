using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace PlaneCombat
{
    public partial class GameForm : Form
    {
        // 游戏常量
        private const int GAME_WIDTH = 400;
        private const int GAME_HEIGHT = 600;
        private const int PLAYER_SPEED = 8;
        private const int BULLET_SPEED = 12;
        private const int ENEMY_SPEED_MIN = 3;
        private const int ENEMY_SPEED_MAX = 7;
        private const int ENEMY_SPAWN_INTERVAL = 60;
        private const int PLAYER_BULLET_COOLDOWN = 10;

        // 游戏对象尺寸
        private const int PLAYER_WIDTH = 50;
        private const int PLAYER_HEIGHT = 40;
        private const int BULLET_WIDTH = 4;
        private const int BULLET_HEIGHT = 12;
        private const int ENEMY_WIDTH = 40;
        private const int ENEMY_HEIGHT = 40;

        // 游戏变量
        private int playerX;
        private int playerY;
        private int score;
        private int lives;
        private int gameTime;
        private bool isGameRunning;
        private bool isGameOver;
        private Random random;

        // 游戏对象列表
        private List<Bullet> playerBullets;
        private List<Bullet> enemyBullets;
        private List<Enemy> enemies;
        private List<Particle> particles;

        // 输入状态
        private bool leftPressed;
        private bool rightPressed;
        private bool upPressed;
        private bool downPressed;
        private bool spacePressed;

        // 计时器
        private int enemySpawnTimer;
        private int playerShootTimer;

        // 双缓冲相关
        private Bitmap backBuffer;
        private Graphics backBufferGraphics;

        // 画笔和画刷
        private Brush playerBrush;
        private Brush enemyBrush;
        private Brush playerBulletBrush;
        private Brush enemyBulletBrush;
        private Brush textBrush;
        private Brush backgroundBrush;
        private Pen borderPen;

        // 字体
        private Font gameFont;
        private Font titleFont;

        // Windows Forms 组件
        private System.ComponentModel.IContainer components = null;
        private Timer gameTimer;

        public GameForm()
        {
            // 启用双缓冲
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.DoubleBuffer, true);

            // 先初始化组件，再初始化游戏
            InitializeComponent();
            InitializeGame();
        }

        // 正确的 InitializeComponent 方法
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.gameTimer = new System.Windows.Forms.Timer(this.components);

            // 窗体属性设置
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(GAME_WIDTH, GAME_HEIGHT);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "GameForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "飞机大战";

            // 确保这些属性在Designer文件中设置
            this.SuspendLayout();
            this.ResumeLayout(false);
        }

        private void InitializeGame()
        {
            // 初始化随机数生成器
            random = new Random();

            // 初始化颜色和画笔
            playerBrush = new SolidBrush(Color.Cyan);
            enemyBrush = new SolidBrush(Color.Red);
            playerBulletBrush = new SolidBrush(Color.Yellow);
            enemyBulletBrush = new SolidBrush(Color.Magenta);
            textBrush = new SolidBrush(Color.White);
            backgroundBrush = new SolidBrush(Color.FromArgb(10, 10, 40));
            borderPen = new Pen(Color.DarkBlue, 3);

            // 初始化字体
            gameFont = new Font("Arial", 12, FontStyle.Bold);
            titleFont = new Font("Arial", 24, FontStyle.Bold);

            // 初始化双缓冲
            InitializeBackBuffer();

            // 初始化游戏状态
            ResetGame();

            // 设置定时器
            gameTimer.Interval = 16; // 约60FPS
            gameTimer.Tick += GameTimer_Tick;

            // 设置键盘事件
            this.KeyPreview = true;
            this.KeyDown += GameForm_KeyDown;
            this.KeyUp += GameForm_KeyUp;

            // 开始游戏
            StartGame();
        }

        private void InitializeBackBuffer()
        {
            if (backBuffer != null)
            {
                backBuffer.Dispose();
                backBufferGraphics?.Dispose();
            }

            backBuffer = new Bitmap(this.ClientSize.Width, this.ClientSize.Height);
            backBufferGraphics = Graphics.FromImage(backBuffer);
        }

        private void ResetGame()
        {
            // 初始化玩家位置
            playerX = GAME_WIDTH / 2 - PLAYER_WIDTH / 2;
            playerY = GAME_HEIGHT - 100;

            // 初始化游戏状态
            score = 0;
            lives = 3;
            gameTime = 0;
            isGameOver = false;

            // 初始化对象列表
            playerBullets = new List<Bullet>();
            enemyBullets = new List<Bullet>();
            enemies = new List<Enemy>();
            particles = new List<Particle>();

            // 初始化计时器
            enemySpawnTimer = 0;
            playerShootTimer = 0;
        }

        private void StartGame()
        {
            isGameRunning = true;
            gameTimer.Start();
            this.Focus();
        }

        private void GameOver()
        {
            isGameRunning = false;
            isGameOver = true;
            gameTimer.Stop();
        }

        private void SpawnEnemy()
        {
            int x = random.Next(0, GAME_WIDTH - ENEMY_WIDTH);
            int y = -ENEMY_HEIGHT;
            int speed = random.Next(ENEMY_SPEED_MIN, ENEMY_SPEED_MAX + 1);

            enemies.Add(new Enemy(x, y, speed));
        }

        private void PlayerShoot()
        {
            if (playerShootTimer <= 0)
            {
                int bulletX = playerX + PLAYER_WIDTH / 2 - BULLET_WIDTH / 2;
                int bulletY = playerY - BULLET_HEIGHT;

                playerBullets.Add(new Bullet(bulletX, bulletY, -BULLET_SPEED, true));

                playerShootTimer = PLAYER_BULLET_COOLDOWN;
            }
        }

        private void UpdateGame()
        {
            if (!isGameRunning) return;

            gameTime++;

            // 更新玩家位置
            if (leftPressed && playerX > 0) playerX -= PLAYER_SPEED;
            if (rightPressed && playerX < GAME_WIDTH - PLAYER_WIDTH) playerX += PLAYER_SPEED;
            if (upPressed && playerY > GAME_HEIGHT / 2) playerY -= PLAYER_SPEED;
            if (downPressed && playerY < GAME_HEIGHT - PLAYER_HEIGHT) playerY += PLAYER_SPEED;

            // 玩家射击
            if (spacePressed)
            {
                PlayerShoot();
            }

            // 更新计时器
            if (playerShootTimer > 0) playerShootTimer--;

            // 生成敌人
            enemySpawnTimer++;
            if (enemySpawnTimer >= ENEMY_SPAWN_INTERVAL)
            {
                SpawnEnemy();
                enemySpawnTimer = 0;
            }

            // 更新玩家子弹
            for (int i = playerBullets.Count - 1; i >= 0; i--)
            {
                Bullet bullet = playerBullets[i];
                bullet.Y += bullet.Speed;

                // 移除超出屏幕的子弹
                if (bullet.Y < -BULLET_HEIGHT)
                {
                    playerBullets.RemoveAt(i);
                }
            }

            // 更新敌人
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                enemy.Y += enemy.Speed;

                // 移除超出屏幕的敌人
                if (enemy.Y > GAME_HEIGHT)
                {
                    enemies.RemoveAt(i);
                }
            }

            // 检测碰撞：玩家子弹 vs 敌人
            for (int i = playerBullets.Count - 1; i >= 0; i--)
            {
                Bullet bullet = playerBullets[i];
                Rectangle bulletRect = new Rectangle(bullet.X, bullet.Y, BULLET_WIDTH, BULLET_HEIGHT);

                for (int j = enemies.Count - 1; j >= 0; j--)
                {
                    Enemy enemy = enemies[j];
                    Rectangle enemyRect = new Rectangle(enemy.X, enemy.Y, ENEMY_WIDTH, ENEMY_HEIGHT);

                    if (bulletRect.IntersectsWith(enemyRect))
                    {
                        // 碰撞发生，移除子弹和敌人
                        playerBullets.RemoveAt(i);
                        enemies.RemoveAt(j);

                        // 增加分数
                        score += 10;

                        // 创建粒子效果
                        CreateExplosion(enemy.X + ENEMY_WIDTH / 2, enemy.Y + ENEMY_HEIGHT / 2);

                        break;
                    }
                }
            }

            // 检测碰撞：玩家 vs 敌人
            Rectangle playerRect = new Rectangle(playerX, playerY, PLAYER_WIDTH, PLAYER_HEIGHT);
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                Rectangle enemyRect = new Rectangle(enemy.X, enemy.Y, ENEMY_WIDTH, ENEMY_HEIGHT);

                if (playerRect.IntersectsWith(enemyRect))
                {
                    // 碰撞发生，移除敌人
                    enemies.RemoveAt(i);

                    // 减少生命值
                    lives--;

                    // 创建粒子效果
                    CreateExplosion(playerX + PLAYER_WIDTH / 2, playerY + PLAYER_HEIGHT / 2);

                    // 检查游戏是否结束
                    if (lives <= 0)
                    {
                        GameOver();
                    }

                    break;
                }
            }

            // 更新粒子
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle particle = particles[i];
                particle.X += particle.VelocityX;
                particle.Y += particle.VelocityY;
                particle.Life--;

                if (particle.Life <= 0)
                {
                    particles.RemoveAt(i);
                }
            }
        }

        private void CreateExplosion(int x, int y)
        {
            // 创建爆炸粒子效果
            for (int i = 0; i < 15; i++)
            {
                float velocityX = (float)(random.NextDouble() * 4 - 2);
                float velocityY = (float)(random.NextDouble() * 4 - 2);
                int life = random.Next(20, 40);

                particles.Add(new Particle(x, y, velocityX, velocityY, life));
            }
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            UpdateGame();
            this.Invalidate();
        }

        private void GameForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.A:
                    leftPressed = true;
                    break;
                case Keys.Right:
                case Keys.D:
                    rightPressed = true;
                    break;
                case Keys.Up:
                case Keys.W:
                    upPressed = true;
                    break;
                case Keys.Down:
                case Keys.S:
                    downPressed = true;
                    break;
                case Keys.Space:
                    spacePressed = true;
                    break;
                case Keys.Enter:
                    if (isGameOver)
                    {
                        ResetGame();
                        StartGame();
                    }
                    break;
                case Keys.Escape:
                    Application.Exit();
                    break;
            }
        }

        private void GameForm_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.A:
                    leftPressed = false;
                    break;
                case Keys.Right:
                case Keys.D:
                    rightPressed = false;
                    break;
                case Keys.Up:
                case Keys.W:
                    upPressed = false;
                    break;
                case Keys.Down:
                case Keys.S:
                    downPressed = false;
                    break;
                case Keys.Space:
                    spacePressed = false;
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // 使用双缓冲绘制
            if (backBuffer != null)
            {
                RenderToBackBuffer();
                e.Graphics.DrawImage(backBuffer, 0, 0);
            }
            else
            {
                base.OnPaint(e);
                RenderGame(e.Graphics);
            }
        }

        private void RenderToBackBuffer()
        {
            if (backBufferGraphics == null) return;

            // 绘制背景
            backBufferGraphics.FillRectangle(backgroundBrush, 0, 0, backBuffer.Width, backBuffer.Height);

            // 绘制游戏内容
            RenderGame(backBufferGraphics);
        }

        private void RenderGame(Graphics g)
        {
            // 绘制玩家
            DrawPlayer(g);

            // 绘制敌人
            DrawEnemies(g);

            // 绘制子弹
            DrawBullets(g);

            // 绘制粒子
            DrawParticles(g);

            // 绘制UI
            DrawUI(g);

            // 绘制游戏结束画面
            if (isGameOver)
            {
                DrawGameOverScreen(g);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // 空实现，避免背景闪烁
        }

        private void DrawPlayer(Graphics g)
        {
            // 绘制玩家飞机（三角形）
            Point[] playerPoints = {
                new Point(playerX + PLAYER_WIDTH / 2, playerY),
                new Point(playerX, playerY + PLAYER_HEIGHT),
                new Point(playerX + PLAYER_WIDTH, playerY + PLAYER_HEIGHT)
            };

            g.FillPolygon(playerBrush, playerPoints);

            // 绘制飞机细节
            Pen playerPen = new Pen(Color.White, 2);
            g.DrawPolygon(playerPen, playerPoints);
            playerPen.Dispose();
        }

        private void DrawEnemies(Graphics g)
        {
            foreach (Enemy enemy in enemies)
            {
                // 绘制敌人（菱形）
                Point[] enemyPoints = {
                    new Point(enemy.X + ENEMY_WIDTH / 2, enemy.Y),
                    new Point(enemy.X + ENEMY_WIDTH, enemy.Y + ENEMY_HEIGHT / 2),
                    new Point(enemy.X + ENEMY_WIDTH / 2, enemy.Y + ENEMY_HEIGHT),
                    new Point(enemy.X, enemy.Y + ENEMY_HEIGHT / 2)
                };

                g.FillPolygon(enemyBrush, enemyPoints);

                // 绘制敌人细节
                Pen enemyPen = new Pen(Color.DarkRed, 2);
                g.DrawPolygon(enemyPen, enemyPoints);
                enemyPen.Dispose();
            }
        }

        private void DrawBullets(Graphics g)
        {
            // 绘制玩家子弹
            foreach (Bullet bullet in playerBullets)
            {
                g.FillRectangle(playerBulletBrush, bullet.X, bullet.Y, BULLET_WIDTH, BULLET_HEIGHT);
            }
        }

        private void DrawParticles(Graphics g)
        {
            foreach (Particle particle in particles)
            {
                // 根据粒子生命值计算大小和透明度
                int size = 2 + particle.Life / 10;
                float alpha = particle.Life / 40.0f;

                Color particleColor = Color.FromArgb((int)(alpha * 255), Color.Orange);
                Brush particleBrush = new SolidBrush(particleColor);

                g.FillEllipse(particleBrush, particle.X, particle.Y, size, size);

                particleBrush.Dispose();
            }
        }

        private void DrawUI(Graphics g)
        {
            // 绘制分数
            string scoreText = $"分数: {score}";
            g.DrawString(scoreText, gameFont, textBrush, 10, 10);

            // 绘制生命值
            string livesText = $"生命: {lives}";
            g.DrawString(livesText, gameFont, textBrush, GAME_WIDTH - 80, 10);

            // 绘制游戏时间
            string timeText = $"时间: {gameTime / 60}";
            g.DrawString(timeText, gameFont, textBrush, GAME_WIDTH / 2 - 40, 10);

            // 绘制边框
            g.DrawRectangle(borderPen, 0, 0, GAME_WIDTH - 1, GAME_HEIGHT - 1);
        }

        private void DrawGameOverScreen(Graphics g)
        {
            // 绘制半透明背景
            Brush overlayBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 0));
            g.FillRectangle(overlayBrush, 0, 0, GAME_WIDTH, GAME_HEIGHT);
            overlayBrush.Dispose();

            // 绘制游戏结束文字
            string gameOverText = "游戏结束";
            string scoreText = $"最终分数: {score}";
            string restartText = "按 Enter 键重新开始";

            SizeF gameOverSize = g.MeasureString(gameOverText, titleFont);
            SizeF scoreSize = g.MeasureString(scoreText, gameFont);
            SizeF restartSize = g.MeasureString(restartText, gameFont);

            float gameOverX = (GAME_WIDTH - gameOverSize.Width) / 2;
            float gameOverY = GAME_HEIGHT / 2 - 60;

            float scoreX = (GAME_WIDTH - scoreSize.Width) / 2;
            float scoreY = gameOverY + 50;

            float restartX = (GAME_WIDTH - restartSize.Width) / 2;
            float restartY = scoreY + 40;

            g.DrawString(gameOverText, titleFont, textBrush, gameOverX, gameOverY);
            g.DrawString(scoreText, gameFont, textBrush, scoreX, scoreY);
            g.DrawString(restartText, gameFont, textBrush, restartX, restartY);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }

                // 释放所有资源
                playerBrush?.Dispose();
                enemyBrush?.Dispose();
                playerBulletBrush?.Dispose();
                enemyBulletBrush?.Dispose();
                textBrush?.Dispose();
                backgroundBrush?.Dispose();
                borderPen?.Dispose();
                gameFont?.Dispose();
                titleFont?.Dispose();
                backBufferGraphics?.Dispose();
                backBuffer?.Dispose();
                gameTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        // 游戏对象类
        private class Bullet
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Speed { get; set; }
            public bool IsPlayerBullet { get; set; }

            public Bullet(int x, int y, int speed, bool isPlayerBullet)
            {
                X = x;
                Y = y;
                Speed = speed;
                IsPlayerBullet = isPlayerBullet;
            }
        }

        private class Enemy
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Speed { get; set; }

            public Enemy(int x, int y, int speed)
            {
                X = x;
                Y = y;
                Speed = speed;
            }
        }

        private class Particle
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float VelocityX { get; set; }
            public float VelocityY { get; set; }
            public int Life { get; set; }

            public Particle(float x, float y, float velocityX, float velocityY, int life)
            {
                X = x;
                Y = y;
                VelocityX = velocityX;
                VelocityY = velocityY;
                Life = life;
            }
        }
    }
}
