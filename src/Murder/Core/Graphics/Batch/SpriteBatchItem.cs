﻿// Based on https://github.com/lucas-miranda/Raccoon

using Microsoft.Xna.Framework.Graphics;
using Murder.Core.Geometry;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace Murder.Core.Graphics
{
    public struct SpriteBatchItem
    {
        public Texture2D? Texture;
        public VertexInfo[] VertexData = new VertexInfo[4];
        public int[] IndexData = new int[6] { 3, 0, 2, 2, 0, 1 };
        public SpriteBatchItem() { }

        public void Set(Texture2D texture, Vector2 position, Vector2 destinationSize, Rectangle? sourceRectangle, float rotation, Vector2 scale, ImageFlip flip, Color color, Vector2 origin, Vector3 colorBlend, float layerDepth = 1f)
        {
            Texture = texture;

            if (!sourceRectangle.HasValue)
            {
                sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            }

            Vector2 topLeft = -origin * scale,
                    topRight = (-origin + new Vector2(destinationSize.Width, 0f)) * scale,
                    bottomRight = (-origin + destinationSize) * scale,
                    bottomLeft = (-origin + new Vector2(0f, destinationSize.Height)) * scale;

            if (rotation != 0)
            {
                float cos = MathF.Cos(rotation),
                      sin = MathF.Sin(rotation);

                topLeft = new Vector2(topLeft.X * cos - topLeft.Y * sin, topLeft.X * sin + topLeft.Y * cos);
                topRight = new Vector2(topRight.X * cos - topRight.Y * sin, topRight.X * sin + topRight.Y * cos);
                bottomRight = new Vector2(bottomRight.X * cos - bottomRight.Y * sin, bottomRight.X * sin + bottomRight.Y * cos);
                bottomLeft = new Vector2(bottomLeft.X * cos - bottomLeft.Y * sin, bottomLeft.X * sin + bottomLeft.Y * cos);
            }

            VertexData[0] = new VertexInfo(
                new Vector3(position + topLeft, layerDepth),
                color,
                new Microsoft.Xna.Framework.Vector2((float)sourceRectangle.Value.Left / texture.Width, (float)sourceRectangle.Value.Top / texture.Height),
                colorBlend
            );

            VertexData[1] = new VertexInfo(
                new Vector3(position + topRight, layerDepth),
                color,
                new Microsoft.Xna.Framework.Vector2((float)sourceRectangle.Value.Right / texture.Width, (float)sourceRectangle.Value.Top / texture.Height),
                colorBlend
            );

            VertexData[2] = new VertexInfo(
                new Vector3(position + bottomRight, layerDepth),
                color,
                new Microsoft.Xna.Framework.Vector2((float)sourceRectangle.Value.Right / texture.Width, (float)sourceRectangle.Value.Bottom / texture.Height),
                colorBlend
            );

            VertexData[3] = new VertexInfo(
                new Vector3(position + bottomLeft, layerDepth),
                color,
                new Microsoft.Xna.Framework.Vector2((float)sourceRectangle.Value.Left / texture.Width, (float)sourceRectangle.Value.Bottom / texture.Height),
                colorBlend
            );

            if ((flip & ImageFlip.Horizontal) != ImageFlip.None)
            {
                Microsoft.Xna.Framework.Vector2 texCoord = VertexData[1].TextureCoordinate;
                VertexData[1].TextureCoordinate = VertexData[0].TextureCoordinate;
                VertexData[0].TextureCoordinate = texCoord;

                texCoord = VertexData[2].TextureCoordinate;
                VertexData[2].TextureCoordinate = VertexData[3].TextureCoordinate;
                VertexData[3].TextureCoordinate = texCoord;
            }

            if ((flip & ImageFlip.Vertical) != ImageFlip.None)
            {
                Microsoft.Xna.Framework.Vector2 texCoord = VertexData[2].TextureCoordinate;
                VertexData[2].TextureCoordinate = VertexData[1].TextureCoordinate;
                VertexData[1].TextureCoordinate = texCoord;

                texCoord = VertexData[3].TextureCoordinate;
                VertexData[3].TextureCoordinate = VertexData[0].TextureCoordinate;
                VertexData[0].TextureCoordinate = texCoord;
            }
        }
    }
}