using Microsoft.Xna.Framework;
using BazookoidsCore.Simulation;
using BazookoidsCore.Utility.Enums;

namespace BazookoidsCore.Utility
{
	public class Camera
	{
		#region Properties

		public CameraMode Mode { get; set; }

		public Vector3 Position { get; private set; }

		public Matrix ViewMatrix { get; private set; }

		public Matrix ProjectionMatrix { get; set; }

		#region Fixed mode

		public Vector3 FixedPosition { get; set; }

		#endregion

		#region Onboard mode

		public Vector3 OnboardPosition { get; set; }

		#endregion

		#endregion

		#region Methods

		public void Update(Vehicle target)
		{
			switch (Mode)
			{
				case CameraMode.Fixed:
					Position = FixedPosition;
					ViewMatrix = Matrix.CreateLookAt(Position, target.State.Position, Vector3.UnitY);
					break;

				case CameraMode.Onboard:
					Position = target.State.Position + Vector3.Transform(OnboardPosition, target.State.Orientation);
					ViewMatrix = Matrix.CreateLookAt(Position, Position + target.State.Orientation.Forward, target.State.Orientation.Up);
					break;
			}
		}

		#endregion
	}
}
