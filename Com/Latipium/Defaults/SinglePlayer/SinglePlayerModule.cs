// SinglePlayerModule.cs
//
// Copyright (c) 2016 Zach Deibert.
// All Rights Reserved.
using System;
using System.Threading;
using Com.Latipium.Core;
using log4net;

namespace Com.Latipium.Defaults.SinglePlayer {
	/// <summary>
	/// The default module implementation for the single player game.
	/// </summary>
	public class SinglePlayerModule : AbstractLatipiumModule {
		private static readonly ILog Log = LogManager.GetLogger(typeof(SinglePlayerModule));

		/// <summary>
		/// Starts the single player game.
		/// </summary>
		[LatipiumMethod("Start")]
		public void Start() {
			LatipiumModule auth = ModuleFactory.FindModule("Com.Latipium.Modules.Authentication");
			LatipiumModule graphics = ModuleFactory.FindModule("Com.Latipium.Modules.Graphics");
			LatipiumModule physics = ModuleFactory.FindModule("Com.Latipium.Modules.Physics");
			LatipiumModule player = ModuleFactory.FindModule("Com.Latipium.Modules.Player");
			LatipiumModule worldGen = ModuleFactory.FindModule("Com.Latipium.Modules.World.Generator");
			LatipiumModule worldSer = ModuleFactory.FindModule("Com.Latipium.Modules.World.Serialization");
			if ( graphics == null ) {
				Log.Error("Unable to find graphics module");
			} else {
				string name = null;
				LatipiumObject world = null;
				if ( worldGen != null || worldSer != null ) {
					if ( auth == null ) {
						name = "Player";
					} else {
						name = auth.InvokeFunction<string>("GetUsername");
					}
					if ( worldSer != null ) {
						world = worldSer.InvokeFunction<LatipiumObject>("Load");
					}
					if ( world == null && worldGen != null ) {
						world = worldGen.InvokeFunction<LatipiumObject>("Generate");
					}
				}
				LatipiumObject p = null;
				if ( world != null ) {
					p = world.InvokeFunction<string, LatipiumObject>("GetPlayer", name);
					if ( player != null && p != null ) {
						player.InvokeProcedure<LatipiumObject>("HandleFor", p);
					}
				}
				Thread physicsThread = null;
				Thread parent = null;
				if ( physics != null ) {
					physicsThread = new Thread(() => {
						physics.InvokeProcedure("Initialize");
						if ( world != null ) {
							physics.InvokeProcedure<LatipiumObject>("LoadWorld", world);
						}
						try {
							physics.InvokeProcedure("Loop");
						} catch ( ThreadInterruptedException ) {
						} finally {
							physics.InvokeProcedure("Destroy");
							if ( parent != null ) {
								parent.Interrupt();
							}
						}
					});
					physicsThread.Start();
				}
				graphics.InvokeProcedure("Initialize");
				if ( world != null ) {
					graphics.InvokeProcedure<LatipiumObject>("LoadWorld", world);
					if ( p != null ) {
						graphics.InvokeProcedure<LatipiumObject>("SetPlayer", p);
					}
				}
				try {
					graphics.InvokeProcedure("Loop");
				} finally {
					graphics.InvokeProcedure("Destroy");
					if ( physicsThread != null ) {
						parent = Thread.CurrentThread;
						physicsThread.Interrupt();
						try {
							Thread.Sleep(int.MaxValue);
						} catch ( ThreadInterruptedException ) {
						}
					}
					if ( world != null && worldSer != null ) {
						worldSer.InvokeProcedure<LatipiumObject>("Save", world);
					}
				}
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Com.Latipium.Defaults.SinglePlayer.SinglePlayerModule"/> class.
		/// </summary>
		public SinglePlayerModule() : base(new string[] { "Com.Latipium.Modules.SinglePlayer" }) {
		}
	}
}

