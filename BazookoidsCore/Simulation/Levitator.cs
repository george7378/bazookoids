using Microsoft.Xna.Framework;

namespace BazookoidsCore.Simulation
{
    public class Levitator
    {
        #region Constants

        public const float LevitationForceMultiplier = 50;

        public const float VerticalFrictionForceMultiplier = 2;
        public const float LateralFrictionForceMultiplier = 0.2f;

        #endregion

        #region Properties

        public Vector3 Positon { get; set; }

        public float Radius { get; set; }

        public float PowerForce { get; set; }

        public float TurningForce { get; set; }

        #endregion

        #region Constructors

        public Levitator(Vector3 position, float radius)
        {
            Positon = position;
            Radius = radius;
        }

        #endregion
    }
}
