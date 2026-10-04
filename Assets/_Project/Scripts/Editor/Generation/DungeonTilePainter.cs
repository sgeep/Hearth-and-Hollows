using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// Paints a character map (<c>#</c>/<c>t</c> wall, <c>o</c> pillar, anything else floor) with the Dungeon tileset:
    /// the 4b test floor's wall faces, ends and junctions, shared with the 4d rooms. <c>at(x, y)</c> gives the
    /// character at a tile (x right, y up) and treats outside the map as wall.
    /// </summary>
    internal sealed class DungeonTilePainter
    {
        readonly Func<int, int, char> m_At;
        readonly int m_Width, m_Height;

        public DungeonTilePainter(int width, int height, Func<int, int, char> at)
        {
            m_Width = width;
            m_Height = height;
            m_At = at;
        }

        char At(int x, int y) => m_At(x, y);
        static bool IsWall(char c) => c is '#' or 't';
        bool IsSolid(int x, int y) => IsWall(At(x, y)) || At(x, y) == 'o';
        bool IsOpen(int x, int y) => !IsSolid(x, y);

        public void PaintFloor(Tilemap floor)
        {
            Tile[] tiles =
            {
                LookTestContent.DungeonTile(13, 2, false), LookTestContent.DungeonTile(14, 2, false),
                LookTestContent.DungeonTile(15, 2, false), LookTestContent.DungeonTile(16, 2, false), LookTestContent.DungeonTile(18, 2, false),
            };
            for (int y = 0; y < m_Height; y++)
            for (int x = 0; x < m_Width; x++)
            {
                // Under everything open, and under pillars and wall bricks so no gap shows at their edges.
                if (IsSolid(x, y) && IsSolid(x, y - 1) && IsSolid(x, y + 1)) continue;
                int roll = Mathf.Abs(x * 7349 + y * 9151) % 17;
                floor.SetTile(new Vector3Int(x, y, 0), tiles[roll < 12 ? roll % 2 : 2 + roll % 3]);
            }
        }

        public void PaintWalls(Tilemap walls)
        {
            for (int y = 0; y < m_Height; y++)
            for (int x = 0; x < m_Width; x++)
            {
                if (At(x, y) == 'o')
                {
                    // A pillar is two tiles: its top face over its brick base.
                    bool top = At(x, y - 1) == 'o';
                    walls.SetTile(new Vector3Int(x, y, 0), LookTestContent.DungeonTile(1, top ? 2 : 3, true));
                }
                else if (IsWall(At(x, y)))
                {
                    Vector2Int cell = WallCell(x, y);
                    walls.SetTile(new Vector3Int(x, y, 0), LookTestContent.DungeonTile(cell.x, cell.y, true));
                }
            }
        }

        enum Face { None, Top, Bricks, BottomTop, BottomBricks }

        /// <summary>How a wall tile reads from the floor directly above or below it.</summary>
        Face FaceOf(int x, int y)
        {
            if (!IsWall(At(x, y))) return Face.None;
            if (IsOpen(x, y - 1)) return Face.Bricks;
            if (IsWall(At(x, y - 1)) && IsOpen(x, y - 2)) return Face.Top;
            if (IsOpen(x, y + 1)) return Face.BottomTop;
            if (IsWall(At(x, y + 1)) && IsOpen(x, y + 2)) return Face.BottomBricks;
            return Face.None;
        }

        /// <summary>
        /// Picks a tile from the Dungeon tileset's wall sample (columns 4–10, rows 5–12): plain
        /// faces in columns 5–6 (alternating), west ends in column 4, east ends in column 10,
        /// and pieces with walls on both sides in column 7.
        /// </summary>
        Vector2Int WallCell(int x, int y)
        {
            int alternate = 5 + Mathf.Abs(x) % 2;
            Face face = FaceOf(x, y);
            bool openLeft = IsOpen(x - 1, y), openRight = IsOpen(x + 1, y);

            if (face != Face.None && !openLeft && !openRight) return new Vector2Int(alternate, Row(face, false));
            if (face == Face.None)
            {
                if (openLeft && openRight) return new Vector2Int(7, 7);
                if (openRight) return new Vector2Int(4, 7);
                if (openLeft) return new Vector2Int(10, 7);

                // A corner or a junction: take the face of the wall run beside it.
                Face right = FaceOf(x + 1, y), left = FaceOf(x - 1, y);
                bool junction = IsWall(At(x, y + 1)) && IsWall(At(x, y - 1));
                if (right != Face.None && left != Face.None) return new Vector2Int(7, Row(right, junction));
                if (right != Face.None) return new Vector2Int(4, Row(right, junction));
                if (left != Face.None) return new Vector2Int(10, Row(left, junction));
                return new Vector2Int(alternate, 5);
            }
            // A face at the end of a run that is open at the side.
            return new Vector2Int(openRight && !openLeft ? 4 : openLeft && !openRight ? 10 : 7, Row(face, false));
        }

        static int Row(Face face, bool junction) => face switch
        {
            Face.Top => junction ? 8 : 5,
            Face.Bricks => junction ? 9 : 6,
            Face.BottomTop => 11,
            Face.BottomBricks => 12,
            _ => 5,
        };
    }
}
