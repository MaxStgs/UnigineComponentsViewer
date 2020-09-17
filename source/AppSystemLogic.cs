using System.Threading.Tasks;
using Unigine;

namespace UnigineApp
{
	class AppSystemLogic : SystemLogic
	{
		// System logic, it exists during the application life cycle.
		// These methods are called right after corresponding system script's (UnigineScript) methods.

		public AppSystemLogic()
		{
		}

		protected virtual void OnShutdown()
		{
			// foreach()
			// EventHandler handler  ShutdownEvents;
			// handler?.Invoke(this, null);
		}

		public override bool Init()
		{
			// Write here code to be called on engine initialization.
			App.SetBackgroundUpdate(true);
			Task.Run(ServerLogic.JoinToLocalServer);
			Unigine.Console.Run("show_messages 1");
			return true;
		}

		// start of the main loop
		public override bool Update()
		{
			// Write here code to be called before updating each render frame.

			return true;
		}

		public override bool PostUpdate()
		{
			// Write here code to be called after updating each render frame.

			return true;
		}
		// end of the main loop

		public override bool Shutdown()
		{
			// Write here code to be called on engine shutdown.
			HttpServer.CloseConnection();
			return true;
		}
	}
}
