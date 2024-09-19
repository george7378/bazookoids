using Microsoft.Xna.Framework;
using BazookoidsCore.Utility;

namespace BazookoidsCore.Simulation
{
    public class Vehicle
    {
        #region Constants

        public const float BodyBoundingVertexReactionForceMultiplier = 75;
        public const float BodyBoundingVertexFrictionForceMultiplier = 5;

        public const float BodyBoundingSphereReactionForceMultiplier = 30;
        public const float BodyBoundingSphereFrictionForceMultiplier = 0.15f;

        public const float PowerForceMagnitude = 10;
        public const float TurningForceMagnitude = 2;

        #endregion

        #region Properties

        public float Mass { get; set; }

        public Matrix InverseBodyInertiaTensor { get; set; }

        /// <summary>
        /// Used to determine collisions between the vehicle body and arena
        /// </summary>
        public Vector3[] BodyBoundingVertices { get; set; }

        /// <summary>
        /// Used to determine collisions between vehicles
        /// </summary>
        public Sphere[] BodyBoundingSpheres { get; set; }

        public Levitator[] Levitators { get; set; }

        public RigidBodyState State { get; set; }

        public Vector3 Force { get; set; }

        public Vector3 Torque { get; set; }

        public Vector3 HighlightColour { get; set; }

        #endregion

        #region Constructors

        public Vehicle()
        {
            BodyBoundingVertices = new Vector3[] { };
            BodyBoundingSpheres = new Sphere[] { };
            Levitators = new Levitator[] { };

            State = new RigidBodyState();
        }

        #endregion

        #region Methods

        public void ApplyPhysics(float timeDelta)
        {
            State.Position += State.Velocity*timeDelta;
            State.Velocity += Force/Mass*timeDelta;

            State.Orientation += Matrix.Transpose(Globals.SkewSymmetricMatrix(State.AngularVelocity)*Matrix.Transpose(State.Orientation))*timeDelta;
            State.Orientation = Globals.OrthonormaliseMatrix(State.Orientation);

            State.AngularMomentum += Torque*timeDelta;
            State.AngularVelocity = Vector3.Transform(State.AngularMomentum, Matrix.Transpose(State.Orientation)*InverseBodyInertiaTensor*State.Orientation);
        }

        #endregion
    }
}
