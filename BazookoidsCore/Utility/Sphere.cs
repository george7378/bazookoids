using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace BazookoidsCore.Utility
{
    public class Sphere
    {
        #region Properties

        public Vector3 Position { get; set; }

        public float Radius { get; set; }

        #endregion

        #region Constructors

        public Sphere(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }

        #endregion

        #region Methods

        public static List<Sphere> GenerateBox(Vector3 min, Vector3 max, Vector3 packing, float radius)
        {
            List<Sphere> result = new List<Sphere>();

            Vector3 separation = (max - min)/new Vector3(packing.X + 1, packing.Y + 1, packing.Z + 1);
            Vector3 startPosition = min + separation;

            for (int x = 0; x < (int)packing.X; x++)
            {
                for (int y = 0; y < (int)packing.Y; y++)
                {
                    for (int z = 0; z < (int)packing.Z; z++)
                    {
                        result.Add(new Sphere(startPosition + separation*new Vector3(x, y, z), radius));
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
