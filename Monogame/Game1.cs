using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Monogame
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // Bird (player) variables
        Texture2D ballTexture;
        Vector2 ballPosition;
        float ballVelocity;
        const float Gravity = 500f;      // pixels per second squared
        const float FlapVelocity = -200f;  // negative value moves upward

        // Pipe variables
        Texture2D pipeTexture;
        List<PipePair> pipePairs;
        float pipeSpawnTimer;
        const float PipeSpawnInterval = 5f; // seconds between new pipes
        const float PipeSpeed = 100f;         // pixels per second

        // Game over state
        bool gameOver = false;

        // To detect a key press (edge detection)
        KeyboardState previousKeyboardState;

        // Random generator for pipe gap positions
        Random random = new Random();

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // Initialize the bird at a starting position
            ballPosition = new Vector2(100, graphics.PreferredBackBufferHeight / 2);
            ballVelocity = 0f;

            // Initialize the list for pipe pairs and the spawn timer
            pipePairs = new List<PipePair>();
            pipeSpawnTimer = 0f;

            previousKeyboardState = Keyboard.GetState();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load your textures from the Content project.
            ballTexture = Content.Load<Texture2D>("ball"); 
            pipeTexture = Content.Load<Texture2D>("pipe");     
        }

        protected override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keyboardState = Keyboard.GetState();

            // Exit if Escape is pressed
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                keyboardState.IsKeyDown(Keys.Escape))
                Exit();

            if (!gameOver) 
            {
                // --- Bird Physics ---
                // Flap when space is pressed (using edge detection)
                if (keyboardState.IsKeyDown(Keys.Space) && !previousKeyboardState.IsKeyDown(Keys.Space))
                {
                    ballVelocity = FlapVelocity;
                }

                // Apply gravity to the bird and update its position
                ballVelocity += Gravity * deltaTime;
                ballPosition.Y += ballVelocity * deltaTime;

                // Prevent the bird from moving above the screen
                if (ballPosition.Y < 0)
                {
                    ballPosition.Y = 0;
                    ballVelocity = 0;
                }
                // If the bird falls below the bottom, trigger game over
                if (ballPosition.Y > graphics.PreferredBackBufferHeight)
                {
                    ballPosition.Y = graphics.PreferredBackBufferHeight;
                    gameOver = true;
                }

                // --- Pipe Spawning and Movement ---
                pipeSpawnTimer += deltaTime;
                if (pipeSpawnTimer >= PipeSpawnInterval)
                {
                    pipeSpawnTimer = 0f;
                    float gapSize = 150f;
                    // Ensure the gap is within a valid range
                    float minGapY = gapSize / 2 + 20;
                    float maxGapY = graphics.PreferredBackBufferHeight - gapSize / 2 - 20;
                    float gapY = (float)(random.NextDouble() * (maxGapY - minGapY) + minGapY);

                    // Create a new pipe pair starting at the right edge of the screen
                    PipePair newPipe = new PipePair(
                        graphics.PreferredBackBufferWidth,
                        gapY,
                        gapSize,
                        pipeTexture,
                        graphics.PreferredBackBufferHeight);
                    pipePairs.Add(newPipe);
                }

                // Update pipes and check for collisions
                for (int i = pipePairs.Count - 1; i >= 0; i--)
                {
                    pipePairs[i].Update(deltaTime, PipeSpeed);

                    // Remove pipes that have moved completely off-screen
                    if (pipePairs[i].Position.X < -pipePairs[i].Width)
                    {
                        pipePairs.RemoveAt(i);
                    }
                    else
                    {
                        // Check collision between the bird and the pipes
                        if (pipePairs[i].Collides(ballPosition, ballTexture.Width, ballTexture.Height))
                        {
                            gameOver = true;
                        }
                    }
                }
            }
            else
            {
                // --- Game Over: Restart the game when space is pressed ---
                if (keyboardState.IsKeyDown(Keys.Space) && !previousKeyboardState.IsKeyDown(Keys.Space))
                {
                    ResetGame();
                }
            }

            previousKeyboardState = keyboardState;
            base.Update(gameTime);
        }

        private void ResetGame()
        {
            ballPosition = new Vector2(100, graphics.PreferredBackBufferHeight / 2);
            ballVelocity = 0f;
            pipePairs.Clear();
            pipeSpawnTimer = 0f;
            gameOver = false;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            // Draw the bird. The origin is set to the center of the texture.
            spriteBatch.Draw(
                ballTexture,
                ballPosition,
                null,
                Color.White,
                0f,
                new Vector2(ballTexture.Width / 2, ballTexture.Height / 2),
                1f,
                SpriteEffects.None,
                0f);

            // Draw all the pipe pairs.
            foreach (var pipe in pipePairs)
            {
                pipe.Draw(spriteBatch);
            }


            spriteBatch.End();

            base.Draw(gameTime);
        }
    }


    public class PipePair
    {
        // The horizontal position of the pipes.
        public Vector2 Position;
        // The vertical center of the gap.
        public float GapY;
        // The size of the gap between the pipes.
        public float GapSize;
        // The texture used to draw both the top and bottom pipes.
        public Texture2D Texture;
        // The height of the game window (to determine the size of the bottom pipe).
        public float ScreenHeight;

        private const float PipeScale = 0.5f;

        // The width of the pipe (based on the texture’s width).
        public float Width => Texture.Width * PipeScale;

        public PipePair(float startX, float gapY, float gapSize, Texture2D texture, float screenHeight)
        {
            Position = new Vector2(startX, 0);
            GapY = gapY;
            GapSize = gapSize;
            Texture = texture;
            ScreenHeight = screenHeight;
        }

        public void Update(float deltaTime, float speed)
        {
            Position.X -= speed * deltaTime;
        }


        public void Draw(SpriteBatch spriteBatch)
        {
            int pipeWidth = (int)(Texture.Width * PipeScale);

            // Calculate the rectangle for the top pipe (from the top of the screen to just before the gap).
            Rectangle topRect = new Rectangle(
                (int)Position.X,
                0,
                pipeWidth,
                (int)(GapY - GapSize / 2));

            // Calculate the rectangle for the bottom pipe (from just after the gap to the bottom of the screen).
            Rectangle bottomRect = new Rectangle(
                (int)Position.X,
                (int)(GapY + GapSize / 2),
                pipeWidth,
                (int)(ScreenHeight - (GapY + GapSize / 2)));

            // Draw the top pipe normally.
            spriteBatch.Draw(Texture, topRect, Color.White);
            // Draw the bottom pipe flipped vertically.
            spriteBatch.Draw(
                Texture,
                bottomRect,
                null,
                Color.White,
                0f,
                Vector2.Zero,
                SpriteEffects.FlipVertically,
                0f);
        }
   
        public bool Collides(Vector2 ballPos, int ballWidth, int ballHeight)
        {
            // Create a bounding rectangle for the bird.
            Rectangle ballRect = new Rectangle(
                (int)(ballPos.X - ballWidth / 2),
                (int)(ballPos.Y - ballHeight / 2),
                ballWidth,
                ballHeight);

            int pipeWidth = (int)(Texture.Width * PipeScale);

            // Define rectangles for the top and bottom pipes.
            Rectangle topRect = new Rectangle(
                (int)Position.X,
                0,
                pipeWidth,
                (int)(GapY - GapSize / 2));
            Rectangle bottomRect = new Rectangle(
                (int)Position.X,
                (int)(GapY + GapSize / 2),
                pipeWidth,
                (int)(ScreenHeight - (GapY + GapSize / 2)));

            // Return true if the bird intersects either pipe.
            return ballRect.Intersects(topRect) || ballRect.Intersects(bottomRect);
        }
    }
}
