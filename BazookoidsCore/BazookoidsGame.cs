using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using BazookoidsCore.Simulation;
using BazookoidsCore.Utility;
using BazookoidsCore.Utility.Enums;
using System;
using System.Linq;
using System.Collections.Generic;

namespace BazookoidsCore
{
	/// <summary>
	/// This is the main type for your game.
	/// </summary>
	public class BazookoidsGame : Game
	{
		#region Constants

		private const string ArenaFloorMeshName = "Floor";

		private const float CollisionBias = 0.004f;

		#endregion

		#region Fields

		private readonly GraphicsDeviceManager _graphics;

		private Viewport _defaultViewport, _redViewport, _yellowViewport;

		private KeyboardState _oldKeyboardState;

		private PointLight _arenaLight;
		private Camera[] _cameras;

		private Vehicle[] _vehicles;
		private Plane[] _arenaBoundaries;

		private Effect _vehicleEffect, _arenaEffect;
		private Effect _skyboxEffect;

		private Texture2D _arenaTexture, _arenaFloorTexture;
		private Texture2D _arenaNormalTexture, _arenaFloorNormalTexture;

		private Model _vehicleModel, _arenaModel;
		private Model _skyboxModel;

		private Random _random;

		private Vector3 _gravity;

		#endregion

		#region Constructors

		public BazookoidsGame()
		{
			_graphics = new GraphicsDeviceManager(this) { PreferMultiSampling = true };

			_graphics.PreferredBackBufferWidth = 1024;
			_graphics.PreferredBackBufferHeight = 480;

			Content.RootDirectory = "Content";
		}

		#endregion

		#region Private methods

		#region Content loading

		private void LoadVehicle()
		{
			_vehicleModel = Content.Load<Model>("Models/Vehicle");

			foreach (ModelMesh mesh in _vehicleModel.Meshes)
			{
				foreach (ModelMeshPart part in mesh.MeshParts)
				{
					part.Effect = _vehicleEffect;
				}
			}
		}

		private void LoadArena()
		{
			_arenaModel = Content.Load<Model>("Models/Arena");

			foreach (ModelMesh mesh in _arenaModel.Meshes)
			{
				foreach (ModelMeshPart part in mesh.MeshParts)
				{
					part.Effect = _arenaEffect;
				}
			}
		}

		private void LoadSkybox()
		{
			_skyboxModel = Content.Load<Model>("Models/Skybox");

			foreach (ModelMesh mesh in _skyboxModel.Meshes)
			{
				foreach (ModelMeshPart part in mesh.MeshParts)
				{
					part.Effect = _skyboxEffect;
				}
			}
		}

		#endregion

		#region Content drawing

		private void DrawSkybox(Camera camera)
		{
			GraphicsDevice.DepthStencilState = DepthStencilState.None;

			Matrix skyboxWorldMatrix = Matrix.CreateTranslation(camera.Position);

			foreach (ModelMesh mesh in _skyboxModel.Meshes)
			{
				foreach (Effect effect in mesh.Effects)
				{
					effect.CurrentTechnique = effect.Techniques["SkyboxTechnique"];

					effect.Parameters["World"].SetValue(skyboxWorldMatrix);
					effect.Parameters["WorldViewProjection"].SetValue(skyboxWorldMatrix*camera.ViewMatrix*camera.ProjectionMatrix);
					effect.Parameters["CameraPosition"].SetValue(camera.Position);
					effect.Parameters["HorizonColour"].SetValue(new Vector3(0.5f, 0.5f, 0.4f));
					effect.Parameters["ZenithColour"].SetValue(new Vector3(0.5f, 0.5f, 1));
				}

				mesh.Draw();
			}

			GraphicsDevice.DepthStencilState = DepthStencilState.Default;
		}

		private void DrawScene(Camera camera)
		{
			// Vehicles
			List<Vehicle> vehiclesToDraw = _vehicles.ToList();
			if (camera.Mode == CameraMode.Onboard)
			{
				if (camera == _cameras[0])
				{
					vehiclesToDraw.RemoveAt(0);
				}
				else if (camera == _cameras[1])
				{
					vehiclesToDraw.RemoveAt(1);
				}
			}

			foreach (Vehicle vehicle in vehiclesToDraw)
			{
				Matrix vehicleWorldMatrix = vehicle.State.Orientation*Matrix.CreateTranslation(vehicle.State.Position);

				foreach (ModelMesh mesh in _vehicleModel.Meshes)
				{
					foreach (Effect effect in mesh.Effects)
					{
						effect.CurrentTechnique = effect.Techniques["VehicleTechnique"];

						effect.Parameters["World"].SetValue(vehicleWorldMatrix);
						effect.Parameters["WorldViewProjection"].SetValue(vehicleWorldMatrix*camera.ViewMatrix*camera.ProjectionMatrix);
						effect.Parameters["LightPower"].SetValue(_arenaLight.Power);
						effect.Parameters["AmbientLightPower"].SetValue(_arenaLight.AmbientPower);
						effect.Parameters["LightAttenuation"].SetValue(_arenaLight.Attenuation);
						effect.Parameters["LightPosition"].SetValue(_arenaLight.Position);
						effect.Parameters["HighlightColour"].SetValue(vehicle.HighlightColour);
					}

					mesh.Draw();
				}
			}

			// Arena
			Vector3[] vehiclePositions = _vehicles.Select(v => v.State.Position).ToArray();

			foreach (ModelMesh mesh in _arenaModel.Meshes)
			{
				Texture2D activeTexture = mesh.Name == ArenaFloorMeshName ? _arenaFloorTexture : _arenaTexture;
				Texture2D activeNormalTexture = mesh.Name == ArenaFloorMeshName ? _arenaFloorNormalTexture : _arenaNormalTexture;

				foreach (Effect effect in mesh.Effects)
				{
					effect.CurrentTechnique = effect.Techniques["ArenaTechnique"];

					effect.Parameters["World"].SetValue(Matrix.Identity);
					effect.Parameters["WorldViewProjection"].SetValue(camera.ViewMatrix*camera.ProjectionMatrix);
					effect.Parameters["LightPower"].SetValue(_arenaLight.Power);
					effect.Parameters["AmbientLightPower"].SetValue(_arenaLight.AmbientPower);
					effect.Parameters["LightAttenuation"].SetValue(_arenaLight.Attenuation);
					effect.Parameters["SpecularExponent"].SetValue(_arenaLight.SpecularExponent);
					effect.Parameters["CameraPosition"].SetValue(camera.Position);
					effect.Parameters["LightPosition"].SetValue(_arenaLight.Position);
					effect.Parameters["VehiclePositions"].SetValue(vehiclePositions);
					effect.Parameters["DiffuseTexture"].SetValue(activeTexture);
					effect.Parameters["NormalMapTexture"].SetValue(activeNormalTexture);
				}

				mesh.Draw();
			}
		}

		#endregion

		#region Misc.

		private void UpdatePhysics(float timeDelta)
		{
			// Calculate vehicle forces/torques
			foreach (Vehicle vehicle in _vehicles)
			{
				vehicle.Force = Vector3.Zero;
				vehicle.Torque = Vector3.Zero;

				foreach (Plane arenaBoundary in _arenaBoundaries)
				{
					// Body-arena interactions
					foreach (Vector3 vertexPosition in vehicle.BodyBoundingVertices)
					{
						Vector3 vertexLocalPosition = Vector3.Transform(vertexPosition, vehicle.State.Orientation);

						float vertexPenetrationDepth = CollisionBias - Globals.PointPlaneDistance(vehicle.State.Position + vertexLocalPosition, arenaBoundary);
						if (vertexPenetrationDepth >= 0)
						{
							Vector3 vertexVelocity = vehicle.State.Velocity + Vector3.Cross(vehicle.State.AngularVelocity, vertexLocalPosition);
							Vector3 vertexContactForce = vehicle.Mass*(Vehicle.BodyBoundingVertexReactionForceMultiplier*arenaBoundary.Normal*vertexPenetrationDepth - Vehicle.BodyBoundingVertexFrictionForceMultiplier*vertexVelocity);

							vehicle.Force += vertexContactForce;
							vehicle.Torque += Vector3.Cross(vertexLocalPosition, vertexContactForce);
						}
					}

					// Levitator-arena interactions
					if (Array.IndexOf(_arenaBoundaries, arenaBoundary) == 0)
					{
						Vector3 projectedForwardDirection = Globals.PlaneProjection(vehicle.State.Orientation.Forward, arenaBoundary);
						if (projectedForwardDirection.Length() > 0)
						{
							projectedForwardDirection = Vector3.Normalize(projectedForwardDirection);
						}

						Vector3 projectedRightDirection = Globals.PlaneProjection(vehicle.State.Orientation.Right, arenaBoundary);
						if (projectedRightDirection.Length() > 0)
						{
							projectedRightDirection = Vector3.Normalize(projectedRightDirection);
						}

						foreach (Levitator levitator in vehicle.Levitators)
						{
							Vector3 levitatorLocalPosition = Vector3.Transform(levitator.Positon, vehicle.State.Orientation);

							float levitatorPenetrationDepth = CollisionBias - Globals.PointPlaneDistance(vehicle.State.Position + levitatorLocalPosition, arenaBoundary) + levitator.Radius;
							if (levitatorPenetrationDepth >= 0)
							{
								Vector3 levitatorVelocity = vehicle.State.Velocity + Vector3.Cross(vehicle.State.AngularVelocity, levitatorLocalPosition);
								Vector3 levitatorProjectedVelocity = Globals.PlaneProjection(levitatorVelocity, arenaBoundary);
								Vector3 levitatorContactForce = vehicle.Mass*(Levitator.LevitationForceMultiplier*arenaBoundary.Normal*levitatorPenetrationDepth - Levitator.VerticalFrictionForceMultiplier*(levitatorVelocity - levitatorProjectedVelocity)*arenaBoundary.Normal - Levitator.LateralFrictionForceMultiplier*levitatorProjectedVelocity);

								vehicle.Force += levitatorContactForce;
								vehicle.Torque += Vector3.Cross(levitatorLocalPosition, levitatorContactForce);

								Vector3 levitatorDriveForce = vehicle.Mass*(levitator.PowerForce*projectedForwardDirection + levitator.TurningForce*projectedRightDirection);

								vehicle.Force += levitatorDriveForce;
								vehicle.Torque += Vector3.Cross(levitatorLocalPosition, levitatorDriveForce);
							}
						}
					}
				}

				// Body-body interactions
				foreach (Vehicle otherVehicle in _vehicles.Where(v => v != vehicle))
				{
					foreach (Sphere boundingSphere in vehicle.BodyBoundingSpheres)
					{
						Vector3 boundingSphereLocalPosition = Vector3.Transform(boundingSphere.Position, vehicle.State.Orientation);
						Vector3 boundingSphereWorldPosition = vehicle.State.Position + boundingSphereLocalPosition;

						foreach (Sphere otherBoundingSphere in otherVehicle.BodyBoundingSpheres)
						{
							Vector3 otherBoundingSphereWorldPosition = otherVehicle.State.Position + Vector3.Transform(otherBoundingSphere.Position, otherVehicle.State.Orientation);

							Vector3 boundingSphereVector = boundingSphereWorldPosition - otherBoundingSphereWorldPosition;
							float boundingSphereDistance = boundingSphereVector.Length();
							float boundingSphereTouchDistance = boundingSphere.Radius + otherBoundingSphere.Radius;
							if (boundingSphereDistance <= boundingSphereTouchDistance)
							{
								Vector3 boundingSphereVelocity = vehicle.State.Velocity + Vector3.Cross(vehicle.State.AngularVelocity, boundingSphereLocalPosition);
								Vector3 boundingSphereContactForce = vehicle.Mass*(Vehicle.BodyBoundingSphereReactionForceMultiplier*Vector3.Normalize(boundingSphereVector)*(1 - boundingSphereDistance/boundingSphereTouchDistance) - Vehicle.BodyBoundingSphereFrictionForceMultiplier*boundingSphereVelocity);

								vehicle.Force += boundingSphereContactForce;
								vehicle.Torque += Vector3.Cross(boundingSphereLocalPosition, boundingSphereContactForce);
							}
						}
					}
				}

				vehicle.Force += vehicle.Mass*_gravity;
			}

			// Apply vehicle forces/torques
			foreach (Vehicle vehicle in _vehicles)
			{
				vehicle.ApplyPhysics(timeDelta);
			}
		}

		private void ResetGame()
		{
			_vehicles[0].State.Position = new Vector3((float)(40*(2*_random.NextDouble() - 1)), 3, (float)(10 + 30*_random.NextDouble()));
			_vehicles[0].State.Velocity = Vector3.Zero;
			_vehicles[0].State.Orientation = Matrix.CreateFromYawPitchRoll((float)(2*Math.PI*_random.NextDouble()), (float)(0.2f*(2*_random.NextDouble() - 1)), (float)(0.2f*(2*_random.NextDouble() - 1)));
			_vehicles[0].State.AngularMomentum = Vector3.Zero;
			_vehicles[0].State.AngularVelocity = Vector3.Zero;

			_vehicles[1].State.Position = new Vector3((float)(40*(2*_random.NextDouble() - 1)), 3, (float)-(10 + 30*_random.NextDouble()));
			_vehicles[1].State.Velocity = Vector3.Zero;
			_vehicles[1].State.Orientation = Matrix.CreateFromYawPitchRoll((float)(2*Math.PI*_random.NextDouble()), (float)(0.2f*(2*_random.NextDouble() - 1)), (float)(0.2f*(2*_random.NextDouble() - 1)));
			_vehicles[1].State.AngularMomentum = Vector3.Zero;
			_vehicles[1].State.AngularVelocity = Vector3.Zero;
		}

		#endregion

		#endregion

		#region Game overrides

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			_defaultViewport = GraphicsDevice.Viewport;
			_redViewport = new Viewport(0, 0, GraphicsDevice.Viewport.Bounds.Width/2, GraphicsDevice.Viewport.Bounds.Height);
			_yellowViewport = new Viewport(GraphicsDevice.Viewport.Bounds.Width/2, 0, GraphicsDevice.Viewport.Bounds.Width/2, GraphicsDevice.Viewport.Bounds.Height);

			_oldKeyboardState = Keyboard.GetState();

			_arenaLight = new PointLight(new Vector3(0, 20, 0), 1, 0.3f, 200, 32);

			Matrix cameraProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(60), _redViewport.AspectRatio, 0.1f, 1000);

			Camera cameraRed = new Camera()
			{
				ProjectionMatrix = cameraProjectionMatrix,
				FixedPosition = new Vector3(0, 10, 0),
				OnboardPosition = new Vector3(0, 0, 0)
			};

			Camera cameraYellow = new Camera()
			{
				ProjectionMatrix = cameraProjectionMatrix,
				FixedPosition = new Vector3(0, 10, 0),
				OnboardPosition = new Vector3(0, 0, 0)
			};

			_cameras = new Camera[] { cameraRed, cameraYellow };

			float vehicleMass = 1000;
			Matrix vehicleInverseBodyInertiaTensor = Matrix.Invert(new Matrix(new Vector4(vehicleMass/12*(4*4 + 1.2f*1.2f), 0, 0, 0), new Vector4(0, vehicleMass/12*(4*4 + 2*2), 0, 0), new Vector4(0, 0, vehicleMass/12*(2*2 + 1.2f*1.2f), 0), new Vector4(0, 0, 0, 1)));
			Vector3[] vehicleBodyBoundingVertices = new Vector3[]
			{
				new Vector3(-1, 0.6f, -2),
				new Vector3(1, 0.6f, -2),
				new Vector3(1, 0.6f, 2),
				new Vector3(-1, 0.6f, 2),
				new Vector3(-1, -0.6f, -2),
				new Vector3(1, -0.6f, -2),
				new Vector3(1, -0.6f, 2),
				new Vector3(-1, -0.6f, 2)
			};
			List<Sphere> boundingSpheres = Sphere.GenerateBox(new Vector3(-1, -0.6f, -2), new Vector3(1, 0.6f, 2), new Vector3(4, 2, 8), 0.4f);
			boundingSpheres.RemoveAt(8);
			boundingSpheres.RemoveAt(23);
			boundingSpheres.RemoveAt(38);
			boundingSpheres.RemoveAt(53);
			boundingSpheres.Add(new Sphere(new Vector3(-1.075f, -0.15f, -1.29f), 0.3f));
			boundingSpheres.Add(new Sphere(new Vector3(1.075f, -0.15f, -1.29f), 0.3f));
			boundingSpheres.Add(new Sphere(new Vector3(1.075f, -0.15f, 1.29f), 0.3f));
			boundingSpheres.Add(new Sphere(new Vector3(-1.075f, -0.15f, 1.29f), 0.3f));
			Sphere[] vehicleBodyBoundingSpheres = boundingSpheres.ToArray();

			Vehicle vehicleRed = new Vehicle()
			{
				Mass = vehicleMass,
				InverseBodyInertiaTensor = vehicleInverseBodyInertiaTensor,
				BodyBoundingVertices = vehicleBodyBoundingVertices,
				BodyBoundingSpheres = vehicleBodyBoundingSpheres,
				Levitators = new Levitator[]
				{
					new Levitator(new Vector3(-1.3f, -0.9f, -2.3f), 0.3f),
					new Levitator(new Vector3(1.3f, -0.9f, -2.3f), 0.3f),
					new Levitator(new Vector3(1.3f, -0.9f, 2.3f), 0.3f),
					new Levitator(new Vector3(-1.3f, -0.9f, 2.3f), 0.3f)
				},
				HighlightColour = new Vector3(0.7f, 0.2f, 0.2f)
			};

			Vehicle vehicleYellow = new Vehicle()
			{
				Mass = vehicleMass,
				InverseBodyInertiaTensor = vehicleInverseBodyInertiaTensor,
				BodyBoundingVertices = vehicleBodyBoundingVertices,
				BodyBoundingSpheres = vehicleBodyBoundingSpheres,
				Levitators = new Levitator[]
				{
					new Levitator(new Vector3(-1.3f, -0.9f, -2.3f), 0.3f),
					new Levitator(new Vector3(1.3f, -0.9f, -2.3f), 0.3f),
					new Levitator(new Vector3(1.3f, -0.9f, 2.3f), 0.3f),
					new Levitator(new Vector3(-1.3f, -0.9f, 2.3f), 0.3f)
				},
				HighlightColour = new Vector3(0.7f, 0.7f, 0)
			};

			_vehicles = new Vehicle[] { vehicleRed, vehicleYellow };

			_arenaBoundaries = new Plane[]
			{
				new Plane(Vector3.UnitY, 0),
				new Plane(Vector3.UnitX, -50),
				new Plane(-Vector3.UnitX, -50),
				new Plane(Vector3.UnitZ, -50),
				new Plane(-Vector3.UnitZ, -50)
			};

			_random = new Random();

			_gravity = new Vector3(0, -9.81f, 0);

			ResetGame();

			base.Initialize();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			_vehicleEffect = Content.Load<Effect>("Effects/VehicleEffect");
			_arenaEffect = Content.Load<Effect>("Effects/ArenaEffect");
			_skyboxEffect = Content.Load<Effect>("Effects/SkyboxEffect");

			_arenaTexture = Content.Load<Texture2D>("Textures/arena");
			_arenaFloorTexture = Content.Load<Texture2D>("Textures/arenaFloor");
			_arenaNormalTexture = Content.Load<Texture2D>("Textures/arenaNormal");
			_arenaFloorNormalTexture = Content.Load<Texture2D>("Textures/arenaFloorNormal");

			LoadVehicle();
			LoadArena();
			LoadSkybox();
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// game-specific content.
		/// </summary>
		protected override void UnloadContent()
		{
			// TODO: Unload any non ContentManager content here
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			KeyboardState newKeyboardState = Keyboard.GetState();

			float redVehiclePowerForce = newKeyboardState.IsKeyDown(Keys.W) ? Vehicle.PowerForceMagnitude : newKeyboardState.IsKeyDown(Keys.S) ? -Vehicle.PowerForceMagnitude : 0;
			float redVehicleFrontTurningForce = newKeyboardState.IsKeyDown(Keys.D) ? Vehicle.TurningForceMagnitude : newKeyboardState.IsKeyDown(Keys.A) ? -Vehicle.TurningForceMagnitude : 0;
			float redVehicleRearTurningForce = -redVehicleFrontTurningForce;

			_vehicles[0].Levitators[0].PowerForce = redVehiclePowerForce;
			_vehicles[0].Levitators[0].TurningForce = redVehicleFrontTurningForce;
			_vehicles[0].Levitators[1].PowerForce = redVehiclePowerForce;
			_vehicles[0].Levitators[1].TurningForce = redVehicleFrontTurningForce;
			_vehicles[0].Levitators[2].PowerForce = redVehiclePowerForce;
			_vehicles[0].Levitators[2].TurningForce = redVehicleRearTurningForce;
			_vehicles[0].Levitators[3].PowerForce = redVehiclePowerForce;
			_vehicles[0].Levitators[3].TurningForce = redVehicleRearTurningForce;

			float yellowVehiclePowerForce = newKeyboardState.IsKeyDown(Keys.Up) ? Vehicle.PowerForceMagnitude : newKeyboardState.IsKeyDown(Keys.Down) ? -Vehicle.PowerForceMagnitude : 0;
			float yellowVehicleFrontTurningForce = newKeyboardState.IsKeyDown(Keys.Right) ? Vehicle.TurningForceMagnitude : newKeyboardState.IsKeyDown(Keys.Left) ? -Vehicle.TurningForceMagnitude : 0;
			float yellowVehicleRearTurningForce = -yellowVehicleFrontTurningForce;

			_vehicles[1].Levitators[0].PowerForce = yellowVehiclePowerForce;
			_vehicles[1].Levitators[0].TurningForce = yellowVehicleFrontTurningForce;
			_vehicles[1].Levitators[1].PowerForce = yellowVehiclePowerForce;
			_vehicles[1].Levitators[1].TurningForce = yellowVehicleFrontTurningForce;
			_vehicles[1].Levitators[2].PowerForce = yellowVehiclePowerForce;
			_vehicles[1].Levitators[2].TurningForce = yellowVehicleRearTurningForce;
			_vehicles[1].Levitators[3].PowerForce = yellowVehiclePowerForce;
			_vehicles[1].Levitators[3].TurningForce = yellowVehicleRearTurningForce;

			if (_oldKeyboardState.IsKeyDown(Keys.C) && newKeyboardState.IsKeyUp(Keys.C))
			{
				switch (_cameras[0].Mode)
				{
					case CameraMode.Fixed:
						_cameras[0].Mode = CameraMode.Onboard;
						break;

					case CameraMode.Onboard:
						_cameras[0].Mode = CameraMode.Fixed;
						break;
				}
			}

			if (_oldKeyboardState.IsKeyDown(Keys.N) && newKeyboardState.IsKeyUp(Keys.N))
			{
				switch (_cameras[1].Mode)
				{
					case CameraMode.Fixed:
						_cameras[1].Mode = CameraMode.Onboard;
						break;

					case CameraMode.Onboard:
						_cameras[1].Mode = CameraMode.Fixed;
						break;
				}
			}

			if (_oldKeyboardState.IsKeyDown(Keys.Space) && newKeyboardState.IsKeyUp(Keys.Space))
			{
				ResetGame();
			}

			UpdatePhysics(gameTime.ElapsedGameTime.Milliseconds/1000.0f);

			_cameras[0].Update(_vehicles[0]);
			_cameras[1].Update(_vehicles[1]);

			_oldKeyboardState = newKeyboardState;

			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			// PASS 1: Draw the scene
			GraphicsDevice.Clear(Color.Black);

			GraphicsDevice.Viewport = _redViewport;

				DrawSkybox(_cameras[0]);
				DrawScene(_cameras[0]);

			GraphicsDevice.Viewport = _yellowViewport;

				DrawSkybox(_cameras[1]);
				DrawScene(_cameras[1]);

			GraphicsDevice.Viewport = _defaultViewport;

			base.Draw(gameTime);
		}

		#endregion
	}
}
