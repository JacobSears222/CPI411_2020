﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Lab09
{
    public class Lab09 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Effect effect;
        Matrix world = Matrix.Identity;
        Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 20), Vector3.Zero, Vector3.UnitY);
        Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), 1f, 1f, 100f);
        Vector3 cameraPosition, cameraTarget, lightPosition;
        Matrix lightView, lightProjection;
        float angle, angle2, angleL, angleL2 = 0;
        float distance = 30;
        MouseState preMouse;
        Model[] models;
        Texture2D shadowMap;

        RenderTarget2D renderTarget;

        public Lab09()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
        }
        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            models = new Model[2];
            models[0] = Content.Load<Model>("Plane");
            models[1] = Content.Load<Model>("torus");
            effect = Content.Load<Effect>("ShadowShader");
            PresentationParameters pp = GraphicsDevice.PresentationParameters;
            renderTarget = new RenderTarget2D(GraphicsDevice, 2048, 2048, false, SurfaceFormat.Single, DepthFormat.Depth24, 0, RenderTargetUsage.PlatformContents);
        }

        protected override void UnloadContent() { }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Left)) angleL += 0.02f;
            if (Keyboard.GetState().IsKeyDown(Keys.Right)) angleL -= 0.02f;
            if (Keyboard.GetState().IsKeyDown(Keys.Up)) angleL2 += 0.02f;
            if (Keyboard.GetState().IsKeyDown(Keys.Down)) angleL2 -= 0.02f;
            if (Keyboard.GetState().IsKeyDown(Keys.S)) { angle = angle2 = angleL = angleL2 = 0; distance = 30; cameraTarget = Vector3.Zero; }
            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                angle -= (Mouse.GetState().X - preMouse.X) / 100f;
                angle2 += (Mouse.GetState().Y - preMouse.Y) / 100f;
            }
            if (Mouse.GetState().RightButton == ButtonState.Pressed)
            {
                distance += (Mouse.GetState().X - preMouse.X) / 100f;
            }

            if (Mouse.GetState().MiddleButton == ButtonState.Pressed)
            {
                Vector3 ViewRight = Vector3.Transform(Vector3.UnitX, Matrix.CreateRotationX(angle2) * Matrix.CreateRotationY(angle));
                Vector3 ViewUp = Vector3.Transform(Vector3.UnitY, Matrix.CreateRotationX(angle2) * Matrix.CreateRotationY(angle));
                cameraTarget -= ViewRight * (Mouse.GetState().X - preMouse.X) / 10f;
                cameraTarget += ViewUp * (Mouse.GetState().Y - preMouse.Y) / 10f;
            }
            preMouse = Mouse.GetState();
            cameraPosition = Vector3.Transform(new Vector3(0, 0, distance), Matrix.CreateRotationX(angle2) * Matrix.CreateRotationY(angle) * Matrix.CreateTranslation(cameraTarget));
            view = Matrix.CreateLookAt(cameraPosition, cameraTarget, Vector3.Transform(Vector3.UnitY, Matrix.CreateRotationX(angle2) * Matrix.CreateRotationY(angle)));
            lightPosition = Vector3.Transform(new Vector3(0, 0, 10), Matrix.CreateRotationX(angleL2) * Matrix.CreateRotationY(angleL));
            lightView = Matrix.CreateLookAt(lightPosition, Vector3.Zero, Vector3.UnitY);
            lightProjection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, 1f, 1f, 50f);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = new DepthStencilState();

            GraphicsDevice.SetRenderTarget(renderTarget);
            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);

            DrawShadowMap();
            GraphicsDevice.SetRenderTarget(null);
            shadowMap = (Texture2D)renderTarget;

            GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.DarkSlateBlue, 1.0f, 0);

            DrawShadowScene();

            shadowMap = null;

            base.Draw(gameTime);
        }

        private void DrawShadowScene()
        {
            effect.CurrentTechnique = effect.Techniques["ShadowedScene"];
            foreach (Model model in models)
            {
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    foreach (ModelMesh mesh in model.Meshes)
                    {
                        foreach (ModelMeshPart part in mesh.MeshParts)
                        {
                            effect.Parameters["World"].SetValue(mesh.ParentBone.Transform);
                            effect.Parameters["View"].SetValue(view);
                            effect.Parameters["Projection"].SetValue(projection);
                            Matrix worldInverseTransposeMatrix = Matrix.Transpose(Matrix.Invert(mesh.ParentBone.Transform));
                            effect.Parameters["WorldInverseTranspose"].SetValue(worldInverseTransposeMatrix);

                            effect.Parameters["LightViewMatrix"].SetValue(lightView);
                            effect.Parameters["LightProjectionMatrix"].SetValue(lightProjection);
                            effect.Parameters["LightPosition"].SetValue(lightPosition);
                            effect.Parameters["ShadowMap"].SetValue(shadowMap);
                            pass.Apply();

                            GraphicsDevice.SetVertexBuffer(part.VertexBuffer);
                            GraphicsDevice.Indices = part.IndexBuffer;
                            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.VertexOffset, part.StartIndex, part.PrimitiveCount);
                        }
                    }
                }
            }
        }

        private void DrawShadowMap()
        {
            effect.CurrentTechnique = effect.Techniques["ShadowMapShader"];
            foreach (Model model in models)
            {
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    foreach (ModelMesh mesh in model.Meshes)
                    {
                        foreach (ModelMeshPart part in mesh.MeshParts)
                        {
                            effect.Parameters["World"].SetValue(mesh.ParentBone.Transform);
                            effect.Parameters["View"].SetValue(view);
                            effect.Parameters["Projection"].SetValue(projection);
                            Matrix worldInverseTransposeMatrix = Matrix.Transpose(Matrix.Invert(mesh.ParentBone.Transform));
                            effect.Parameters["WorldInverseTranspose"].SetValue(worldInverseTransposeMatrix);
                            //Other parameters here
                            effect.Parameters["LightViewMatrix"].SetValue(lightView);
                            effect.Parameters["LightProjectionMatrix"].SetValue(lightProjection);
                            effect.Parameters["CameraPosition"].SetValue(cameraPosition);
                            effect.Parameters["LightPosition"].SetValue(lightPosition);

                            pass.Apply();
                            GraphicsDevice.SetVertexBuffer(part.VertexBuffer);
                            GraphicsDevice.Indices = part.IndexBuffer;
                            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.VertexOffset, part.StartIndex, part.PrimitiveCount);
                        }
                    }
                }
            }
        }
    }
}
